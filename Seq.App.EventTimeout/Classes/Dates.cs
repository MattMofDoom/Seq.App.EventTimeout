using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Seq.App.EventTimeout.Enums;

namespace Seq.App.EventTimeout.Classes
{
    /// <summary>
    ///     Date calculations for inclusions/exclusions
    /// </summary>
    public static class Dates
    {
        /// <summary>
        ///     Return a DateExpression based on expressed day order, day type, day match type, and the next local day and month
        ///     timeout start
        /// </summary>
        /// <param name="dayOrder"></param>
        /// <param name="dayType"></param>
        /// <param name="matchDay"></param>
        /// <param name="nextStart"></param>
        /// <returns></returns>
        private static DateExpression GetDayOfMonth(DayOrder dayOrder, DayType dayType, DayMatch matchDay,
            DateTime nextStart)
        {
            var firstDay = new DateTime(nextStart.Year, nextStart.Month, 1);
            var lastDay = new DateTime(nextStart.Year, nextStart.Month,
                DateTime.DaysInMonth(nextStart.Year, nextStart.Month));

            // ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
            // ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (dayType)
            {
                case DayType.DayOfMonth:
                    //Only first or last days of month are handled
                    switch (dayOrder)
                    {
                        case DayOrder.First:
                            return new DateExpression(dayOrder, dayType, firstDay.DayOfWeek, firstDay.Day);
                        case DayOrder.Last:
                            return new DateExpression(dayOrder, dayType, lastDay.DayOfWeek, lastDay.Day);
                        default:
                            return new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                    }
                case DayType.Weekday:
                    //Only first or last weekday expressions are handled
                    switch (dayOrder)
                    {
                        case DayOrder.First:
                            //Find the first weekday
                            switch (firstDay.DayOfWeek)
                            {
                                case DayOfWeek.Sunday:
                                    firstDay = firstDay.AddDays(1);
                                    break;
                                case DayOfWeek.Saturday:
                                    firstDay = firstDay.AddDays(2);
                                    break;
                            }

                            return new DateExpression(dayOrder, dayType, firstDay.DayOfWeek, firstDay.Day);
                        case DayOrder.Last:
                            //Find the last weekday
                            switch (lastDay.DayOfWeek)
                            {
                                case DayOfWeek.Sunday:
                                    lastDay = lastDay.AddDays(-2);
                                    break;
                                case DayOfWeek.Saturday:
                                    lastDay = lastDay.AddDays(-1);
                                    break;
                            }

                            return new DateExpression(dayOrder, dayType, lastDay.DayOfWeek, lastDay.Day);
                        default:
                            return new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                    }
                case DayType.DayOfWeek:
                    //Calculate the day of week in the month, according to the DayExpression (First, Second, Third, Fourth, Fifth, Last)
                    if (dayOrder == DayOrder.Last)
                    {
                        while ((int) lastDay.DayOfWeek != (int) matchDay) lastDay = lastDay.AddDays(-1);

                        return new DateExpression(dayOrder, dayType, lastDay.DayOfWeek, lastDay.Day);
                    }
                    else if (dayOrder != DayOrder.None)
                    {
                        //Match the first day of month for this DayOfWeek
                        while ((int) firstDay.DayOfWeek != (int) matchDay) firstDay = firstDay.AddDays(1);

                        //Calculate the nth DayOfWeek
                        firstDay = firstDay.AddDays((int) dayOrder * 7);

                        //Make sure the nth DayOfWeek is still in the same month
                        return nextStart.Month == firstDay.Month
                            ? new DateExpression(dayOrder, dayType, firstDay.DayOfWeek, firstDay.Day)
                            : new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                    }

                    return new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                case DayType.Day:
                    //Just return a date expression for the date passed in
                    return new DateExpression(dayOrder, dayType, nextStart.DayOfWeek, nextStart.Day);
            }

            return new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
        }

        /// <summary>
        ///     Return a date expression given the type of day, using the the next local day and month timeout start
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        private static DateExpression GetDayType(string day)
        {
            if (string.IsNullOrEmpty(day))
                return new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);

            var dateNow = DateTime.Today;
            var firstDay = new DateTime(dateNow.Year, dateNow.Month, 1);
            var lastDay = new DateTime(firstDay.Year, firstDay.Month,
                DateTime.DaysInMonth(firstDay.Year, firstDay.Month));

            //If it's a simple integer, we can just return the day
            if (int.TryParse(day, out var dayResult) && dayResult > 0)
                return GetDayOfMonth(DayOrder.None, DayType.Day, DayMatch.None,
                    new DateTime(firstDay.Year, firstDay.Month, dayResult));

