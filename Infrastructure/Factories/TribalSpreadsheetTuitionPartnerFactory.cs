﻿using System.Text.RegularExpressions;
using Application.Constants;
using Application.Extensions;
using Application.Extraction;
using Application.Factories;
using Domain;
using Domain.Constants;
using Infrastructure.Mapping;
using Microsoft.Extensions.Logging;
using GroupSize = Domain.Enums.GroupSize;
using KeyStage = Domain.Enums.KeyStage;
using TuitionType = Domain.Enums.TuitionType;

namespace Infrastructure.Factories;

public class TribalSpreadsheetTuitionPartnerFactory : ITribalSpreadsheetTuitionPartnerFactory
{
    public const string OrganisationDetailsSheetName = "Organisation Details";
    private const string DeliverySheetName = "Delivery";
    private const string PricingSheetName = "Pricing";
    private const int MaxRows = 100000;

    private readonly ILogger<TribalSpreadsheetTuitionPartnerFactory> _logger;
    private readonly IDictionary<string, ImportMap> _organisationDetailsMapping;

    private IList<Region>? _regions;
    private IList<Subject>? _subjects;
    private IList<OrganisationType>? _organisationTypes;
    private IDictionary<string, DateTime>? _tpImportedDates;
    private ISpreadsheetExtractor? _spreadsheetExtractor;

    private List<string> _warnings = new();
    private List<string> _errors = new();

    public TribalSpreadsheetTuitionPartnerFactory(ILogger<TribalSpreadsheetTuitionPartnerFactory> logger)
    {
        _logger = logger;

        TuitionPartner tpMapping = new();
        _organisationDetailsMapping = new Dictionary<string, ImportMap>
        {
            { "Organisation_Ref_ID_s", new ImportMap(tpMapping, nameof(tpMapping.ImportId)) },
            { "Organisation_s", new ImportMap(tpMapping, nameof(tpMapping.Name)) { RecommendedMaxStringLength = 120 } },
            { "Organisation_Address1_s", new ImportMap(tpMapping, nameof(tpMapping.Address)) { RecommendedMaxStringLength = 100 } },
            { "Organisation_Address2_s", new ImportMap() { IsStoredInNtp = true, RecommendedMaxStringLength = 100 } },
            { "Organisation_Address3_s", new ImportMap() { IsStoredInNtp = true, RecommendedMaxStringLength = 100 } },
            { "Organisation_Town_s", new ImportMap() { IsStoredInNtp = true, IsRequired = true, RecommendedMaxStringLength = 100 } },
            { "Organisation_County_s", new ImportMap() { IsStoredInNtp = true, IsRequired = true, RecommendedMaxStringLength = 100 } },
            { "Organisation_PostCode_s", new ImportMap() { IsStoredInNtp = true, IsRequired = true, RecommendedMaxStringLength = 8 } },
            { "Organisation_Tel_s", new ImportMap(tpMapping, nameof(tpMapping.PhoneNumber)) { RecommendedMaxStringLength = 40 } },
            { "Organisation_TP_Link_s", new ImportMap(tpMapping, nameof(tpMapping.Website)) { RecommendedMaxStringLength = 200 } },
            { "Organisation_Email_s", new ImportMap(tpMapping, nameof(tpMapping.Email)) { RecommendedMaxStringLength = 100 } },
            { "Organisation_Introduction_s", new ImportMap(tpMapping, nameof(tpMapping.Description)) { RecommendedMaxStringLength = 350 } },
            { "Organisation_LegalStatus_s", new ImportMap() { IsStoredInNtp = true, IsRequired = true } },
            { "Organisation_LogoVector_s", new ImportMap() },
            { "Organisation_ChargeVAT_s", new ImportMap(tpMapping, nameof(tpMapping.IsVatCharged)) {IsRequired = false} },
            { "Organisation_LastUpdated_d", new ImportMap(tpMapping, nameof(tpMapping.TPLastUpdatedData)) },
        };
    }

