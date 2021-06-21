using System;
using System.Collections.Generic;
using System.Text;

namespace Seq.App.EventTimeout.Classes
{
    public class Showtime
    {
        public DateTime Start { get; }
        public DateTime End { get; }

        public Showtime(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }
    }
}
