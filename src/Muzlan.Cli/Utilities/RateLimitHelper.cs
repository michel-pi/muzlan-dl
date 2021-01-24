using System;
using System.Threading;
using System.Threading.Tasks;

using Muzlan.Api;

namespace Muzlan.Cli.Utilities
{
    public static class RateLimitHelper
    {
        public static async ValueTask<MuzlanResponse<T>> BypassRateLimitAsync<T>(
            DefaultOptionsHandler handler,
            Func<MuzlanClient, ValueTask<MuzlanResponse<T>>> action,
            CancellationToken cancellationToken = default)
        {
            var response = await action(handler.GetCurrentMuzlanClient()).ConfigureAwait(false);

            if (handler.Proxies == null) return response;

            while (response.IsRateLimit() && !handler.Proxies.HasReachedEnd)
            {
                var client = handler.GetNextMuzlanClient();

                await client.Auth.Authenticate(cancellationToken).ConfigureAwait(false);

                response = await action(client).ConfigureAwait(false);
            }

            return response;
        }
    }
}
