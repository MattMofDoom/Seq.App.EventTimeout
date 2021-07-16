using System;

namespace Seq.App.EventTimeout.Classes
{
    public class Showtime
    {
        public Showtime(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        public DateTime Start { get; }
        public DateTime End { get; }
    }
}