using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Html.Parser;

using Muzlan.Api.Utilities;

namespace Muzlan.Api.Endpoints
{
    public class AuthEndpoint : MuzlanEndpoint
    {
        private static readonly Regex _csrfRegex = new Regex(
            "<meta name=\"csrf-token\" content=\"(.+?)\">",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Uri _firstServerUri = new Uri("https://s1.muzlan.com/api.php?action=check");
        private static readonly Uri _secondServerUri = new Uri("https://s2.muzlan.com/api.php?action=check");

        private readonly Uri _mediaTokenUri;

        public bool? ServerOnline { get; private set; }

        public string? CsrfToken { get; private set; }
        public string? MediaToken { get; private set; }

        internal AuthEndpoint(Uri baseUri, HttpClient client, HtmlParser parser) : base(baseUri, client, parser)
        {
            _mediaTokenUri = new Uri($"https://{baseUri.DnsSafeHost}/ajax/get-token");
        }

        public async ValueTask<MuzlanResponse<AuthRecord>> Authenticate(CancellationToken token = default)
        {
            ServerOnline = null;
            CsrfToken = null;
            MediaToken = null;

            var pageUri = _firstServerUri;

            try
            {
                var online = await CheckServerResponse(pageUri, token).ConfigureAwait(false);

                if (!online)
                {
                    pageUri = _secondServerUri;

                    online = await CheckServerResponse(pageUri, token).ConfigureAwait(false);

                    if (!online)
                    {
                        ServerOnline = false;

                        throw new HttpRequestException("Authentication servers seem to be unavailable.", null, HttpStatusCode.NoContent);
                    }
                }

                ServerOnline = true;

                pageUri = _baseUri;

                CsrfToken = await GetCsrfToken(pageUri, token).ConfigureAwait(false);

                pageUri = _mediaTokenUri;

                MediaToken = await GetMediaToken(pageUri, CsrfToken, token).ConfigureAwait(false);

                return MuzlanResponse<AuthRecord>.FromResult(new AuthRecord(CsrfToken, MediaToken), pageUri);
            }
            catch (Exception ex)
            {
                return MuzlanResponse<AuthRecord>.FromException(ex, pageUri);
            }
        }

        private async ValueTask<string> GetCsrfToken(Uri uri, CancellationToken token = default)
        {
            var content = await _client.TryGetStringAsync(uri, token).ConfigureAwait(false);

            var matchCsrf = _csrfRegex.Match(content);

            if (!matchCsrf.Success)
            {
                throw new HttpRequestException("Failed to read csrf token from page.", null, HttpStatusCode.Unauthorized);
            }

            return matchCsrf.Groups[1].Value;
        }

        private async ValueTask<string> GetMediaToken(Uri uri, string csrfToken, CancellationToken token = default)
        {
            var formData = new KeyValuePair<string?, string?>[]
            {
                new KeyValuePair<string?, string?>("_csrf", csrfToken)
            };

            var formContent = new FormUrlEncodedContent(formData);

            using var response = await _client.PostAsync(
                uri,
                formContent,
                token).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(content))
            {
                throw new HttpRequestException("Media token endpoint did not return any data.", null, HttpStatusCode.NoContent);
            }

            return content;
        }

        private async ValueTask<bool> CheckServerResponse(Uri url, CancellationToken token = default)
        {
            using var response = await _client.GetAsync(url, token).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            return !string.IsNullOrEmpty(content)
                && string.Equals(content, "ok", StringComparison.OrdinalIgnoreCase);
        }
    }
}
