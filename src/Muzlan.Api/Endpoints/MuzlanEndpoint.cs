using System;
using System.Net.Http;

using AngleSharp.Html.Parser;

namespace Muzlan.Api.Endpoints
{
    public class MuzlanEndpoint
    {
        internal Uri _baseUri;
        internal HttpClient _client;
        internal HtmlParser _parser;

        internal MuzlanEndpoint(Uri baseUri, HttpClient client, HtmlParser parser)
        {
            _baseUri = baseUri;
            _client = client;
            _parser = parser;
        }
    }
}
