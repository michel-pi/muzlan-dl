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

using Muzlan.Api.Types;
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

        public async ValueTask<IList<MuzlanTrack>> FindTracks(string query, CancellationToken token = default)
        {
            var pagination = new HttpPagination(_baseUri, _client, _parser);

            var tracks = new List<MuzlanTrack>();

            await foreach (var page in pagination.Follow($"https://{_baseUri.DnsSafeHost}/search/{Uri.EscapeDataString(query)}", token))
            {
                await foreach (var track in EnumerateTracks(page, token))
                {
                    tracks.Add(track);
                }
            }

            return tracks;
        }

        public async ValueTask<IList<MuzlanTrack>> FindTracks(string query, int page, CancellationToken token = default)
        {
            var content = await _client.GetStringAsync(
                $"https://{_baseUri.DnsSafeHost}/search/{Uri.EscapeDataString(query)}/p/{page}",
                token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(content))
            {
                throw MuzlanException.ForEmptyResponse();
            }

            var tracks = new List<MuzlanTrack>();

            await foreach (var track in EnumerateTracks(content, token))
            {
                tracks.Add(track);
            }

            return tracks;
        }

        public async ValueTask<IList<MuzlanSearchArtist>> FindArtists(string query, CancellationToken token = default)
        {
            var content = await _client.GetStringAsync(
                $"https://{_baseUri.DnsSafeHost}/search/{Uri.EscapeDataString(query)}",
                token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(content))
            {
                throw MuzlanException.ForEmptyResponse();
            }

            using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

            var artists = new List<MuzlanSearchArtist>();

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

                var tags = new List<MuzlanTag>();

                foreach (var tagElement in artistElement.QuerySelectorAll<IHtmlAnchorElement>("div.item-info div.item-meta a.btn"))
                {
                    var tagName = tagElement.Text.Trim();
                    var tagUri = new Uri($"https://{_baseUri.DnsSafeHost}{tagElement.PathName}");

                    tags.Add(new MuzlanTag(tagName, tagUri));
                }

                var artist = new MuzlanArtist(artistName, artistUri, artistSearchUri, artistImageUri);

                artists.Add(new MuzlanSearchArtist(artist, tags, description));
            }

            return artists;
        }

        private async IAsyncEnumerable<MuzlanTrack> EnumerateTracks(string content, [EnumeratorCancellation] CancellationToken token = default)
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

                yield return new MuzlanTrack(
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
