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

            await muzlan.Authenticate().ConfigureAwait(false);

            Console.WriteLine("Csrf: " + muzlan.CsrfToken);
            Console.WriteLine("Media: " + muzlan.MediaToken);

            //var artists = await muzlan.GetPopularArtists().ConfigureAwait(false);
            //var searches = await muzlan.GetRecentSearches().ConfigureAwait(false);
            //var searchArtist = await muzlan.SearchArtists("Landser").ConfigureAwait(false);
            var searchTracks = await muzlan.SearchTracks("Landser").ConfigureAwait(false);

            Console.ReadLine();
        }
    }
}
