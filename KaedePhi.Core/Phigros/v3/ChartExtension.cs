using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace KaedePhi.Core.Phigros.v3
{
    public partial class Chart
    {
        /// <summary>
        /// 序列化谱面
        /// </summary>
        /// <param name="format">是否需要格式化</param>
        /// <returns>Json</returns>
        [PublicAPI]
        public string ExportToJson(bool format)
        {
            return JsonConvert.SerializeObject(this, format ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// 将谱面序列化为Json并写入流
        /// </summary>
        /// <param name="stream">流</param>
        /// <param name="format">是否需要格式化</param>
        public void ExportToJsonStream(Stream stream, bool format)
        {
            using var streamWriter = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
            var serializer = new JsonSerializer
            {
                Formatting = format ? Formatting.Indented : Formatting.None
            };

            using var jsonWriter = new JsonTextWriter(streamWriter) { CloseOutput = false };
            serializer.Serialize(jsonWriter, this);
            jsonWriter.Flush();
            streamWriter.Flush();
        }

        /// <summary>
        /// 异步将谱面序列化为Json并写入流
        /// </summary>
        /// <param name="stream">流</param>
        /// <param name="format">是否需要格式化</param>
        public async Task ExportToJsonStreamAsync(Stream stream, bool format)
        {
            await using var streamWriter =
                new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
            var serializer = new JsonSerializer
            {
                Formatting = format ? Formatting.Indented : Formatting.None
            };

            await Task.Run(() =>
            {
                using var jsonWriter = new JsonTextWriter(streamWriter) { CloseOutput = false };
                serializer.Serialize(jsonWriter, this);
                jsonWriter.Flush();
            });

            await streamWriter.FlushAsync();
        }

        /// <summary>
        /// 异步序列化为Json
        /// </summary>
        /// <param name="format">是否需要格式化</param>
        /// <returns>json</returns>
        public Task<string> ExportToJsonAsync(bool format)
            => Task.Run(() => ExportToJson(format));


        /// <summary>
        /// 从Json反序列化
        /// </summary>
        /// <param name="json">谱面Json数据</param>
        /// <returns>谱面对象</returns>
        /// <exception cref="InvalidOperationException">谱面json数据无法正确序列化</exception>
        [PublicAPI]
        public static Chart LoadFromJson(string json)
        {
            var chart = JsonConvert.DeserializeObject<Chart>(json) ??
                        throw new InvalidOperationException(
                            "Failed to deserialize Chart from JSON.");
            return chart;
        }

        /// <summary>
        /// 异步从Json反序列化
        /// </summary>
        /// <param name="json">谱面Json数据</param>
        /// <returns>谱面</returns>
        public static Task<Chart> LoadFromJsonAsync(string json)
            => Task.Run(() => LoadFromJson(json));

        /// <summary>
        /// 从流反序列化
        /// </summary>
        /// <param name="stream">流</param>
        /// <returns>谱面</returns>
        /// <exception cref="InvalidOperationException">反序列化失败</exception>
        public static Chart LoadFromStream(Stream stream)
        {
            using var streamReader = new StreamReader(
                stream,
                new UTF8Encoding(false),
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024,
                leaveOpen: true);
            using var jsonReader = new JsonTextReader(streamReader);
            var serializer = new JsonSerializer();
            var chart = serializer.Deserialize<Chart>(jsonReader) ??
                        throw new InvalidOperationException(
                            "Failed to deserialize Chart from stream.");
            return chart;
        }

        /// <summary>
        /// 从流反序列化
        /// </summary>
        /// <param name="stream">流</param>
        /// <returns>谱面</returns>
        /// <exception cref="InvalidOperationException">反序列化失败</exception>
        public static Task<Chart> LoadFromStreamAsync(Stream stream)
        {
            try
            {
                using var streamReader = new StreamReader(
                    stream,
                    new UTF8Encoding(false),
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: 1024,
                    leaveOpen: true);
                using var jsonReader = new JsonTextReader(streamReader);
                var serializer = new JsonSerializer();
                var chart = serializer.Deserialize<Chart>(jsonReader) ??
                            throw new InvalidOperationException(
                                "Failed to deserialize Chart from stream.");

                return Task.FromResult(chart);
            }
            catch (Exception exception)
            {
                return Task.FromException<Chart>(exception);
            }
        }
    }
}