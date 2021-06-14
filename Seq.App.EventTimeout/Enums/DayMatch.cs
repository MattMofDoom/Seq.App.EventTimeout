using System;

namespace Seq.App.EventTimeout.Enums
{
    /// <summary>
    ///     Match days of week, or none
    /// </summary>
    // ReSharper disable UnusedMember.Global
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
}