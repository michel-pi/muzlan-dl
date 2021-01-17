using System;
using System.Threading.Tasks;

using Muzlan.Api;

namespace Muzlan.Cli
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var muzlan = new MuzlanClient();

            await muzlan.Auth.Authenticate().ConfigureAwait(false);

            //var popularArtists = await muzlan.Meta.ListPopularArtists().ConfigureAwait(false);
            //var recentSearches = await muzlan.Meta.ListRecentSearches().ConfigureAwait(false);

            var tracks = await muzlan.Search.FindTracks("landser").ConfigureAwait(false);
            //var artists = await muzlan.Search.FindArtists("landser").ConfigureAwait(false);

            Console.ReadLine();
        }
    }
}
