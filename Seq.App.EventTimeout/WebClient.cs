using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Content;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.EventTimeout
{
    public class SeqClient : DefaultHttpClientFactory
    {
        string appName { get; set; }
        bool useProxy { get; set;  }
        WebProxy proxy { get; set;  }

        public override HttpMessageHandler CreateMessageHandler()
        {
            if (useProxy)
                return new HttpClientHandler { Proxy = proxy, UseProxy = useProxy, UseDefaultCredentials = true, AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            else
                return new HttpClientHandler { UseDefaultCredentials = true, AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
        }

        public override HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            HttpClient httpClient = base.CreateHttpClient(handler);
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(appName);
            httpClient.Timeout = new TimeSpan(0, 2, 0);
            httpClient.MaxResponseContentBufferSize = 262144;

            return httpClient;
        }

        public SeqClient(string AppName, bool UseProxy, string Proxy = null, string ProxyUser = null, string ProxyPass = null, bool ProxyBypass = false, string[] LocalUrls = null)
        {
            appName = AppName;
            if (UseProxy && !string.IsNullOrEmpty(Proxy))
            {
                useProxy = UseProxy;
                proxy = new WebProxy
                {
                    Address = new Uri(Proxy),
                    BypassProxyOnLocal = ProxyBypass,
                    BypassList = LocalUrls,
                    UseDefaultCredentials = false
                };

                if (!string.IsNullOrEmpty(ProxyUser) && !string.IsNullOrEmpty(ProxyPass))
                    proxy.Credentials = new NetworkCredential(ProxyUser, ProxyPass);
                else
                    proxy.UseDefaultCredentials = true;
            }
            else
                useProxy = false;

        }
    }

    public static class WebClient
    {
        public static void setFlurlConfig(string appName, bool useProxy, string proxy = null, string proxyUser = null, string proxyPass = null, bool proxyBypass = false, string[] localUrls = null)
        {
            FlurlHttp.Configure(config => { config.HttpClientFactory = new SeqClient(appName, useProxy, proxy, proxyUser, proxyPass, proxyBypass, localUrls) ; });
        }

        public static string getUrl(string apiKey, string country, DateTime date)
        {
            return string.Concat("https://holidays.abstractapi.com/v1/?api_key=", apiKey, "&country=", country, "&year=", date.Year, "&month=", date.Month, "&day=", date.Day);
        }

        public static async Task<List<AbstractApiHolidays>> getHolidays(string apiKey, string country, DateTime date)
        {
            return await getUrl(apiKey,country,date).GetJsonAsync<List<AbstractApiHolidays>>();
        }

    }
}
