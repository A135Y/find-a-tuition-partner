﻿using Application.Common.Structs;
using TuitionType = Domain.Enums.TuitionType;

namespace UI.Extensions
{
    public static class GroupPriceExtensions
    {
        public static bool ContainsInSchoolPrice(this Dictionary<int, GroupPrice> prices)
            => prices.Any(x => x.Value.SchoolMin.HasValue || x.Value.SchoolMax.HasValue);

        public static bool ContainsOnlinePrice(this Dictionary<int, GroupPrice> prices)
            => prices.Any(x => x.Value.OnlineMin.HasValue || x.Value.OnlineMax.HasValue);

        public static string FormatFor(this GroupPrice price, TuitionType tuitionType, bool addVAT)
        {
            return tuitionType switch
            {
                TuitionType.InSchool => FormatPrices(price.SchoolMin, price.SchoolMax, addVAT),
                TuitionType.Online => FormatPrices(price.OnlineMin, price.OnlineMax, addVAT),
                _ => "",
            };

            static string FormatPrices(decimal? min, decimal? max, bool addVAT) =>
                (min, max) switch
                {
                    _ when min != null && min == max => addVAT ? $"{min.Value.AddVAT().FormatPrice()}" : $"{min.Value.FormatPrice()}",
                    _ when min != null && max != null => addVAT ? $"{min.Value.AddVAT().FormatPrice()} to {max.Value.AddVAT().FormatPrice()}" : $"{min.Value.FormatPrice()} to {max.Value.FormatPrice()}",
                    _ => "",
                };
        }
    }
}
