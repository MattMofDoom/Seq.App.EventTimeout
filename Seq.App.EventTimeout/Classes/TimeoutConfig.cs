using System;
using System.Collections.Generic;
using Lurgle.Dates.Classes;
using Seq.Apps.LogEvents;

namespace Seq.App.EventTimeout.Classes
{
    public class TimeoutConfig
    {
        public string AppName { get; set; }
        public bool BypassLocal { get; set; }
        public List<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek>();
        public string DueDate { get; set; }
        public string EndFormat { get; set; } = "H:mm:ss";
        public bool IncludeApp { get; set; }
        public bool IncludeBank { get; set; }
        public bool IncludeDescription { get; set; }
        public bool IncludeWeekends { get; set; }
        public string InitialTimeEstimate { get; set; }
        public bool Is24H { get; set; }
        public bool IsTags { get; set; }
        public string[] LocalAddresses { get; set; } = Array.Empty<string>();
        public List<string> LocaleMatch { get; set; } = new List<string>();
        public string Priority { get; set; }
        public string ProjectKey { get; set; }
        public string Proxy { get; set; }
        public string ProxyPass { get; set; }
        public string ProxyUser { get; set; }
        public string RemainingTimeEstimate { get; set; }
        public bool RepeatTimeout { get; set; }
        public TimeSpan RepeatTimeoutSuppress { get; set; }
        public string Responders { get; set; }
        public string StartFormat { get; set; } = "H:mm:ss";
        public TimeSpan SuppressionTime { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string TestDate { get; set; }
        public TimeSpan TimeOut { get; set; }
        public LogEventLevel TimeoutLogLevel { get; set; }
        public bool UseHandlebars { get; set; }
        public bool UseHolidays { get; set; }
        public bool UseProxy { get; set; }
        public List<DateTime> ExcludeDays { get; set; } = new List<DateTime>();
        public List<DateTime> IncludeDays { get; set; } = new List<DateTime>();
        public List<AbstractApiHolidays> Holidays { get; set; } = new List<AbstractApiHolidays>();
        public DateTime TestOverrideTime { get; set; } = DateTime.Now;
        public bool UseTestOverrideTime { get; set; }
        public string ApiKey { get; set; }
        public string Country { get; set; }
        public bool Diagnostics { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public List<string> HolidayMatch { get; set; } = new List<string>();
    }
}