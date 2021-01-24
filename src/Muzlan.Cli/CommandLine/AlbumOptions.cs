using System;

using CommandLine;

namespace Muzlan.Cli.CommandLine
{
    [Verb("albums", HelpText = "Find albums by artist and get their tracks.")]
    public class AlbumOptions : Options
    {
        [Value(0, MetaName = "url", HelpText = "Album url.")]
        public string? Url { get;  set; }

        [Option("artist", Group = "by-artist", HelpText = "Artist name.")]
        public string? Artist { get; set; }

        [Option("album", Group = "by-artist", HelpText = "Album name.")]
        public string? Album { get; set; }
    }
}
