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
    internal class MuzlanPagination
    {
        private static readonly string[] _ignorablePaginationElementClasses = new string[]
        {
            "prev",
            "hide",
            "next"
        };

        private const string _activePaginationElementClass = "active";

        private readonly HttpClient _client;
        private readonly HtmlParser _parser;

        public int PageNumber { get; private set; }
        public int PageCount { get; private set; }

        public Uri PageUri { get; private set; }

        public MuzlanPagination(Uri baseUri, HttpClient client, HtmlParser parser)
        {
            _client = client;
            _parser = parser;

            PageUri = baseUri;
            PageNumber = -1;
        }

        public async IAsyncEnumerable<string> FollowForward([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            PageCount = 0;

            var content = await _client.TryGetStringAsync(PageUri, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(content))
            {
                yield break;
            }

            using var document = await _parser.ParseDocumentAsync(content, cancellationToken).ConfigureAwait(false);

            var currentPageAnchor = document.QuerySelector($"div.page-content div ul.pagination li.{_activePaginationElementClass} a");

            PageNumber = currentPageAnchor != null && int.TryParse(currentPageAnchor.TextContent.Trim(), out var number)
                ? number
                : 0;

            PageCount++;

            yield return content;

            bool skipPreviousPages = true;

            foreach (var pagination in document.QuerySelectorAll("div.page-content div ul.pagination li"))
            {
                if (pagination.ClassList.Contains(_ignorablePaginationElementClasses)) continue;

                if (skipPreviousPages)
                {
                    if (pagination.ClassList.Contains(_activePaginationElementClass))
                    {
                        skipPreviousPages = false;
                    }

                    continue;
                }

                var anchor = pagination.QuerySelector<IHtmlAnchorElement>("a");

                if (anchor == null) continue;

                if (!int.TryParse(anchor.Text.Trim(), out var pageNumber)) continue;

                PageUri = new Uri($"https://{PageUri.DnsSafeHost}{anchor.PathName}");
                PageNumber = pageNumber;
                PageCount++;

                content = await _client.TryGetStringAsync(PageUri, cancellationToken).ConfigureAwait(false);

                yield return content;
            }
        }
    }
}
