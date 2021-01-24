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
    public class MetaEndpoint : MuzlanEndpoint
    {
        internal MetaEndpoint(Uri baseUri, HttpClient client, HtmlParser parser) : base(baseUri, client, parser)
        {
        }

        public async ValueTask<MuzlanResponse<IList<ArtistRecord>>> ListPopularArtists(CancellationToken token = default)
        {
            var pageUri = _baseUri;

            try
            {
                var content = await _client.TryGetStringAsync(pageUri, token).ConfigureAwait(false);

                using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

                var artists = new List<ArtistRecord>();

                foreach (var artistElement in document.QuerySelectorAll<IHtmlDivElement>("div.page-content div.row-col div div.padding div.row div div.item"))
                {
                    var anchor = artistElement.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-title.text-ellipsis a");
                    var image = artistElement.QuerySelector<IHtmlImageElement>("div.item-media a.item-media-content img.lazy.img-responsive");

                    var name = anchor.Text.Trim();

                    var artistUri = new Uri($"https://{_baseUri.DnsSafeHost}/artist/{Uri.EscapeDataString(name)}");
                    var searchUri = new Uri($"https://{_baseUri.DnsSafeHost}{anchor.PathName}");
                    var imageUri = new Uri($"https://{_baseUri.DnsSafeHost}{image.Dataset["original"]}");

                    var artist = new ArtistRecord(name, artistUri, searchUri, imageUri);

                    artists.Add(artist);
                }

                return MuzlanResponse<IList<ArtistRecord>>.FromResult(artists, pageUri);
            }
            catch (Exception ex)
            {
                return MuzlanResponse<IList<ArtistRecord>>.FromException(ex, pageUri);
            }
        }

        public async ValueTask<MuzlanResponse<IList<SearchQueryRecord>>> ListRecentSearches(CancellationToken token = default)
        {
            var pageUri = new Uri($"https://{_baseUri.DnsSafeHost}/last-queries");

            try
            {
                var content = await _client.TryGetStringAsync(pageUri, token).ConfigureAwait(false);

                using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

                var searches = new List<SearchQueryRecord>();

                foreach (var searchElement in document.QuerySelectorAll<IHtmlAnchorElement>("div.page-content div.item-meta a.btn"))
                {
                    var name = searchElement.Text.Trim();
                    var searchUri = new Uri($"https://{_baseUri.DnsSafeHost}{searchElement.PathName}");

                    var search = new SearchQueryRecord(name, searchUri);

                    searches.Add(search);
                }

                return MuzlanResponse<IList<SearchQueryRecord>>.FromResult(searches, pageUri);
            }
            catch (Exception ex)
            {
                return MuzlanResponse<IList<SearchQueryRecord>>.FromException(ex, pageUri);
            }
        }
    }
}
