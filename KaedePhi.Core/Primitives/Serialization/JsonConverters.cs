using System;
using KaedePhi.Core.Primitives;
using Newtonsoft.Json;

namespace KaedePhi.Core.Primitives.Serialization
{
    public class BeatJsonConverter : JsonConverter<Beat>
    {
        public override void WriteJson(JsonWriter writer, Beat value, JsonSerializer serializer)
        {
            // 将 Beat 序列化为 int[] 数组
            int[] beatArray = value;
            serializer.Serialize(writer, beatArray);
        }

        public override Beat ReadJson(
            JsonReader reader,
            Type objectType,
            Beat existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            if (reader.TokenType == JsonToken.Null)
                return default;

            var beatArray = serializer.Deserialize<int[]>(reader);
            if (beatArray is null)
                return default;

            return new Beat(beatArray);
        }
    }
}
