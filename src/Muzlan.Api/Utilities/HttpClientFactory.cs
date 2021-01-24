using System;
using System.Net;
using System.Net.Http;

namespace Muzlan.Api.Utilities
{
    internal static class HttpClientFactory
    {
        public static HttpClient Create(MuzlanClientConfig config)
        {
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.All,
                CookieContainer = new CookieContainer(),
                UseCookies = true
            };

            if (config.NetworkUsername != null)
            {
                handler.Credentials = new NetworkCredential(config.NetworkUsername, config.NetworkPassword);
                handler.PreAuthenticate = true;
            }

            if (!string.IsNullOrEmpty(config.ProxyAddress))
            {
                handler.Proxy = new WebProxy(config.ProxyAddress);

                if (config.ProxyUsername != null)
                {
                    handler.Proxy.Credentials = new NetworkCredential(config.ProxyUsername, config.ProxyPassword);
                }

                handler.UseProxy = true;
            }

            var client = new HttpClient(handler, true)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            client.DefaultRequestHeaders.UserAgent.ParseAdd(config.UserAgent);

            return client;
        }
    }
}
