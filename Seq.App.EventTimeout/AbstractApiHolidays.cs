using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Seq.App.EventTimeout
{
    public class AbstractApiHolidays
    {
        public string name { get; set; }
        public string name_local { get; set; }
        public string language { get; set; }
        public string description { get; set; }
        public string country { get; set; }
        public string location { get; set; }
        public List<string> locations { get; set; }
        public string type { get; set; }
        public DateTime localStart { get; set; }
        public DateTime utcStart { get; set; }
        public DateTime utcEnd { get; set; }
        public string date { get; set; }
        public string date_year { get; set; }
        public string date_month { get; set; }
        public string date_day { get; set; }
        public string week_day { get; set; }

        public AbstractApiHolidays(string Name, string Name_Local, string Language, string Description, string Country, string Location, string Type, string Date, string Date_Year, string Date_Month, string Date_Day, string Week_Day)
        {
            name = Name;
            name_local = Name_Local;
            language = Language;
            description = Description;
            location = Location;
            if (location.Contains(" - "))
                locations = location.Substring(location.IndexOf(" - ") + 3, location.Length - location.IndexOf(" - ") - 3).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
            else
                locations = location.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();

            country = Country;
            type = Type;
            date = Date;
            date_year = date_year;
            date_month = Date_Month;
            date_day = Date_Day;
            week_day = Week_Day;
            localStart = DateTime.ParseExact(date, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
            utcStart = localStart.ToUniversalTime();
            utcEnd = localStart.AddDays(1).ToUniversalTime();
        }
    }

    public static class Holidays
    {

        public static bool validateCountry(string countryCode)
        {
            return CultureInfo
                .GetCultures(CultureTypes.SpecificCultures)
                    .Select(culture => new RegionInfo(culture.LCID))
                        .Any(region => region.TwoLetterISORegionName.ToLower() == countryCode.ToLower());
        }

        public static List<AbstractApiHolidays> validateHolidays(List<AbstractApiHolidays> HolidayList, List<string> HolidayMatch, List<string> LocaleMatch, bool IncludeBank, bool IncludeWeekends)
        {
            List<AbstractApiHolidays> result = new List<AbstractApiHolidays>();
            foreach (AbstractApiHolidays holiday in HolidayList)
            {
                bool hasType = false;
                bool hasRegion = false;
                bool isBank = false;
                bool isWeekend = false;

                if (HolidayMatch.Count > 0)
                    foreach (string match in HolidayMatch)
                        if (holiday.type.IndexOf(match, StringComparison.CurrentCultureIgnoreCase) >= 0)
                            hasType = true;

                if (LocaleMatch.Count > 0)
                    foreach (string match in LocaleMatch)
                        if (holiday.locations.FindIndex(loc => loc.Equals(match, StringComparison.CurrentCultureIgnoreCase)) >= 0)
                            hasRegion = true;

                if (!IncludeBank && holiday.name.IndexOf("Bank Holiday", StringComparison.CurrentCultureIgnoreCase) >= 0)
                    isBank = true;

                if (!IncludeWeekends && (holiday.localStart.DayOfWeek == DayOfWeek.Sunday || holiday.localStart.DayOfWeek == DayOfWeek.Saturday))
                    isWeekend = true;

                if (HolidayMatch.Count > 0 && LocaleMatch.Count > 0 && hasType && hasRegion && !isBank && !isWeekend)
                    result.Add(holiday);
                else if (HolidayMatch.Count > 0 && LocaleMatch.Count == 0 && hasType && !isBank && !isWeekend)
                    result.Add(holiday);
                else if (HolidayMatch.Count == 0 && LocaleMatch.Count > 0 && hasRegion && !isBank && !isWeekend)
                    result.Add(holiday);
                else if (HolidayMatch.Count == 0 && LocaleMatch.Count == 0 && !isBank && !isWeekend)
                    result.Add(holiday);
            }

            return result;
        }
    }
}
