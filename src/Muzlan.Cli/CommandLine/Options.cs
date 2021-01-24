using System;

using CommandLine;

namespace Muzlan.Cli.CommandLine
{
    public class Options
    {
        [Option('h', "host", Default = "https://muzlan.top", HelpText = "Specifies the host address to use. Defaults to https://muzlan.top.")]
        public string? Host { get; set; }

        [Option("output-pretty", HelpText = "Pretty print any output.")]
        public bool? OutputPretty { get; set; }

        [Option('q', "quiet", HelpText = "Activates quiet output mode.")]
        public bool? Quiet { get; set; }

        [Option('v', "verbose", HelpText = "Outputs various debug information.")]
        public bool? Verbose { get; set; }

        [Option("proxy", HelpText = "Use a single proxy or a list of proxies provided by a file. (IP PORT[ USERNAME[ PASSWORD])")]
        public string? Proxy { get; set; }
    }
}
