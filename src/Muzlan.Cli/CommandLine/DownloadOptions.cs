using System;
using System.Collections.Generic;

using CommandLine;

namespace Muzlan.Cli.CommandLine
{
    [Verb("download", HelpText = "Download tracks.")]
    public class DownloadOptions : Options
    {
        [Option('t', "tracks", Min = 1, HelpText = "Set a list of tracks.")]
        public IEnumerable<string>? Tracks { get; set; }

        [Option('d', "direct", Min = 1, HelpText = "Set a list of direct download links.")]
        public IEnumerable<string>? DownloadLinks { get; set; }
    }
}
