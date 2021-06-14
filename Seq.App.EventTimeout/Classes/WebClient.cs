using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl.Http;

namespace Seq.App.EventTimeout.Classes
{
    /// <summary>
    ///     Web access methods
    /// </summary>
    public static class WebClient
    {
        /// <summary>
        ///     Configure Flurl.Http to use an ApiClient, given the configured parameters
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="useProxy"></param>
        /// <param name="proxy"></param>
        /// <param name="proxyUser"></param>
        /// <param name="proxyPass"></param>
        /// <param name="proxyBypass"></param>
        /// <param name="localUrls"></param>
        public static void SetFlurlConfig(string appName, bool useProxy, string proxy = null, string proxyUser = null,
            string proxyPass = null, bool proxyBypass = false, string[] localUrls = null)
        {
            FlurlHttp.Configure(config =>
            {
                config.HttpClientFactory = new ApiClient(appName, useProxy, proxy, proxyUser, proxyPass,
                    proxyBypass, localUrls);
            });
        }

        /// <summary>
        ///     Return an AbstractAPI Holidays API URL, given an API key, country, and date
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="country"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string GetUrl(string apiKey, string country, DateTime date)
        {
            return string.Concat("https://holidays.abstractapi.com/v1/?api_key=", apiKey, "&country=", country,
                "&year=", date.Year, "&month=", date.Month, "&day=", date.Day);
        }

        /// <summary>
        ///     Retrieve an AbstractAPI Holidays API result, given API key, country, and date
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="country"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static async Task<List<AbstractApiHolidays>> GetHolidays(string apiKey, string country, DateTime date)
        {
            return await GetUrl(apiKey, country, date).GetJsonAsync<List<AbstractApiHolidays>>();
        }
    }
}