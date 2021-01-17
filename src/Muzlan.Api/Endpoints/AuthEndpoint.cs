using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Html.Parser;

namespace Muzlan.Api.Endpoints
{
    public class AuthEndpoint : MuzlanEndpoint
    {
        private static readonly Regex _csrfRegex = new Regex(
            "<meta name=\"csrf-token\" content=\"(.+?)\">",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public bool? ServerOnline { get; private set; }

        public string? CsrfToken { get; private set; }
        public string? MediaToken { get; private set; }

        internal AuthEndpoint(Uri baseUri, HttpClient client, HtmlParser parser) : base(baseUri, client, parser)
        {
        }

        public async ValueTask<bool> Authenticate(CancellationToken token = default)
        {
            ServerOnline = null;
            CsrfToken = null;
            MediaToken = null;

            var online = await CheckServers(token).ConfigureAwait(false);

            ServerOnline = online;

            if (!online)
            {
                return false;
            }

            await RefreshMediaToken(token).ConfigureAwait(false);

            return !string.IsNullOrEmpty(CsrfToken) && !string.IsNullOrEmpty(MediaToken);
        }

        public async ValueTask RefreshMediaToken(CancellationToken token = default)
        {
            string content;

            MediaToken = null;

            if (ServerOnline != true)
            {
                throw new InvalidOperationException("Muzlan servers seem to be unavailable or an authorization hasn't been started.");
            }

            if (string.IsNullOrEmpty(CsrfToken))
            {
                content = await _client.GetStringAsync(_baseUri, token).ConfigureAwait(false);

                if (string.IsNullOrEmpty(content))
                {
                    throw MuzlanException.ForEmptyResponse();
                }

                var match = _csrfRegex.Match(content);

                if (!match.Success)
                {
                    throw new MuzlanException("Failed to find the required csrf token.");
                }

                CsrfToken = match.Groups[1].Value;
            }

            var formData = new KeyValuePair<string?, string?>[]
            {
                new KeyValuePair<string?, string?>("_csrf", CsrfToken)
            };
            var formContent = new FormUrlEncodedContent(formData);

            using var response = await _client.PostAsync(
                $"https://{_baseUri.DnsSafeHost}/ajax/get-token",
                formContent,
                token).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new MuzlanException("Failed to get media token for current session.");
            }

            content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(content))
            {
                throw MuzlanException.ForEmptyResponse();
            }

            MediaToken = content;
        }

        private async ValueTask<bool> CheckServers(CancellationToken token = default)
        {
            var firstTask = CheckServerResponse("https://s1.muzlan.com/api.php?action=check", token).ConfigureAwait(false);
            var secondTask = CheckServerResponse("https://s2.muzlan.com/api.php?action=check", token).ConfigureAwait(false);

            ServerOnline = await firstTask || await secondTask;

            return ServerOnline.Value;
        }

        private async ValueTask<bool> CheckServerResponse(string url, CancellationToken token = default)
        {
            using var response = await _client.GetAsync(url, token).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK) return false;

            var content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            return !string.IsNullOrEmpty(content)
                && string.Equals(content, "ok", StringComparison.OrdinalIgnoreCase);
        }
    }
}
