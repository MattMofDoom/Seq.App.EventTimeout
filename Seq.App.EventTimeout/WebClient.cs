using Flurl.Http;
using Flurl.Http.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Seq.App.EventTimeout
{
    /// <summary>
    /// HTTP Client for retrieving Holidays API
    /// </summary>
    public class ApiClient : DefaultHttpClientFactory
    {
        private string AppName { get; set; }
        private bool UseProxy { get; set; }
        private WebProxy Proxy { get; set; }

        /// <summary>
        /// API client message handler
        /// </summary>
        /// <returns></returns>
        public override HttpMessageHandler CreateMessageHandler()
        {
            if (UseProxy)
            {
                return new HttpClientHandler { Proxy = Proxy, UseProxy = UseProxy, UseDefaultCredentials = true, AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            }
            else
            {
                return new HttpClientHandler { UseDefaultCredentials = true, AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            }
        }

        /// <summary>
        /// API Client HttpClient
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public override HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            HttpClient httpClient = base.CreateHttpClient(handler);
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(AppName);
            httpClient.Timeout = new TimeSpan(0, 2, 0);
            httpClient.MaxResponseContentBufferSize = 262144;

            return httpClient;
        }

        /// <summary>
        /// ApiClient instance
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="useProxy"></param>
        /// <param name="proxy"></param>
        /// <param name="proxyUser"></param>
        /// <param name="proxyPass"></param>
        /// <param name="proxyBypass"></param>
        /// <param name="localUrls"></param>
        public ApiClient(string appName, bool useProxy, string proxy = null, string proxyUser = null, string proxyPass = null, bool proxyBypass = false, string[] localUrls = null)
        {
            AppName = appName;
            if (useProxy && !string.IsNullOrEmpty(proxy))
            {
                UseProxy = useProxy;
                Proxy = new WebProxy
                {
                    Address = new Uri(proxy),
                    BypassProxyOnLocal = proxyBypass,
                    BypassList = localUrls,
                    UseDefaultCredentials = false
                };

                if (!string.IsNullOrEmpty(proxyUser) && !string.IsNullOrEmpty(proxyPass))
                {
                    Proxy.Credentials = new NetworkCredential(proxyUser, proxyPass);
                }
                else
                {
                    Proxy.UseDefaultCredentials = true;
                }
            }
            else
            {
                UseProxy = false;
            }
        }
    }

    public static class WebClient
    {
        /// <summary>
        /// Configure Flurl.Http to use an ApiClient, given the configured parameters
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="useProxy"></param>
        /// <param name="proxy"></param>
        /// <param name="proxyUser"></param>
        /// <param name="proxyPass"></param>
        /// <param name="proxyBypass"></param>
        /// <param name="localUrls"></param>
        public static void SetFlurlConfig(string appName, bool useProxy, string proxy = null, string proxyUser = null, string proxyPass = null, bool proxyBypass = false, string[] localUrls = null)
        {
            FlurlHttp.Configure(config => { config.HttpClientFactory = new ApiClient(appName, useProxy, proxy, proxyUser, proxyPass, proxyBypass, localUrls); });
        }

        /// <summary>
        /// Return an AbstractAPI Holidays API URL, given an API key, country, and date
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="country"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string GetUrl(string apiKey, string country, DateTime date)
        {
            return string.Concat("https://holidays.abstractapi.com/v1/?api_key=", apiKey, "&country=", country, "&year=", date.Year, "&month=", date.Month, "&day=", date.Day);
        }

        /// <summary>
        /// Retrieve an AbstractAPI Holidays API result, given API key, country, and date
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
