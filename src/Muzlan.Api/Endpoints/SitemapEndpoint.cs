using System;
using System.Net.Http;

using AngleSharp.Html.Parser;

namespace Muzlan.Api.Endpoints
{
    public class SitemapEndpoint : MuzlanEndpoint
    {
        internal SitemapEndpoint(Uri baseUri, HttpClient client, HtmlParser parser) : base(baseUri, client, parser)
        {
        }
    }
}
