using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Html.Parser;

namespace Muzlan.Api.Endpoints
{
    public class DownloadEndpoint : MuzlanEndpoint
    {
        public event AsyncDownloadProgressChanged? DownloadProgressChanged;

        internal DownloadEndpoint(Uri baseUri, HttpClient client, HtmlParser parser) : base(baseUri, client, parser)
        {
        }

        public async ValueTask<MuzlanResponse<DownloadRecord>> DownloadTrack(TrackRecord track, AuthRecord auth, CancellationToken cancellationToken = default)
        {
            var sourceUri = new Uri(track.DownloadUri, $"?key={auth.MediaToken}");

            var fileExtension = NormalizeFileExtension(Path.GetExtension(sourceUri.LocalPath));
            var fileName = track.Name;

            try
            {
                using var response = await _client.GetAsync(sourceUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                var contentLength = response.Content.Headers.GetValues("Content-Length").First();

                var fileSize = int.Parse(contentLength);
                var fileOffset = 0;
                var bytesRead = 0;
                var fileData = new byte[fileSize];

                await OnDownloadProgressChanged(fileOffset, fileSize).ConfigureAwait(false);

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                bool hasCompletedEvent = false;

                do
                {
                    if (fileOffset >= fileSize) break;

                    bytesRead = await stream.ReadAsync(
                        ConvertToMemory(fileData, fileOffset, 8192),
                        cancellationToken).ConfigureAwait(false);

                    fileOffset += bytesRead;

                    if (fileOffset == fileSize)
                    {
                        hasCompletedEvent = true;
                    }

                    await OnDownloadProgressChanged(fileOffset, fileSize).ConfigureAwait(false);
                } while (bytesRead > 0);

                if (!hasCompletedEvent)
                {
                    await OnDownloadProgressChanged(fileSize, fileSize).ConfigureAwait(false);
                }

                var file = new DownloadRecord(fileName + fileExtension, fileData, sourceUri);

                return MuzlanResponse<DownloadRecord>.FromResult(file, sourceUri);
            }
            catch (Exception ex)
            {
                return MuzlanResponse<DownloadRecord>.FromException(ex, sourceUri);
            }
        }

        private async ValueTask OnDownloadProgressChanged(int bytesReceived, int bytesTotal)
        {
            var eventList = DownloadProgressChanged;

            if (eventList == null) return;

            var args = new AsyncDownloadProgressChangedEventArgs(bytesReceived, bytesTotal);

            var tasks = new List<Task>();

            foreach (AsyncDownloadProgressChanged handler in eventList.GetInvocationList())
            {
                tasks.Add(handler(this, args));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private static Memory<byte> ConvertToMemory(byte[] buffer, int offset, int length)
        {
            var remainingLength = offset + length > buffer.Length
                ? buffer.Length - offset
                : length;

            return buffer.AsMemory(offset, remainingLength);
        }

        private static string NormalizeFileExtension(string extension)
        {
            return extension switch
            {
                ".mp4" or ".m4v" => ".mp4",
                ".webm" or ".webma" or ".webmv" => ".webm",
                ".ogg" or ".oga" or ".ogv" => ".ogg",
                _ => extension,
            };
        }
    }
}
