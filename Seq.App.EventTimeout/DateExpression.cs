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

    public class DateExpression
    {
        public DayExpression dayOrder { get; set; }
        public DayType dayType { get; set; }
        public DayOfWeek weekDay { get; set; }
        public int day { get; set; }

        public DateExpression(DayExpression o, DayType type, DayOfWeek w, int d)
        {
            dayOrder = o;
            dayType = type;
            weekDay = w;
            day = d;
        }
    }

    public static class Dates
    {
        public static DateExpression getDay(string day)
        {
            if (string.IsNullOrEmpty(day))
                return new DateExpression(DayExpression.None, DayType.NoMatch, DayOfWeek.Sunday, -1);

            DateTime firstDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime lastDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));

            //If it's a simple integer, we can just return the day
            int dayResult;
            if (int.TryParse(day, out dayResult))
                if (dayResult > 0 && dayResult < lastDay.Day)
                    return new DateExpression(DayExpression.None, DayType.Day, new DateTime(DateTime.Now.Year, DateTime.Now.Month, dayResult).DayOfWeek, dayResult);
                else
                    return new DateExpression(DayExpression.None, DayType.NoMatch, DayOfWeek.Sunday, -1);

            switch (day.ToLower())
            {
                case "first":
                    return new DateExpression(DayExpression.First, DayType.DayOfMonth, firstDay.DayOfWeek, firstDay.Day);
                case "last":
                    return new DateExpression(DayExpression.Last, DayType.DayOfMonth, lastDay.DayOfWeek, lastDay.Day);
                case "first weekday":
                    int firstWeekday = firstDay.Day;
                    switch (firstDay.DayOfWeek)
                    {
                        case DayOfWeek.Sunday:
                            firstWeekday = firstWeekday++;
                            break;
                        case DayOfWeek.Saturday:
                            firstWeekday = firstWeekday + 2;
                            break;
                    }
                    return new DateExpression(DayExpression.First, DayType.DayOfWeek, new DateTime(DateTime.Now.Year, DateTime.Now.Month, firstWeekday).DayOfWeek, firstWeekday);
                case "last weekday":
                    int lastWeekday = lastDay.Day;
                    switch (lastDay.DayOfWeek)
                    {
                        case DayOfWeek.Sunday:
                            lastWeekday = lastWeekday - 2;
                            break;
                        case DayOfWeek.Saturday:
                            lastWeekday = lastWeekday--;
                            break;
                    }
                    return new DateExpression(DayExpression.Last, DayType.DayOfWeek, new DateTime(DateTime.Now.Year, DateTime.Now.Month, lastWeekday).DayOfWeek, lastWeekday);
                default:
                    string[] dayExp = day.Split(' ');
                    if (dayExp.Length == 2)
                    {
                        //parse to first, second, third, fourth, last
                        DayExpression expResult = DayExpression.None;
                        if (!Enum.TryParse<DayExpression>(dayExp[0], true, out expResult) || expResult == DayExpression.None)
                            return new DateExpression(DayExpression.None, DayType.NoMatch, DayOfWeek.Sunday, -1);

                        DayOfWeek dow;
                        if (!Enum.TryParse<DayOfWeek>(dayExp[1], true, out dow))
                            return new DateExpression(DayExpression.None, DayType.NoMatch, DayOfWeek.Sunday, -1);

                        if (expResult == DayExpression.Last)
                        {
                            DateTime mDate = lastDay;
                            while (mDate.DayOfWeek != dow)
                                mDate = mDate.AddDays(-1);

                            return new DateExpression(expResult, DayType.DayOfWeek, dow, mDate.Day);
                        }
                        else
                        {
                            //Match the first day of month for this DayOfWeek
                            DateTime nDate = firstDay;
                            while (nDate.DayOfWeek != dow)
                                nDate = nDate.AddDays(1);

                            //Calculate the nth DayOfWeek
                            nDate = nDate.AddDays((int)expResult * 7);

                            if (nDate.Month == firstDay.Month)
                                return new DateExpression(expResult, DayType.DayOfWeek, nDate.DayOfWeek, nDate.Day);
                            else
                                return new DateExpression(DayExpression.None, DayType.NoMatch, DayOfWeek.Sunday, -1);
                        }
                    }

                    break;
            }

            return new DateExpression(DayExpression.None, DayType.NoMatch, DayOfWeek.Sunday, -1);

        }

        public static List<int> getDaysOfMonth(string Days, string StartTime, string StartFormat)
        {
            List<int> dayResult = new List<int>();
            if (!string.IsNullOrEmpty(Days))
            {
                List<string> dayList = Days.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();

                DateTime localStart = DateTime.ParseExact(StartTime, StartFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);

                foreach (string day in dayList)
                {
                    DateExpression dayExp = getDay(day);
                    if (dayExp.dayType != DayType.NoMatch)
                    {
                        //Calculate UTC day based on start time
                        DateTime resultDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, dayExp.day, localStart.Hour, localStart.Minute, localStart.Second).ToUniversalTime();
                        if (!dayResult.Contains(resultDay.Day))
                            dayResult.Add(resultDay.Day);
                    }
                }
            }

            dayResult.Sort();
            return dayResult;
        }

        public static List<DayOfWeek> getDaysOfWeek(string Days, DateTime StartTime, DateTime EndTime)
        {
            List<DayOfWeek> dayResult = new List<DayOfWeek>();
            if (!string.IsNullOrEmpty(Days))
            {
                string[] days = Days.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray();
                if (days.Length > 0)
                {
                    dayResult = new List<DayOfWeek>();
                    bool crossesUtcDay = false;

                    if ((int)StartTime.DayOfWeek < (int)EndTime.DayOfWeek || ((int)StartTime.DayOfWeek == 6 && (int)EndTime.DayOfWeek == 0))
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

                        dayResult.Add(dow);
                    }
                }
            }

            if (dayResult.Count == 0)
                dayResult = new List<DayOfWeek>() { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };

            return dayResult;
        }
    }
}
