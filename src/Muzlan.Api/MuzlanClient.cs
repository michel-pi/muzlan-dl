using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

/* 
 * Rate Limits:
 * - every page will redirect to /vip
 * - html should contain this in div.page-content
 * <div id="w0" class="alert-danger alert fade in"><button type="button" class="close" data-dismiss="alert" aria-hidden="true">×</button><strong>The limit of such actions per day is reached! You can remove this restriction on this page or retry tomorrow</strong></div>
 * 
 * TODO: Null checks for QuerySelector
 * TODO: MuzlanException
 * TODO: Sitemap (https://muzlan.top/sitemap/artists, https://muzlan.top/sitemap/tracks, https://muzlan.top/sitemap/videos)
 * TODO: Artist Endpoints (/artist/{Name}/all-albums, /artist/{Name}/top-albums)
 * TODO: Proxy rotation/balancing when rate limit hit
 * TODO: Import of cookies to support premium
 * TODO: Premium client?
 */

namespace Muzlan.Api
{
    public class MuzlanClient : IDisposable
    {
        private static readonly Regex _csrfRegex = new Regex(
            "<meta name=\"csrf-token\" content=\"(.+?)\">",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex _bgImageRegex = new Regex(
            @"background-image: url\('(.+?)'\);",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private bool disposedValue;

        private readonly HttpClient _client;
        private readonly HtmlParser _parser;

        public Uri BaseUri { get; }

        public bool? ServerOnline { get; private set; }

        public string? CsrfToken { get; private set; }
        public string? MediaToken { get; private set; }

        public bool IsAvailable => ServerOnline == true;
        public bool IsAuthenticated => !string.IsNullOrEmpty(CsrfToken) && !string.IsNullOrEmpty(MediaToken);

        public MuzlanClient() : this(MuzlanClientConfig.Default)
        {
        }

        public MuzlanClient(MuzlanClientConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            BaseUri = new Uri(config.Host, UriKind.Absolute);

            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = true,
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

            _client = new HttpClient(handler, true)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            _client.DefaultRequestHeaders.UserAgent.ParseAdd(config.UserAgent);

            _parser = new HtmlParser();
        }

        ~MuzlanClient()
        {
            Dispose(false);
        }

        public async ValueTask<IList<MuzlanTrack>> SearchTracks(string query, int page, CancellationToken token = default)
        {
            if (!IsAvailable) throw new InvalidOperationException("Muzlan servers unavailable.");
            if (!IsAuthenticated) throw new InvalidOperationException("Authenticate with muzlan servers before calling any methods.");

            using var response = await _client.GetAsync(
                $"https://{BaseUri.DnsSafeHost}/search/{Uri.EscapeDataString(query)}/p/{page}",
                token).ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

            var tracks = new List<MuzlanTrack>();

            foreach (var trackElement in document.QuerySelectorAll<IHtmlDivElement>("div.page-content div#search-result-items.row.item-list div div div.item.track"))
            {
                var trackAnchor = trackElement.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-title a");
                var artistAnchor = trackElement.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-author a");

                var trackSource = trackElement.Dataset["src"];

                var trackName = trackAnchor.Text.Trim();
                var trackUri = new Uri($"https://{BaseUri.DnsSafeHost}{trackAnchor.PathName}");

                var artistName = artistAnchor.Text.Trim();

                var downloadUri = new Uri($"https://{BaseUri.DnsSafeHost}{trackSource}?key={MediaToken}");

                var track = new MuzlanTrack(trackName, artistName, trackUri, downloadUri);

                tracks.Add(track);
            }

            return tracks;
        }

        public async ValueTask<IList<MuzlanTrack>> SearchTracks(string query, CancellationToken token = default)
        {
            if (!IsAvailable) throw new InvalidOperationException("Muzlan servers unavailable.");
            if (!IsAuthenticated) throw new InvalidOperationException("Authenticate with muzlan servers before calling any methods.");

            var tracks = new List<MuzlanTrack>();

            foreach (var page in await DownloadTrackPages(query, token).ConfigureAwait(false))
            {
                using var document = await _parser.ParseDocumentAsync(page, token).ConfigureAwait(false);

                foreach (var trackElement in document.QuerySelectorAll<IHtmlDivElement>("div.page-content div#search-result-items.row.item-list div div div.item.track"))
                {
                    var trackAnchor = trackElement.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-title a");
                    var artistAnchor = trackElement.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-author a");

                    var trackSource = trackElement.Dataset["src"];

                    var trackName = trackAnchor.Text.Trim();
                    var trackUri = new Uri($"https://{BaseUri.DnsSafeHost}{trackAnchor.PathName}");

                    var artistName = artistAnchor.Text.Trim();

                    var downloadUri = new Uri($"https://{BaseUri.DnsSafeHost}{trackSource}?key={MediaToken}");

                    var track = new MuzlanTrack(trackName, artistName, trackUri, downloadUri);

                    tracks.Add(track);
                }
            }

            return tracks;
        }

        public async ValueTask<IList<MuzlanSearchArtist>> SearchArtists(string query, CancellationToken token = default)
        {
            if (!IsAvailable) throw new InvalidOperationException("Muzlan servers unavailable.");
            if (!IsAuthenticated) throw new InvalidOperationException("Authenticate with muzlan servers before calling any methods.");

            using var response = await _client.GetAsync($"https://{BaseUri.DnsSafeHost}/search/{Uri.EscapeDataString(query)}", token).ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

            var artists = new List<MuzlanSearchArtist>();

            foreach (var artistElement in document.QuerySelectorAll<IHtmlDivElement>("div.page-content div.row-col div.row.item-list:not(#search-result-items)"))
            {
                var imageElement = artistElement.QuerySelector<IHtmlAnchorElement>("div.item-media a.item-media-content");
                var titleElement = artistElement.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-title.text-ellipsis a");
                var descriptionElement = artistElement.QuerySelector<IHtmlDivElement>("div.item-info div.item-except");

                var tagElements = artistElement.QuerySelectorAll<IHtmlAnchorElement>("div.item-info div.item-meta a.btn");

                var artistName = titleElement.Text.Trim();
                var artistUri = new Uri($"https://{BaseUri.DnsSafeHost}/artist/{Uri.EscapeDataString(query)}");
                var artistSearchUri = new Uri($"https://{BaseUri.DnsSafeHost}/search/{Uri.EscapeDataString(query)}");

                var match = _bgImageRegex.Match(imageElement.OuterHtml);

                var artistImageUri = new Uri($"https://{BaseUri.DnsSafeHost}{match.Groups[1].Value}");

                var description = string.IsNullOrEmpty(descriptionElement.TextContent)
                    ? string.Empty
                    : descriptionElement.TextContent.Trim();

                var tags = new List<MuzlanTag>();

                foreach (var tagElement in tagElements)
                {
                    var tagName = tagElement.Text.Trim();
                    var tagUri = new Uri($"https://{BaseUri.DnsSafeHost}{tagElement.PathName}");

                    tags.Add(new MuzlanTag(tagName, tagUri));
                }

                var artist = new MuzlanArtist(artistName, artistUri, artistSearchUri, artistImageUri);

                artists.Add(new MuzlanSearchArtist(artist, tags, description));
            }

            return artists;
        }

        public async ValueTask<IList<MuzlanSearchQuery>> GetRecentSearches(CancellationToken token = default)
        {
            if (!IsAvailable) throw new InvalidOperationException("Muzlan servers unavailable.");
            if (!IsAuthenticated) throw new InvalidOperationException("Authenticate with muzlan servers before calling any methods.");

            using var response = await _client.GetAsync($"https://{BaseUri.DnsSafeHost}/last-queries", token).ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

            var searches = new List<MuzlanSearchQuery>();

            foreach (var searchElement in document.QuerySelectorAll<IHtmlAnchorElement>("div.page-content div.item-meta a.btn"))
            {
                var name = searchElement.Text.Trim();
                var searchUri = new Uri($"https://{BaseUri.DnsSafeHost}{searchElement.PathName}");

                var search = new MuzlanSearchQuery(name, searchUri);

                searches.Add(search);
            }

            return searches;
        }

        public async ValueTask<IList<MuzlanArtist>> GetPopularArtists(CancellationToken token = default)
        {
            if (!IsAvailable) throw new InvalidOperationException("Muzlan servers unavailable.");
            if (!IsAuthenticated) throw new InvalidOperationException("Authenticate with muzlan servers before calling any methods.");

            using var response = await _client.GetAsync(BaseUri, token).ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

            var artists = new List<MuzlanArtist>();

            foreach (var artistElement in document.QuerySelectorAll<IHtmlDivElement>("div.page-content div.row div.col-xs-4.col-sm-4.col-md-3"))
            {
                var anchor = artistElement.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-title.text-ellipsis a");
                var image = artistElement.QuerySelector<IHtmlImageElement>("div.item-media a.item-media-content img.lazy.img-responsive");

                var name = anchor.Text.Trim();

                var artistUri = new Uri($"https://{BaseUri.DnsSafeHost}/artist/{Uri.EscapeDataString(name)}");
                var searchUri = new Uri($"https://{BaseUri.DnsSafeHost}{anchor.PathName}");
                var imageUri = new Uri($"https://{BaseUri.DnsSafeHost}{image.Dataset["original"]}");

                var artist = new MuzlanArtist(name, artistUri, searchUri, imageUri);

                artists.Add(artist);
            }

            return artists;
        }

        public async ValueTask<bool> Authenticate(CancellationToken token = default)
        {
            ServerOnline = null;
            CsrfToken = null;

            var serverOnline = await CheckServers(token).ConfigureAwait(false);

            if (!serverOnline)
            {
                return false;
            }

            return await RefreshMediaToken(token).ConfigureAwait(false);
        }

        public async ValueTask<bool> RefreshMediaToken(CancellationToken token = default)
        {
            MediaToken = null;

            string content;

            if (!IsAvailable) throw new InvalidOperationException("Muzlan servers unavailable.");

            if (string.IsNullOrEmpty(CsrfToken))
            {
                using var csrfResponse = await _client.GetAsync(BaseUri, token).ConfigureAwait(false);

                if (csrfResponse.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }

                content = await csrfResponse.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                var match = _csrfRegex.Match(content);

                if (!match.Success)
                {
                    return false;
                }

                CsrfToken = match.Groups[1].Value;
            }

            var formData = new KeyValuePair<string?, string?>[]
            {
                new KeyValuePair<string?, string?>("_csrf", CsrfToken)
            };
            var formContent = new FormUrlEncodedContent(formData);

            using var response = await _client.PostAsync($"https://{BaseUri.DnsSafeHost}/ajax/get-token", formContent, token).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(content))
            {
                MediaToken = content;

                return true;
            }
            else
            {
                return false;
            }
        }

        private async ValueTask<bool> CheckServers(CancellationToken token = default)
        {
            var firstTask = CheckServer("https://s1.muzlan.com/api.php?action=check", token).ConfigureAwait(false);
            var secondTask = CheckServer("https://s2.muzlan.com/api.php?action=check", token).ConfigureAwait(false);

            ServerOnline = await firstTask || await secondTask;

            return ServerOnline.Value;
        }

        private async ValueTask<bool> CheckServer(string url, CancellationToken token = default)
        {
            using var response = await _client.GetAsync(url, token).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK) return false;

            var content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            return !string.IsNullOrEmpty(content)
                && string.Equals(content, "ok", StringComparison.OrdinalIgnoreCase);
        }

        private async ValueTask<IList<string>> DownloadTrackPages(string query, CancellationToken token = default)
        {
            var result = new List<string>();

            using var response = await _client.GetAsync($"https://{BaseUri.DnsSafeHost}/search/{Uri.EscapeDataString(query)}", token).ConfigureAwait(false);

            var content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            result.Add(content);

            using var document = await _parser.ParseDocumentAsync(content).ConfigureAwait(false);

            foreach (var pageElement in document.QuerySelectorAll<IHtmlAnchorElement>("div.page-content div ul.pagination li a"))
            {
                if (!int.TryParse(pageElement.Text.Trim(), out var pageNumber)) continue;

                using var pageResponse = await _client.GetAsync(
                    $"https://{BaseUri.DnsSafeHost}/search/{Uri.EscapeDataString(query)}/p/{pageNumber}",
                    token).ConfigureAwait(false);

                content = await pageResponse.Content.ReadAsStringAsync(token).ConfigureAwait(false);

                result.Add(content);
            }

            return result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _client.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
