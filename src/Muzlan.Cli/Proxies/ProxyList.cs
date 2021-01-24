using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Muzlan.Cli.Proxies
{
    public class ProxyList
    {
        private int _currentProxyIndex;

        public IReadOnlyList<Proxy> Proxies { get; }

        public bool IsEmpty => Proxies.Count == 0;
        public int Count => Proxies.Count;
        public bool HasReachedEnd => _currentProxyIndex >= Proxies.Count;

        public ProxyList(IList<Proxy> proxies)
        {
            if (proxies == null) throw new ArgumentNullException(nameof(proxies));

            Proxies = (IReadOnlyList<Proxy>)proxies;

            _currentProxyIndex = -1;
        }

        public Proxy GetNextProxy()
        {
            _currentProxyIndex++;

            var index = _currentProxyIndex % Proxies.Count;

            return Proxies[index];
        }

        public static async ValueTask<ProxyList> FromFileAsync(string path, CancellationToken cancellationToken = default)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var proxies = new List<Proxy>();

            foreach (var line in await File.ReadAllLinesAsync(path, Encoding.Default, cancellationToken).ConfigureAwait(false))
            {
                if (string.IsNullOrEmpty(line)) continue;

                if (Proxy.TryParse(line, out var proxy) && proxy != null)
                {
                    proxies.Add(proxy);
                }
            }

            return new ProxyList(proxies);
        }
    }
}
