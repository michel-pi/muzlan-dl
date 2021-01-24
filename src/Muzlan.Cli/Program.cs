using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using CommandLine;

using Muzlan.Api;
using Muzlan.Cli.CommandLine;
using Muzlan.Cli.Utilities;

namespace Muzlan.Cli
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var verbs = LoadVerbs();

            await Parser.Default.ParseArguments(args, verbs)
                .MapResult
                <AlbumOptions, AuthOptions, DownloadOptions, MetaOptions, SearchOptions, SitemapOptions, Task>(
                HandleAlbumOptions,
                HandleAuthOptions,
                HandleDownloadOptions,
                HandleMetaOptions,
                HandleSearchOptions,
                HandleSitemapOptions,
                _ => ExitCodeHelper.SetAsync(ExitCode.ParserError)).ConfigureAwait(false);

            return Environment.ExitCode;
        }

        private static async Task<int> HandleAlbumOptions(AlbumOptions options)
        {
            var handler = await DefaultOptionsHandler.CreateAsync(options).ConfigureAwait(false);

            var client = handler.GetCurrentMuzlanClient();

            var authResponse = await client.Auth.Authenticate().ConfigureAwait(false);

            if (!authResponse.HasResult)
            {
                return ExitCodeHelper.Set(ExitCode.AuthenticationFailure);
            }

            if (!string.IsNullOrEmpty(options.Url))
            {
                // TODO: parse album or artist url

                return ExitCodeHelper.Set(ExitCode.ParserError);
            }
            else if (string.IsNullOrEmpty(options.Album))
            {
                var artistResponse = await RateLimitHelper.BypassRateLimitAsync(
                    handler,
                    (client) => client.Albums.FindAlbums(options.Artist ?? string.Empty)).ConfigureAwait(false);

                if (artistResponse.HasResult)
                {
                    var json = await JsonHelper.SerializeAsync(artistResponse.Result, handler.PrettyPrint).ConfigureAwait(false);

                    Console.WriteLine(json);
                }

                return ExitCodeHelper.Set(artistResponse);
            }
            else
            {
                var albumResponse = await RateLimitHelper.BypassRateLimitAsync(
                    handler,
                    (client) => client.Albums.GetTracks(options.Artist ?? string.Empty, options.Album ?? string.Empty))
                    .ConfigureAwait(false);

                if (albumResponse.HasResult)
                {
                    var json = await JsonHelper.SerializeAsync(albumResponse.Result, handler.PrettyPrint).ConfigureAwait(false);

                    Console.WriteLine(json);
                }

                return ExitCodeHelper.Set(albumResponse);
            }
        }

        private static Task<int> HandleAuthOptions(AuthOptions options)
        {
            return Task.FromResult(0);
        }

        private static Task<int> HandleDownloadOptions(DownloadOptions options)
        {
            return Task.FromResult(0);
        }

        private static Task<int> HandleMetaOptions(MetaOptions options)
        {
            return Task.FromResult(0);
        }

        private static Task<int> HandleSearchOptions(SearchOptions options)
        {
            return Task.FromResult(0);
        }

        private static Task<int> HandleSitemapOptions(SitemapOptions options)
        {
            return Task.FromResult(0);
        }

        private static Type[] LoadVerbs()
        {
            return typeof(Program)
                .Assembly
                .GetTypes()
                .Where(x => x.GetCustomAttribute<VerbAttribute>() != null)
                .ToArray();
        }
    }
}
