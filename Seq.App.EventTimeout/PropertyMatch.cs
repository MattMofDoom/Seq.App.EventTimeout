using System;
using System.Collections.Generic;

namespace Seq.App.EventTimeout
{
    /// <summary>
    /// Property matching methods
    /// </summary>
    public static class PropertyMatch
    {
        /// <summary>
        /// Return a property matching rule given a property number, property name, and text to match
        /// </summary>
        /// <param name="property"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyMatch"></param>
        /// <returns></returns>
        public static KeyValuePair<string, string> GetProperty(int property, string propertyName, string propertyMatch)
        {
            string key = string.Empty;
            string value = string.Empty;

            if (string.IsNullOrEmpty(propertyName) && property == 1)
            {
                key = "@Message";
            }
            else if (!string.IsNullOrEmpty(propertyName))
            {
                key = propertyName.Trim();
            }

            if (!string.IsNullOrEmpty(key))
            {
                value = string.IsNullOrWhiteSpace(propertyMatch) ? string.Empty : propertyMatch.Trim();
            }

            return new KeyValuePair<string, string>(key, value);
        }

        /// <summary>
        /// Return a human readable match expression, given a list of match rules
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static string MatchConditions(Dictionary<string, string> properties)
        {
            int count = 0;
            string condition = string.Empty;
            foreach (KeyValuePair<string, string> property in properties)
            {
                count++;
                string propValue = "'" + property.Value + "'";
                if (string.IsNullOrEmpty(property.Value))
                {
                    propValue = "ANY value";
                }

                if (count == 1)
                {
                    condition = property.Key + " contains " + propValue;
                }
                else if (!string.IsNullOrEmpty(property.Key))
                {
                    condition = condition + " AND " + property.Key + " contains " + propValue;
                }
            }

            return condition;
        }

        /// <summary>
        /// Validate whether a case-insensitive match can be obtained
        /// </summary>
        /// <param name="text"></param>
        /// <param name="matchText"></param>
        /// <returns></returns>
        public static bool Matches(string text, string matchText)
        {
            if (string.IsNullOrEmpty(matchText))
            {
                return true;
            }
            else
            {
                return text.IndexOf(matchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
    }
}
