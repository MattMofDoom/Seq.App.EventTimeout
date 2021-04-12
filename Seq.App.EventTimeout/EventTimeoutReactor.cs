using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        List<DayOfWeek> _daysOfWeek;

        TimeSpan _timeOut;
        TimeSpan _suppressionTime;
        string _textMatch;
        string _alertMessage;
        string _alertDescription;
        string _timeoutLogLevel;
        bool _isAlert;
        Timer _timer;
        bool _isShowtime;
        bool _includeApp;
        bool _diagnostics;

        bool _isTags;
        string[] _tags;

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
            HelpText = "Time period in which a matching log entry must be seen. After this, an alert will be raised")]
        public int Timeout { get; set; }

        [SeqAppSetting(
            DisplayName = "Days of Week",
            HelpText = "Comma-delimited - Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday",
            IsOptional = true)]
        public string DaysOfWeek { get; set; }

        [SeqAppSetting(
            DisplayName = "Suppression Interval (seconds)",
            HelpText = "If an alert has been raised, further alerts will be suppressed for this time.")]
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

        protected override void OnAttached()
        {
            LogMessage("debug", "Check {AppName} diagnostic level ({Diagnostics}) ...", App.Title, Diagnostics);
            _diagnostics = Diagnostics;

            if (_diagnostics)
                LogMessage("debug", "Check include {AppName} ({IncludeApp}) ...", App.Title, IncludeApp);
            _includeApp = IncludeApp;
            if (!_includeApp && _diagnostics)
                LogMessage("debug", "App name {AppName} will not be included in alert message ...", App.Title);
            else if (_diagnostics)
                LogMessage("debug", "App name {AppName} will be included in alert message ...", App.Title);

            if (_diagnostics)
                LogMessage("debug", "Convert Timeout {timeout} to TimeSpan ...", Timeout);
            _timeOut = TimeSpan.FromSeconds(Timeout);
            if (_diagnostics)
                LogMessage("debug", "Parsed Timeout is {timeout} ...", _timeOut.TotalSeconds);

            if (_diagnostics)
                LogMessage("debug", "Convert Suppression {suppression} to TimeSpan ...", SuppressionTime);
            _suppressionTime = TimeSpan.FromSeconds(SuppressionTime);
            if (_diagnostics)
                LogMessage("debug", "Parsed Suppression is {timeout} ...", _suppressionTime.TotalSeconds);

            if (_diagnostics)
                LogMessage("debug", "Convert Start Time {time} to UTC DateTime ...", StartTime, App.Title);
            _startTime = DateTime.ParseExact(StartTime, "H:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None).ToUniversalTime();
            if (_startTime < DateTime.UtcNow)
                _startTime = _startTime.AddDays(1);

            if (_diagnostics)
                LogMessage("debug", "Parsed UTC Start Time is {time}, UTC Day {dayofweek} ...", _startTime.ToShortTimeString(), _startTime.DayOfWeek);

            if (_diagnostics)
                LogMessage("debug", "Convert End Time {time} to UTC DateTime ...", EndTime, StartTime);
            _endTime = DateTime.ParseExact(EndTime, "H:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None).ToUniversalTime();
            if (_endTime <= _startTime)
                if (_endTime.AddDays(1) < _startTime)
                    _endTime = _endTime.AddDays(2);
                else
                    _endTime = _endTime.AddDays(1);


            if (_diagnostics)
                LogMessage("debug", "Parsed UTC End Time is {time}, UTC Day {dayofweek} ...", _endTime.ToShortTimeString(), _endTime.DayOfWeek);

            if (_diagnostics)
                LogMessage("debug", "Convert Days of Week {daysofweek} to UTC Days of Week ...", DaysOfWeek);
            if (string.IsNullOrEmpty(DaysOfWeek))
                _daysOfWeek = new List<DayOfWeek>() { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
            else
            {
                string[] days = DaysOfWeek.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray();
                if (days.Length > 0)
                {
                    _daysOfWeek = new List<DayOfWeek>();
                    foreach (string day in days)
                        if (_startTime.DayOfWeek < DateTime.ParseExact(StartTime, "H:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None).DayOfWeek)
                            if ((int)(DayOfWeek)Enum.Parse(typeof(DayOfWeek), day) - 1 < 0)
                                _daysOfWeek.Add(DayOfWeek.Saturday);
                            else
                                _daysOfWeek.Add((DayOfWeek)((int)(DayOfWeek)Enum.Parse(typeof(DayOfWeek), day) - 1));
                        else
                            _daysOfWeek.Add((DayOfWeek)Enum.Parse(typeof(DayOfWeek), day));
                }
                else
                    _daysOfWeek = new List<DayOfWeek>() { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
            }
            if (_diagnostics)
                LogMessage("debug", "UTC Days of Week {daysofweek} will be used ...", _daysOfWeek.ToArray());
            
            if (_diagnostics)
                LogMessage("debug", "Validate Text Match '{TextMatch}' ...", TextMatch);
            _textMatch = string.IsNullOrWhiteSpace(TextMatch) ? "Match text" : TextMatch.Trim();
            if (_diagnostics)
                LogMessage("debug", "Text Match '{TextMatch}' will be used ...", _textMatch);

            if (_diagnostics)
                LogMessage("debug", "Validate Alert Message '{AlertMessage}' ...", AlertMessage);
            _alertMessage = string.IsNullOrWhiteSpace(AlertMessage) ? "An event timeout has occurred!" : AlertMessage.Trim();
            if (_diagnostics)
                LogMessage("debug", "Alert Message '{AlertMessage}' will be used ...", _alertMessage);

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
                LogMessage("debug", "Log level {loglevel} will be used for timeouts on {Instance} ...", false, _timeoutLogLevel, App.Title);

            if (_diagnostics)
                LogMessage("debug", "Starting timer ...");
            _timer = new Timer(1000);
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
            if (_diagnostics)
                LogMessage("debug", "Timer started ...");

        }


        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            DateTime timeNow = DateTime.UtcNow;
                        
            if (timeNow >= _startTime && timeNow < _endTime && _daysOfWeek.Contains(_startTime.DayOfWeek))
            {
                if (!_isShowtime)
                {
                    LogMessage("debug", "UTC Start Time {Time} ({DayOfWeek}), monitoring for {MatchText} within {Timeout} seconds, until UTC End time {EndTime} ({EndDayOfWeek}) ...", _startTime.ToShortTimeString(), _startTime.DayOfWeek, _textMatch, _timeOut.TotalSeconds, _endTime.ToShortTimeString(), _endTime.DayOfWeek);
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
                if (_isShowtime)
                    LogMessage("debug", "UTC End Time {Time} ({DayOfWeek}), no longer monitoring for {MatchText} ...", _endTime.ToShortTimeString(), _endTime.DayOfWeek, _textMatch);

                //Reset the match counters
                _lastLog = timeNow;
                _lastCheck = timeNow;
                _matched = 0;
                _isAlert = false;
                _isShowtime = false;
            } 
            
            if (!_isShowtime && _startTime < timeNow)
            {
                //Day rollover, we need to ensure the next start and end is in the future
                _startTime = DateTime.ParseExact(StartTime, "H:mm:ss", null, System.Globalization.DateTimeStyles.None).ToUniversalTime();
                if (_startTime < timeNow)
                    _startTime = _startTime.AddDays(1);

                _endTime = DateTime.ParseExact(EndTime, "H:mm:ss", null, System.Globalization.DateTimeStyles.None).ToUniversalTime();
                if (_endTime <= _startTime)
                    if (_endTime.AddDays(1) < _startTime)
                        _endTime = _endTime.AddDays(2);
                    else
                        _endTime = _endTime.AddDays(1);

                if (_diagnostics)
                    LogMessage("debug", "UTC Day Rollover, Next UTC Start Time {StartTime} ({StartDayOfWeek}), UTC End Time {EndTime} ({EndDayOfWeek})...", _startTime.ToShortTimeString(), _startTime.DayOfWeek, _endTime.ToShortTimeString(), _endTime.DayOfWeek);
            }


        }

        public void On(Event<LogEventData> evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            DateTime timeNow = DateTime.UtcNow;
            if (_matched == 0 && timeNow >= _startTime && timeNow < _endTime && _daysOfWeek.Contains(_startTime.DayOfWeek))
            {
                if (evt.Data.RenderedMessage.IndexOf(TextMatch,StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    _matched++;
                    _lastCheck = timeNow;
                    LogMessage("debug", "Successfully matched {TextMatch}! Further matches will not be logged ...", _textMatch);
                }
            }
        }

        private void LogMessage(string level, string message, params object[] args)
        {
            LogMessage(level, message, true, args);
        }

        private void LogMessage(string level, string message, bool logTags = true, params object[] args)
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
    }
}
