using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KaedePhi.Core.Primitives.Serialization
{
    internal static class JsonCodec
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            MaxDepth = 64,
        };

        internal static string Serialize<T>(T value, bool indented) =>
            JsonConvert.SerializeObject(
                value,
                indented ? Formatting.Indented : Formatting.None
            );

        internal static void Serialize<T>(Stream stream, T value, bool indented)
        {
            using var streamWriter = new StreamWriter(
                stream,
                JsonDefaults.NoBomUtf8,
                1024,
                leaveOpen: true
            );
            var serializer = CreateSerializer(indented);
            using var jsonWriter = new JsonTextWriter(streamWriter) { CloseOutput = false };
            serializer.Serialize(jsonWriter, value);
            jsonWriter.Flush();
            streamWriter.Flush();
        }

        internal static async Task SerializeAsync<T>(Stream stream, T value, bool indented)
        {
            await using var streamWriter = new StreamWriter(
                stream,
                JsonDefaults.NoBomUtf8,
                1024,
                leaveOpen: true
            );
            var serializer = CreateSerializer(indented);
            using var jsonWriter = new JsonTextWriter(streamWriter) { CloseOutput = false };
            serializer.Serialize(jsonWriter, value);
            await jsonWriter.FlushAsync().ConfigureAwait(false);
            await streamWriter.FlushAsync().ConfigureAwait(false);
        }

        internal static T? Deserialize<T>(string json) =>
            JsonConvert.DeserializeObject<T>(json, Settings);

        internal static T? Deserialize<T>(Stream stream)
        {
            using var streamReader = new StreamReader(
                stream,
                JsonDefaults.NoBomUtf8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024,
                leaveOpen: true
            );
            using var jsonReader = new JsonTextReader(streamReader);
            return CreateSerializer(indented: false).Deserialize<T>(jsonReader);
        }

        private static JsonSerializer CreateSerializer(bool indented) =>
            new()
            {
                Formatting = indented ? Formatting.Indented : Formatting.None,
                MaxDepth = 64,
            };
    }
}
