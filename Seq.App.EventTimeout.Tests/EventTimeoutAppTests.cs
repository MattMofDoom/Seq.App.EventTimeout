using System;
using System.Collections.Generic;
using System.Threading;
using Seq.App.EventTimeout.Classes;
using Seq.App.EventTimeout.Tests.Support;
using Xunit;
using Xunit.Abstractions;

namespace Seq.App.EventTimeout.Tests
{
    public class EventTimeoutAppTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public EventTimeoutAppTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private EventTimeoutReactor getReactor(string start, string end, int timeout, int suppression)
        {
            return new EventTimeoutReactor
            {
                Diagnostics = true,
                StartTime = start,
                EndTime = end,
                Timeout = timeout,
                RepeatTimeout = false,
                SuppressionTime = suppression,
                TimeoutLogLevel = "Error",
                Priority = "P1",
                Responders = "Everyone Ever",
                Property1Name = "@Message",
                TextMatch = "Event That Is Not Matchable",
                AlertMessage = "An alert!",
                AlertDescription = "An alert has arisen!",
                Tags = "Alert,Message",
                IncludeApp = true

            };
        }

        [Fact]
        public void AppTriggersTimeouts()
        {
            var app = getReactor(DateTime.Now.AddSeconds(1).ToString("H:mm:ss"),
                DateTime.Now.AddMinutes(1).ToString("H:mm:ss"), 1, 59);

            app.Attach(TestAppHost.Instance);
            Thread.Sleep(2000);
            Assert.True(app.IsShowtime);
            var evt = Some.LogEvent();
            app.On(evt);
            Thread.Sleep(2000);
            Assert.True(app.IsAlert);
        }

        [Fact]
        public void AppAllows24H()
        {
            var app = getReactor(DateTime.Today.ToString("H:mm:ss"), DateTime.Today.ToString("H:mm:ss"), 1, 59);
            app.Attach(TestAppHost.Instance);
            var showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("Current UTC: " + DateTime.Now.ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " + showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Expect Start: " + DateTime.Today.ToUniversalTime().ToString("F") + " to " + DateTime.Today.AddDays(1).ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("Hours: " + (DateTime.Today.AddDays(1).ToUniversalTime() -
                                        DateTime.Today.ToUniversalTime()).TotalHours);
            Assert.True(showTime.Start == DateTime.Today.ToUniversalTime());
            Assert.True(showTime.End == DateTime.Today.AddDays(1).ToUniversalTime());
        }

        [Fact]
        public void AppStartsDuringShowTime()
        {
            var start = DateTime.Now.AddHours(-1);
            var end = DateTime.Now.AddHours(1);

            var app = getReactor(start.ToString("H:mm:ss"), end.ToString("H:mm:ss"), 1, 59);
            app.Attach(TestAppHost.Instance);
            var showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("Current UTC: " + DateTime.Now.ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " + showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Expect Start: " + start.ToUniversalTime().ToString("F") + " to " + end.ToUniversalTime().ToString("F"));
            Assert.True(showTime.Start.ToString("F") == start.AddDays(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.End.ToString("F") == end.AddDays(1).ToUniversalTime().ToString("F"));
        }

        [Fact]
        public void AppStartsBeforeShowTime()
        {
            var start = DateTime.Now.AddHours(1);
            var end = DateTime.Now.AddHours(2);

            var app = getReactor(start.ToString("H:mm:ss"), end.ToString("H:mm:ss"), 1, 59);
            app.Attach(TestAppHost.Instance);
            var showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("Current UTC: " + DateTime.Now.ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " + showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Expect Start: " + start.ToUniversalTime().ToString("F") + " to " + end.ToUniversalTime().ToString("F"));
            Assert.True(showTime.Start.ToString("F") == start.ToUniversalTime().ToString("F"));
            Assert.True(showTime.End.ToString("F") == end.ToUniversalTime().ToString("F"));
        }

        [Fact]
        public void AppStartsAfterShowTime()
        {
            var start = DateTime.Now.AddHours(-2);
            var end = DateTime.Now.AddHours(-1);

            var app = getReactor(start.ToString("H:mm:ss"), end.ToString("H:mm:ss"), 1, 59);
            app.Attach(TestAppHost.Instance);
            var showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("Current UTC: " + DateTime.Now.ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " + showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Expect Start: " + start.AddDays(1).ToUniversalTime().ToString("F") + " to " + end.AddDays(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.Start.ToString("F") == start.AddDays(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.End.ToString("F") == end.AddDays(1).ToUniversalTime().ToString("F"));
        }

        [Fact]
        public void RolloverWithHoliday()
        {
            var start = DateTime.Now.AddHours(1);
            var end = DateTime.Now.AddHours(2);
            var holiday = new AbstractApiHolidays("Timeout Day", "", "AU", "", "AU", "Australia - New South Wales",
                "Local holiday", DateTime.Today.ToString("MM/dd/yyyy"), DateTime.Today.Year.ToString(),
                DateTime.Today.Month.ToString(), DateTime.Today.Day.ToString(), DateTime.Today.DayOfWeek.ToString());

            var app = getReactor(start.ToString("H:mm:ss"), end.ToString("H:mm:ss"), 1, 59);
            app.Attach(TestAppHost.Instance);
            app._holidays = new List<AbstractApiHolidays> {holiday};
            app.UtcRollover(DateTime.Now.ToUniversalTime(), true);
            var showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("Current UTC: " + DateTime.Now.ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " + showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Expect Start: " + start.AddDays(1).ToUniversalTime().ToString("F") + " to " + end.AddDays(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.Start.ToString("F") == start.AddDays(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.End.ToString("F") == end.AddDays(1).ToUniversalTime().ToString("F"));
        }
        
        [Fact]
        public void RolloverWithoutHoliday()
        {
            var start = DateTime.Now.AddHours(-1);
            var end = DateTime.Now.AddSeconds(-1);
            var app = getReactor(start.ToString("H:mm:ss"), end.ToString("H:mm:ss"), 1, 59);

            app.Attach(TestAppHost.Instance);
            app.UtcRollover(DateTime.Now.ToUniversalTime());
            var showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("Current UTC: " + DateTime.Now.ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " + showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Expect Start: " + start.AddDays(1).ToUniversalTime().ToString("F") + " to " + end.AddDays(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.Start.ToString("F") == start.AddDays(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.End.ToString("F") == end.AddDays(1).ToUniversalTime().ToString("F"));
        }

        [Fact]
        public void HolidaysMatch()
        {
            var holiday = new AbstractApiHolidays("Timeout Day", "", "AU", "", "AU", "Australia - New South Wales",
                "Local holiday", DateTime.Today.ToString("MM/dd/yyyy"), DateTime.Today.Year.ToString(),
                DateTime.Today.Month.ToString(), DateTime.Today.Day.ToString(), DateTime.Today.DayOfWeek.ToString());

            Assert.True(Holidays.ValidateHolidays(new List<AbstractApiHolidays> {holiday},
                new List<string> {"National", "Local"}, new List<string> {"Australia", "New South Wales"}, false,
                false).Count > 0);
        }

        [Fact]
        public void DatesExpressed()
        {
            _testOutputHelper.WriteLine(string.Join(",",Dates.GetDaysOfMonth("first,last,first weekday,last weekday,first monday", "12:00", "H:mm").ToArray()));
            Assert.True(Dates.GetDaysOfMonth("first,last,first weekday,last weekday,first monday", "12:00", "H:mm").Count > 0);
        }

        [Fact]
        public void PropertyMatched()
        {
            Assert.True(PropertyMatch.Matches("A Matchable Event", "matchable"));
        }
    }
}
