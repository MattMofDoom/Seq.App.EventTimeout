using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Timers;
using Lurgle.Dates;
using Lurgle.Dates.Classes;
using Seq.App.EventTimeout.Classes;
using Seq.Apps;
using Seq.Apps.LogEvents;

// ReSharper disable MemberCanBePrivate.Global

namespace Seq.App.EventTimeout
{
    [SeqApp("Event Timeout", AllowReprocessing = false,
        Description =
            "Super-powered monitoring of Seq events with start/end times, timeout and suppression intervals, matching multiple properties, day of week and day of month inclusion/exclusion, and optional holiday API!")]
    // ReSharper disable once UnusedType.Global
    public class EventTimeoutReactor : SeqApp, ISubscribeTo<LogEventData>
    {
        private static readonly Dictionary<string, string> ResponderLookup = new Dictionary<string, string>();
        private string _alertDescription;
        private string _alertMessage;
        private string _apiKey;
        private bool _bypassLocal;
        private bool _cannotMatchAlerted;
        private string _country;
        private List<DayOfWeek> _daysOfWeek;
        private bool _diagnostics;
        private string _dueDate;
        private string _endFormat = "H:mm:ss";
        private DateTime _endTime;
        private int _errorCount;
        private List<string> _holidayMatch;
        private bool _includeApp;
        private bool _includeBank;
        private bool _includeDescription;
        private bool _includeWeekends;
        private string _initialTimeEstimate;

        private bool _is24H;
        private bool _isTags;
        private bool _isUpdating;
        private DateTime _lastCheck;
        private DateTime _lastDay;
        private DateTime _lastError;
        private DateTime _lastLog;
        private int _lastMatched;
        private DateTime _lastMatchLog;
        private DateTime _lastUpdate;
        private string[] _localAddresses;

        private List<string> _localeMatch;
        private string _priority;
        private string _projectKey;
        private Dictionary<string, string> _properties;
        private string _proxy;
        private string _proxyPass;
        private string _proxyUser;
        private string _remainingTimeEstimate;
        private bool _repeatTimeout;
        private TimeSpan _repeatTimeoutSuppress;
        private string _responders;
        private int _retryCount;
        private bool _skippedShowtime;
        private string _startFormat = "H:mm:ss";
        private DateTime _startTime;
        private TimeSpan _suppressionTime;
        private string[] _tags;
        private string _testDate;
        private TimeSpan _timeOut;
        private LogEventLevel _timeoutLogLevel;
        private Timer _timer;
        private bool _useHolidays;
        private bool _useProxy; // ReSharper disable MemberCanBePrivate.Global
        public List<DateTime> ExcludeDays;
        public List<DateTime> IncludeDays;
        public List<AbstractApiHolidays> Holidays;
        public bool IsAlert;
        public bool IsShowtime;

        // Count of matches
        public int Matched;
        public DateTime TestOverrideTime = DateTime.Now;
        public bool UseTestOverrideTime;

        // ReSharper disable UnusedAutoPropertyAccessor.Global
        // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
        [SeqAppSetting(
            DisplayName = "Diagnostic logging",
            HelpText = "Send extra diagnostic logging to the stream. Recommended to enable.")]
        public bool Diagnostics { get; set; } = true;

        [SeqAppSetting(
            DisplayName = "Start time",
            HelpText = "The time (H:mm:ss, 24 hour format) to start monitoring.")]
        public string StartTime { get; set; }

        [SeqAppSetting(
            DisplayName = "End time",
            HelpText = "The time (H:mm:ss, 24 hour format) to stop monitoring, up to 24 hours after start time.")]
        public string EndTime { get; set; }

        [SeqAppSetting(
            DisplayName = "Timeout interval (seconds)",
            HelpText =
                "Time period in which a matching log entry must be seen. After this, an alert will be raised. Default 60, Minimum 1.",
            InputType = SettingInputType.Integer)]
        public int Timeout { get; set; } = 60;

        [SeqAppSetting(
            DisplayName = "Enable repeat timeout",
            HelpText =
                "Optionally re-arm the timeout after a match, within the start/end time parameters - useful for a 'heartbeat' style alert. Disabled by default.",
            IsOptional = true)]
        public bool RepeatTimeout { get; set; }

        [SeqAppSetting(
            DisplayName = "Suppression interval (seconds)",
            HelpText =
                "If an alert has been raised, further alerts will be suppressed for this time. Default 60, Minimum 0.",
            InputType = SettingInputType.Integer)]
        public int SuppressionTime { get; set; } = 60;

        [SeqAppSetting(
            DisplayName = "Repeat timeout suppression (seconds)",
            HelpText =
                "If Repeat timeout is enabled, suppress further 'matched' entries for this time. Default 60, Minimum 0.",
            InputType = SettingInputType.Integer,
            IsOptional = true)]
        public int RepeatTimeoutSuppress { get; set; } = 60;

        [SeqAppSetting(DisplayName = "Log level for timeouts",
            HelpText = "Verbose, Debug, Information, Warning, Error, Fatal. Defaults to Error.",
            IsOptional = true)]
        public string TimeoutLogLevel { get; set; }

        [SeqAppSetting(DisplayName = "Priority for timeouts",
            HelpText = "Optional Priority property to pass for timeouts, for use with other apps.",
            IsOptional = true)]
        public string Priority { get; set; }

        [SeqAppSetting(DisplayName = "Responders for timeouts",
            HelpText = "Optional Responders property to pass for timeouts, for use with other apps.",
            IsOptional = true)]
        public string Responders { get; set; }

        [SeqAppSetting(DisplayName = "Project Key for scheduled logs",
            HelpText = "Optional Project Key property to pass for scheduled logs, for use with other apps.",
            IsOptional = true)]
        public string ProjectKey { get; set; }

