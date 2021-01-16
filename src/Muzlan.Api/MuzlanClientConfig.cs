using System;

namespace Muzlan.Api
{
    public class MuzlanClientConfig
    {
        public const string DefaultHost = "https://muzlan.top";
        public const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:84.0) Gecko/20100101 Firefox/84.0";

        public static readonly MuzlanClientConfig Default = new MuzlanClientConfig();

        public string Host { get; set; }
        public string UserAgent { get; set; }

        public string? NetworkUsername { get; set; }
        public string? NetworkPassword { get; set; }

        public string? ProxyAddress { get; set; }
        public string? ProxyUsername { get; set; }
        public string? ProxyPassword { get; set; }

        public MuzlanClientConfig()
        {
            Host = DefaultHost;
            UserAgent = DefaultUserAgent;
        }
    }
}
