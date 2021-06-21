﻿using System;
using System.Collections.Generic;
using System.Text;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.EventTimeout.Tests.Support
{
    public static class Some
    {
        public static string String()
        {
            return Guid.NewGuid().ToString();
        }

        public static uint Uint()
        {
            return 5417u;
        }

        public static uint EventType()
        {
            return Uint();
        }

        public abstract class ParametersMustBeNamed { }
        
        public static Event<LogEventData> LogEvent(
            ParametersMustBeNamed _ = null,
            LogEventLevel level = LogEventLevel.Fatal,
            IDictionary<string, object> include = null)
        {
            var id = EventId();
            var timestamp = UtcTimestamp();
            var properties = new Dictionary<string, object>
            {
                {"Who", "world"},
                {"Number", 42}
            };

            if (include != null)
            {
                foreach (var includedProperty in include)
                {
                    properties.Add(includedProperty.Key, includedProperty.Value);
                }
            }

            return new Event<LogEventData>(id, EventType(), timestamp, new LogEventData
            {
                Exception = null,
                Id = id,
                Level = level,
                LocalTimestamp = new DateTimeOffset(timestamp),
                MessageTemplate = "Hello, {Who}",
                RenderedMessage = "Hello, world",
                Properties = properties
            });
        }

        public static string EventId()
        {
            return "event-" + String();
        }

        public static DateTime UtcTimestamp()
        {
            return DateTime.UtcNow;
        }

        public static Host Host()
        {
            return new Host("https://seq.example.com", String() );
        }
    }
}
