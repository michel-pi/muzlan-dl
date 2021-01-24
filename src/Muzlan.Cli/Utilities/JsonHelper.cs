using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Muzlan.Cli.Utilities
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions _defaultOptions;
        private static readonly JsonSerializerOptions _prettyOptions;

        static JsonHelper()
        {
            _defaultOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            _prettyOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        public static string Serialize<T>(T value, bool pretty = false)
        {
            return JsonSerializer.Serialize(value, pretty ? _prettyOptions : _defaultOptions);
        }

        public static async Task<string> SerializeAsync<T>(T value, bool pretty = false, CancellationToken cancellationToken = default)
        {
            await using var stream = new MemoryStream();

            await JsonSerializer.SerializeAsync(
                stream,
                value,
                pretty ? _prettyOptions : _defaultOptions,
                cancellationToken).ConfigureAwait(false);

            if (!stream.TryGetBuffer(out ArraySegment<byte> buffer))
            {
                buffer = stream.ToArray();
            }

            return Encoding.UTF8.GetString(buffer);
        }

        public static T? Deserialize<T>(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            return JsonSerializer.Deserialize<T>(value, _defaultOptions);
        }

        public static async ValueTask<T?> DeserializeAsync<T>(string value, CancellationToken cancellationToken = default)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var buffer = Encoding.UTF8.GetBytes(value);

            await using var stream = new MemoryStream(buffer);

            return await JsonSerializer.DeserializeAsync<T>(stream, _defaultOptions, cancellationToken).ConfigureAwait(false);
        }
    }
}
