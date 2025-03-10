﻿using System.Diagnostics;
using Application.Common.Interfaces;
using Domain.Search;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.MetricLogging;

public class LoggingTuitionPartnerService : ITuitionPartnerService
{
    private readonly ITuitionPartnerService _inner;
    private readonly ILogger _logger;

    public LoggingTuitionPartnerService(ITuitionPartnerService inner, ILogger<LoggingTuitionPartnerService> logger)
        => (_inner, _logger) = (inner, logger);

    public async Task<int[]?> GetTuitionPartnersFilteredAsync(TuitionPartnersFilter filter, CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScope("{@Filter}", filter);
        _logger.LogInformation("Filtering tuition partners");

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var result = await _inner.GetTuitionPartnersFilteredAsync(filter, cancellationToken);
        stopwatch.Stop();

        var resultCount = result?.Length;

        //Log warning if no results returned but trying to get all TPs (so no filters) or a collection of TPs using the SeoUrls
        var logLevel = (resultCount == 0 &&
                            string.IsNullOrWhiteSpace(filter.Name) &&
                            filter.TuitionTypeId is null &&
                            filter.SubjectIds is null &&
                            filter.LocalAuthorityDistrictId is null &&
                            (filter.SeoUrls is null || filter.SeoUrls.Length > 1)
                       )
                       ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(logLevel, "GetTuitionPartnersFilteredAsync found {Count} TPs in {Elapsed}ms",
                   resultCount, stopwatch.ElapsedMilliseconds);

        return result;
    }

    public async Task<IEnumerable<TuitionPartnerResult>> GetTuitionPartnersAsync(TuitionPartnerRequest request, CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScope("{@Request}", request);
        using var schoolDataScope = _logger.BeginScope("{@Urn}", request.Urn);
        _logger.LogInformation("Get tuition partners");

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var result = await _inner.GetTuitionPartnersAsync(request, cancellationToken);
        stopwatch.Stop();

        var names = result.Select(x => x.Name).ToList();
        var logLevel = ((request.TuitionPartnerIds == null && !result.Any()) ||
            (request.TuitionPartnerIds != null && request.TuitionPartnerIds?.Length != result.Count())) ?
            LogLevel.Error : LogLevel.Information;

        using (_logger.BeginScope("{@TuitionPartners}", names))
        {
            _logger.Log(logLevel, "GetTuitionPartnersAsync found {Count} TP results in {Elapsed}ms",
                    result.Count(), stopwatch.ElapsedMilliseconds);
        }

        return result;
    }

    public IEnumerable<TuitionPartnerResult> FilterTuitionPartnersData(IEnumerable<TuitionPartnerResult> results, TuitionPartnersDataFilter dataFilter)
    {
        _logger.LogInformation("Filter data for tuition partners");

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var result = _inner.FilterTuitionPartnersData(results, dataFilter);
        stopwatch.Stop();

        _logger.LogInformation("FilterTuitionPartnersData {Count} TP results in {Elapsed}ms",
                    result.Count(), stopwatch.ElapsedMilliseconds);

        return result;
    }

    public IEnumerable<TuitionPartnerResult> OrderTuitionPartners(IEnumerable<TuitionPartnerResult> results, TuitionPartnerOrdering ordering)
    {
        _logger.LogInformation("Order tuition partners");

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var result = _inner.OrderTuitionPartners(results, ordering);
        stopwatch.Stop();

        _logger.LogInformation("OrderTuitionPartners ordered {Count} TP results by {OrderBy} in {Elapsed}ms",
                    result.Count(), ordering.OrderBy.ToString(), stopwatch.ElapsedMilliseconds);

        return result;
    }
}
