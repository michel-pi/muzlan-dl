using System;
using System.Net;

namespace Muzlan.Cli.Proxies
{
    public class Proxy
    {
        public IPAddress Address { get; }
        public int Port { get; }

        public string? Username { get; }
        public string? Password { get; }

        public Proxy(IPAddress address, int port, string? username = null, string? password = null)
        {
            if (port < 1 || port > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(port));

            Address = address ?? throw new ArgumentNullException(nameof(address));
            Port = port;
            Username = username;
            Password = password;
        }

        public static bool TryParse(string value, out Proxy? proxy)
        {
            proxy = default;

            if (string.IsNullOrEmpty(value)) return false;

            var items = value.Split(' ', 4, StringSplitOptions.None);

            if (items == null || items.Length < 2) return false;

            if (!IPAddress.TryParse(items[0], out var address))
            {
                return false;
            }

            if (!ushort.TryParse(items[1], out var port))
            {
                return false;
            }

            var username = items.Length >= 3
                ? items[2]
                : null;

            var password = items.Length >= 4
                ? items[3]
                : null;

            proxy = new Proxy(address, port, username, password);

            return true;
        }
    }
}
