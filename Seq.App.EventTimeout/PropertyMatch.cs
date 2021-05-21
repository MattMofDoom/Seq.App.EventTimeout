using System;
using System.Collections.Generic;

namespace Seq.App.EventTimeout
{
    public static class PropertyMatch
    {
        public static KeyValuePair<string, string> getProperty(int property, string propertyName, string propertyMatch)
        {
            string key = string.Empty;
            string value = string.Empty;

            if (string.IsNullOrEmpty(propertyName) && property == 1)
                key = "@Message";
            else if (!string.IsNullOrEmpty(propertyName))
                key = propertyName.Trim();

            if (!string.IsNullOrEmpty(key))
                value = string.IsNullOrWhiteSpace(propertyMatch) ? string.Empty : propertyMatch.Trim();

            return new KeyValuePair<string, string>(key, value);
        }

        public static string matchConditions(Dictionary<string,string> Properties)
        {
            int count = 0;
            string condition = string.Empty;
            foreach (KeyValuePair<string, string> property in Properties)
            {
                count++;
                string propValue = "'" + property.Value + "'";
                if (string.IsNullOrEmpty(property.Value))
                    propValue = "ANY value";

                if (count == 1)
                    condition = property.Key + " contains " + propValue;
                else if (!string.IsNullOrEmpty(property.Key))
                    condition = condition + " AND " + property.Key + " contains " + propValue;
            }

            return condition;
        }

        public static bool matches(string Text, string MatchText)
        {
            if (string.IsNullOrEmpty(MatchText))
                return true;
            else
                return Text.IndexOf(MatchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
