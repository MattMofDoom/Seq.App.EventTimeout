using System;
using Seq.App.EventTimeout.Enums;

namespace Seq.App.EventTimeout.Classes
{
    /// <summary>
    ///     Date Expression comprising the day order, day type, day of week, and day of month
    /// </summary>
    public class DateExpression
    {
        /// <summary>
        ///     Date Expression comprising the day order, day type, day of week, and day of month
        /// </summary>
        /// <param name="order"></param>
        /// <param name="type"></param>
        /// <param name="weekday"></param>
        /// <param name="dayofmonth"></param>
        public DateExpression(DayOrder order, DayType type, DayOfWeek weekday, int dayofmonth)
        {
            DayOrder = order;
            DayType = type;
            DayOfWeek = weekday;
            Day = dayofmonth;
        } // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public DayOrder DayOrder { get; }
        public DayType DayType { get; }
        public DayOfWeek DayOfWeek { get; }
        public int Day { get; }
    }
}