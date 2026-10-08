using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using REDox.Json;

namespace KaedePhi.Core.Primitives.Serialization
{
    internal static class JsonCodec
    {
        private static readonly RedoxJsonSettings Settings = new(
            new JsonSerializerSettings { MaxDepth = 64 }
        );

        private static readonly JsonDocumentOptions DocumentOptions = new() { MaxDepth = 64 };

        internal static string Serialize<T>(T value, bool indented) =>
            REDox.Json.JsonSerializer.Serialize(value, Settings, CreateWriteOptions(indented));

        internal static void Serialize<T>(Stream stream, T value, bool indented) =>
            REDox.Json.JsonSerializer.Serialize(
                stream,
                value,
                Settings,
                CreateWriteOptions(indented)
            );

        internal static async Task SerializeAsync<T>(Stream stream, T value, bool indented)
        {
            Serialize(stream, value, indented);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        internal static T? Deserialize<T>(string json)
        {
            try
            {
                return REDox.Json.JsonSerializer.Deserialize<T>(json, Settings, DocumentOptions);
            }
            catch (REDox.Serialization.SerializationException ex)
                when (ex.InnerException is JsonSerializationException jsonException)
            {
                throw new TargetInvocationException(jsonException);
            }
        }

        internal static T? Deserialize<T>(Stream stream)
        {
            if (stream is null)
                throw new System.ArgumentNullException(nameof(stream));

            var prefix = new byte[4];
            var prefixLength = 0;
            while (prefixLength < prefix.Length)
            {
                var bytesRead = stream.Read(prefix, prefixLength, prefix.Length - prefixLength);
                if (bytesRead == 0)
                    break;

                prefixLength += bytesRead;
            }

            using var replayStream = new PrefixReadStream(stream, prefix, prefixLength);
            if (HasNonUtf8Bom(prefix, prefixLength))
            {
                using var streamReader = new StreamReader(
                    replayStream,
                    JsonDefaults.NoBomUtf8,
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: 1024,
                    leaveOpen: true
                );
                using var jsonReader = new JsonTextReader(streamReader);
                var serializer = Newtonsoft.Json.JsonSerializer.Create(
                    new JsonSerializerSettings { MaxDepth = 64 }
                );
                return serializer.Deserialize<T>(jsonReader);
            }

            try
            {
                return REDox.Json.JsonSerializer.Deserialize<T>(
                    replayStream,
                    Settings,
                    DocumentOptions
                );
            }
            catch (REDox.Serialization.SerializationException ex)
                when (ex.InnerException is JsonSerializationException jsonException)
            {
                throw new TargetInvocationException(jsonException);
            }
        }

        private static bool HasNonUtf8Bom(byte[] prefix, int length)
        {
            if (
                length >= 4
                && (
                    (prefix[0] == 0x00 && prefix[1] == 0x00 && prefix[2] == 0xfe && prefix[3] == 0xff)
                    || (prefix[0] == 0xff && prefix[1] == 0xfe && prefix[2] == 0x00 && prefix[3] == 0x00)
                )
            )
                return true;

            return length >= 2
                && (
                    (prefix[0] == 0xff && prefix[1] == 0xfe)
                    || (prefix[0] == 0xfe && prefix[1] == 0xff)
                );
        }

        private static JsonWriteOptions CreateWriteOptions(bool indented) =>
            new() { MaxDepth = 64, WriteIndented = indented };
    }
}
