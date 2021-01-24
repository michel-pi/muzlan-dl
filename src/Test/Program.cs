using System;
using System.Threading.Tasks;

using Muzlan.Api;
using System.IO;

namespace Test
{
    public static class Program
    {
        public async static Task Main(string[] _)
        {
            var config = new MuzlanClientConfig();
            var client = new MuzlanClient();

            var auth = await client.Auth.Authenticate().ConfigureAwait(false);

            var searchResults = await client.Search.FindTracks("Landser", 1).ConfigureAwait(false);

            var track = searchResults.Result[0];

            Console.WriteLine($"Track: {track.Name} - {track.Artist}");

            client.Download.DownloadProgressChanged += Download_DownloadProgressChanged;

            var download = await client.Download.DownloadTrack(track, auth.Result).ConfigureAwait(false);

            await File.WriteAllBytesAsync(download.Result.Filename, download.Result.Data).ConfigureAwait(false);

            Console.ReadLine();
        }

        private static Task Download_DownloadProgressChanged(object sender, AsyncDownloadProgressChangedEventArgs eventArgs)
        {
            Console.WriteLine($"{eventArgs.ProgressPercentage:0.##}%");

            return Task.CompletedTask;
        }
    }
}
