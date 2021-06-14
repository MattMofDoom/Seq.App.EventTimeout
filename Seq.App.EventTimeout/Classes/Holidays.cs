using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Seq.App.EventTimeout.Classes
{
    /// <summary>
    ///     Holidays methods
    /// </summary>
    public static class Holidays
    {
        /// <summary>
        ///     Validate that a correct country code has been passed
        /// </summary>
        /// <param name="countryCode"></param>
        /// <returns></returns>
        public static bool ValidateCountry(string countryCode)
        {
            return CultureInfo
                .GetCultures(CultureTypes.SpecificCultures)
                .Select(culture => new RegionInfo(culture.LCID))
                .Any(region =>
                    string.Equals(region.TwoLetterISORegionName, countryCode, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Validate AbstractAPI Holidays API output based on a given set of rules for holiday types, locales, and
        ///     including/excluding bank holidays and weekends
        /// </summary>
        /// <param name="holidayList"></param>
        /// <param name="holidayMatch"></param>
        /// <param name="localeMatch"></param>
        /// <param name="includeBank"></param>
        /// <param name="includeWeekends"></param>
        /// <returns></returns>
        public static List<AbstractApiHolidays> ValidateHolidays(IEnumerable<AbstractApiHolidays> holidayList,
            List<string> holidayMatch, List<string> localeMatch, bool includeBank, bool includeWeekends)
        {
            var result = new List<AbstractApiHolidays>();
            foreach (var holiday in holidayList)
            {
                var hasType = false;
                var hasRegion = false;
                var isBank = false;
                var isWeekend = false;

                if (holidayMatch.Count > 0)
                    // ReSharper disable once UnusedVariable
                    foreach (var match in holidayMatch.Where(match =>
                        holiday.type.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0))
                        hasType = true;

                if (localeMatch.Count > 0)
                    // ReSharper disable once UnusedVariable
                    foreach (var match in localeMatch.Where(match =>
                        holiday.Locations.FindIndex(loc => loc.Equals(match, StringComparison.OrdinalIgnoreCase)) >=
                        0))
                        hasRegion = true;

                if (!includeBank && holiday.name.IndexOf("Bank Holiday", StringComparison.OrdinalIgnoreCase) >= 0)
                    isBank = true;

                if (!includeWeekends && (holiday.LocalStart.DayOfWeek == DayOfWeek.Sunday ||
                                         holiday.LocalStart.DayOfWeek == DayOfWeek.Saturday)) isWeekend = true;

                switch (holidayMatch.Count > 0)
                {
                    case true when localeMatch.Count > 0 && hasType && hasRegion && !isBank && !isWeekend:
                    case true when localeMatch.Count == 0 && hasType && !isBank && !isWeekend:
                        result.Add(holiday);
                        break;
                    default:
                    {
                        switch (holidayMatch.Count)
                        {
                            case 0 when localeMatch.Count > 0 && hasRegion && !isBank && !isWeekend:
                            case 0 when localeMatch.Count == 0 && !isBank && !isWeekend:
                                result.Add(holiday);
                                break;
                        }

                        break;
                    }
                }
            }

            return result;
        }
    }
}