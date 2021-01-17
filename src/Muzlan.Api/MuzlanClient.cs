using System;
using System.Net.Http;
using AngleSharp.Html.Parser;

using Muzlan.Api.Endpoints;
using Muzlan.Api.Utilities;

namespace Muzlan.Api
{
    public class MuzlanClient : IDisposable
    {
        private bool disposedValue;

        private readonly HttpClient _client;
        private readonly HtmlParser _parser;

        public Uri BaseUri { get; }

        public AuthEndpoint Auth { get; }
        public MetaEndpoint Meta { get; }
        public SearchEndpoint Search { get; }
        public SitemapEndpoint Sitemap { get; }

        public bool IsAvailable => Auth.ServerOnline == true;
        public bool IsAuthenticated => !string.IsNullOrEmpty(Auth.CsrfToken) && !string.IsNullOrEmpty(Auth.MediaToken);

        public MuzlanClient() : this(MuzlanClientConfig.Default)
        {
        }

        public MuzlanClient(MuzlanClientConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            BaseUri = new Uri(config.Host, UriKind.Absolute);

            _client = HttpClientFactory.Create(config);

            _parser = new HtmlParser();

            Auth = new AuthEndpoint(BaseUri, _client, _parser);
            Meta = new MetaEndpoint(BaseUri, _client, _parser);
            Search = new SearchEndpoint(BaseUri, _client, _parser);
            Sitemap = new SitemapEndpoint(BaseUri, _client, _parser);
        }

        ~MuzlanClient()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _client.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
