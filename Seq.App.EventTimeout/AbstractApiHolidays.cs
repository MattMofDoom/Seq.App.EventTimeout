using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

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
}
