using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace Muzlan.Api.Utilities
{
    internal class HttpPagination
    {
        private readonly Uri _baseUri;
        private readonly HttpClient _client;
        private readonly HtmlParser _parser;

        public HttpPagination(Uri baseUri, HttpClient client, HtmlParser parser)
        {
            _baseUri = baseUri;
            _client = client;
            _parser = parser;
        }

        public async IAsyncEnumerable<string> Follow(string url, [EnumeratorCancellation] CancellationToken token = default)
        {
            var content = await _client.GetStringAsync(url, token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(content))
            {
                yield break;
            }

            yield return content;

            using var document = await _parser.ParseDocumentAsync(content, token).ConfigureAwait(false);

            foreach (var pagination in document.QuerySelectorAll<IHtmlAnchorElement>("div.page-content div ul.pagination li a"))
            {
                if (!int.TryParse(pagination.Text.Trim(), out var pageNumber)) continue;
                if (pageNumber == 1) continue;

                var pageUrl = $"https://{_baseUri.DnsSafeHost}{pagination.PathName}";

                content = await _client.GetStringAsync(pageUrl, token).ConfigureAwait(false);

                yield return content;
            }
        }
    }
}
