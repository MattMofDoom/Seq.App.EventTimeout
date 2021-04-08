using System;
using System.Collections.Generic;
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
        DateTime _lastTime;
        DateTime _lastLog;

        TimeSpan _timeOut;
        TimeSpan _suppressionTime;
        string _textMatch;
        string[] _tags;
        string _alertMessage;
        string _alertDescription;
        string _timeoutLogLevel;
        bool _isAlert;
        Timer _timer;
        
        [SeqAppSetting(
            DisplayName = "Start Time",
            HelpText = "The time (H:mm:ss, 24 hour format) to start monitoring")]
        public string StartTime { get; set; }

        [SeqAppSetting(
            DisplayName = "End Time",
            HelpText = "The time (H:mm:ss, 24 hour format) to stop monitoring")]
        public string EndTime { get; set; }

        [SeqAppSetting(
            DisplayName = "Timeout Interval (seconds)",
            HelpText = "Time period in which a matching log entry must be seen. After this, an alert will be raised")]
        public int Timeout { get; set; }

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

        protected override void OnAttached()
        {
            LogMessage("debug", "Convert Timeout {timeout} to TimeSpan ", Timeout, App.Title);
            _timeOut = TimeSpan.FromSeconds(Timeout);
            LogMessage("debug", "Parsed Timeout is {timeout} ", _timeOut.TotalSeconds);

            LogMessage("debug", "Convert Suppression {suppression} to TimeSpan ", SuppressionTime, App.Title);
            _suppressionTime = TimeSpan.FromSeconds(SuppressionTime);
            LogMessage("debug", "Parsed Suppression is {timeout} ", _suppressionTime.TotalSeconds);

            LogMessage("debug", "Convert Start Time {time} to UTC DateTime ", StartTime,App.Title);
            _startTime = DateTime.ParseExact(StartTime, "H:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None).ToUniversalTime();
            LogMessage("debug", "Parsed UTC Start Time is {time} ", _startTime.ToShortTimeString(), App.Title);

            LogMessage("debug", "Convert End Time {time} to UTC DateTime ", EndTime, StartTime, App.Title);
            _endTime = DateTime.ParseExact(EndTime, "H:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None).ToUniversalTime();
            LogMessage("debug", "Parsed UTC End Time is {time} ", _endTime.ToShortTimeString(), App.Title);

            LogMessage("debug", "Validate Text Match '{TextMatch}' ...", TextMatch, App.Title);
            _textMatch = string.IsNullOrWhiteSpace(TextMatch) ? "Match text" : TextMatch.Trim();
            LogMessage("debug", "Text Match '{TextMatch}' will be used ...", _textMatch, App.Title);

            LogMessage("debug", "Validate Alert Message '{AlertMessage}' ", AlertMessage, App.Title);
            _alertMessage = string.IsNullOrWhiteSpace(AlertMessage) ? "An event timeout has occurred" : AlertMessage.Trim();
            LogMessage("debug", "Alert Message '{AlertMessage}' will be used ...", _alertMessage, App.Title);

            LogMessage("debug", "Validate Alert Description '{AlertDescription}' ", AlertDescription, App.Title);
            _alertDescription = string.IsNullOrWhiteSpace(AlertDescription) ? _alertMessage + " : Generated by Seq " + Host.BaseUri : AlertDescription.Trim();
            LogMessage("debug", "Alert Description '{AlertDescription}' will be used ...", _alertDescription, App.Title);

            LogMessage("debug", "Validate Tags '{Tags}' ", Tags, App.Title);
            _tags = (Tags ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToArray();
            LogMessage("debug", "Tags '{Tags}' will be used ", string.Join(",",_tags), App.Title);

            if (string.IsNullOrWhiteSpace(TimeoutLogLevel))
            {
                TimeoutLogLevel = "Error";
            }
            _timeoutLogLevel = TimeoutLogLevel.Trim().ToLowerInvariant();
            LogMessage("debug", "Log level {loglevel} will be used for timeouts on {Instance}", _timeoutLogLevel, App.Title);

            LogMessage("debug", "Starting timer ...");
            _timer = new Timer(1000);
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
            LogMessage("debug", "Timer started ...");
        }


        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            DateTime timeNow = DateTime.UtcNow;

            if (timeNow.Day != _lastTime.Day)
            {
                //Account for day rollover
                _startTime = DateTime.ParseExact(StartTime, "H:mm:ss", null, System.Globalization.DateTimeStyles.None).ToUniversalTime();
                _endTime = DateTime.ParseExact(EndTime, "H:mm:ss", null, System.Globalization.DateTimeStyles.None).ToUniversalTime();
            }

            if (timeNow >= _startTime && timeNow < _endTime)
            {
                TimeSpan difference = timeNow - _startTime;
                if (difference.TotalSeconds > _timeOut.TotalSeconds && _matched == 0)
                {
                    TimeSpan suppressDiff = _lastLog - timeNow;
                    if (_isAlert && suppressDiff.TotalSeconds < _suppressionTime.TotalSeconds)
                        return;

                    //Log event
                    LogMessage(_timeoutLogLevel, "{Message}", _alertMessage, _alertDescription, _tags, _textMatch, App.Title);
                    _lastLog = timeNow;
                    _isAlert = true;
                }
            }
            else if (DateTime.UtcNow < _startTime || DateTime.UtcNow >= _endTime)
            {
                //Reset the match counters
                _lastTime = timeNow;
                _lastLog = timeNow;
                _matched = 0;
                _isAlert = false;
            }
        }

        public void On(Event<LogEventData> evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            if (DateTime.UtcNow >= _startTime && DateTime.UtcNow < _endTime)
            {
                if (evt.Data.RenderedMessage.Contains(TextMatch))
                {
                    _matched++;
                    if (_matched == 1)
                        LogMessage("debug", "Successfully matched {TextMatch}! Further matches will not be logged.", _textMatch, _tags, App.Title);
                }
            }
        }

        private void LogMessage(string level, string message, params object[] args)
        {
            var firstChar = level[0];
            switch (firstChar)
            {
                case 'v':
                    Log.Verbose(message, args);
                    break;
                case 'd':
                    Log.Debug(message, args);
                    break;
                case 'i':
                    Log.Information(message, args);
                    break;
                case 'w':
                    Log.Warning(message, args);
                    break;
                case 'e':
                    Log.Error(message, args);
                    break;
                case 'f':
                    Log.Fatal(message, args);
                    break;
            }
        }
    }
}
