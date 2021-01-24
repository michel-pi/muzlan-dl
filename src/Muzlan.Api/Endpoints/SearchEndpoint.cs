using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

using Muzlan.Api.Utilities;

namespace Muzlan.Api.Endpoints
{
    public class SearchEndpoint : MuzlanEndpoint
    {
        private static readonly Regex _bgImageRegex = new Regex(
            @"background-image: url\('(.+?)'\);",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        internal SearchEndpoint(Uri baseUri, HttpClient client, HtmlParser parser) : base(baseUri, client, parser)
        {
        }

        public async ValueTask<MuzlanResponse<IList<TrackRecord>>> FindTracks(
            Uri pageUri,
            int? limitItems = null,
            int? limitPages = null,
            CancellationToken cancellationToken = default)
        {
            var pagination = new MuzlanPagination(pageUri, _client, _parser);

            var tracks = new List<TrackRecord>();

            try
            {
                bool reachedLimit = false;
                int itemCount = 0;

                await foreach (var pageContent in pagination.FollowForward(cancellationToken).ConfigureAwait(false))
                {
                    reachedLimit = limitPages > 0 && pagination.PageCount >= limitPages;

                    await foreach (var track in EnumerateTracks(pageContent, cancellationToken).ConfigureAwait(false))
                    {
                        tracks.Add(track);

                        itemCount++;

                        reachedLimit = limitItems > 0 && itemCount >= limitItems;
                    }

                    if (reachedLimit) break;
                }

                return MuzlanResponse<IList<TrackRecord>>.FromResult(tracks, pagination.PageUri);
            }
            catch (Exception ex)
            {
                if (pagination.PageNumber == -1)
                {
                    return MuzlanResponse<IList<TrackRecord>>.FromException(ex, pagination.PageUri, pagination.PageNumber);
                }

                return MuzlanResponse<IList<TrackRecord>>.FromPartialResult(tracks, pagination.PageUri, pagination.PageNumber);
            }
        }

        public ValueTask<MuzlanResponse<IList<TrackRecord>>> FindTracks(
            string query,
            int? limitItems = null,
            int? limitPages = null,
            CancellationToken cancellationToken = default)
        {
            return FindTracks(
                new Uri($"https://{_baseUri.DnsSafeHost}/search/{Uri.EscapeDataString(query)}"),
                limitItems,
                limitPages,
                cancellationToken);
        }

        public ValueTask<MuzlanResponse<IList<TrackRecord>>> FindTracks(
            string query,
            int page,
            int? limitItems = null,
            int? limitPages = null,
            CancellationToken cancellationToken = default)
        {
            return FindTracks(
                new Uri($"https://{_baseUri.DnsSafeHost}/search/{Uri.EscapeDataString(query)}/p/{page}"),
                limitItems,
                limitPages,
                cancellationToken);
        }

        public async ValueTask<MuzlanResponse<IList<SearchArtistRecord>>> FindArtists(
            string query,
            CancellationToken token = default)
        {
            var pageUri = new Uri($"https://{_baseUri.DnsSafeHost}/search/{Uri.EscapeDataString(query)}");

            try
            {
                var content = await _client.TryGetStringAsync(
                    pageUri,
                    token).ConfigureAwait(false);

                using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

                var artists = new List<SearchArtistRecord>();

                foreach (var artistElement in document.QuerySelectorAll<IHtmlDivElement>("div.page-content div.row-col div.row.item-list:not(#search-result-items)"))
                {
                    var imageElement = artistElement.QuerySelector<IHtmlAnchorElement>("div.item-media a.item-media-content");
                    var titleElement = artistElement.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-title.text-ellipsis a");
                    var descriptionElement = artistElement.QuerySelector<IHtmlDivElement>("div.item-info div.item-except");

                    var artistName = titleElement.Text.Trim();
                    var artistUri = new Uri($"https://{_baseUri.DnsSafeHost}/artist/{Uri.EscapeDataString(query)}");
                    var artistSearchUri = new Uri($"https://{_baseUri.DnsSafeHost}/search/{Uri.EscapeDataString(query)}");

                    var match = _bgImageRegex.Match(imageElement.OuterHtml);

                    var artistImageUri = new Uri($"https://{_baseUri.DnsSafeHost}{match.Groups[1].Value}");

                    var description = string.IsNullOrEmpty(descriptionElement.TextContent)
                        ? string.Empty
                        : descriptionElement.TextContent.Trim();

                    var tags = new List<TagRecord>();

                    foreach (var tagElement in artistElement.QuerySelectorAll<IHtmlAnchorElement>("div.item-info div.item-meta a.btn"))
                    {
                        var tagName = tagElement.Text.Trim();
                        var tagUri = new Uri($"https://{_baseUri.DnsSafeHost}{tagElement.PathName}");

                        tags.Add(new TagRecord(tagName, tagUri));
                    }

                    var artist = new ArtistRecord(artistName, artistUri, artistSearchUri, artistImageUri);

                    artists.Add(new SearchArtistRecord(artist, tags, description));
                }

                return MuzlanResponse<IList<SearchArtistRecord>>.FromResult(artists, pageUri);
            }
            catch (Exception ex)
            {
                return MuzlanResponse<IList<SearchArtistRecord>>.FromException(ex, pageUri);
            }
        }

        private async IAsyncEnumerable<TrackRecord> EnumerateTracks(string content, [EnumeratorCancellation] CancellationToken token = default)
        {
            using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

            foreach (var trackElement in document.QuerySelectorAll<IHtmlDivElement>("div.page-content div#search-result-items.row.item-list div div div.item.track"))
            {
                var artistAnchor = trackElement.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-author a");

                var trackSource = trackElement.Dataset["src"];

                var (trackName, trackPath) = FindTrackInfo(trackElement);

                var trackUri = new Uri($"https://{_baseUri.DnsSafeHost}{trackPath}");

                var artistName = artistAnchor.Text.Trim();

                var downloadUri = new Uri($"https://{_baseUri.DnsSafeHost}{trackSource}");

                yield return new TrackRecord(
                    trackName,
                    artistName,
                    trackUri,
                    downloadUri);
            }
        }

        private static (string trackName, string trackPath) FindTrackInfo(IHtmlDivElement parent)
        {
            var trackAnchor = parent.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-title a");

            if (trackAnchor != null)
            {
                return (trackAnchor.Text.Trim(), trackAnchor.PathName);
            }

            var trackOverlayInfo = parent.QuerySelector<IHtmlDivElement>("div.item-info div.item-title");

            return (trackOverlayInfo.TextContent.Trim(), $"/track/{parent.Dataset["id"]}");
        }
    }
}
