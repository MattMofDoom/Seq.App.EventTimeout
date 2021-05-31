using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Seq.App.EventTimeout
{
    /// <summary>
    /// AbstractAPI Holidays API format
    /// </summary>
    public class AbstractApiHolidays
    {
        public string Name { get; set; }
        public string Name_local { get; set; }
        public string Language { get; set; }
        public string Description { get; set; }
        public string Country { get; set; }
        public string Location { get; set; }
        public List<string> Locations { get; set; }
        public string Type { get; set; }
        public DateTime LocalStart { get; set; }
        public DateTime UtcStart { get; set; }
        public DateTime UtcEnd { get; set; }
        public string Date { get; set; }
        public string Date_year { get; set; }
        public string Date_month { get; set; }
        public string Date_day { get; set; }
        public string Week_day { get; set; }

        public AbstractApiHolidays(string name, string name_local, string language, string description, string country, string location, string type, string date, string date_year, string date_month, string date_day, string week_day)
        {
            Name = name;
            Name_local = name_local;
            Language = language;
            Description = description;
            Location = location;
            if (Location.Contains(" - "))
            {
                Locations = Location.Substring(Location.IndexOf(" - ") + 3, Location.Length - Location.IndexOf(" - ") - 3).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
            }
            else
            {
                Locations = Location.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
            }

            Country = country;
            Type = type;
            Date = date;
            Date_year = date_year;
            Date_month = date_month;
            Date_day = date_day;
            Week_day = week_day;
            LocalStart = DateTime.ParseExact(Date, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
            UtcStart = LocalStart.ToUniversalTime();
            UtcEnd = LocalStart.AddDays(1).ToUniversalTime();
        }
    }

    public static class Holidays
    {

        public static bool ValidateCountry(string countryCode)
        {
            return CultureInfo
                .GetCultures(CultureTypes.SpecificCultures)
                    .Select(culture => new RegionInfo(culture.LCID))
                        .Any(region => region.TwoLetterISORegionName.ToLower() == countryCode.ToLower());
        }

        /// <summary>
        /// Validate AbstractAPI Holidays API output based on a given set of rules for holiday types, locales, and including/excluding bank holidays and weekends
        /// </summary>
        /// <param name="holidayList"></param>
        /// <param name="holidayMatch"></param>
        /// <param name="localeMatch"></param>
        /// <param name="includeBank"></param>
        /// <param name="includeWeekends"></param>
        /// <returns></returns>
        public static List<AbstractApiHolidays> ValidateHolidays(List<AbstractApiHolidays> holidayList, List<string> holidayMatch, List<string> localeMatch, bool includeBank, bool includeWeekends)
        {
            List<AbstractApiHolidays> result = new List<AbstractApiHolidays>();
            foreach (AbstractApiHolidays holiday in holidayList)
            {
                bool hasType = false;
                bool hasRegion = false;
                bool isBank = false;
                bool isWeekend = false;

                if (holidayMatch.Count > 0)
                {
                    foreach (string match in holidayMatch)
                    {
                        if (holiday.Type.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            hasType = true;
                        }
                    }
                }

                if (localeMatch.Count > 0)
                {
                    foreach (string match in localeMatch)
                    {
                        if (holiday.Locations.FindIndex(loc => loc.Equals(match, StringComparison.OrdinalIgnoreCase)) >= 0)
                        {
                            hasRegion = true;
                        }
                    }
                }

                if (!includeBank && holiday.Name.IndexOf("Bank Holiday", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isBank = true;
                }

                if (!includeWeekends && (holiday.LocalStart.DayOfWeek == DayOfWeek.Sunday || holiday.LocalStart.DayOfWeek == DayOfWeek.Saturday))
                {
                    isWeekend = true;
                }

                if (holidayMatch.Count > 0 && localeMatch.Count > 0 && hasType && hasRegion && !isBank && !isWeekend)
                {
                    result.Add(holiday);
                }
                else if (holidayMatch.Count > 0 && localeMatch.Count == 0 && hasType && !isBank && !isWeekend)
                {
                    result.Add(holiday);
                }
                else if (holidayMatch.Count == 0 && localeMatch.Count > 0 && hasRegion && !isBank && !isWeekend)
                {
                    result.Add(holiday);
                }
                else if (holidayMatch.Count == 0 && localeMatch.Count == 0 && !isBank && !isWeekend)
                {
                    result.Add(holiday);
                }
            }

            return result;
        }
    }
}
