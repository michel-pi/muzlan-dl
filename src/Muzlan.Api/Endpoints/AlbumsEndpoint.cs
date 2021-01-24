using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

using Muzlan.Api.Utilities;

namespace Muzlan.Api.Endpoints
{
    public class AlbumsEndpoint : MuzlanEndpoint
    {
        internal AlbumsEndpoint(Uri baseUri, HttpClient client, HtmlParser parser) : base(baseUri, client, parser)
        {
        }

        public async ValueTask<MuzlanResponse<IList<AlbumRecord>>> FindAlbums(string artist, CancellationToken token = default)
        {
            var pageUri = new Uri($"https://{_baseUri.DnsSafeHost}/artist/{Uri.EscapeDataString(artist)}/all-albums");

            try
            {
                var content = await _client.TryGetStringAsync(pageUri, token).ConfigureAwait(false);

                using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

                var albums = new List<AlbumRecord>();

                foreach (var item in document.QuerySelectorAll<IHtmlDivElement>("div.page-content div.row div.item"))
                {
                    var mediaImage = item.QuerySelector<IHtmlImageElement>("div.item-media a.item-media-content img");
                    var mediaTitle = item.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-title a");

                    var imageUri = new Uri($"https://{_baseUri.DnsSafeHost}{mediaImage.Dataset["original"]}");

                    var albumName = mediaTitle.Text.Trim();
                    var albumUri = new Uri($"https://{_baseUri.DnsSafeHost}{mediaTitle.PathName}");

                    var album = new AlbumRecord(artist, albumName, albumUri, imageUri);

                    albums.Add(album);
                }

                return MuzlanResponse<IList<AlbumRecord>>.FromResult(albums, pageUri);
            }
            catch (Exception ex)
            {
                return MuzlanResponse<IList<AlbumRecord>>.FromException(ex, pageUri);
            }
        }

        public async ValueTask<MuzlanResponse<IList<TrackRecord>>> GetTracks(string artist, string album, CancellationToken token = default)
        {
            var pageUri = new Uri($"https://{_baseUri.DnsSafeHost}/artist/{Uri.EscapeDataString(artist)}/album/{Uri.EscapeDataString(album)}");

            try
            {
                var content = await _client.TryGetStringAsync(pageUri, token).ConfigureAwait(false);

                using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

                var tracks = new List<TrackRecord>();

                foreach (var item in document.QuerySelectorAll<IHtmlDivElement>("div.page-content div.row-col div.row.item-list div.item.track"))
                {
                    var trackItem = item.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-title a");
                    var authorItem = item.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-author a");

                    var trackName = trackItem.Text.Trim();
                    var trackUri = new Uri($"https://{_baseUri.DnsSafeHost}{trackItem.PathName}");

                    var trackDownload = new Uri($"https://{_baseUri.DnsSafeHost}{item.Dataset["src"]}");

                    var artistName = authorItem.Text.Trim();

                    var track = new TrackRecord(trackName, artistName, trackUri, trackDownload);

                    tracks.Add(track);
                }

                return MuzlanResponse<IList<TrackRecord>>.FromResult(tracks, pageUri);
            }
            catch (Exception ex)
            {
                return MuzlanResponse<IList<TrackRecord>>.FromException(ex, pageUri);
            }
        }
    }
}