        [SeqAppSetting(DisplayName = "Initial Time Estimate for scheduled logs",
            HelpText = "Optional Initial Time Estimate property to pass for scheduled logs, for use with other apps.",
            IsOptional = true)]
        public string InitialTimeEstimate { get; set; }

        [SeqAppSetting(DisplayName = "Remaining Time Estimate for scheduled logs",
            HelpText = "Optional Remaining Time Estimate property to pass for scheduled logs, for use with other apps.",
            IsOptional = true)]
        public string RemainingTimeEstimate { get; set; }

        [SeqAppSetting(DisplayName = "Due Date for scheduled logs",
            HelpText = "Optional Due Date property to pass for scheduled logs, for use with other apps.",
            IsOptional = true)]
        public string DueDate { get; set; }

        [SeqAppSetting(
            DisplayName = "Days of week",
            HelpText = "Comma-delimited - Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday.",
            IsOptional = true)]
        public string DaysOfWeek { get; set; }

        [SeqAppSetting(
            DisplayName = "Include days of month",
            HelpText =
                "Only run on these days. Comma-delimited - first,last,first weekday,last weekday,first-fourth sunday-saturday,1-31.",
            IsOptional = true)]
        public string IncludeDaysOfMonth { get; set; }

        [SeqAppSetting(
            DisplayName = "Exclude days of month",
            HelpText = "Never run on these days. Comma-delimited - first,last,1-31.",
            IsOptional = true)]
        public string ExcludeDaysOfMonth { get; set; }


