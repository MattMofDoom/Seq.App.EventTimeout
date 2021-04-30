using System;
using System.Collections.Generic;
using System.Linq;

namespace Seq.App.EventTimeout
{
    public enum DayType
    {
        Day = 0,
        DayOfWeek = 1,
        DayOfMonth = 2,
        Weekday = 3,
        NoMatch = -1
    }

    public enum DayExpression
    {
        First = 0,
        Second = 1,
        Third = 2,
        Fourth = 3,
        Fifth = 4,
        Last = 5,
        None = -1
    }

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

    public class DateExpression
    {
        public DayExpression dayOrder { get; set; }
        public DayType dayType { get; set; }
        public DayOfWeek dayOfWeek { get; set; }
        public int day { get; set; }

        public DateExpression(DayExpression order, DayType type, DayOfWeek weekday, int dayofmonth)
        {
            dayOrder = order;
            dayType = type;
            dayOfWeek = weekday;
            day = dayofmonth;
        }
    }

    public static class Dates
    {
        private static DateExpression getDayOfMonth(DayExpression dayExp,  DayType dayType, DayMatch matchDay, DateTime targetDate)
        {
            DateTime firstDay = new DateTime(targetDate.Year, targetDate.Month, 1);
            DateTime lastDay = new DateTime(targetDate.Year, targetDate.Month, DateTime.DaysInMonth(targetDate.Year, targetDate.Month));

            switch (dayType)
            {
                case DayType.DayOfMonth:
                    //Only first or last days of month are handled
                    switch (dayExp)
                    {
                        case DayExpression.First:
                            return new DateExpression(dayExp, dayType, firstDay.DayOfWeek, firstDay.Day);
                        case DayExpression.Last:
                            return new DateExpression(dayExp, dayType, lastDay.DayOfWeek, lastDay.Day);
                        default:
                            return new DateExpression(DayExpression.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                    }
                case DayType.Weekday:
                    //Only first or last weekday expressions are handled
                    switch (dayExp)
                    {
                        case DayExpression.First:
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

                            return new DateExpression(dayExp, dayType, firstDay.DayOfWeek, firstDay.Day);
                        case DayExpression.Last:
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

                            return new DateExpression(dayExp, dayType, lastDay.DayOfWeek, lastDay.Day);
                        default:
                            return new DateExpression(DayExpression.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                    }
                case DayType.DayOfWeek:
                    //Calculate the day of week in the month, according to the DayExpression (First, Second, Third, Fourth, Fifth, Last)
                    if (dayExp == DayExpression.Last)
                    {
                        while ((int)lastDay.DayOfWeek != (int)matchDay)
                            lastDay = lastDay.AddDays(-1);

                        return new DateExpression(dayExp, dayType, lastDay.DayOfWeek, lastDay.Day);
                    }
                    else if (dayExp != DayExpression.None)
                    {
                        //Match the first day of month for this DayOfWeek
                        while ((int)firstDay.DayOfWeek != (int)matchDay)
                            firstDay = firstDay.AddDays(1);

                        //Calculate the nth DayOfWeek
                        firstDay = firstDay.AddDays((int)dayExp * 7);

                        //Make sure the nth DayOfWeek is still in the same month
                        if (targetDate.Month == firstDay.Month)
                            return new DateExpression(dayExp, dayType, firstDay.DayOfWeek, firstDay.Day);
                        else
                            return new DateExpression(DayExpression.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                    }

                    return new DateExpression(DayExpression.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                case DayType.Day:
                    //Just return a date expression for the date passed in
                    return new DateExpression(dayExp, dayType, targetDate.DayOfWeek, targetDate.Day);
            }

            return new DateExpression(DayExpression.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
        }

        private static DateExpression getDay(string day, DateTime localStart)
        {
            if (string.IsNullOrEmpty(day))
                return new DateExpression(DayExpression.None, DayType.NoMatch, DayOfWeek.Sunday, -1);

            DateTime dateNow = DateTime.Today;
            DateTime firstDay = new DateTime(dateNow.Year, dateNow.Month, 1);
            DateTime lastDay = new DateTime(firstDay.Year, firstDay.Month, DateTime.DaysInMonth(firstDay.Year, firstDay.Month));

            //If it's a simple integer, we can just return the day
            int dayResult;
            if (int.TryParse(day, out dayResult) && dayResult > 0)
                return getDayOfMonth(DayExpression.None, DayType.Day, DayMatch.None, new DateTime(firstDay.Year, firstDay.Month, dayResult));


            switch (day.ToLower())
            {
                case "first":
                    return getDayOfMonth(DayExpression.First, DayType.DayOfMonth, DayMatch.None, firstDay);
                case "last":
                    return getDayOfMonth(DayExpression.Last, DayType.DayOfMonth, DayMatch.None, lastDay);
                case "first weekday":
                    return getDayOfMonth(DayExpression.First, DayType.Weekday, DayMatch.None, firstDay);
                case "last weekday":
                    return getDayOfMonth(DayExpression.First, DayType.Weekday, DayMatch.None, lastDay);
                default:
                    string[] dayExpressionString = day.Split(' ');
                    if (dayExpressionString.Length == 2)
                    {
                        //Parse to first, second, third, fourth, last
                        DayExpression dayExpression = DayExpression.None;
                        //Parse on the dayofweek to match
                        DayMatch dayMatch = DayMatch.None;
                        if (Enum.TryParse<DayExpression>(dayExpressionString[0], true, out dayExpression) && dayExpression != DayExpression.None)
                            if (Enum.TryParse<DayMatch>(dayExpressionString[1], true, out dayMatch))
                                return getDayOfMonth(dayExpression, DayType.DayOfWeek, dayMatch, firstDay);
                    }
                    return new DateExpression(DayExpression.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
            }
        }

        public static List<int> getDaysOfMonth(string Days, string StartTime, string StartFormat)
        {
            List<int> dayResult = new List<int>();
            if (!string.IsNullOrEmpty(Days))
            {
                List<string> dayList = Days.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();

                DateTime localStart = DateTime.ParseExact(StartTime, StartFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
                //Always calculate based on next start
                if (localStart < DateTime.Now)
                    localStart = localStart.AddDays(1);

                foreach (string day in dayList)
                {
                    DateExpression dayExpression = getDay(day, localStart);
                    if (dayExpression.dayType != DayType.NoMatch)
                    {
                        //Calculate UTC day based on start time
                        DateTime resultDay = new DateTime(localStart.Year, localStart.Month, dayExpression.day, localStart.Hour, localStart.Minute, localStart.Second).ToUniversalTime();
                        if (!dayResult.Contains(resultDay.Day))
                            dayResult.Add(resultDay.Day);
                    }
                }
            }

            dayResult.Sort();
            return dayResult;
        }

        public static List<DayOfWeek> getDaysOfWeek(string Days, string StartTime, string StartFormat)
        {
            List<DayOfWeek> dayResult = new List<DayOfWeek>();
            DateTime localStart = DateTime.ParseExact(StartTime, StartFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
            DateTime utcStart = localStart.ToUniversalTime();

            //Always calculate based on next start
            if (localStart < DateTime.Now)
                localStart = localStart.AddDays(1);

            if (!string.IsNullOrEmpty(Days))
            {
                string[] days = Days.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray();
                if (days.Length > 0)
                {
                    //Calculate dasys of week based on UTC start times
                    DayOfWeek dayOfWeek = DayOfWeek.Sunday;
                    foreach (string day in days)
                        if (Enum.TryParse(day, out dayOfWeek))
                            if (localStart.ToUniversalTime().DayOfWeek < localStart.DayOfWeek || ((int)localStart.DayOfWeek == 0 && (int)utcStart.DayOfWeek == 6))
                                if (dayOfWeek - 1 >= 0)
                                    dayResult.Add(dayOfWeek - 1);
                                else
                                    dayResult.Add(DayOfWeek.Saturday);
                            else
                                dayResult.Add(dayOfWeek);
                }
            }

            if (dayResult.Count == 0)
                dayResult = new List<DayOfWeek>() { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };

            return dayResult;
        }
    }
}