    public TuitionPartner GetTuitionPartner(ISpreadsheetExtractor spreadsheetExtractor, string filename,
        IList<Region> regions, IList<Subject> subjects, IList<OrganisationType> organisationTypes,
        IDictionary<string, DateTime> tpImportedDates)
    {
        _regions = regions;
        _subjects = subjects;
        _organisationTypes = organisationTypes;
        _tpImportedDates = tpImportedDates;

        _warnings = new();
        _errors = new();
        TuitionPartner tuitionPartner = new();

        _spreadsheetExtractor = spreadsheetExtractor;

        if (!_spreadsheetExtractor.WorksheetExists(OrganisationDetailsSheetName))
        {
            _errors.Add($"Missing '{OrganisationDetailsSheetName}' worksheet");
        }
        if (!_spreadsheetExtractor.WorksheetExists(PricingSheetName))
        {
            _errors.Add($"Missing '{PricingSheetName}' worksheet");
        }
        if (!_spreadsheetExtractor.WorksheetExists(DeliverySheetName))
        {
            _errors.Add($"Missing '{DeliverySheetName}' worksheet");
        }

        if (_errors.Count == 0)
        {
            tuitionPartner = GetOrganisationDetails();

            PopulateLocalAuthorityDistrictCoverage(tuitionPartner);

            PopulateSubjectCoverageAndPrice(tuitionPartner);
        }

        if (_warnings.Any())
        {
            _logger.LogWarning("Issues importing Tribal spreadsheet '{filename}': {warnings}", filename, string.Join(Environment.NewLine, _warnings));
        }

        if (_errors.Any())
        {
            throw new InvalidOperationException($"Error importing Tribal spreadsheet '{filename}': {string.Join(Environment.NewLine, _errors)}");
        }

        return tuitionPartner;
    }

    private void ProcessSheet(string sheetName, string tableHeaderColumn, string tableHeader, string dataColumn, Action<ISpreadsheetExtractor?, string, string, int> addRowToDictionary)
    {
        var completedLoop = false;
        var passedHeader = false;
        int row = 1;
        while (!completedLoop)
        {
            if (row == MaxRows)
            {
                _errors.Add($"Searched '{MaxRows}' rows in '{sheetName}' worksheet");
                completedLoop = true;
            }
            else if (passedHeader)
            {
                var data = _spreadsheetExtractor!.GetCellValue(sheetName, dataColumn, row);
                if (!string.IsNullOrWhiteSpace(data))
                {
                    addRowToDictionary(_spreadsheetExtractor, sheetName, data, row);
                }
                else
                {
                    completedLoop = true;
                }
            }
            else
            {
                passedHeader = _spreadsheetExtractor!.GetCellValue(sheetName, tableHeaderColumn, row) == tableHeader;
            }

            row++;
        }
    }

