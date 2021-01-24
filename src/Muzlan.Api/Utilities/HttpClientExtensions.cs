using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Muzlan.Api.Utilities
{
    internal static class HttpClientExtensions
    {
        public static async ValueTask<string> TryGetStringAsync(this HttpClient client, Uri uri, CancellationToken cancellationToken = default)
        {
            using var response = await FollowRedirects(client, uri, cancellationToken).ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var rateLimit = response.RequestMessage?.RequestUri?.AbsolutePath == "/vip";

            if (rateLimit)
            {
                throw new HttpRequestException("Rate limit hit.", null, HttpStatusCode.PaymentRequired);
            }

            return content;
        }

        private static async ValueTask<HttpResponseMessage> FollowRedirects(HttpClient client, Uri uri, CancellationToken cancellationToken = default)
        {
            var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            while (IsRedirect(response))
            {
                var location = response.Headers.Location;

                if (location == null) break;

                response.Dispose();

                if (location.AbsolutePath == "/vip")
                {
                    throw new HttpRequestException("Rate limit hit.", null, HttpStatusCode.PaymentRequired);
                }

                response = await client.GetAsync(location, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }

        private static bool IsRedirect(HttpResponseMessage response)
        {
            return response.StatusCode switch
            {
                HttpStatusCode.Moved
                or HttpStatusCode.Redirect
                or HttpStatusCode.RedirectMethod
                or HttpStatusCode.RedirectKeepVerb
                or HttpStatusCode.PermanentRedirect => true,
                _ => false,
            };
        }
    }
}