            switch (day.ToLower())
            {
                case "first":
                    //Month rollover - if the next local start is the last day of the month, ensure that we are returning next month's first day
                    if (dateNow.Day == lastDay.Day && dateNow.Month == lastDay.Month)
                        return GetDayOfMonth(DayOrder.First, DayType.DayOfMonth, DayMatch.None, firstDay.AddMonths(1));
                    else
                        return GetDayOfMonth(DayOrder.First, DayType.DayOfMonth, DayMatch.None, firstDay);

                case "last":
                    return GetDayOfMonth(DayOrder.Last, DayType.DayOfMonth, DayMatch.None, lastDay);
                case "first weekday":
                    //Month rollover - if the next local start is the last day of the month, ensure that we are returning next month's first day
                    if (dateNow.Day == lastDay.Day && dateNow.Month == lastDay.Month)
                        return GetDayOfMonth(DayOrder.First, DayType.Weekday, DayMatch.None, firstDay.AddMonths(1));
                    else
                        return GetDayOfMonth(DayOrder.First, DayType.Weekday, DayMatch.None, firstDay);

                case "last weekday":
                    return GetDayOfMonth(DayOrder.Last, DayType.Weekday, DayMatch.None, lastDay);
                default:
                    var dayExpressionString = day.Split(' ');
                    if (dayExpressionString.Length != 2)
                        return new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                    if (!Enum.TryParse(dayExpressionString[0], true, out DayOrder dayExpression) ||
                        dayExpression == DayOrder.None)
                        return new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                    if (!Enum.TryParse(dayExpressionString[1], true, out DayMatch dayMatch))
                        return new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                    //Month rollover - if the next local start is the last day of the month, ensure that we are returning next month's first day
                    if (dayExpression == DayOrder.First && dateNow.Day == lastDay.Day &&
                        dateNow.Month == lastDay.Month)
                        return GetDayOfMonth(dayExpression, DayType.DayOfWeek, dayMatch,
                            firstDay.AddMonths(1));
                    return GetDayOfMonth(dayExpression, DayType.DayOfWeek, dayMatch, firstDay);
            }
        }

        /// <summary>
        ///     Return the UTC days of month that are included/excluded, given a date expression and start time
        /// </summary>
        /// <param name="dateExpression"></param>
        /// <param name="startTime"></param>
        /// <param name="startFormat"></param>
        /// <returns></returns>
        public static List<int> GetDaysOfMonth(string dateExpression, string startTime, string startFormat)
        {
            var dayResult = new List<int>();
            if (!string.IsNullOrEmpty(dateExpression))
            {
                var dayList = dateExpression.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim()).ToList();

                var localStart = DateTime.ParseExact(startTime, startFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None);
                //Always calculate based on next start
                if (localStart < DateTime.Now) localStart = localStart.AddDays(1);

                foreach (var resultDay in from day in dayList
                    select GetDayType(day)
                    into dayExpression
                    where dayExpression.DayType != DayType.NoMatch
                    select new DateTime(localStart.Year, localStart.Month, dayExpression.Day,
                        localStart.Hour, localStart.Minute, localStart.Second).ToUniversalTime()
                    into resultDay
                    where !dayResult.Contains(resultDay.Day)
                    select resultDay)
                    dayResult.Add(resultDay.Day);
            }

            dayResult.Sort();
            return dayResult;
        }

        /// <summary>
        ///     Return a list of DayOfWeek, given a comma-delimited string and start time />
        /// </summary>
        /// <param name="daysOfWeek"></param>
        /// <param name="startTime"></param>
        /// <param name="startFormat"></param>
        /// <returns></returns>
        public static List<DayOfWeek> GetDaysOfWeek(string daysOfWeek, string startTime, string startFormat)
        {
            var dayResult = new List<DayOfWeek>();
            var localStart =
                DateTime.ParseExact(startTime, startFormat, CultureInfo.InvariantCulture, DateTimeStyles.None);
            var utcStart = localStart.ToUniversalTime();

            //Always calculate based on next start
            if (localStart < DateTime.Now) localStart = localStart.AddDays(1);

            if (!string.IsNullOrEmpty(daysOfWeek))
            {
                var dayArray = daysOfWeek.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim()).ToArray();
                if (dayArray.Length > 0)
                    //Calculate days of week based on UTC start times
                    foreach (var day in dayArray)
                        if (Enum.TryParse(day, out DayOfWeek dayOfWeek))
                        {
                            if (localStart.ToUniversalTime().DayOfWeek < localStart.DayOfWeek ||
                                localStart.DayOfWeek == 0 && (int) utcStart.DayOfWeek == 6)
                            {
                                if (dayOfWeek - 1 >= 0)
                                    dayResult.Add(dayOfWeek - 1);
                                else
                                    dayResult.Add(DayOfWeek.Saturday);
                            }
                            else
                            {
                                dayResult.Add(dayOfWeek);
                            }
                        }
            }

            if (dayResult.Count == 0)
                dayResult = new List<DayOfWeek>
                {
                    DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
                    DayOfWeek.Friday, DayOfWeek.Saturday
                };

            return dayResult;
        }
    }
}