    private TuitionPartner GetOrganisationDetails()
    {
        const string KeyColumn = "A";
        const string ValueColumn = "C";
        const string TableHeaderColumn = "A";
        const string TableHeader = "Title";

        TuitionPartner tuitionPartner = new();
        var sheetName = OrganisationDetailsSheetName;

        foreach (var organisationDetailMapping in _organisationDetailsMapping)
        {
            organisationDetailMapping.Value.ClearValue();
        }

        //Add the data from the spreadsheet in to the _organisationDetailsMapping dictionary value class
        ProcessSheet(sheetName, TableHeaderColumn, TableHeader, KeyColumn,
            (spreadsheetExtractor, sheetName, data, row) =>
            {
                var key = data;
                if (_organisationDetailsMapping.ContainsKey(key))
                {
                    if (!_organisationDetailsMapping[key].HasConvertedValue)
                    {
                        var value = spreadsheetExtractor!.GetCellValue(sheetName, ValueColumn, row);
                        _organisationDetailsMapping[key].SetValue(value);
                    }
                    else
                    {
                        _errors.Add($"Duplicate '{key}' in '{sheetName}' worksheet");
                    }
                }
                else
                {
                    _warnings.Add($"Unexpected '{key}' key exists in '{sheetName}' worksheet");
                }
            });

        //Use the _organisationDetailsMapping dictionary to push the data in to the TuitionPartner class
        var mappedProperties = _organisationDetailsMapping.Where(x => x.Value.IsStoredInNtp && x.Value.HasConvertedValue);
        foreach (var mappedProperty in mappedProperties)
        {
            mappedProperty.Value.ApplyConvertedValueToProperty(tuitionPartner);
        }

        //Get Organisation Type Id
        var organisationTypeName = _organisationDetailsMapping.SingleOrDefault(x => x.Key == "Organisation_LegalStatus_s").Value.SourceValue;
        var organisationType = _organisationTypes!.FirstOrDefault(x => string.Equals(x.Name, organisationTypeName, StringComparison.InvariantCultureIgnoreCase));
        if (organisationType == null)
        {
            _errors.Add($"No matching organisation type in the db for '{organisationTypeName}' in '{sheetName}' worksheet");
        }
        else
        {
            tuitionPartner.OrganisationTypeId = organisationType!.Id;
        }

        //Update the TuitionPartner class with specific mapping info
        tuitionPartner.Website = tuitionPartner.Website.ParseUrl();

        //Populate Address from multiple cells
        var postcode = _organisationDetailsMapping.SingleOrDefault(x => x.Key == "Organisation_PostCode_s").Value.SourceValue;
        var addressLines = new string?[]
        {
        tuitionPartner.Address,
        _organisationDetailsMapping.SingleOrDefault(x => x.Key == "Organisation_Address2_s").Value.SourceValue,
        _organisationDetailsMapping.SingleOrDefault(x => x.Key == "Organisation_Address3_s").Value.SourceValue,
        _organisationDetailsMapping.SingleOrDefault(x => x.Key == "Organisation_Town_s").Value.SourceValue,
        _organisationDetailsMapping.SingleOrDefault(x => x.Key == "Organisation_County_s").Value.SourceValue,
        postcode
        };

        tuitionPartner.Address = string.Join(Environment.NewLine, addressLines.Where(x => x != null));

        tuitionPartner.SeoUrl = tuitionPartner.Name.ToSeoUrl() ?? "";

        //Deal with warnings and errors
        if (!string.IsNullOrWhiteSpace(tuitionPartner.Website) && !Regex.Match(tuitionPartner.Website, StringConstants.WebsiteURLRegExp).Success)
        {
            _warnings.Add($"The website supplied is invalid in the '{sheetName}' worksheet");
        }
        if (!string.IsNullOrWhiteSpace(tuitionPartner.Email) && !Regex.Match(tuitionPartner.Email, StringConstants.EmailRegExp).Success)
        {
            _warnings.Add($"The email supplied is invalid in the '{sheetName}' worksheet");
        }
        if (!string.IsNullOrWhiteSpace(postcode) && !Regex.Match(postcode, StringConstants.PostcodeRegExp).Success)
        {
            _warnings.Add($"The postcode supplied is invalid in the '{sheetName}' worksheet");
        }
        if (!string.IsNullOrWhiteSpace(tuitionPartner.PhoneNumber) && !Regex.Match(tuitionPartner.PhoneNumber, StringConstants.PhoneNumberRegExp).Success)
        {
            _warnings.Add($"The phone number supplied is invalid in the '{sheetName}' worksheet");
        }

        var missingNTPProperties = _organisationDetailsMapping.Where(x => !x.Value.IsInSourceData);
        if (missingNTPProperties.Any())
        {
            _warnings.Add($"The following were expected and not supplied in the '{sheetName}' worksheet: {string.Join(", ", missingNTPProperties.Select(x => x.Key).ToArray())}");
        }
        var exceedMaxLengthNTPProperties = _organisationDetailsMapping.Where(x => x.Value.IsInSourceData &&
                                                                                    !string.IsNullOrWhiteSpace(x.Value.SourceValue) &&
                                                                                    x.Value.RecommendedMaxStringLength != null &&
                                                                                    x.Value.SourceValue.Length > x.Value.RecommendedMaxStringLength);
        if (exceedMaxLengthNTPProperties.Any())
        {
            _warnings.Add($"The following exceeded the suggested max length in the '{sheetName}' worksheet: {string.Join(", ", exceedMaxLengthNTPProperties.Select(x => $"{x.Key} (suggested max length {x.Value.RecommendedMaxStringLength}, actual length {x.Value.SourceValue!.Length})").ToArray())}");
        }
        var missingRequiredNTPProperties = _organisationDetailsMapping.Where(x => x.Value.IsRequired && string.IsNullOrWhiteSpace(x.Value.SourceValue));
        if (missingRequiredNTPProperties.Any())
        {
            _errors.Add($"The following were required and not set in the '{sheetName}' worksheet: {string.Join(", ", missingRequiredNTPProperties.Select(x => x.Key).ToArray())}");
        }
        if (_tpImportedDates != null && _tpImportedDates.ContainsKey(tuitionPartner.Name.ToLower()))
        {
            var previousLastUpdated = _tpImportedDates[tuitionPartner.Name.ToLower()];
            if (previousLastUpdated > tuitionPartner.TPLastUpdatedData)
            {
                _errors.Add($"The existing TP has a last updated date of {previousLastUpdated} and the spreadsheet last updated date is {tuitionPartner.TPLastUpdatedData}, which is before");
            }
        }

        return tuitionPartner;
    }

