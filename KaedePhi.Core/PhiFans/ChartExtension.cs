using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using KaedePhi.Core.Common;
using Newtonsoft.Json;

namespace KaedePhi.Core.PhiFans
{
    public partial class Chart
    {
        /// <summary>
        /// 序列化谱面为 JSON。
        /// </summary>
        /// <param name="format">是否需要格式化</param>
        /// <returns>JSON 字符串</returns>
        [PublicAPI]
        public string ExportToJson(bool format)
        {
            return JsonConvert.SerializeObject(
                this,
                format ? Formatting.Indented : Formatting.None
            );
        }

        /// <summary>
        /// 将谱面序列化为 JSON 并写入流。
        /// </summary>
        /// <param name="stream">目标流</param>
        /// <param name="format">是否需要格式化</param>
        public void ExportToJsonStream(Stream stream, bool format)
        {
            using var streamWriter = new StreamWriter(
                stream,
                JsonDefaults.NoBomUtf8,
                1024,
                leaveOpen: true
            );
            var serializer = JsonDefaults.CreateSerializer(
                format ? Formatting.Indented : Formatting.None
            );

            using var jsonWriter = new JsonTextWriter(streamWriter) { CloseOutput = false };
            serializer.Serialize(jsonWriter, this);
            jsonWriter.Flush();
            streamWriter.Flush();
        }

        /// <summary>
        /// 异步将谱面序列化为 JSON 并写入流。
        /// </summary>
        /// <param name="stream">目标流</param>
        /// <param name="format">是否需要格式化</param>
        public async Task ExportToJsonStreamAsync(Stream stream, bool format)
        {
            await using var streamWriter = new StreamWriter(
                stream,
                JsonDefaults.NoBomUtf8,
                1024,
                leaveOpen: true
            );
            var serializer = JsonDefaults.CreateSerializer(
                format ? Formatting.Indented : Formatting.None
            );

            await Task.Run(() =>
            {
                using var jsonWriter = new JsonTextWriter(streamWriter) { CloseOutput = false };
                serializer.Serialize(jsonWriter, this);
                jsonWriter.Flush();
            });

            await streamWriter.FlushAsync();
        }

        /// <summary>
        /// 异步序列化为 JSON。
        /// </summary>
        /// <param name="format">是否需要格式化</param>
        /// <returns>JSON 字符串</returns>
        public Task<string> ExportToJsonAsync(bool format) => Task.Run(() => ExportToJson(format));

        /// <summary>
        /// 从 JSON 反序列化谱面。
        /// </summary>
        /// <param name="json">谱面 JSON 数据</param>
        /// <returns>谱面对象</returns>
        /// <exception cref="InvalidOperationException">反序列化失败</exception>
        [PublicAPI]
        public static Chart LoadFromJson(string json)
        {
            return JsonConvert.DeserializeObject<Chart>(json, JsonDefaults.DeserializeSettings)
                ?? throw new InvalidOperationException(
                    "Failed to deserialize PhiFans Chart from JSON."
                );
        }

        /// <summary>
        /// 异步从 JSON 反序列化谱面。
        /// </summary>
        /// <param name="json">谱面 JSON 数据</param>
        /// <returns>谱面</returns>
        public static Task<Chart> LoadFromJsonAsync(string json) =>
            Task.Run(() => LoadFromJson(json));

        /// <summary>
        /// 从流反序列化谱面。
        /// </summary>
        /// <param name="stream">流</param>
        /// <returns>谱面</returns>
        /// <exception cref="InvalidOperationException">反序列化失败</exception>
        public static Chart LoadFromStream(Stream stream)
        {
            using var streamReader = new StreamReader(
                stream,
                JsonDefaults.NoBomUtf8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024,
                leaveOpen: true
            );
            using var jsonReader = new JsonTextReader(streamReader);
            var serializer = JsonDefaults.CreateSerializer(Formatting.None);
            return serializer.Deserialize<Chart>(jsonReader)
                ?? throw new InvalidOperationException(
                    "Failed to deserialize PhiFans Chart from stream."
                );
        }

        /// <summary>
        /// 异步从流反序列化谱面。
        /// </summary>
        /// <param name="stream">流</param>
        /// <returns>谱面</returns>
        public static Task<Chart> LoadFromStreamAsync(Stream stream)
        {
            try
            {
                using var streamReader = new StreamReader(
                    stream,
                    JsonDefaults.NoBomUtf8,
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: 1024,
                    leaveOpen: true
                );
                using var jsonReader = new JsonTextReader(streamReader);
                var serializer = JsonDefaults.CreateSerializer(Formatting.None);
                var chart =
                    serializer.Deserialize<Chart>(jsonReader)
                    ?? throw new InvalidOperationException(
                        "Failed to deserialize PhiFans Chart from stream."
                    );
                return Task.FromResult(chart);
            }
            catch (Exception exception)
            {
                return Task.FromException<Chart>(exception);
            }
        }
    }
}
