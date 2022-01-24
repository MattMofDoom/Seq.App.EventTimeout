using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using HandlebarsDotNet;

namespace Seq.App.EventTimeout.Classes
{
    public class HandlebarsTemplate
    {
        private readonly Func<object, string> _template;

        public HandlebarsTemplate(string template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            var compiled = Handlebars.Compile(template);
            _template = o => compiled(o);
        }

        public string Render(TimeoutConfig config, TimeoutCounters counters)
        {
            return FormatTemplate(_template, config, counters);
        }

        private static string FormatTemplate(Func<object, string> template, TimeoutConfig config,
            TimeoutCounters counters)
        {
            var payload = (IDictionary<string, object>) ToDynamic(new Dictionary<string, object>
            {
                {"AppName", config.AppName},
                {"TimeNow", DateTime.Now.ToLongTimeString()},
                {"DateNowLong", DateTime.Now.ToLongDateString()},
                {"DateNowShort", DateTime.Now.ToShortDateString()},
                {"DateTimeNow", DateTime.Now.ToString("F")},
                {"StartTime", counters.StartTime.ToString("F")},
                {"EndTime", counters.EndTime.ToString("F")},
                {"Timeout", config.TimeOut.TotalSeconds},
                {"TimeoutMins", config.TimeOut.TotalMinutes.ToString("N2")},
                {"TimeoutHours", config.TimeOut.TotalHours.ToString("N2")},
                {"RepeatTimeout", config.RepeatTimeout},
                {"SuppressTime", config.SuppressionTime.TotalSeconds},
                {"SuppressTimeMins", config.SuppressionTime.TotalMinutes.ToString("N2")},
                {"SuppressTimeHours", config.SuppressionTime.TotalHours.ToString("N2")},
                {"RepeatSuppressTime", config.SuppressionTime.TotalSeconds},
                {"RepeatSuppressTimeMins", config.RepeatTimeoutSuppress.TotalMinutes.ToString("N2")},
                {"RepeatSuppressTimeHours", config.RepeatTimeoutSuppress.TotalHours.ToString("N2")},
                {"Tags", string.Join(",", config.Tags)},
                {"Responders", config.Responders ?? ""},
                {"Priority", config.Priority ?? ""},
                {"ProjectKey", config.ProjectKey ?? ""},
                {"DueDate", config.DueDate ?? ""},
                {"InitialTimeEstimate", config.InitialTimeEstimate ?? ""},
                {"RemainingTimeEstimate", config.RemainingTimeEstimate ?? ""}
            });

            return template(payload);
        }

        private static object ToDynamic(object o)
        {
            switch (o)
            {
                case IEnumerable<KeyValuePair<string, object>> dictionary:
                {
                    var result = new ExpandoObject();
                    var asDict = (IDictionary<string, object>) result;
                    foreach (var kvp in dictionary)
                        asDict.Add(kvp.Key, ToDynamic(kvp.Value));
                    return result;
                }
                case IEnumerable<object> enumerable:
                    return enumerable.Select(ToDynamic).ToArray();
                default:
                    return o;
            }
        }
    }
}