        [SeqAppSetting(
            DisplayName = "Property 1 name",
            HelpText =
                "Case insensitive property name (must be a full match). If not configured, the @Message property will be used. If this is not seen in the configured timeout, an alert will be raised.",
            IsOptional = true)]
        public string Property1Name { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 1 match",
            HelpText =
                "Case insensitive text to match - partial match okay. If not configured, ANY text will match. If this is not seen in the configured timeout, an alert will be raised.",
            IsOptional = true)]
        public string TextMatch { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 2 name",
            HelpText =
                "Case insensitive property name (must be a full match). If not configured, this will not be evaluated.",
            IsOptional = true)]
        public string Property2Name { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 2 match",
            HelpText =
                "Case insensitive text to match - partial match okay. If property name is set and this is not configured, ANY text will match.",
            IsOptional = true)]
        public string Property2Match { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 3 name",
            HelpText =
                "Case insensitive property name (must be a full match). If not configured, this will not be evaluated.",
            IsOptional = true)]
        public string Property3Name { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 3 match",
            HelpText =
                "Case insensitive text to match - partial match okay. If property name is set and this is not configured, ANY text will match.",
            IsOptional = true)]
        public string Property3Match { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 4 name",
            HelpText =
                "Case insensitive property name (must be a full match). If not configured, this will not be evaluated.",
            IsOptional = true)]
        public string Property4Name { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 4 match",
            HelpText =
                "Case insensitive text to match - partial match okay. If property name is set and this is not configured, ANY text will match.",
            IsOptional = true)]
        public string Property4Match { get; set; }

        [SeqAppSetting(
            DisplayName = "Alert message",
            HelpText =
                "Event message to raise. Allows tokens for date parts: Day: {d}/{dd}/{ddd}/{dddd}, Month: {M}/{MM}/{MMM}/{MMMM}, Year: {yy}/{yyyy}. These are not case sensitive.")]
        public string AlertMessage { get; set; }

        [SeqAppSetting(
            IsOptional = true,
            DisplayName = "Alert description",
            HelpText =
                "Optional description associated with the event raised. Allows tokens for date parts: Day: {d}/{dd}/{ddd}/{dddd}, Month: {M}/{MM}/{MMM}/{MMMM}, Year: {yy}/{yyyy}. These are not case sensitive.")]
        public string AlertDescription { get; set; }

        [SeqAppSetting(
            DisplayName = "Include description with log message",
            HelpText =
                "If selected, the configured description will be part of the log message. Otherwise it will only show as a log property, which can be used by other Seq apps.",
            IsOptional = true)]
        public bool? IncludeDescription { get; set; } = false;

        [SeqAppSetting(
            IsOptional = true,
            DisplayName = "Alert tags",
            HelpText =
                "Tags for the event, separated by commas.  Allows tokens for date parts: Day: {d}/{dd}/{ddd}/{dddd}, Month: {M}/{MM}/{MMM}/{MMMM}, Year: {yy}/{yyyy}. These are not case sensitive.")]
        public string Tags { get; set; }

        [SeqAppSetting(
            DisplayName = "Include instance name in alert message",
            HelpText = "Prepend the instance name to the alert message.")]
        public bool IncludeApp { get; set; }


        [SeqAppSetting(
            DisplayName = "Holidays - use Holidays API for public holiday detection",
            HelpText = "Connect to the AbstractApi Holidays service to detect public holidays.")]
        public bool UseHolidays { get; set; } = false;

        [SeqAppSetting(
            DisplayName = "Holidays - Retry count",
            HelpText = "Retry count for retrieving the Holidays API. Default 10, minimum 0, maximum 100.",
            InputType = SettingInputType.Integer,
            IsOptional = true)]
        public int RetryCount { get; set; } = 10;

        [SeqAppSetting(
            DisplayName = "Holidays - Country code",
            HelpText = "Two letter country code (eg. AU).",
            IsOptional = true)]
        public string Country { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - API key",
            HelpText = "Sign up for an API key at https://www.abstractapi.com/holidays-api and enter it here.",
            IsOptional = true,
            InputType = SettingInputType.Password)]
        public string ApiKey { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - match these holiday types",
            HelpText =
                "Comma-delimited list of holiday types (eg. National, Local) - case insensitive, partial match okay.",
            IsOptional = true)]
        public string HolidayMatch { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - match these locales",
            HelpText =
                "Holidays are valid if the location matches one of these comma separated values (eg. Australia,New South Wales) - case insensitive, must be a full match.",
            IsOptional = true)]
        public string LocaleMatch { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - include weekends",
            HelpText = "Include public holidays that are returned for weekends.")]
        public bool IncludeWeekends { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - include Bank Holidays.",
            HelpText = "Include bank holidays")]
        public bool IncludeBank { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - test date",
            HelpText = "yyyy-M-d format. Used only for diagnostics - should normally be empty.",
            IsOptional = true)]
        public string TestDate { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - proxy address",
            HelpText = "Proxy address for Holidays API.",
            IsOptional = true)]
        public string Proxy { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - proxy bypass local addresses",
            HelpText = "Bypass local addresses for proxy.")]
        public bool BypassLocal { get; set; } = true;

        [SeqAppSetting(
            DisplayName = "Holidays - local addresses for proxy bypass",
            HelpText = "Local addresses to bypass, comma separated.",
            IsOptional = true)]
        public string LocalAddresses { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - proxy username",
            HelpText = "Username for proxy authentication.",
            IsOptional = true)]
        public string ProxyUser { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - proxy password",
            HelpText = "Username for proxy authentication.",
            IsOptional = true,
            InputType = SettingInputType.Password)]


        public string ProxyPass { get; set; }

        public void On(Event<LogEventData> evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            var timeNow = DateTime.UtcNow;
            var cannotMatch = false;
            var cannotMatchProperties = new List<string>();
            var properties = 0;
            var matches = 0;

            if (Matched != 0 && !_repeatTimeout || !IsShowtime) return;
            foreach (var property in _properties)
            {
                properties++;
                if (property.Key.Equals("@Message", StringComparison.OrdinalIgnoreCase))
                {
                    if (PropertyMatch.Matches(evt.Data.RenderedMessage, property.Value)) matches++;
                }
                else
                {
                    var matchedKey = false;

                    //IReadOnlyDictionary ContainsKey is case sensitive, so we need to iterate
                    foreach (var key in evt.Data.Properties)
                        if (key.Key.Equals(property.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedKey = true;
                            if (string.IsNullOrEmpty(property.Value) || !PropertyMatch.Matches(
                                evt.Data.Properties[property.Key].ToString(),
                                property.Value)) continue;
                            matches++;
                            break;
                        }

                    //If one of the configured properties doesn't have a matching property on the event, we won't be able to raise an alert
                    if (matchedKey) continue;
                    cannotMatch = true;
                    cannotMatchProperties.Add(property.Key);
                }
            }

            switch (cannotMatch)
            {
                case true when !_cannotMatchAlerted:
                    LogEvent(LogEventLevel.Debug,
                        "Warning - An event was seen without the properties {PropertyName}, which may impact the ability to alert on failures - further failures will not be logged ...",
                        string.Join(",", cannotMatchProperties.ToArray()));
                    _cannotMatchAlerted = true;
                    break;
                //If all configured properties were present and had matches, log an event
                case false when properties == matches:
                {
                    Matched++;
                    var lastMatch = _lastMatched;

                    var difference = timeNow - _lastMatchLog;
                    _lastCheck = timeNow;

                    //Allow for repeating timeouts
                    if (!_repeatTimeout)
                    {
                        LogEvent(LogEventLevel.Debug,
                            "Successfully matched {TextMatch}! Further matches will not be logged ...",
                            PropertyMatch.MatchConditions(_properties));
                    }
                    else
                    {
                        if (lastMatch == 0 || difference.TotalSeconds > _repeatTimeoutSuppress.TotalSeconds)
                        {
                            _lastMatchLog = timeNow;
                            //Only log one event regardless of how many match the first event                        
                            if (lastMatch == 0 && Matched == 1 || _lastMatched > 0)
                                LogEvent(LogEventLevel.Debug,
                                    "Successfully matched {TextMatch}! Total matches {Total} - resetting timeout to {Timeout} seconds, further matches will not be logged for {Suppression} seconds ...",
                                    PropertyMatch.MatchConditions(_properties),
                                    Matched, _timeOut.TotalSeconds, _repeatTimeoutSuppress.TotalSeconds);
                        }
                    }

                    break;
                }
            }
        }

        protected override void OnAttached()
        {
            LogEvent(LogEventLevel.Debug, "Check {AppName} diagnostic level ({Diagnostics}) ...", App.Title,
                Diagnostics);
            _diagnostics = Diagnostics;

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Check include {AppName} ({IncludeApp}) ...", App.Title, IncludeApp);

            _includeApp = IncludeApp;
            if (!_includeApp && _diagnostics)
                LogEvent(LogEventLevel.Debug, "App name {AppName} will not be included in alert message ...",
                    App.Title);
            else if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "App name {AppName} will be included in alert message ...", App.Title);

            if (!DateTime.TryParseExact(StartTime, "H:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out _))
            {
                if (DateTime.TryParseExact(StartTime, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out _))
                    _startFormat = "H:mm";
                else
                    LogEvent(LogEventLevel.Debug,
                        "Start Time {StartTime} does  not parse to a valid DateTime - app will exit ...", StartTime);
            }

            if (!DateTime.TryParseExact(EndTime, "H:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out _))
            {
                if (DateTime.TryParseExact(EndTime, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out _))
                    _endFormat = "H:mm";
                else
                    LogEvent(LogEventLevel.Debug,
                        "End Time {EndTime} does  not parse to a valid DateTime - app will exit ...", EndTime);
            }

            LogEvent(LogEventLevel.Debug,
                "Use Holidays API {UseHolidays}, Country {Country}, Has API key {IsEmpty} ...", UseHolidays, Country,
                !string.IsNullOrEmpty(ApiKey));
            SetHolidays();
            RetrieveHolidays(DateTime.Today, DateTime.UtcNow);

            if (!_useHolidays || _isUpdating) UtcRollover(DateTime.UtcNow);

            //Enforce minimum timeout interval
            if (Timeout <= 0)
                Timeout = 1;
            if (_diagnostics) LogEvent(LogEventLevel.Debug, "Convert Timeout {Timeout} to TimeSpan ...", Timeout);

            _timeOut = TimeSpan.FromSeconds(Timeout);
            if (_diagnostics) LogEvent(LogEventLevel.Debug, "Parsed Timeout is {Timeout} ...", _timeOut.TotalSeconds);

            if (_diagnostics) LogEvent(LogEventLevel.Debug, "Repeat Timeout: {RepeatTimeout} ...", RepeatTimeout);

            _repeatTimeout = RepeatTimeout;

            //Negative values not permitted
            if (SuppressionTime < 0)
                SuppressionTime = 0;
            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Convert Suppression {Suppression} to TimeSpan ...", SuppressionTime);

            _suppressionTime = TimeSpan.FromSeconds(SuppressionTime);
            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Parsed Suppression is {Suppression} ...", _suppressionTime.TotalSeconds);

            //Negative values not permitted
            if (RepeatTimeoutSuppress < 0)
                RepeatTimeoutSuppress = 0;
            if (_diagnostics)
                LogEvent(LogEventLevel.Debug,
                    "Convert Repeat Timeout Suppression {RepeatTimeoutSuppress} to TimeSpan ...",
                    RepeatTimeoutSuppress);
            _repeatTimeoutSuppress = TimeSpan.FromSeconds(RepeatTimeoutSuppress);
            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Parsed Repeat Timeout Suppression is {RepeatTimeoutSuppress} ...",
                    _repeatTimeoutSuppress.TotalSeconds);

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Convert Days of Week {DaysOfWeek} to UTC Days of Week ...", DaysOfWeek);


            _daysOfWeek = Dates.GetUtcDaysOfWeek(DaysOfWeek, StartTime, _startFormat);

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "UTC Days of Week {DaysOfWeek} will be used ...", _daysOfWeek.ToArray());

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Include Days of Month {IncludeDays} ...", IncludeDaysOfMonth);

            IncludeDays = Dates.GetUtcDaysOfMonth(IncludeDaysOfMonth, StartTime, _startFormat, DateTime.Now);
            if (IncludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Include UTC Days of Month: {IncludeDays} ...", IncludeDays.ToArray());
            else
                LogEvent(LogEventLevel.Debug, "Include UTC Days of Month: ALL ...");

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Exclude Days of Month {ExcludeDays} ...", ExcludeDaysOfMonth);

            ExcludeDays = Dates.GetUtcDaysOfMonth(ExcludeDaysOfMonth, StartTime, _startFormat, DateTime.Now);
            if (ExcludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Exclude UTC Days of Month: {ExcludeDays} ...", ExcludeDays.ToArray());
            else
                LogEvent(LogEventLevel.Debug, "Exclude UTC Days of Month: NONE ...");

            //Evaluate the properties we will match
            _properties = SetProperties();
            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Match criteria will be: {MatchText}",
                    PropertyMatch.MatchConditions(_properties));

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Alert Message '{AlertMessage}' ...", AlertMessage);

            _alertMessage = string.IsNullOrWhiteSpace(AlertMessage)
                ? "An event timeout has occurred!"
                : AlertMessage.Trim();
            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Alert Message '{AlertMessage}' will be used ...", _alertMessage);

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Alert Description '{AlertDescription}' ...", AlertDescription);

            _alertDescription = string.IsNullOrWhiteSpace(AlertDescription)
                ? ""
                : AlertDescription.Trim();
            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Alert Description '{AlertDescription}' will be used ...",
                    _alertDescription);

            if (IncludeDescription != null)
                _includeDescription = (bool) IncludeDescription;
            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Include Description in Log Message: '{IncludeDescription}' ...",
                    _includeDescription);

            if (_diagnostics) LogEvent(LogEventLevel.Debug, "Convert Tags '{Tags}' to array ...", Tags);

            _tags = (Tags ?? "")
                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToArray();
            if (_tags.Length > 0) _isTags = true;

            if (string.IsNullOrWhiteSpace(TimeoutLogLevel)) TimeoutLogLevel = "Error";
            if (!Enum.TryParse(TimeoutLogLevel, out _timeoutLogLevel)) _timeoutLogLevel = LogEventLevel.Error;

            if (!string.IsNullOrEmpty(Priority))
                _priority = Priority;

            if (!string.IsNullOrEmpty(Responders))
            {
                if (Responders.Contains('='))
                {
                    LogEvent(LogEventLevel.Debug, "Convert Responders to dictionary ...");
                    var responderList = (Responders ?? "")
                        .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .ToList();
                    foreach (var x in from responder in responderList
                        where responder.Contains("=")
                        select responder.Split('='))
                    {
                        ResponderLookup.Add(x[0], x[1]);
                        if (_diagnostics)
                            LogEvent(LogEventLevel.Debug, "Add mapping for {LogToken} to {Responder}", x[0], x[1]);
                    }
                }
                else
                {
                    _responders = Responders;
                    if (_diagnostics)
                        LogEvent(LogEventLevel.Debug, "Set responder to {Responder}", _responders);
                }
            }

            if (!string.IsNullOrEmpty(ProjectKey))
            {
                if (_diagnostics)
                    LogEvent(LogEventLevel.Debug, "Set Project Key to {Value}", ProjectKey);
                _projectKey = ProjectKey;
            }

            if (!string.IsNullOrEmpty(InitialTimeEstimate) && DateTokens.ValidDateExpression(InitialTimeEstimate))
            {
                if (_diagnostics)
                    LogEvent(LogEventLevel.Debug, "Set Initial Time Estimate to {Value}",
                        DateTokens.SetValidExpression(InitialTimeEstimate));
                _initialTimeEstimate = DateTokens.SetValidExpression(InitialTimeEstimate);
            }

            if (!string.IsNullOrEmpty(RemainingTimeEstimate) && DateTokens.ValidDateExpression(RemainingTimeEstimate))
            {
                if (_diagnostics)
                    LogEvent(LogEventLevel.Debug, "Set Remaining Time Estimate to {Value}",
                        DateTokens.SetValidExpression(RemainingTimeEstimate));
                _remainingTimeEstimate = DateTokens.SetValidExpression(RemainingTimeEstimate);
            }

            if (!string.IsNullOrEmpty(DueDate) &&
                (DateTokens.ValidDateExpression(DueDate) || DateTokens.ValidDate(DueDate)))
            {
                if (_diagnostics)
                    LogEvent(LogEventLevel.Debug, "Set Due Date to {Value}",
                        DateTokens.ValidDate(DueDate) ? DueDate : DateTokens.SetValidExpression(DueDate));
                _dueDate = DateTokens.ValidDate(DueDate) ? DueDate : DateTokens.SetValidExpression(DueDate);
            }

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Log level {loglevel} will be used for timeouts on {Instance} ...",
                    _timeoutLogLevel, App.Title);

            if (_diagnostics) LogEvent(LogEventLevel.Debug, "Starting timer ...");

            _timer = new Timer(1000)
            {
                AutoReset = true
            };
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
            if (_diagnostics) LogEvent(LogEventLevel.Debug, "Timer started ...");
        }


        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var timeNow = DateTime.UtcNow;
            var localDate = DateTime.Today;
            if (!string.IsNullOrEmpty(_testDate))
                localDate = DateTime.ParseExact(_testDate, "yyyy-M-d", CultureInfo.InvariantCulture,
                    DateTimeStyles.None);

            if (_lastDay < localDate) RetrieveHolidays(localDate, timeNow);

            //We can only enter showtime if we're not currently retrying holidays, but existing showtimes will continue to monitor
            if ((!_useHolidays || IsShowtime || !IsShowtime && !_isUpdating) && timeNow >= _startTime &&
                timeNow < _endTime)
            {
                if (!IsShowtime && (!_daysOfWeek.Contains(_startTime.DayOfWeek) ||
                                    IncludeDays.Count > 0 && !IncludeDays.Contains(_startTime) ||
                                    ExcludeDays.Contains(_startTime)))
                {
                    //Log that we have skipped a day due to an exclusion
                    if (!_skippedShowtime)
                        LogEvent(LogEventLevel.Debug,
                            "Matching will not be performed due to exclusions - Day of Week Excluded {DayOfWeek}, Day Of Month Not Included {IncludeDay}, Day of Month Excluded {ExcludeDay} ...",
                            !_daysOfWeek.Contains(_startTime.DayOfWeek),
                            IncludeDays.Count > 0 && !IncludeDays.Contains(_startTime),
                            ExcludeDays.Count > 0 && ExcludeDays.Contains(_startTime));

                    _skippedShowtime = true;
                }
                else
                {
                    //Showtime! - Evaluate whether we have matched properties with log events
                    if (!IsShowtime)
                    {
                        LogEvent(LogEventLevel.Debug,
                            "UTC Start Time {Time} ({DayOfWeek}), monitoring for {MatchText} within {Timeout} seconds, until UTC End time {EndTime} ({EndDayOfWeek}) ...",
                            _startTime.ToShortTimeString(), _startTime.DayOfWeek,
                            PropertyMatch.MatchConditions(_properties), _timeOut.TotalSeconds,
                            _endTime.ToShortTimeString(), _endTime.DayOfWeek);
                        IsShowtime = true;
                        _lastCheck = timeNow;
                        _lastMatchLog = timeNow;
                    }

                    var difference = timeNow - _lastCheck;
                    //Check the timeout versus any successful matches. If repeating timeouts are enabled, we'll compare matched with lastMatched to detect if there's been any matches
                    if (difference.TotalSeconds > _timeOut.TotalSeconds &&
                        (Matched == 0 || _repeatTimeout && Matched == _lastMatched))
                    {
                        var suppressDiff = timeNow - _lastLog;
                        if (IsAlert && suppressDiff.TotalSeconds < _suppressionTime.TotalSeconds) return;

                        //Log event
                        var message = DateTokens.HandleTokens(_alertMessage);
                        var description = DateTokens.HandleTokens(_alertDescription);

                        //Log event
                        ScheduledLogEvent(_timeoutLogLevel, message, description);

                        _lastLog = timeNow;
                        IsAlert = true;
                    }
                    else
                    {
                        if (_repeatTimeout)
                            IsAlert = false;
                    }

                    //Grab a snapshot of the match count for next evaluation
                    _lastMatched = Matched;
                }
            }
            else if (timeNow < _startTime || timeNow >= _endTime)
            {
                //Showtime can end even if we're retrieving holidays
                if (IsShowtime)
                    LogEvent(LogEventLevel.Debug,
                        "UTC End Time {Time} ({DayOfWeek}), no longer monitoring for {MatchText}, total matches {Matches} ...",
                        _endTime.ToShortTimeString(), _endTime.DayOfWeek, PropertyMatch.MatchConditions(_properties),
                        Matched);

                //Reset the match counters
                _lastLog = timeNow;
                _lastCheck = timeNow;
                _lastMatchLog = timeNow;
                Matched = 0;
                _lastMatched = 0;
                IsAlert = false;
                IsShowtime = false;
                _cannotMatchAlerted = false;
                _skippedShowtime = false;
            }

            //We can only do UTC rollover if we're not currently retrying holidays and it's not during showtime
            if (IsShowtime || _useHolidays && _isUpdating || _startTime > timeNow ||
                !string.IsNullOrEmpty(_testDate)) return;
            UtcRollover(timeNow);
            //Take the opportunity to refresh include/exclude days to allow for month rollover
            IncludeDays = Dates.GetUtcDaysOfMonth(IncludeDaysOfMonth, StartTime, _startFormat, DateTime.Now);
            if (IncludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Include UTC Days of Month: {IncludeDays} ...",
                    IncludeDays.ToArray());

            ExcludeDays = Dates.GetUtcDaysOfMonth(ExcludeDaysOfMonth, StartTime, _startFormat, DateTime.Now);
            if (ExcludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Exclude UTC Days of Month: {ExcludeDays} ...",
                    ExcludeDays.ToArray());
        }

        /// <summary>
        ///     Create a dictionary of rules for event properties and case-insensitive text match
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> SetProperties()
        {
            var properties = new Dictionary<string, string>();

            //Property 1 is mandatory, and will be @Message unless PropertyName is overriden
            var property = GetProperty(1, Property1Name, TextMatch);
            properties.Add(property.Key, property.Value);
            property = GetProperty(2, Property2Name, Property2Match);
            if (!string.IsNullOrEmpty(property.Key)) properties.Add(property.Key, property.Value);

            property = GetProperty(3, Property3Name, Property3Match);
            if (!string.IsNullOrEmpty(property.Key)) properties.Add(property.Key, property.Value);

            property = GetProperty(4, Property4Name, Property4Match);
            if (!string.IsNullOrEmpty(property.Key)) properties.Add(property.Key, property.Value);

            return properties;
        }

        /// <summary>
        ///     Return a property and case-insensitive text match rule
        /// </summary>
        /// <param name="property"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyMatch"></param>
        /// <returns></returns>
        private KeyValuePair<string, string> GetProperty(int property, string propertyName, string propertyMatch)
        {
            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Property {PropertyNo}: '{PropertyNameValue}' ...", property,
                    propertyName);

            var propertyResult = PropertyMatch.GetProperty(property, propertyName, propertyMatch);

            if (!string.IsNullOrEmpty(propertyResult.Key) && _diagnostics)
            {
                if (!string.IsNullOrEmpty(propertyMatch))
                    LogEvent(LogEventLevel.Debug,
                        "Property {PropertyNo} '{PropertyName}' will be used to match '{PropertyMatch}'...", property,
                        propertyResult.Key, propertyResult.Value);
                else
                    LogEvent(LogEventLevel.Debug,
                        "Property {PropertyNo} '{PropertyName}' will be used to match ANY text ...", property,
                        propertyResult.Key);
            }
            else if (_diagnostics)
            {
                LogEvent(LogEventLevel.Debug, "Property {PropertyNo} will not be used to match values ...", property);
            }

            return propertyResult;
        }

        /// <summary>
        ///     Configure Abstract API Holidays for this instance
        /// </summary>
        private void SetHolidays()
        {
            switch (UseHolidays)
            {
                case true when !string.IsNullOrEmpty(Country) && !string.IsNullOrEmpty(ApiKey):
                {
                    if (_diagnostics) LogEvent(LogEventLevel.Debug, "Validate Country {Country}", Country);

                    if (Lurgle.Dates.Holidays.ValidateCountry(Country))
                    {
                        _useHolidays = true;
                        _retryCount = 10;
                        if (RetryCount >= 0 && RetryCount <= 100)
                            _retryCount = RetryCount;
                        _country = Country;
                        _apiKey = ApiKey;
                        _includeWeekends = IncludeWeekends;
                        _includeBank = IncludeBank;

                        if (string.IsNullOrEmpty(HolidayMatch))
                            _holidayMatch = new List<string>();
                        else
                            _holidayMatch = HolidayMatch.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim()).ToList();

                        if (string.IsNullOrEmpty(LocaleMatch))
                            _localeMatch = new List<string>();
                        else
                            _localeMatch = LocaleMatch.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim()).ToList();

                        if (!string.IsNullOrEmpty(Proxy))
                        {
                            _useProxy = true;
                            _proxy = Proxy;
                            _bypassLocal = BypassLocal;
                            _localAddresses = LocalAddresses.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim()).ToArray();
                            _proxyUser = ProxyUser;
                            _proxyPass = ProxyPass;
                        }

                        if (_diagnostics)
                            LogEvent(LogEventLevel.Debug,
                                "Holidays API Enabled: {UseHolidays}, Country {Country}, Use Proxy {UseProxy}, Proxy Address {Proxy}, BypassLocal {BypassLocal}, Authentication {Authentication} ...",
                                _useHolidays, _country,
                                _useProxy, _proxy, _bypassLocal,
                                !string.IsNullOrEmpty(ProxyUser) && !string.IsNullOrEmpty(ProxyPass));

                        WebClient.SetConfig(App.Title, _useProxy, _proxy, _proxyUser, _proxyPass, _bypassLocal,
                            _localAddresses);
                    }
                    else
                    {
                        _useHolidays = false;
                        LogEvent(LogEventLevel.Debug,
                            "Holidays API Enabled: {UseHolidays}, Could not parse country {CountryCode} to valid region ...",
                            _useHolidays, _country);
                    }

                    break;
                }
                case true:
                    _useHolidays = false;
                    LogEvent(LogEventLevel.Debug, "Holidays API Enabled: {UseHolidays}, One or more parameters not set",
                        _useHolidays);
                    break;
            }

            _lastDay = DateTime.Today.AddDays(-1);
            _lastError = DateTime.Now.AddDays(-1);
            _lastUpdate = DateTime.Now.AddDays(-1);
            _errorCount = 0;
            _testDate = TestDate;
            Holidays = new List<AbstractApiHolidays>();
        }

        /// <summary>
        ///     Update AbstractAPI Holidays for this instance, given local and UTC date
        /// </summary>
        /// <param name="localDate"></param>
        /// <param name="utcDate"></param>
        private void RetrieveHolidays(DateTime localDate, DateTime utcDate)
        {
            if (_useHolidays && (!_isUpdating || _isUpdating && (DateTime.Now - _lastUpdate).TotalSeconds > 10 &&
                (DateTime.Now - _lastError).TotalSeconds > 10 && _errorCount < _retryCount))
            {
                _isUpdating = true;
                if (!string.IsNullOrEmpty(_testDate))
                    localDate = DateTime.ParseExact(_testDate, "yyyy-M-d", CultureInfo.InvariantCulture,
                        DateTimeStyles.None);

                if (_diagnostics)
                    LogEvent(LogEventLevel.Debug,
                        "Retrieve holidays for {Date}, Last Update {LastUpdateDate} {LastUpdateTime} ...",
                        localDate.ToShortDateString(), _lastUpdate.ToShortDateString(),
                        _lastUpdate.ToShortTimeString());

                var holidayUrl = WebClient.GetUrl(_apiKey, _country, localDate);
                if (_diagnostics) LogEvent(LogEventLevel.Debug, "URL used is {url} ...", holidayUrl);

                try
                {
                    _lastUpdate = DateTime.Now;
                    var result = WebClient.GetHolidays(_apiKey, _country, localDate).Result;
                    Holidays = Lurgle.Dates.Holidays.ValidateHolidays(result, _holidayMatch, _localeMatch, _includeBank,
                        _includeWeekends);
                    _lastDay = localDate;
                    _errorCount = 0;

                    if (_diagnostics && !string.IsNullOrEmpty(_testDate))
                    {
                        LogEvent(LogEventLevel.Debug,
                            "Test date {testDate} used, raw holidays retrieved {testCount} ...", _testDate,
                            result.Count);
                        foreach (var holiday in result)
                            LogEvent(LogEventLevel.Debug,
                                "Holiday Name: {Name}, Local Name {LocalName}, Start {LocalStart}, Start UTC {Start}, End UTC {End}, Type {Type}, Location string {Location}, Locations parsed {Locations} ...",
                                holiday.Name, holiday.Name_Local, holiday.LocalStart, holiday.UtcStart, holiday.UtcEnd,
                                holiday.Type, holiday.Location, holiday.Locations.ToArray());
                    }

                    LogEvent(LogEventLevel.Debug, "Holidays retrieved and validated {HolidayCount} ...",
                        Holidays.Count);
                    foreach (var holiday in Holidays)
                        LogEvent(LogEventLevel.Debug,
                            "Holiday Name: {Name}, Local Name {LocalName}, Start {LocalStart}, Start UTC {Start}, End UTC {End}, Type {Type}, Location string {Location}, Locations parsed {Locations} ...",
                            holiday.Name, holiday.Name_Local, holiday.LocalStart, holiday.UtcStart, holiday.UtcEnd,
                            holiday.Type, holiday.Location, holiday.Locations.ToArray());

                    _isUpdating = false;
                    if (!IsShowtime) UtcRollover(utcDate, true);
                }
                catch (Exception ex)
                {
                    _errorCount++;
                    LogEvent(LogEventLevel.Debug, ex,
                        "Error {Error} retrieving holidays, public holidays cannot be evaluated (Try {Count} of {retryCount})...",
                        ex.Message, _errorCount, _retryCount);
                    _lastError = DateTime.Now;
                }
            }
            else if (!_useHolidays || _isUpdating && _errorCount >= 10)
            {
                _isUpdating = false;
                _lastDay = localDate;
                _errorCount = 0;
                Holidays = new List<AbstractApiHolidays>();
                if (_useHolidays && !IsShowtime) UtcRollover(utcDate, true);
            }
        }

        /// <summary>
        ///     Day rollover based on UTC date
        /// </summary>
        /// <param name="utcDate"></param>
        /// <param name="isUpdateHolidays"></param>
        public void UtcRollover(DateTime utcDate, bool isUpdateHolidays = false)
        {
            LogEvent(LogEventLevel.Debug, "UTC Time is currently {UtcTime} ...",
                UseTestOverrideTime
                    ? TestOverrideTime.ToUniversalTime().ToShortTimeString()
                    : DateTime.Now.ToUniversalTime().ToShortTimeString());

            //Day rollover, we need to ensure the next start and end is in the future
            if (!string.IsNullOrEmpty(_testDate))
                _startTime = DateTime.ParseExact(_testDate + " " + StartTime, "yyyy-M-d " + _startFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
            else if (UseTestOverrideTime)
                _startTime = DateTime
                    .ParseExact(TestOverrideTime.ToString("yyyy-M-d") + " " + StartTime, "yyyy-M-d " + _startFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None)
                    .ToUniversalTime();
            else
                _startTime = DateTime
                    .ParseExact(StartTime, _startFormat, CultureInfo.InvariantCulture, DateTimeStyles.None)
                    .ToUniversalTime();

            if (!string.IsNullOrEmpty(_testDate))
                _endTime = DateTime.ParseExact(_testDate + " " + EndTime, "yyyy-M-d " + _endFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
            else if (UseTestOverrideTime)
                _endTime = DateTime.ParseExact(TestOverrideTime.ToString("yyyy-M-d") + " " + EndTime,
                    "yyyy-M-d " + _endFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
            else
                _endTime = DateTime.ParseExact(EndTime, _endFormat, CultureInfo.InvariantCulture, DateTimeStyles.None)
                    .ToUniversalTime();

            //Detect a 24  hour instance and handle it
            if (_endTime == _startTime)
            {
                _endTime = _endTime.AddDays(1);
                _is24H = true;
            }

            //If there are holidays, account for them
            if (Holidays.Any(holiday => _startTime >= holiday.UtcStart && _startTime < holiday.UtcEnd))
            {
                _startTime = _startTime.AddDays(1);
                _endTime = _endTime.AddDays(_endTime.AddDays(1) < _startTime ? 2 : 1);
            }

            //If we updated holidays or this is a 24h instance, don't automatically put start time to the future
            if (!_is24H &&
                (!UseTestOverrideTime && _startTime < utcDate ||
                 UseTestOverrideTime && _startTime < TestOverrideTime.ToUniversalTime()) &&
                !isUpdateHolidays) _startTime = _startTime.AddDays(1);

            if (_endTime < _startTime)
                _endTime = _endTime.AddDays(_endTime.AddDays(1) < _startTime ? 2 : 1);

            LogEvent(LogEventLevel.Debug,
                isUpdateHolidays
                    ? "UTC Day Rollover (Holidays Updated), Parse {LocalStart} To Next UTC Start Time {StartTime} ({StartDayOfWeek}), Parse {LocalEnd} to UTC End Time {EndTime} ({EndDayOfWeek})..."
                    : "UTC Day Rollover, Parse {LocalStart} To Next UTC Start Time {StartTime} ({StartDayOfWeek}), Parse {LocalEnd} to UTC End Time {EndTime} ({EndDayOfWeek})...",
                StartTime, _startTime.ToShortTimeString(), _startTime.DayOfWeek, EndTime,
                _endTime.ToShortTimeString(), _endTime.DayOfWeek);
        }

        public Showtime GetShowtime()
        {
            return new Showtime(_startTime, _endTime);
        }

        /// <summary>
        ///     Output a scheduled log event that always defines the Message and Description tags for use with other apps
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        /// <param name="description"></param>
        /// <param name="token"></param>
        private void ScheduledLogEvent(LogEventLevel logLevel, string message, string description,
            KeyValuePair<string, string>? token = null)
        {
            string include = "{AppName} - ";
            if (!_includeApp) include = string.Empty;

            var responder = string.Empty;
            if (ResponderLookup.Count > 0)
            {
                if (token != null)
                    foreach (var responderPair in from responderPair in ResponderLookup
                        let tokenPair = (KeyValuePair<string, string>) token
                        where responderPair.Key.Equals(tokenPair.Key, StringComparison.OrdinalIgnoreCase)
                        select responderPair)
                    {
                        responder = responderPair.Value;
                        break;
                    }
            }
            else
            {
                responder = _responders;
            }


            if (_isTags)
                Log.ForContext(nameof(Tags), DateTokens.HandleTokens(_tags, token)).ForContext("AppName", App.Title)
                    .ForContext(nameof(Priority), _priority).ForContext(nameof(Responders), responder)
                    .ForContext(nameof(InitialTimeEstimate), _initialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), _remainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), _projectKey).ForContext(nameof(DueDate), _dueDate)
                    .ForContext("ErrorCount", _errorCount).ForContext("Message", message)
                    .ForContext("Description", description)
                    .Write((Serilog.Events.LogEventLevel) logLevel,
                        string.IsNullOrEmpty(description) || !_includeDescription
                            ? include + "{Message}"
                            : include + "{Message} : {Description}");
            else
                Log.ForContext("AppName", App.Title).ForContext(nameof(Priority), _priority)
                    .ForContext(nameof(Responders), responder).ForContext("ErrorCount", _errorCount)
                    .ForContext(nameof(InitialTimeEstimate), _initialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), _remainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), _projectKey).ForContext(nameof(DueDate), _dueDate)
                    .ForContext("Message", message).ForContext("Description", description)
                    .Write((Serilog.Events.LogEventLevel) logLevel,
                        string.IsNullOrEmpty(description) || !_includeDescription
                            ? include + "{Message}"
                            : include + "{Message} : {Description}");
        }

        /// <summary>
        ///     Output a log event to Seq stream
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private void LogEvent(LogEventLevel logLevel, string message, params object[] args)
        {
            string include = "{AppName} - ";
            if (!_includeApp) include = string.Empty;

            if (_isTags)
                Log.ForContext(nameof(Tags), _tags).ForContext("AppName", App.Title)
                    .ForContext(nameof(Priority), _priority).ForContext(nameof(Responders), _responders)
                    .ForContext(nameof(InitialTimeEstimate), _initialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), _remainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), _projectKey).ForContext(nameof(DueDate), _dueDate)
                    .Write((Serilog.Events.LogEventLevel) logLevel, $"{include}{message}", args);
            else
                Log.ForContext("AppName", App.Title).ForContext(nameof(Priority), _priority)
                    .ForContext(nameof(Responders), _responders).ForContext(nameof(InitialTimeEstimate), _initialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), _remainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), _projectKey).ForContext(nameof(DueDate), _dueDate)
                    .Write((Serilog.Events.LogEventLevel) logLevel, $"{include}{message}", args);
        }

        /// <summary>
        ///     Output an exception log event to Seq stream
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="exception"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private void LogEvent(LogEventLevel logLevel, Exception exception, string message, params object[] args)
        {
            string include = "{AppName} - ";
            if (!_includeApp) include = string.Empty;

            if (_isTags)
                Log.ForContext(nameof(Tags), _tags).ForContext("AppName", App.Title)
                    .ForContext(nameof(Priority), _priority).ForContext(nameof(Responders), _responders)
                    .ForContext(nameof(InitialTimeEstimate), _initialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), _remainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), _projectKey).ForContext(nameof(DueDate), _dueDate)
                    .Write((Serilog.Events.LogEventLevel) logLevel, exception, $"{include}{message}", args);
            else
                Log.ForContext("AppName", App.Title).ForContext(nameof(Priority), _priority)
                    .ForContext(nameof(Responders), _responders).ForContext(nameof(InitialTimeEstimate), _initialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), _remainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), _projectKey).ForContext(nameof(DueDate), _dueDate)
                    .Write((Serilog.Events.LogEventLevel) logLevel, exception, $"{include}{message}", args);
        }
    }
}