using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

using Muzlan.Api.Types;

namespace Muzlan.Api.Endpoints
{
    public class MetaEndpoint : MuzlanEndpoint
    {
        internal MetaEndpoint(Uri baseUri, HttpClient client, HtmlParser parser) : base(baseUri, client, parser)
        {
        }

        public async ValueTask<IList<MuzlanArtist>> ListPopularArtists(CancellationToken token = default)
        {
            var content = await _client.GetStringAsync(_baseUri, token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(content))
            {
                throw MuzlanException.ForEmptyResponse();
            }

            using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

            var artists = new List<MuzlanArtist>();

            foreach (var artistElement in document.QuerySelectorAll<IHtmlDivElement>("div.page-content div.row-col div div.padding div.row div div.item"))
            {
                var anchor = artistElement.QuerySelector<IHtmlAnchorElement>("div.item-info div.item-title.text-ellipsis a");
                var image = artistElement.QuerySelector<IHtmlImageElement>("div.item-media a.item-media-content img.lazy.img-responsive");

                var name = anchor.Text.Trim();

                var artistUri = new Uri($"https://{_baseUri.DnsSafeHost}/artist/{Uri.EscapeDataString(name)}");
                var searchUri = new Uri($"https://{_baseUri.DnsSafeHost}{anchor.PathName}");
                var imageUri = new Uri($"https://{_baseUri.DnsSafeHost}{image.Dataset["original"]}");

                var artist = new MuzlanArtist(name, artistUri, searchUri, imageUri);

                artists.Add(artist);
            }

            return artists;
        }

        public async ValueTask<IList<MuzlanSearchQuery>> ListRecentSearches(CancellationToken token = default)
        {
            var content = await _client.GetStringAsync($"https://{_baseUri.DnsSafeHost}/last-queries", token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(content))
            {
                throw MuzlanException.ForEmptyResponse();
            }

            using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

            var searches = new List<MuzlanSearchQuery>();

            foreach (var searchElement in document.QuerySelectorAll<IHtmlAnchorElement>("div.page-content div.item-meta a.btn"))
            {
                var name = searchElement.Text.Trim();
                var searchUri = new Uri($"https://{_baseUri.DnsSafeHost}{searchElement.PathName}");

                var search = new MuzlanSearchQuery(name, searchUri);

                searches.Add(search);
            }

            return searches;
        }
    }
}
