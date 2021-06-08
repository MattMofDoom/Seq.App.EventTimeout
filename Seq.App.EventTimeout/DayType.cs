namespace Seq.App.EventTimeout
{
    /// <summary>
    ///     Day type within a date expression
    /// </summary>
    public enum DayType
    {
        Day = 0,
        DayOfWeek = 1,
        DayOfMonth = 2,
        Weekday = 3,
        NoMatch = -1
    }
}