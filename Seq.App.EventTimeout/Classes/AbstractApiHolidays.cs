using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Seq.App.EventTimeout.Classes
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
            this.name = name;
            this.name_local = name_local;
            this.language = language;
            this.description = description;
            this.location = location;
            if (this.location.Contains(" - "))
                Locations = this.location
                    .Substring(this.location.IndexOf(" - ", StringComparison.Ordinal) + 3,
                        this.location.Length - this.location.IndexOf(" - ", StringComparison.Ordinal) - 3)
                    .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
            else
                Locations = this.location.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();

            this.country = country;
            this.type = type;
            this.date = date;
            this.date_year = date_year;
            this.date_month = date_month;
            this.date_day = date_day;
            this.week_day = week_day;
            LocalStart = DateTime.ParseExact(this.date, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
            UtcStart = LocalStart.ToUniversalTime();
            UtcEnd = LocalStart.AddDays(1).ToUniversalTime();
        }

        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public string name { get; }
        public string name_local { get; }
        public string language { get; }
        public string description { get; }
        public string country { get; }
        public string location { get; }
        public List<string> Locations { get; }
        public string type { get; }
        public DateTime LocalStart { get; }
        public DateTime UtcStart { get; }
        public DateTime UtcEnd { get; }
        public string date { get; }
        public string date_year { get; }
        public string date_month { get; }
        public string date_day { get; }
        public string week_day { get; }
    }
}