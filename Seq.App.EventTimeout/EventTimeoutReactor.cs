using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.EventTimeout
{
    [SeqApp("Event Timeout", Description = "Log events if a matching event is not seen by a configured interval in a given timeframe.")]
    public class EventTimeoutReactor : SeqApp, ISubscribeTo<LogEventData>
    {
        // Count of matches
        int _matched;
        bool _cannotMatchAlerted;
        bool _skippedShowtime;

        string startFormat = "H:mm:ss";
        string endFormat = "H:mm:ss";

        DateTime _startTime;
        DateTime _endTime;
        DateTime _lastLog;
        DateTime _lastCheck;
        DateTime _lastDay;

        bool _isUpdating;
        DateTime _lastUpdate;
        DateTime _lastError;
        int _errorCount;

        List<DayOfWeek> _daysOfWeek;
        List<int> _includeDays;
        List<int> _excludeDays;
        string _testDate;

        TimeSpan _timeOut;
        TimeSpan _suppressionTime;

        Dictionary<string, string> _properties;
        
        string _alertMessage;
        string _alertDescription;
        string _timeoutLogLevel;
        bool _isAlert;
        System.Timers.Timer _timer;
        const int _retryCount = 10;
        bool _isShowtime;
        bool _includeApp;
        bool _diagnostics;

        bool _isTags;
        string[] _tags;

        bool _useHolidays;
        string _country;
        string _apiKey;
        List<string> _holidayMatch;
        List<string> _localeMatch;
        bool _includeWeekends;
        bool _includeBank;
        bool _useProxy;
        string _proxy;
        bool _bypassLocal;
        string[] _localAddresses;
        string _proxyUser;
        string _proxyPass;
        List<AbstractApiHolidays> _holidays;

        [SeqAppSetting(
            DisplayName = "Diagnostic logging",
            HelpText = "Send extra diagnostic logging to the stream")]
        public bool Diagnostics { get; set; }

        [SeqAppSetting(
            DisplayName = "Start Time",
            HelpText = "The time (H:mm:ss, 24 hour format) to start monitoring")]
        public string StartTime { get; set; }

        [SeqAppSetting(
            DisplayName = "End Time",
            HelpText = "The time (H:mm:ss, 24 hour format) to stop monitoring, up to 24 hours after start time")]
        public string EndTime { get; set; }

        [SeqAppSetting(
            DisplayName = "Timeout Interval (seconds)",
            HelpText = "Time period in which a matching log entry must be seen. After this, an alert will be raised",
            InputType = SettingInputType.Integer)]
        public int Timeout { get; set; }

        [SeqAppSetting(
            DisplayName = "Days of Week",
            HelpText = "Comma-delimited - Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday",
            IsOptional = true)]
        public string DaysOfWeek { get; set; }

        [SeqAppSetting(
            DisplayName = "Include Days of Month",
            HelpText = "Only run on these days. Comma-delimited - first,last,1-31",
            IsOptional = true)]
        public string IncludeDaysOfMonth { get; set; }

        [SeqAppSetting(
            DisplayName = "Exclude Days of Month",
            HelpText = "Never run on these days. Comma-delimited - first,last,1-31",
            IsOptional = true)]
        public string ExcludeDaysOfMonth { get; set; }

        [SeqAppSetting(
            DisplayName = "Suppression Interval (seconds)",
            HelpText = "If an alert has been raised, further alerts will be suppressed for this time.",
            InputType = SettingInputType.Integer)]
        public int SuppressionTime { get; set; }

        [SeqAppSetting(DisplayName = "Log level for timeouts",
          HelpText = "Verbose, Debug, Information, Warning, Error, Fatal",
          IsOptional = true)]
        public string TimeoutLogLevel { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 1 Name",
            HelpText = "Case insensitive property name (must be a full match). If not configured, the @Message property will be used. If this is not seen in the configured timeout, an alert will be raised.",
            IsOptional = true)]
        public string Property1Name { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 1 Match",
            HelpText = "Case insensitive text to match - partial match okay. If this is not seen in the configured timeout, an alert will be raised.")]
        public string TextMatch { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 2 Name",
            HelpText = "Case insensitive property name (must be a full match). If not configured, this will not be evaluated.",
            IsOptional = true)]
        public string Property2Name { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 2 Match",
            HelpText = "Case insensitive text to match - partial match okay. If not configured, this will not be evaluated.",
            IsOptional = true)]
        public string Property2Match { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 3 Name",
            HelpText = "Case insensitive property name (must be a full match). If not configured, this will not be evaluated.",
            IsOptional = true)]
        public string Property3Name { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 3 Match",
            HelpText = "Case insensitive text to match - partial match okay. If not configured, this will not be evaluated.",
            IsOptional = true)]
        public string Property3Match { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 4 Name",
            HelpText = "Case insensitive property name (must be a full match). If not configured, this will not be evaluated.",
            IsOptional = true)]
        public string Property4Name { get; set; }

        [SeqAppSetting(
            DisplayName = "Property 4 Match",
            HelpText = "Case insensitive text to match - partial match okay. If not configured, this will not be evaluated.",
            IsOptional = true)]
        public string Property4Match { get; set; }

        [SeqAppSetting(
            DisplayName = "Alert message",
            HelpText = "Event message to raise.")]
        public string AlertMessage { get; set; }

        [SeqAppSetting(
            IsOptional = true,
            DisplayName = "Alert description",
            HelpText = "Optional description associated with the event raised.")]
        public string AlertDescription { get; set; }

        [SeqAppSetting(
            IsOptional = true,
            DisplayName = "Alert tags",
            HelpText = "Tags for the event, separated by commas.")]
        public string Tags { get; set; }

        [SeqAppSetting(
            DisplayName = "Include instance name in alert message",
            HelpText = "Prepend the instance name to the alert message")]
        public bool IncludeApp { get; set; }


        [SeqAppSetting(
            DisplayName = "Use Holidays API for public holiday detection",
            HelpText = "Connect to the AbstractApi Holidays service to detect public holidays")]
        public bool UseHolidays { get; set; }

        [SeqAppSetting(
            DisplayName = "Country code",
            HelpText = "Two letter country code (eg. AU)",
            IsOptional = true)]
        public string Country { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays API key",
            HelpText = "Sign up for an API key at https://www.abstractapi.com/holidays-api and enter it here",
            IsOptional = true,
            InputType = SettingInputType.Password)]
        public string ApiKey { get; set; }

        [SeqAppSetting(
            DisplayName = "Match these holidays",
            HelpText = "Comma-delimited list of holiday types (eg. National, Local) - case insensitive, partial match okay",
            IsOptional = true)]
        public string HolidayMatch { get; set; }

        [SeqAppSetting(
            DisplayName = "Local Holidays",
            HelpText = "Holidays are valid if the location matches one of these comma separated values - case insensitive, must be a full match",
            IsOptional = true)]
        public string LocaleMatch { get; set; }

        [SeqAppSetting(
            DisplayName = "Include Weekends",
            HelpText = "Include public holidays that are returned for weekends")]
        public bool IncludeWeekends { get; set; }

        [SeqAppSetting(
            DisplayName = "Include Bank Holidays",
            HelpText = "Include bank holidays")]
        public bool IncludeBank { get; set; }

        [SeqAppSetting(
            DisplayName = "Test Date",
            HelpText = "yyyy-M-d format. Used only for diagnostics - should normally be empty",
            IsOptional = true)]
        public string TestDate { get; set; }

        [SeqAppSetting(
            DisplayName = "Proxy address",
            HelpText = "Proxy address for Holidays API",
            IsOptional = true)]
        public string Proxy { get; set; }

        [SeqAppSetting(
            DisplayName = "Proxy bypass local addresses",
            HelpText = "Bypass local addresses for proxy")]
        public bool BypassLocal { get; set; }

        [SeqAppSetting(
            DisplayName = "Local addresses for proxy bypass",
            HelpText = "Local addresses to bypass, comma separated",
            IsOptional = true)]
        public string LocalAddresses { get; set; }

        [SeqAppSetting(
            DisplayName = "Proxy username",
            HelpText = "Username for proxy authentication",
            IsOptional = true)]
        public string ProxyUser { get; set; }

        [SeqAppSetting(
            DisplayName = "Proxy password",
            HelpText = "Username for proxy authentication",
            IsOptional = true,
            InputType = SettingInputType.Password)]
        public string ProxyPass { get; set; }

        protected override void OnAttached()
        {
            LogMessage(false, "debug", "Check {AppName} diagnostic level ({Diagnostics}) ...", App.Title, Diagnostics);
            _diagnostics = Diagnostics;

            if (_diagnostics)
                LogMessage(false, "debug", "Check include {AppName} ({IncludeApp}) ...", App.Title, IncludeApp);
            _includeApp = IncludeApp;
            if (!_includeApp && _diagnostics)
                LogMessage(false, "debug", "App name {AppName} will not be included in alert message ...", App.Title);
            else if (_diagnostics)
                LogMessage(false, "debug", "App name {AppName} will be included in alert message ...", App.Title);

            DateTime testStartTime;
            if (!DateTime.TryParseExact(StartTime, "H:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out testStartTime))
                if (DateTime.TryParseExact(StartTime, "H:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out testStartTime))
                    startFormat = "H:mm";
                else
                    LogMessage(false, "debug", "Start Time {StartTime} does  not parse to a valid DateTime - app will exit ...", StartTime);

            DateTime testEndTime;
            if (!DateTime.TryParseExact(EndTime, "H:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out testEndTime))
                if (DateTime.TryParseExact(EndTime, "H:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out testEndTime))
                    endFormat = "H:mm";
                else
                    LogMessage(false, "debug", "End Time {EndTime} does  not parse to a valid DateTime - app will exit ...", EndTime);

            LogMessage(false, "debug", "Use Holidays API {UseHolidays}, Country {Country}, Has API key {IsEmpty} ...", UseHolidays, Country, !string.IsNullOrEmpty(ApiKey));
            if (UseHolidays && !string.IsNullOrEmpty(Country) && !string.IsNullOrEmpty(ApiKey))
            {
                if (_diagnostics)
                    LogMessage(false, "Debug", "Validate Country {Country}", Country);
                if (validateCountry(Country))
                {
                    _useHolidays = true;
                    _country = Country;
                    _apiKey = ApiKey;
                    _includeWeekends = IncludeWeekends;
                    _includeBank = IncludeBank;

                    if (string.IsNullOrEmpty(HolidayMatch))
                        _holidayMatch = new List<string>();
                    else
                        _holidayMatch = HolidayMatch.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
                    if (string.IsNullOrEmpty(LocaleMatch))
                        _localeMatch = new List<string>();
                    else
                        _localeMatch = LocaleMatch.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();

                    if (!string.IsNullOrEmpty(Proxy))
                    {
                        _useProxy = true;
                        _proxy = Proxy;
                        _bypassLocal = BypassLocal;
                        _localAddresses = LocalAddresses.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray();
                        _proxyUser = ProxyUser;
                        _proxyPass = ProxyPass;
                    }

                    if (_diagnostics)
                        LogMessage(false, "debug", "Holidays API Enabled: {UseHolidays}, Country {Country}, Use Proxy {UseProxy}, Proxy Address {Proxy}, BypassLocal {BypassLocal}, Authentication {Authentication} ...", _useHolidays, _country,
                            _useProxy, _proxy, _bypassLocal, !string.IsNullOrEmpty(ProxyUser) && !string.IsNullOrEmpty(ProxyPass));
                    WebClient.setFlurlConfig(App.Title, _useProxy, _proxy, _proxyUser, _proxyPass, _bypassLocal, _localAddresses);
                }
                else
                {
                    _useHolidays = false;
                    LogMessage(false, "debug", "Holidays API Enabled: {UseHolidays}, Could not parse country {CountryCode} to valid region ...", _useHolidays, _country);
                }
            } else if (UseHolidays)
            {
                _useHolidays = false;
                LogMessage(false, "debug", "Holidays API Enabled: {UseHolidays}, One or more parameters not set", _useHolidays);
            }


            _lastDay = DateTime.Today.AddDays(-1);
            _lastError = DateTime.Now.AddDays(-1);
            _lastUpdate = DateTime.Now.AddDays(-1);
            _errorCount = 0;
            _testDate = TestDate;
            _holidays = new List<AbstractApiHolidays>();

            retrieveHolidays(DateTime.Today, DateTime.UtcNow);

            if (!_useHolidays || _isUpdating)
                utcRollover(DateTime.UtcNow);

            if (_diagnostics)
                LogMessage(false, "debug", "Convert Timeout {timeout} to TimeSpan ...", Timeout);
            _timeOut = TimeSpan.FromSeconds(Timeout);
            if (_diagnostics)
                LogMessage(false, "debug", "Parsed Timeout is {timeout} ...", _timeOut.TotalSeconds);

            if (_diagnostics)
                LogMessage(false, "debug", "Convert Suppression {suppression} to TimeSpan ...", SuppressionTime);
            _suppressionTime = TimeSpan.FromSeconds(SuppressionTime);
            if (_diagnostics)
                LogMessage(false, "debug", "Parsed Suppression is {timeout} ...", _suppressionTime.TotalSeconds);

            if (_diagnostics)
                LogMessage(false, "debug", "Convert Days of Week {daysofweek} to UTC Days of Week ...", DaysOfWeek);
            if (string.IsNullOrEmpty(DaysOfWeek))
                _daysOfWeek = new List<DayOfWeek>() { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
            else
            {
                string[] days = DaysOfWeek.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray();
                if (days.Length > 0)
                {
                    _daysOfWeek = new List<DayOfWeek>();
                    bool crossesUtcDay = false;

                    if ((int)_startTime.DayOfWeek < (int)_endTime.DayOfWeek || ((int)_startTime.DayOfWeek == 6 && (int)_endTime.DayOfWeek == 0))
                        crossesUtcDay = true;

                    foreach (string day in days)
                    {
                        DayOfWeek dow;

                        if (!crossesUtcDay)
                            dow = (DayOfWeek)((int)(DayOfWeek)Enum.Parse(typeof(DayOfWeek), day));
                        else if ((int)(DayOfWeek)Enum.Parse(typeof(DayOfWeek), day) - 1 < 0)
                            dow = DayOfWeek.Saturday;
                        else
                            dow = (DayOfWeek)((int)(DayOfWeek)Enum.Parse(typeof(DayOfWeek), day) - 1);

                        _daysOfWeek.Add(dow);
                    }
                }
                else
                    _daysOfWeek = new List<DayOfWeek>() { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
            }
            if (_diagnostics)
                LogMessage(false, "debug", "UTC Days of Week {daysofweek} will be used ...", _daysOfWeek.ToArray());

            if (_diagnostics)
                LogMessage(false, "debug", "Validate Include Days of Month {includedays} ...", IncludeDaysOfMonth);
            _includeDays = getDaysOfMonth(IncludeDaysOfMonth);
            if (_diagnostics)
                if (_includeDays.Count > 0)
                    LogMessage(false, "debug", "Include UTC Days of Month: {includedays} ...", _includeDays.ToArray());
                else
                    LogMessage(false, "debug", "Include UTC Days of Month: ALL ...");

            if (_diagnostics)
                LogMessage(false, "debug", "Validate Exclude Days of Month {excludedays} ...", ExcludeDaysOfMonth);
            _excludeDays = getDaysOfMonth(ExcludeDaysOfMonth);
            if (_diagnostics)
                if (_includeDays.Count > 0)
                    LogMessage(false, "debug", "Exclude UTC Days of Month: {includedays} ...", _excludeDays.ToArray());
                else
                    LogMessage(false, "debug", "Exclude UTC Days of Month: NONE ...");

            //Evaluate the properties we will match
            _properties = new Dictionary<string, string>();

            KeyValuePair<string, string> property = getProperty(1, Property1Name, TextMatch);
            _properties.Add(property.Key, property.Value);
            property = getProperty(2, Property2Name, Property2Match);
            if (!string.IsNullOrEmpty(property.Key))
                _properties.Add(property.Key, property.Value);
            property = getProperty(3, Property3Name, Property3Match);
            if (!string.IsNullOrEmpty(property.Key))
                _properties.Add(property.Key, property.Value);
            property = getProperty(4, Property4Name, Property4Match);
            if (!string.IsNullOrEmpty(property.Key))
                _properties.Add(property.Key, property.Value);

            if (_diagnostics)
                LogMessage(false, "debug", "Match criteria will be: {MatchText}", matchConditions());

            if (_diagnostics)
                LogMessage(false, "debug", "Validate Alert Message '{AlertMessage}' ...", AlertMessage);
            _alertMessage = string.IsNullOrWhiteSpace(AlertMessage) ? "An event timeout has occurred!" : AlertMessage.Trim();
            if (_diagnostics)
                LogMessage(false, "debug", "Alert Message '{AlertMessage}' will be used ...", _alertMessage);

            if (_diagnostics)
                LogMessage("debug", "Validate Alert Description '{AlertDescription}' ...", AlertDescription);
            _alertDescription = string.IsNullOrWhiteSpace(AlertDescription) ? _alertMessage + " : Generated by Seq " + Host.BaseUri : AlertDescription.Trim();
            if (_diagnostics)
                LogMessage("debug", "Alert Description '{AlertDescription}' will be used ...", _alertDescription);

            if (_diagnostics)
                LogMessage("debug", "Convert Tags '{Tags}' to array ...", Tags);
            _tags = (Tags ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToArray();
            if (_tags.Length > 0)
                _isTags = true;

            if (string.IsNullOrWhiteSpace(TimeoutLogLevel))
            {
                TimeoutLogLevel = "Error";
            }
            _timeoutLogLevel = TimeoutLogLevel.Trim().ToLowerInvariant();
            if (_diagnostics)
                LogMessage(false, "debug", "Log level {loglevel} will be used for timeouts on {Instance} ...", _timeoutLogLevel, App.Title);

            if (_diagnostics)
                LogMessage(false, "debug", "Starting timer ...");
            _timer = new System.Timers.Timer(1000);
            _timer.AutoReset = true;
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
            if (_diagnostics)
                LogMessage(false, "debug", "Timer started ...");

        }

        private List<int> getDaysOfMonth(string days)
        {
            List<int> dayResult = new List<int>();
            if (!string.IsNullOrEmpty(days))
            {
                List<string> dayList = days.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();

                DateTime localStart = DateTime.ParseExact(StartTime, startFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);

                foreach (string day in dayList)
                    switch (day.ToLower())
                    {
                        case "first":
                            DateTime firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, localStart.Hour, localStart.Minute, localStart.Second).ToUniversalTime();
                            dayResult.Add(firstDay.Day);
                            break;
                        case "last":
                            DateTime lastDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month), localStart.Hour, localStart.Minute, localStart.Second).ToUniversalTime();
                            dayResult.Add(lastDay.Day);
                            break;
                        default:
                            int result;
                            if (int.TryParse(day, out result))
                            {
                                DateTime resultDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, result, localStart.Hour, localStart.Minute, localStart.Second).ToUniversalTime();
                                dayResult.Add(result);
                            }
                            break;
                    }
            }

            return dayResult;
        }

        private KeyValuePair<string, string> getProperty(int property, string propertyName, string propertyMatch)
        {
            string key = string.Empty;
            string value = string.Empty;

            if (_diagnostics)
                LogMessage(false, "debug", "Validate Property {PropertyNo}: '{PropertyNameValue}' ...", property, propertyName);
            if (string.IsNullOrEmpty(propertyName) && property == 1)
                key = "@Message";
            else if (!string.IsNullOrEmpty(propertyName))
                key = propertyName.Trim();

            if (!string.IsNullOrEmpty(key))
            {
                if (_diagnostics)
                    LogMessage(false, "debug", "Validate Property {PropertyNo} Match Value '{PropertyMatch}' ...", property, propertyMatch);
                value = string.IsNullOrWhiteSpace(propertyMatch) ? "Match text" : propertyMatch.Trim();
                if (_diagnostics)
                    LogMessage(false, "debug", "Property {PropertyNo} '{PropertyName}' will be used to match '{PropertyMatch}'...", property, key, value);
            }
            else if (_diagnostics)
                LogMessage(false, "debug", "Property {PropertyNo} will not be used to match values ...", property);

            return new KeyValuePair<string, string>(key, value);
        }

        private void retrieveHolidays(DateTime localDate, DateTime utcDate)
        {
            if (_useHolidays && (!_isUpdating || (_isUpdating && (DateTime.Now - _lastUpdate).TotalSeconds > 10 && (DateTime.Now - _lastError).TotalSeconds > 10 && _errorCount < _retryCount)))
            {
                _isUpdating = true;
                if (!string.IsNullOrEmpty(_testDate))
                    localDate = DateTime.ParseExact(_testDate, "yyyy-M-d", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);

                if (_diagnostics)
                    LogMessage(false, "debug", "Retrieve holidays for {Date}, Last Update {lastUpdateDate} {lastUpdateTime} ...", localDate.ToShortDateString(), _lastUpdate.ToShortDateString(), _lastUpdate.ToShortTimeString());
                string holidayUrl = WebClient.getUrl(_apiKey, _country, localDate);
                if (_diagnostics)
                    LogMessage(false, "debug", "URL used is {url} ...", holidayUrl);
                try
                {
                    _lastUpdate = DateTime.Now;
                    List<AbstractApiHolidays> result = WebClient.getHolidays(_apiKey, _country, localDate).Result;
                    _holidays = validateHolidays(result);
                    _lastDay = localDate;

                    if (_diagnostics)
                    {
                        if (!string.IsNullOrEmpty(_testDate))
                        {
                            LogMessage(false, "debug", "Test date {testDate} used, raw holidays retrieved {testCount} ...", _testDate, result.Count);
                            foreach (AbstractApiHolidays holiday in result)
                                LogMessage(false, "debug", "Holiday Name: {Name}, Local Name {LocalName}, Start {LocalStart}, Start UTC {Start}, End UTC {End}, Type {Type}, Location string {Location}, Locations parsed {Locations} ...",
                                    holiday.name, holiday.name_local, holiday.localStart, holiday.utcStart, holiday.utcEnd, holiday.type, holiday.location, holiday.locations.ToArray());
                        }
                    }

                    LogMessage(false, "debug", "Holidays retrieved and validated {holidayCount} ...", _holidays.Count);
                    foreach (AbstractApiHolidays holiday in _holidays)
                        LogMessage(false, "debug", "Holiday Name: {Name}, Local Name {LocalName}, Start {LocalStart}, Start UTC {Start}, End UTC {End}, Type {Type}, Location string {Location}, Locations parsed {Locations} ...",
                            holiday.name, holiday.name_local, holiday.localStart, holiday.utcStart, holiday.utcEnd, holiday.type, holiday.location, holiday.locations.ToArray());

                    _isUpdating = false;
                    if (!_isShowtime)
                        utcRollover(utcDate, true);
                }
                catch (Exception ex)
                {
                    _errorCount++;
                    LogMessage(ex, false, "debug", "Error {Error} retrieving holidays, public holidays cannot be evaluated (Try {Count} of {retryCount})...", ex.Message, _errorCount, _retryCount);
                    _lastError = DateTime.Now;
                }

            }
            else if (!_useHolidays || (_isUpdating && _errorCount >= 10))
            {
                _isUpdating = false;
                _lastDay = localDate;
                _errorCount = 0;
                _holidays = new List<AbstractApiHolidays>();
                if (_useHolidays && !_isShowtime)
                    utcRollover(utcDate, true);
            }
        }

        private List<AbstractApiHolidays> validateHolidays(List<AbstractApiHolidays> holidayList)
        {
            List<AbstractApiHolidays> result = new List<AbstractApiHolidays>();
            foreach (AbstractApiHolidays holiday in holidayList)
            {
                bool hasType = false;
                bool hasRegion = false;
                bool isBank = false;
                bool isWeekend = false;

                if (_holidayMatch.Count > 0)
                    foreach (string match in _holidayMatch)
                        if (holiday.type.IndexOf(match, StringComparison.CurrentCultureIgnoreCase) >= 0)
                            hasType = true;

                if (_localeMatch.Count > 0)
                    foreach (string match in _localeMatch)
                        if (holiday.locations.FindIndex(loc => loc.Equals(match, StringComparison.CurrentCultureIgnoreCase)) >= 0)
                            hasRegion = true;

                if (!_includeBank && holiday.name.IndexOf("Bank Holiday", StringComparison.CurrentCultureIgnoreCase) >= 0)
                    isBank = true;

                if (!_includeWeekends && (holiday.localStart.DayOfWeek == DayOfWeek.Sunday || holiday.localStart.DayOfWeek == DayOfWeek.Saturday))
                    isWeekend = true;

                if (_holidayMatch.Count > 0 && _localeMatch.Count > 0 && hasType && hasRegion && !isBank && !isWeekend)
                    result.Add(holiday);
                else if (_holidayMatch.Count > 0 && _localeMatch.Count == 0 && hasType && !isBank && !isWeekend)
                    result.Add(holiday);
                else if (_holidayMatch.Count == 0 && _localeMatch.Count > 0 && hasRegion && !isBank && !isWeekend)
                    result.Add(holiday);
                else if (_holidayMatch.Count == 0 && _localeMatch.Count == 0 && !isBank && !isWeekend)
                    result.Add(holiday);
            }

            return result;
        }

        private bool validateCountry(string countryCode)
        {
            return CultureInfo
                .GetCultures(CultureTypes.SpecificCultures)
                    .Select(culture => new RegionInfo(culture.LCID))
                        .Any(region => region.TwoLetterISORegionName.ToLower() == countryCode.ToLower());
        }

        private void utcRollover(DateTime utcDate, bool isUpdateHolidays = false)
        {
            //Day rollover, we need to ensure the next start and end is in the future
            if (!string.IsNullOrEmpty(_testDate))
                _startTime = DateTime.ParseExact(_testDate + " " + StartTime, "yyyy-M-d " + startFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None).ToUniversalTime();
            else
                _startTime = DateTime.ParseExact(StartTime, startFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None).ToUniversalTime();

            //If we updated holidays, don't automatically put start time to the future
            if (_startTime < utcDate && !isUpdateHolidays)
                _startTime = _startTime.AddDays(1);

            //If there are holidays, account for them
            foreach (AbstractApiHolidays holiday in _holidays)
                if (_startTime >= holiday.utcStart && _startTime < holiday.utcEnd)
                {
                    _startTime = _startTime.AddDays(1);
                    break;
                }

            if (!string.IsNullOrEmpty(_testDate))
                _endTime = DateTime.ParseExact(_testDate + " " + EndTime, "yyyy-M-d " + endFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None).ToUniversalTime();
            else
                _endTime = DateTime.ParseExact(EndTime, endFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None).ToUniversalTime();

            if (_endTime <= _startTime)
                if (_endTime.AddDays(1) < _startTime)
                    _endTime = _endTime.AddDays(2);
                else
                    _endTime = _endTime.AddDays(1);

                if (isUpdateHolidays)
                    LogMessage("debug", "UTC Day Rollover (Holidays Updated), Parse {LocalStart} To Next UTC Start Time {StartTime} ({StartDayOfWeek}), Parse {LocalEnd} to UTC End Time {EndTime} ({EndDayOfWeek})...",
                    StartTime, _startTime.ToShortTimeString(), _startTime.DayOfWeek, EndTime, _endTime.ToShortTimeString(), _endTime.DayOfWeek);
                else
                    LogMessage("debug", "UTC Day Rollover, Parse {LocalStart} To Next UTC Start Time {StartTime} ({StartDayOfWeek}), Parse {LocalEnd} to UTC End Time {EndTime} ({EndDayOfWeek})...",
                            StartTime, _startTime.ToShortTimeString(), _startTime.DayOfWeek, EndTime, _endTime.ToShortTimeString(), _endTime.DayOfWeek);
        }

        public string matchConditions()
        {
            int count = 0;
            string condition = string.Empty;
            foreach (KeyValuePair<string,string> property in _properties)
            {
                count++;
                if (count == 1)
                    condition = property.Key + " contains '" + property.Value + "'";
                else if (!string.IsNullOrEmpty(property.Key))
                    condition = condition + " AND " + property.Key + " contains '" + property.Value + "'";
            }

            return condition;
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            DateTime timeNow = DateTime.UtcNow;
            DateTime localDate = DateTime.Today;
            if (!string.IsNullOrEmpty(_testDate))
                localDate = DateTime.ParseExact(_testDate, "yyyy-M-d", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);

            if (_lastDay < localDate)
                retrieveHolidays(localDate, timeNow);

            //We can only enter showtime if we're not currently retrying holidays, but existing showtimes will continue to monitor
            if ((!_useHolidays || (_isShowtime || (!_isShowtime && !_isUpdating))) && timeNow >= _startTime && timeNow < _endTime)
            {
                if (!_daysOfWeek.Contains(_startTime.DayOfWeek) || (_includeDays.Count > 0 && !_includeDays.Contains(_startTime.Day)) || _excludeDays.Contains(_startTime.Day))
                {
                    //Log that we have skipped a day due to an exclusion
                    if (!_skippedShowtime)
                        LogMessage("debug", "Matching will not be performed due to exclusions - Day of Week Excluded {DayOfWeek}, Day Of Month Included {IncludeDay}, Day of Month Excluded {ExcludeDay} ...", 
                            !_daysOfWeek.Contains(_startTime.DayOfWeek), _includeDays.Contains(_startTime.Day), _excludeDays.Contains(_startTime.Day));
                    _skippedShowtime = true;
                }
                else
                {
                    //Showtime! - Evaluate whether we have matched properties with log events
                    if (!_isShowtime)
                    {
                        LogMessage("debug", "UTC Start Time {Time} ({DayOfWeek}), monitoring for {MatchText} within {Timeout} seconds, until UTC End time {EndTime} ({EndDayOfWeek}) ...",
                            _startTime.ToShortTimeString(), _startTime.DayOfWeek, matchConditions(), _timeOut.TotalSeconds, _endTime.ToShortTimeString(), _endTime.DayOfWeek);
                        _isShowtime = true;
                        _lastCheck = timeNow;
                    }

                    TimeSpan difference = timeNow - _lastCheck;
                    if (difference.TotalSeconds > _timeOut.TotalSeconds && _matched == 0)
                    {
                        TimeSpan suppressDiff = timeNow - _lastLog;
                        if (_isAlert && suppressDiff.TotalSeconds < _suppressionTime.TotalSeconds)
                            return;

                        //Log event                    
                        LogMessage(_timeoutLogLevel, "{Message} : {Description}", _alertMessage, _alertDescription);
                        _lastLog = timeNow;
                        _isAlert = true;
                    }
                }
            }
            else if (timeNow < _startTime || timeNow >= _endTime)
            {
                //Showtime can end even if we're retrieving holidays
                if (_isShowtime)
                    LogMessage("debug", "UTC End Time {Time} ({DayOfWeek}), no longer monitoring for {MatchText} ...", _endTime.ToShortTimeString(), _endTime.DayOfWeek, matchConditions());

                //Reset the match counters
                _lastLog = timeNow;
                _lastCheck = timeNow;
                _matched = 0;
                _isAlert = false;
                _isShowtime = false;
                _cannotMatchAlerted = false;
                _skippedShowtime = false;
            }

            //We can only do UTC rollover if we're not currently retrying holidays and it's not during showtime
            if (!_isShowtime && (!_useHolidays || !_isUpdating) && (_startTime <= timeNow && string.IsNullOrEmpty(_testDate)))
            {
                utcRollover(timeNow);
                //Take the opportunity to refresh include/exclude days to allow for month rollover
                _includeDays = getDaysOfMonth(IncludeDaysOfMonth);
                _excludeDays = getDaysOfMonth(ExcludeDaysOfMonth);
            }
        }

        public void On(Event<LogEventData> evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            DateTime timeNow = DateTime.UtcNow;
            DateTime localDate = DateTime.Today;
            bool cannotMatch = false;
            List<string> cannotMatchProperties = new List<string>();
            int properties = 0;
            int matches = 0;

            if (_matched == 0 && _isShowtime)
            {
                foreach (KeyValuePair<string, string> property in _properties)
                {
                    properties++;
                    if (property.Key.Equals("@Message", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (evt.Data.RenderedMessage.IndexOf(property.Value, StringComparison.CurrentCultureIgnoreCase) >= 0)
                            matches++;

                    }
                    else if (evt.Data.Properties.ContainsKey(property.Key))
                    {

                        if (evt.Data.Properties[property.Key].ToString().IndexOf(property.Value, StringComparison.CurrentCultureIgnoreCase) >= 0)
                            matches++;

                    }
                    else
                    {
                        cannotMatch = true;
                        cannotMatchProperties.Add(property.Key);
                    }
                }

                if (cannotMatch && !_cannotMatchAlerted)
                {
                    LogMessage("debug", "Warning - An event was seen without the properties {PropertyName}, which may impact the ability to alert on failures - further failures will not be logged ...", string.Join(",", cannotMatchProperties.ToArray()));
                    _cannotMatchAlerted = true;
                }

                if (!cannotMatch && properties == matches)
                {
                    _matched++;
                    _lastCheck = timeNow;
                    LogMessage("debug", "Successfully matched {TextMatch}! Further matches will not be logged ...", matchConditions());
                }
            }
        }

        private void LogMessage(string level, string message, params object[] args)
        {
            LogMessage(true, level, message, args);
        }

        private void LogMessage(Exception exception, string level, string message, params object[] args)
        {
            LogMessage(exception, true, level, message, args);
        }

        private void LogMessage(bool logTags, string level, string message, params object[] args)
        {
            List<object> logArgsList = args.ToList();

            if (_includeApp)
            {
                message = "[{AppName}] -" + message;
                logArgsList.Insert(0, App.Title);
            }

            if (_isTags && logTags)
            {
                message = message + " - [Tags: {Tags}]";
                logArgsList.Add(_tags);
            }

            object[] logArgs = logArgsList.ToArray();

            var firstChar = level[0];
            switch (firstChar)
            {
                case 'v':
                    Log.Verbose(message, logArgs);
                    break;
                case 'd':
                    Log.Debug(message, logArgs);
                    break;
                case 'i':
                    Log.Information(message, logArgs);
                    break;
                case 'w':
                    Log.Warning(message, logArgs);
                    break;
                case 'e':
                    Log.Error(message, logArgs);
                    break;
                case 'f':
                    Log.Fatal(message, logArgs);
                    break;
            }
        }

        private void LogMessage(Exception exception, bool logTags, string level, string message, params object[] args)
        {
            List<object> logArgsList = args.ToList();

            if (_includeApp)
            {
                message = "[{AppName}] -" + message;
                logArgsList.Insert(0, App.Title);
            }

            if (_isTags && logTags)
            {
                message = message + " - [Tags: {Tags}]";
                logArgsList.Add(_tags);
            }

            object[] logArgs = logArgsList.ToArray();

            var firstChar = level[0];
            switch (firstChar)
            {
                case 'v':
                    Log.Verbose(exception, message, logArgs);
                    break;
                case 'd':
                    Log.Debug(exception, message, logArgs);
                    break;
                case 'i':
                    Log.Information(exception, message, logArgs);
                    break;
                case 'w':
                    Log.Warning(exception, message, logArgs);
                    break;
                case 'e':
                    Log.Error(exception, message, logArgs);
                    break;
                case 'f':
                    Log.Fatal(exception, message, logArgs);
                    break;
            }
        }
    }
}