    private void PopulateLocalAuthorityDistrictCoverage(TuitionPartner tuitionPartner)
    {
        const string LADCodeColumn = "A";
        const string LADNameColumn = "B";
        const string RegionCodeColumn = "C";
        const string RegionNameColumn = "D";
        const string InSchoolColumn = "E";
        const string OnlineColumn = "F";

        const string TableHeaderColumn = "A";
        const string TableHeader = "LAD Code";

        var sheetName = DeliverySheetName;
        var ladsCovered = new Dictionary<string, (int ladId, string ladName, string regionCode, string regionName, bool inSchool, bool online)>();
        var regionsAndLADs = _regions!
            .SelectMany(r => r.LocalAuthorityDistricts
                .Select(l => new { RegionName = r.Name, LocalAuthorityDistrictId = l.Id, LocalAuthorityDistrictCode = l.Code, LocalAuthorityDistrictName = l.Name })
            );

        //Add the data from the spreadsheet in to the ladsCovered dictionary
        ProcessSheet(sheetName, TableHeaderColumn, TableHeader, LADCodeColumn,
            (spreadsheetExtractor, sheetName, data, row) =>
            {
                var ladCode = data;
                if (!ladsCovered.ContainsKey(ladCode))
                {
                    var ladName = spreadsheetExtractor!.GetCellValue(sheetName, LADNameColumn, row);
                    var regionCode = spreadsheetExtractor!.GetCellValue(sheetName, RegionCodeColumn, row);
                    var regionName = spreadsheetExtractor!.GetCellValue(sheetName, RegionNameColumn, row);

                    if (string.IsNullOrWhiteSpace(ladName) ||
                        string.IsNullOrWhiteSpace(regionCode) ||
                        string.IsNullOrWhiteSpace(regionName))
                    {
                        _warnings.Add($"Missing LAD or Region details on '{sheetName}' worksheet for LAD Code '{ladCode}'");
                    }

                    var ladId = regionsAndLADs.FirstOrDefault(x => x.LocalAuthorityDistrictCode == ladCode);

                    if (ladId == null)
                    {
                        _errors.Add($"Cannot find LAD Id in NTP database for LAD Code '{ladCode}' in '{sheetName}' worksheet");
                    }
                    else
                    {
                        var inSchool = spreadsheetExtractor!.GetCellValue(sheetName, InSchoolColumn, row).ParseBoolean();
                        var online = spreadsheetExtractor!.GetCellValue(sheetName, OnlineColumn, row).ParseBoolean();

                        ladsCovered[ladCode] = (ladId.LocalAuthorityDistrictId, ladName, regionCode, regionName, inSchool, online);
                    }
                }
                else
                {
                    _errors.Add($"Duplicate '{ladCode}' in '{sheetName}' worksheet");
                }
            });

        if (ladsCovered.Count == 0)
        {
            _errors.Add($"No entries added in the '{sheetName}' worksheet");
        }
        else
        {
            foreach ((_, (int ladId, string ladName, string regionCode, string regionName, bool inSchool, bool online)) in ladsCovered)
            {
                if (inSchool)
                {
                    AddLocalAuthorityDistrictCoverage(tuitionPartner, TuitionType.InSchool, ladId);
                }
                if (online)
                {
                    AddLocalAuthorityDistrictCoverage(tuitionPartner, TuitionType.Online, ladId);
                }
            }

            if (!tuitionPartner.LocalAuthorityDistrictCoverage.Any())
            {
                _errors.Add($"No entries set to True in the '{sheetName}' worksheet");
            }
            else
            {
                //Warn for invalid LAD Name, Region Code and Region Name
                var invalidLADNames = ladsCovered
                    .Select(tribalData => tribalData.Value.ladName)
                    .Distinct()
                    .Where(tribalData => !string.IsNullOrWhiteSpace(tribalData) && !regionsAndLADs.Any(db => db.LocalAuthorityDistrictName == tribalData));
                if (invalidLADNames.Any())
                {
                    _warnings.Add($"Invalid LAD Names on '{sheetName}' worksheet: {string.Join(", ", invalidLADNames)}");
                }

                var invalidRegionNames = ladsCovered
                    .Select(tribalData => tribalData.Value.regionName)
                    .Distinct()
                    .Where(tribalData => !string.IsNullOrWhiteSpace(tribalData) && !regionsAndLADs.Any(db => db.RegionName == tribalData));
                if (invalidRegionNames.Any())
                {
                    _warnings.Add($"Invalid Region Names on '{sheetName}' worksheet: {string.Join(", ", invalidRegionNames)}");
                }

                List<string> invalidRegionCodes = new();
                var regionCodes = ladsCovered
                    .Select(tribalData => tribalData.Value.regionCode)
                    .Distinct();
                foreach (var regionCode in regionCodes)
                {
                    if (!Regions.InitialsToId.TryGetValue(regionCode, out var regionId))
                    {
                        invalidRegionCodes.Add(regionCode);
                    }
                }
                if (invalidRegionCodes.Any())
                {
                    _warnings.Add($"Invalid Region Codes on '{sheetName}' worksheet: {string.Join(", ", invalidRegionCodes)}");
                }
            }
        }
    }

