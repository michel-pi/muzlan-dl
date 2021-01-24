using System;
using System.Threading;
using System.Threading.Tasks;

using Muzlan.Api;
using Muzlan.Cli.CommandLine;
using Muzlan.Cli.Proxies;

namespace Muzlan.Cli.Utilities
{
    public sealed class DefaultOptionsHandler
    {
        private readonly Options _options;

        private MuzlanClient? _client;

        public string Host => _options.Host ?? MuzlanClientConfig.DefaultHost;

        public bool PrettyPrint => _options.OutputPretty == true;

        public bool Quiet => _options.Quiet == true;
        public bool Verbose => _options.Verbose == true;

        public Proxy? Proxy { get; }
        public bool HasProxy => Proxy != null;

        public ProxyList? Proxies { get; }
        public bool HasProxies => Proxies != null;

        private DefaultOptionsHandler(Options options, Proxy? proxy = null, ProxyList? proxies = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            Proxy = proxy;
            Proxies = proxies;
        }

        public MuzlanClient GetCurrentMuzlanClient()
        {
            return _client ??= GetNextMuzlanClient();
        }

        public MuzlanClient GetNextMuzlanClient()
        {
            var config = new MuzlanClientConfig();

            if (Proxy != null)
            {
                config.ProxyAddress = $"{Proxy.Address}:{Proxy.Port}";

                config.NetworkUsername = Proxy.Username;
                config.NetworkPassword = Proxy.Password;
            }

            if (Proxies?.IsEmpty == false)
            {
                var proxy = Proxies.GetNextProxy();

                config.ProxyAddress = $"{proxy.Address}:{proxy.Port}";

                config.NetworkUsername = proxy.Username;
                config.NetworkPassword = proxy.Password;
            }

            _client = new MuzlanClient(config);

            return _client;
        }

        public static async ValueTask<DefaultOptionsHandler> CreateAsync(Options options, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(options.Proxy))
            {
                return new DefaultOptionsHandler(options);
            }

            if (Proxy.TryParse(options.Proxy, out var proxy) && proxy != null)
            {
                return new DefaultOptionsHandler(options, proxy);
            }

            try
            {
                var proxies = await ProxyList.FromFileAsync(options.Proxy, cancellationToken).ConfigureAwait(false);

                return proxies.IsEmpty
                    ? new DefaultOptionsHandler(options)
                    : new DefaultOptionsHandler(options, null, proxies);
            }
            catch
            {
                return new DefaultOptionsHandler(options);
            }
        }
    }
}
