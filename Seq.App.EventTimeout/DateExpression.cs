using System;
using System.Collections.Generic;
using System.Linq;

namespace Seq.App.EventTimeout
{
    /// <summary>
    /// Day type within a date expression
    /// </summary>
    public enum DayType
    {
        Day = 0,
        DayOfWeek = 1,
        DayOfMonth = 2,
        Weekday = 3,
        NoMatch = -1
    }

    /// <summary>
    /// Day order within a date expression
    /// </summary>
    public enum DayOrder
    {
        First = 0,
        Second = 1,
        Third = 2,
        Fourth = 3,
        Fifth = 4,
        Last = 5,
        None = -1
    }

    /// <summary>
    /// Match days of week, or none
    /// </summary>
    public enum DayMatch
    {
        Sunday = DayOfWeek.Sunday,
        Monday = DayOfWeek.Monday,
        Tuesday = DayOfWeek.Tuesday,
        Wednesday = DayOfWeek.Wednesday,
        Thursday = DayOfWeek.Thursday,
        Friday = DayOfWeek.Friday,
        Saturday = DayOfWeek.Saturday,
        None = -1
    }

    /// <summary>
    /// Date Expression comprising the day order, day type, day of week, and day of month
    /// </summary>
    public class DateExpression
    {
        public DayOrder DayOrder { get; set; }
        public DayType DayType { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int Day { get; set; }

        public DateExpression(DayOrder order, DayType type, DayOfWeek weekday, int dayofmonth)
        {
            DayOrder = order;
            DayType = type;
            DayOfWeek = weekday;
            Day = dayofmonth;
        }
    }

    /// <summary>
    /// Date calculations for inclusions/exclusions
    /// </summary>
    public static class Dates
    {
        /// <summary>
        /// Return a DateExpression based on expressed day order, day type, day match type, and the next local day and month timeout start
        /// </summary>
        /// <param name="dayOrder"></param>
        /// <param name="dayType"></param>
        /// <param name="matchDay"></param>
        /// <param name="nextStart"></param>
        /// <returns></returns>
        private static DateExpression GetDayOfMonth(DayOrder dayOrder, DayType dayType, DayMatch matchDay, DateTime nextStart)
        {
            DateTime firstDay = new DateTime(nextStart.Year, nextStart.Month, 1);
            DateTime lastDay = new DateTime(nextStart.Year, nextStart.Month, DateTime.DaysInMonth(nextStart.Year, nextStart.Month));

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
                        while ((int)lastDay.DayOfWeek != (int)matchDay)
                        {
                            lastDay = lastDay.AddDays(-1);
                        }

                        return new DateExpression(dayOrder, dayType, lastDay.DayOfWeek, lastDay.Day);
                    }
                    else if (dayOrder != DayOrder.None)
                    {
                        //Match the first day of month for this DayOfWeek
                        while ((int)firstDay.DayOfWeek != (int)matchDay)
                        {
                            firstDay = firstDay.AddDays(1);
                        }

                        //Calculate the nth DayOfWeek
                        firstDay = firstDay.AddDays((int)dayOrder * 7);

                        //Make sure the nth DayOfWeek is still in the same month
                        if (nextStart.Month == firstDay.Month)
                        {
                            return new DateExpression(dayOrder, dayType, firstDay.DayOfWeek, firstDay.Day);
                        }
                        else
                        {
                            return new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                        }
                    }

                    return new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                case DayType.Day:
                    //Just return a date expression for the date passed in
                    return new DateExpression(dayOrder, dayType, nextStart.DayOfWeek, nextStart.Day);
            }

            return new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
        }

