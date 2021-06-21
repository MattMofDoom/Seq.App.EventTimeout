using System;
using System.Collections.Generic;
using System.Threading;
using Seq.App.EventTimeout.Classes;
using Seq.App.EventTimeout.Enums;
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

        [Fact]
        public void AppTriggersTimeouts()
        {
            var app = new EventTimeoutReactor()
            {
                Diagnostics = true,
                StartTime = DateTime.Now.AddSeconds(1).ToString("H:mm:ss"),
                EndTime = DateTime.Now.AddMinutes(1).ToString("H:mm:ss"),
                Timeout = 10,
                RepeatTimeout = false,
                SuppressionTime = 50,
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

            app.Attach(TestAppHost.Instance);
            Thread.Sleep(2000);
            Assert.True(app.IsShowtime);
            var evt = Some.LogEvent();
            app.On(evt);
            Thread.Sleep(10000);
            Assert.True(app.IsAlert);
        }

        [Fact]
        public void HolidaysMatch()
        {
            var holiday = new AbstractApiHolidays("Timeout Day", "", "AU", "", "AU", "Australia - New South Wales",
                "Local holiday", DateTime.Today.ToString("MM/dd/yyyy"), DateTime.Today.Year.ToString(),
                DateTime.Today.Month.ToString(), DateTime.Today.Day.ToString(), DateTime.Today.DayOfWeek.ToString());

            Assert.True(Holidays.ValidateHolidays(new List<AbstractApiHolidays>() {holiday},
                new List<string>() {"National", "Local"}, new List<string>() {"Australia", "New South Wales"}, false,
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
