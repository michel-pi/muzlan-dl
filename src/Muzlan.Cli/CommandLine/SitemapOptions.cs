using System;

using CommandLine;

namespace Muzlan.Cli.CommandLine
{
    [Verb("sitemap", HelpText = "Lists sitemap items.")]
    public class SitemapOptions
    {
        [Option('i', "limit-items", HelpText = "Limits the number of items returned.")]
        public int? LimitItems { get; set; }

        [Option('p', "limit-pages", HelpText = "Limits the number of pages included in the search.")]
        public int? LimitPages { get; set; }

        [Value(0, MetaName = "path", Required = true, HelpText = "Path of the sitemap. Can be 'artists', 'tracks', 'videos'.")]
        public string? Path { get; set; }
    }
}