        /// <summary>
        /// Return a date expression given the type of day, using the the next local day and month timeout start
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        private static DateExpression GetDayType(string day)
        {
            if (string.IsNullOrEmpty(day))
            {
                return new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
            }

            DateTime dateNow = DateTime.Today;
            DateTime firstDay = new DateTime(dateNow.Year, dateNow.Month, 1);
            DateTime lastDay = new DateTime(firstDay.Year, firstDay.Month, DateTime.DaysInMonth(firstDay.Year, firstDay.Month));

            //If it's a simple integer, we can just return the day
            if (int.TryParse(day, out int dayResult) && dayResult > 0)
            {
                return GetDayOfMonth(DayOrder.None, DayType.Day, DayMatch.None, new DateTime(firstDay.Year, firstDay.Month, dayResult));
            }

            switch (day.ToLower())
            {
                case "first":
                    //Month rollover - if the next local start is the last day of the month, ensure that we are returning next month's first day
                    if (dateNow.Day == lastDay.Day && dateNow.Month == lastDay.Month)
                    {
                        return GetDayOfMonth(DayOrder.First, DayType.DayOfMonth, DayMatch.None, firstDay.AddMonths(1));
                    }
                    else
                    {
                        return GetDayOfMonth(DayOrder.First, DayType.DayOfMonth, DayMatch.None, firstDay);
                    }

                case "last":
                    return GetDayOfMonth(DayOrder.Last, DayType.DayOfMonth, DayMatch.None, lastDay);
                case "first weekday":
                    //Month rollover - if the next local start is the last day of the month, ensure that we are returning next month's first day
                    if (dateNow.Day == lastDay.Day && dateNow.Month == lastDay.Month)
                    {
                        return GetDayOfMonth(DayOrder.First, DayType.Weekday, DayMatch.None, firstDay.AddMonths(1));
                    }
                    else
                    {
                        return GetDayOfMonth(DayOrder.First, DayType.Weekday, DayMatch.None, firstDay);
                    }

                case "last weekday":
                    return GetDayOfMonth(DayOrder.Last, DayType.Weekday, DayMatch.None, lastDay);
                default:
                    string[] dayExpressionString = day.Split(' ');
                    if (dayExpressionString.Length == 2)
                    {
                        //Parse on the dayofweek to match                        
                        if (Enum.TryParse<DayOrder>(dayExpressionString[0], true, out DayOrder dayExpression) && dayExpression != DayOrder.None)
                        {
                            //Parse to first, second, third, fourth, last
                            if (Enum.TryParse<DayMatch>(dayExpressionString[1], true, out DayMatch dayMatch))
                            {
                                //Month rollover - if the next local start is the last day of the month, ensure that we are returning next month's first day
                                if (dayExpression == DayOrder.First && dateNow.Day == lastDay.Day && dateNow.Month == lastDay.Month)
                                {
                                    return GetDayOfMonth(dayExpression, DayType.DayOfWeek, dayMatch, firstDay.AddMonths(1));
                                }
                                else
                                {
                                    return GetDayOfMonth(dayExpression, DayType.DayOfWeek, dayMatch, firstDay);
                                }
                            }
                        }
                    }
                    return new DateExpression(DayOrder.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
            }
        }

        /// <summary>
        /// Return the UTC days of month that are included/excluded, given a date expression and start time
        /// </summary>
        /// <param name="dateExpression"></param>
        /// <param name="startTime"></param>
        /// <param name="startFormat"></param>
        /// <returns></returns>
        public static List<int> GetDaysOfMonth(string dateExpression, string startTime, string startFormat)
        {
            List<int> dayResult = new List<int>();
            if (!string.IsNullOrEmpty(dateExpression))
            {
                List<string> dayList = dateExpression.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();

                DateTime localStart = DateTime.ParseExact(startTime, startFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
                //Always calculate based on next start
                if (localStart < DateTime.Now)
                {
                    localStart = localStart.AddDays(1);
                }

                foreach (string day in dayList)
                {
                    DateExpression dayExpression = GetDayType(day);
                    if (dayExpression.DayType != DayType.NoMatch)
                    {
                        //Calculate UTC day based on start time
                        DateTime resultDay = new DateTime(localStart.Year, localStart.Month, dayExpression.Day, localStart.Hour, localStart.Minute, localStart.Second).ToUniversalTime();
                        if (!dayResult.Contains(resultDay.Day))
                        {
                            dayResult.Add(resultDay.Day);
                        }
                    }
                }
            }

            dayResult.Sort();
            return dayResult;
        }

        /// <summary>
        /// Return a list of DayOfWeek, given a comma-delimited string and start time />
        /// </summary>
        /// <param name="daysOfWeek"></param>
        /// <param name="startTime"></param>
        /// <param name="startFormat"></param>
        /// <returns></returns>
        public static List<DayOfWeek> GetDaysOfWeek(string daysOfWeek, string startTime, string startFormat)
        {
            List<DayOfWeek> dayResult = new List<DayOfWeek>();
            DateTime localStart = DateTime.ParseExact(startTime, startFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
            DateTime utcStart = localStart.ToUniversalTime();

            //Always calculate based on next start
            if (localStart < DateTime.Now)
            {
                localStart = localStart.AddDays(1);
            }

            if (!string.IsNullOrEmpty(daysOfWeek))
            {
                string[] dayArray = daysOfWeek.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray();
                if (dayArray.Length > 0)
                {
                    //Calculate days of week based on UTC start times
                    DayOfWeek dayOfWeek = DayOfWeek.Sunday;
                    foreach (string day in dayArray)
                    {
                        if (Enum.TryParse(day, out dayOfWeek))
                        {
                            if (localStart.ToUniversalTime().DayOfWeek < localStart.DayOfWeek || (localStart.DayOfWeek == 0 && (int)utcStart.DayOfWeek == 6))
                            {
                                if (dayOfWeek - 1 >= 0)
                                {
                                    dayResult.Add(dayOfWeek - 1);
                                }
                                else
                                {
                                    dayResult.Add(DayOfWeek.Saturday);
                                }
                            }
                            else
                            {
                                dayResult.Add(dayOfWeek);
                            }
                        }
                    }
                }
            }

            if (dayResult.Count == 0)
            {
                dayResult = new List<DayOfWeek>() { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
            }

            return dayResult;
        }
    }
}
