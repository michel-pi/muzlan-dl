using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

using Muzlan.Api.Utilities;

namespace Muzlan.Api.Endpoints
{
    public class SitemapEndpoint : MuzlanEndpoint
    {
        internal SitemapEndpoint(Uri baseUri, HttpClient client, HtmlParser parser) : base(baseUri, client, parser)
        {
        }

        public async ValueTask<MuzlanResponse<IList<SitemapRecord>>> GetArtists(CancellationToken token = default)
        {
            var pageUri = new Uri($"{_baseUri.DnsSafeHost}/sitemap/artists");
            var pagination = new MuzlanPagination(pageUri, _client, _parser);

            var items = new List<SitemapRecord>();

            try
            {
                await foreach (var page in pagination.FollowForward(token).ConfigureAwait(false))
                {
                    await foreach (var item in EnumerateSitemapItems(page, token).ConfigureAwait(false))
                    {
                        items.Add(item);
                    }
                }

                return MuzlanResponse<IList<SitemapRecord>>.FromResult(items, pagination.PageUri);
            }
            catch (Exception ex)
            {
                if (pagination.PageNumber == -1)
                {
                    return MuzlanResponse<IList<SitemapRecord>>.FromException(ex, pagination.PageUri);
                }

                return MuzlanResponse<IList<SitemapRecord>>.FromPartialResult(items, pagination.PageUri, pagination.PageNumber);
            }
        }

        public async ValueTask<MuzlanResponse<IList<SitemapRecord>>> GetTracks(CancellationToken token = default)
        {
            var pageUri = new Uri($"{_baseUri.DnsSafeHost}/sitemap/tracks");
            var pagination = new MuzlanPagination(pageUri, _client, _parser);

            var items = new List<SitemapRecord>();

            try
            {
                await foreach (var page in pagination.FollowForward(token).ConfigureAwait(false))
                {
                    await foreach (var item in EnumerateSitemapItems(page, token).ConfigureAwait(false))
                    {
                        items.Add(item);
                    }
                }

                return MuzlanResponse<IList<SitemapRecord>>.FromResult(items, pagination.PageUri);
            }
            catch (Exception ex)
            {
                if (pagination.PageNumber == -1)
                {
                    return MuzlanResponse<IList<SitemapRecord>>.FromException(ex, pagination.PageUri);
                }

                return MuzlanResponse<IList<SitemapRecord>>.FromPartialResult(items, pagination.PageUri, pagination.PageNumber);
            }
        }

        public async ValueTask<MuzlanResponse<IList<SitemapRecord>>> GetVideos(CancellationToken token = default)
        {
            var pageUri = new Uri($"{_baseUri.DnsSafeHost}/sitemap/videos");
            var pagination = new MuzlanPagination(pageUri, _client, _parser);

            var items = new List<SitemapRecord>();

            try
            {
                await foreach (var page in pagination.FollowForward(token).ConfigureAwait(false))
                {
                    await foreach (var item in EnumerateSitemapItems(page, token).ConfigureAwait(false))
                    {
                        items.Add(item);
                    }
                }

                return MuzlanResponse<IList<SitemapRecord>>.FromResult(items, pagination.PageUri);
            }
            catch (Exception ex)
            {
                if (pagination.PageNumber == -1)
                {
                    return MuzlanResponse<IList<SitemapRecord>>.FromException(ex, pagination.PageUri);
                }

                return MuzlanResponse<IList<SitemapRecord>>.FromPartialResult(items, pagination.PageUri, pagination.PageNumber);
            }
        }

        private async IAsyncEnumerable<SitemapRecord> EnumerateSitemapItems(string content, [EnumeratorCancellation] CancellationToken token = default)
        {
            using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

            foreach (var item in document.QuerySelectorAll<IHtmlAnchorElement>("div.page-content div.row-col div.nav li.nav-item a"))
            {
                var name = item.Title ?? string.Empty;
                var uri = new Uri(item.Href);

                yield return new SitemapRecord(name, uri);
            }
        }
    }
}
