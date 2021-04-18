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
        string _testDate;

        TimeSpan _timeOut;
        TimeSpan _suppressionTime;
        string _textMatch;
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
            DisplayName = "Suppression Interval (seconds)",
            HelpText = "If an alert has been raised, further alerts will be suppressed for this time.",
            InputType = SettingInputType.Integer)]
        public int SuppressionTime { get; set; }

        [SeqAppSetting(DisplayName = "Log level for timeouts",
          HelpText = "Verbose, Debug, Information, Warning, Error, Fatal",
          IsOptional = true)]
        public string TimeoutLogLevel { get; set; }

        [SeqAppSetting(
            DisplayName = "Text match",
            HelpText = "Case insensitive text to match. If this is not seen in the configured timeout, an alert will be raised.")]
        public string TextMatch { get; set; }

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
                LogMessage(false, "debug", "Validate Text Match '{TextMatch}' ...", TextMatch);
            _textMatch = string.IsNullOrWhiteSpace(TextMatch) ? "Match text" : TextMatch.Trim();
            if (_diagnostics)
                LogMessage(false, "debug", "Text Match '{TextMatch}' will be used ...", _textMatch);

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

        private void retrieveHolidays(DateTime localDate, DateTime utcDate)
        {
            if (_useHolidays && (!_isUpdating || (_isUpdating && (DateTime.Now - _lastError).TotalSeconds > 10 && _errorCount < _retryCount)))
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
                if (!_isShowtime)
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
                _startTime = DateTime.ParseExact(_testDate + " " + StartTime, "yyyy-M-d H:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None).ToUniversalTime();
            else
                _startTime = DateTime.ParseExact(StartTime, "H:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None).ToUniversalTime();

            if (_startTime < utcDate)
                _startTime = _startTime.AddDays(1);

            //If there are holidays, account for them
            foreach (AbstractApiHolidays holiday in _holidays)
                if (_startTime >= holiday.utcStart && _startTime < holiday.utcEnd)
                {
                    _startTime = _startTime.AddDays(1);
                    break;
                }


            if (!string.IsNullOrEmpty(_testDate))
                _endTime = DateTime.ParseExact(_testDate + " " + EndTime, "yyyy-M-d H:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None).ToUniversalTime();
            else
                _endTime = DateTime.ParseExact(EndTime, "H:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None).ToUniversalTime();

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

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            DateTime timeNow = DateTime.UtcNow;
            DateTime localDate = DateTime.Today;
            if (!string.IsNullOrEmpty(_testDate))
                localDate = DateTime.ParseExact(_testDate, "yyyy-M-d", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);

            if (_lastDay < localDate)
                retrieveHolidays(localDate, timeNow);

            //We can only do UTC rollover if we're not currently retrying holidays and it's not during showtime
            if (!_isShowtime && (!_useHolidays || !_isUpdating) && (_startTime <= timeNow && string.IsNullOrEmpty(_testDate)))
                utcRollover(timeNow);

            //We can only enter showtime if we're not currently retrying holidays, but existing showtimes will continue to monitor
            if ((!_useHolidays || (_isShowtime || (!_isShowtime && !_isUpdating))) && timeNow >= _startTime && timeNow < _endTime && _daysOfWeek.Contains(_startTime.DayOfWeek))
            {
                if (!_isShowtime)
                {
                    LogMessage("debug", "UTC Start Time {Time} ({DayOfWeek}), monitoring for {MatchText} within {Timeout} seconds, until UTC End time {EndTime} ({EndDayOfWeek}) ...", 
                        _startTime.ToShortTimeString(), _startTime.DayOfWeek, _textMatch, _timeOut.TotalSeconds, _endTime.ToShortTimeString(), _endTime.DayOfWeek);
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
            else if (timeNow < _startTime || timeNow >= _endTime)
            {
                //Showtime can end even if we're retrieving holidays
                if (_isShowtime)
                    LogMessage("debug", "UTC End Time {Time} ({DayOfWeek}), no longer monitoring for {MatchText} ...", _endTime.ToShortTimeString(), _endTime.DayOfWeek, _textMatch);

                //Reset the match counters
                _lastLog = timeNow;
                _lastCheck = timeNow;
                _matched = 0;
                _isAlert = false;
                _isShowtime = false;
            }
        }

        public void On(Event<LogEventData> evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            DateTime timeNow = DateTime.UtcNow;
            if (_matched == 0 && timeNow >= _startTime && timeNow < _endTime && _daysOfWeek.Contains(_startTime.DayOfWeek))
            {
                if (evt.Data.RenderedMessage.IndexOf(TextMatch, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    _matched++;
                    _lastCheck = timeNow;
                    LogMessage("debug", "Successfully matched {TextMatch}! Further matches will not be logged ...", _textMatch);
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
