using System;

using CommandLine;

namespace Muzlan.Cli.CommandLine
{
    [Verb("auth", HelpText = "Authenticates with the muzlan servers.")]
    public class AuthOptions : Options
    {
        [Option("media-token", HelpText = "Only print the media token.")]
        public bool? OnlyPrintMediaToken { get; set; }

        [Option("csrf-token", HelpText = "Only print the csrf token.")]
        public bool? OnlyPrintCsrfToken { get; set; }
    }
}
