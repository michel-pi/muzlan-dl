using System;

using CommandLine;

namespace Muzlan.Cli.CommandLine
{
    [Verb("meta", HelpText = "List popular artists and tracks.")]
    public class MetaOptions : Options
    {
        [Option('a', "artists", HelpText = "List popular artists.")]
        public bool? Artists { get; set; }

        [Option('s', "searches", HelpText = "List recent searches.")]
        public bool? Searches { get; set; }
    }
}
