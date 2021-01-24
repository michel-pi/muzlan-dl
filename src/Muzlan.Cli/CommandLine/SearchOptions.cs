using System;

using CommandLine;

namespace Muzlan.Cli.CommandLine
{
    [Verb("search", HelpText = "Searches for tracks and artists.")]
    public class SearchOptions : Options
    {
        [Option('t', "tracks", Group = "search-tracks", HelpText = "Searches for tracks.")]
        public bool? SearchTracks { get; set; }

        [Option('a', "artists", Group = "search-artists", HelpText = "Searches for artists.")]
        public bool? SearchArtists { get; set; }

        [Option('i', "limit-items", HelpText = "Limits the number of items returned.")]
        public int? LimitItems { get; set; }

        [Option('p', "limit-pages", HelpText = "Limits the number of pages included in the search.")]
        public int? LimitPages { get; set; }

        [Value(0, MetaName = "query", Required = true, HelpText = "Query or url.")]
        public string? Query { get; set; }
    }
}