    private static void AddLocalAuthorityDistrictCoverage(TuitionPartner tuitionPartner, TuitionType tuitionType, int ladId)
    {
        var coverage = new LocalAuthorityDistrictCoverage
        {
            TuitionPartner = tuitionPartner,
            TuitionTypeId = (int)tuitionType,
            LocalAuthorityDistrictId = ladId
        };

        tuitionPartner.LocalAuthorityDistrictCoverage.Add(coverage);
    }

    private void PopulateSubjectCoverageAndPrice(TuitionPartner tuitionPartner)
    {
        const string GroupColumn = "A";
        const string KeyStageColumn = "B";
        const string SubjectColumn = "C";
        const string TuitionTypeColumn = "D";
        const string RateColumn = "E";

        const string TableHeaderColumn = "A";
        const string TableHeader = "Group";

        var sheetName = PricingSheetName;
        var subjectCoverageAndPrices = new Dictionary<(GroupSize groupSize, KeyStage keyStage, Domain.Enums.Subject subject, int subjectId, TuitionType tuitionType), decimal>();

        bool castError = false;

        //Add the data from the spreadsheet in to the subjectCoverageAndPrices dictionary
        ProcessSheet(sheetName, TableHeaderColumn, TableHeader, GroupColumn,
            (spreadsheetExtractor, sheetName, data, row) =>
            {
                var groupString = data;
                var keyStageString = spreadsheetExtractor!.GetCellValue(sheetName, KeyStageColumn, row);
                var subjectString = spreadsheetExtractor!.GetCellValue(sheetName, SubjectColumn, row);
                var tuitionTypeString = spreadsheetExtractor!.GetCellValue(sheetName, TuitionTypeColumn, row);

                //Cast to required types
                if (!groupString.TryParse(out GroupSize groupSize))
                {
                    castError = true;
                    _errors.Add($"Invalid Group conversion, should be in '1 to x' format.  '{groupString}' is on row {row} on '{sheetName}' worksheet");
                }
                if (!keyStageString.TryParse(out KeyStage keyStage))
                {
                    castError = true;
                    _errors.Add($"Invalid KeyStage conversion, should be in 'Key Stage x' format.  '{keyStageString}' is on row {row} on '{sheetName}' worksheet");
                }
                if (!subjectString.TryParse(out Domain.Enums.Subject subjectEnum))
                {
                    castError = true;
                    _errors.Add($"Invalid Subject conversion.  '{subjectString}' is on row {row} on '{sheetName}' worksheet");
                }
                var tuitionTypeStringReplaced = tuitionTypeString.Replace("Both", TuitionType.Any.DisplayName(), StringComparison.InvariantCultureIgnoreCase);
                if (!tuitionTypeStringReplaced.TryParse(out TuitionType tuitionType))
                {
                    castError = true;
                    _errors.Add($"Invalid TuitionType conversion.  '{tuitionTypeString}' is on row {row} on '{sheetName}' worksheet");
                }
                int subjectId = 0;
                if (!castError)
                {
                    //Get subject id
                    var subject = _subjects!.FirstOrDefault(x => x.Name.ToLower() == subjectEnum.DisplayName().ToLower() && x.KeyStageId == (int)keyStage);
                    if (subject == null)
                    {
                        castError = true;
                        _errors.Add($"Invalid Subject '{subjectString}' for Key Stage '{keyStageString}' on row {row} on '{sheetName}' worksheet");
                    }
                    else
                    {
                        subjectId = subject.Id;
                    }
                }

                if (castError)
                {
                    return;
                }

                var key = (groupSize, keyStage, subjectEnum, subjectId, tuitionType);
                if (!subjectCoverageAndPrices.ContainsKey(key))
                {
                    //We store all prices exclusive of VAT
                    subjectCoverageAndPrices[key] = spreadsheetExtractor!.GetCellValue(sheetName, RateColumn, row).ParsePrice();
                }
                else
                {
                    _errors.Add($"Duplicate '{groupString}', '{keyStageString}', '{subjectString}' and '{tuitionTypeString}', in '{sheetName}' worksheet");
                }
            });

        if (subjectCoverageAndPrices.Count == 0)
        {
            _errors.Add($"No entries added in the '{sheetName}' worksheet");
        }
        else
        {
            var ladHasInSchool = tuitionPartner.LocalAuthorityDistrictCoverage.Any(x => x.TuitionTypeId == (int)TuitionType.InSchool);
            var ladHasOnline = tuitionPartner.LocalAuthorityDistrictCoverage.Any(x => x.TuitionTypeId == (int)TuitionType.Online);

            foreach (((GroupSize groupSize, KeyStage keyStage, Domain.Enums.Subject subject, int subjectId, TuitionType tuitionType), decimal rate) in subjectCoverageAndPrices)
            {
                if (ladHasInSchool && rate > 0 && (tuitionType == TuitionType.InSchool || tuitionType == TuitionType.Any))
                {
                    AddPrice(tuitionPartner, TuitionType.InSchool, subjectId, groupSize, rate);
                }
                if (ladHasOnline && rate > 0 && (tuitionType == TuitionType.Online || tuitionType == TuitionType.Any))
                {
                    AddPrice(tuitionPartner, TuitionType.Online, subjectId, groupSize, rate);
                }
            }

            if (!tuitionPartner.Prices.Any())
            {
                _errors.Add($"No Price entries added from '{sheetName}' worksheet");
            }
            else
            {
                //Add subject coverage
                var subjectCoverages = tuitionPartner.Prices.Select(x => new { x.TuitionTypeId, x.SubjectId }).Distinct();
                foreach (var subjectCoverageLoop in subjectCoverages)
                {
                    var coverage = new SubjectCoverage
                    {
                        TuitionPartner = tuitionPartner,
                        TuitionTypeId = subjectCoverageLoop.TuitionTypeId,
                        SubjectId = subjectCoverageLoop.SubjectId
                    };

                    tuitionPartner.SubjectCoverage.Add(coverage);
                }

                //Warn - if any £0
                if (subjectCoverageAndPrices.Any(x => x.Value == 0))
                {
                    _warnings.Add($"Some zero rates on '{sheetName}' worksheet");
                }
                //Warn - see if any in school or online when not set for LADs
                if (!ladHasInSchool && subjectCoverageAndPrices.Any(x => x.Value > 0 && (x.Key.tuitionType == TuitionType.InSchool || x.Key.tuitionType == TuitionType.Any)))
                {
                    _warnings.Add($"Some subjects and prices exist for In School on '{sheetName}' worksheet.  But no LADs are In School");
                }
                if (!ladHasOnline && subjectCoverageAndPrices.Any(x => x.Value > 0 && (x.Key.tuitionType == TuitionType.Online || x.Key.tuitionType == TuitionType.Any)))
                {
                    _warnings.Add($"Some subjects and prices exist for Online on '{sheetName}' worksheet.  But no LADs are Online");
                }
            }
        }
    }

    private static void AddPrice(TuitionPartner tuitionPartner, TuitionType tuitionType, int subjectId, GroupSize groupSize, decimal rate)
    {
        var price = new Price
        {
            TuitionPartner = tuitionPartner,
            TuitionTypeId = (int)tuitionType,
            SubjectId = subjectId,
            GroupSize = (int)groupSize,
            HourlyRate = rate
        };

        tuitionPartner.Prices.Add(price);
    }
}