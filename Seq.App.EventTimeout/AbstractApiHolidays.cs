using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Seq.App.EventTimeout
{
    /// <summary>
    ///     AbstractAPI Holidays API format
    /// </summary>
    public abstract class AbstractApiHolidays
    {
        /// <summary>
        ///     AbstractAPI Holidays API format
        /// </summary>
        /// <param name="name"></param>
        /// <param name="name_local"></param>
        /// <param name="language"></param>
        /// <param name="description"></param>
        /// <param name="country"></param>
        /// <param name="location"></param>
        /// <param name="type"></param>
        /// <param name="date"></param>
        /// <param name="date_year"></param>
        /// <param name="date_month"></param>
        /// <param name="date_day"></param>
        /// <param name="week_day"></param>
        // ReSharper disable InconsistentNaming
        protected AbstractApiHolidays(string name, string name_local, string language, string description,
            string country,
            string location, string type, string date, string date_year, string date_month, string date_day,
            string week_day)
        {
            Name = name;
            Name_local = name_local;
            Language = language;
            Description = description;
            Location = location;
            if (Location.Contains(" - "))
                Locations = Location
                    .Substring(Location.IndexOf(" - ", StringComparison.Ordinal) + 3,
                        Location.Length - Location.IndexOf(" - ", StringComparison.Ordinal) - 3)
                    .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
            else
                Locations = Location.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim())
                    .ToList();

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

        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public string Name { get; }
        public string Name_local { get; }
        public string Language { get; }
        public string Description { get; }
        public string Country { get; }
        public string Location { get; }
        public List<string> Locations { get; }
        public string Type { get; }
        public DateTime LocalStart { get; }
        public DateTime UtcStart { get; }
        public DateTime UtcEnd { get; }
        public string Date { get; }
        public string Date_year { get; }
        public string Date_month { get; }
        public string Date_day { get; }
        public string Week_day { get; }
    }
}