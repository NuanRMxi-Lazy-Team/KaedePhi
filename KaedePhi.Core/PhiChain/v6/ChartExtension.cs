using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using KaedePhi.Core.Common;
using Newtonsoft.Json;

namespace KaedePhi.Core.PhiChain.v6
{
    /// <summary>
    /// PhiChain v6 Chart 扩展方法，提供 JSON 的序列化与反序列化功能
    /// </summary>
    public partial class Chart
    {
        /// <summary>
        /// 从 JSON 字符串反序列化为 Chart 对象
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>Chart 对象</returns>
        /// <exception cref="InvalidOperationException">当 JSON 无法反序列化时抛出</exception>
        [PublicAPI]
        public static Chart LoadFromJson(string json)
        {
            try
            {
                var chart =
                    JsonConvert.DeserializeObject<Chart>(json, JsonDefaults.DeserializeSettings)
                    ?? throw new InvalidOperationException(
                        "Failed to deserialize Chart from JSON: result is null"
                    );

                // 确保 BpmList 状态正确
                chart.BpmList.ComputeTimes();

                return chart;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize Chart from JSON: {ex.Message}",
                    ex
                );
            }
        }

        /// <summary>
        /// 从 JSON 流反序列化为 Chart 对象（流式读取，不将整个流加载到内存）。
        /// </summary>
        /// <param name="stream">JSON 流</param>
        /// <returns>Chart 对象</returns>
        public static Task<Chart> LoadFromJsonStreamAsync(Stream stream)
        {
            using var reader = new StreamReader(
                stream,
                JsonDefaults.NoBomUtf8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 4096,
                leaveOpen: true
            );
            using var jsonReader = new JsonTextReader(reader);
            return Task.FromResult(DeserializeFromJsonReader(jsonReader));
        }

        /// <summary>
        /// 从 JSON 流反序列化为 Chart 对象（真正的流式读取，不会将整个流加载到内存）
        /// </summary>
        /// <param name="stream">JSON 流</param>
        /// <returns>Chart 对象</returns>
        public static Chart LoadFromJsonStream(Stream stream)
        {
            using var reader = new StreamReader(
                stream,
                JsonDefaults.NoBomUtf8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 4096,
                leaveOpen: true
            );
            using var jsonReader = new JsonTextReader(reader);
            return DeserializeFromJsonReader(jsonReader);
        }

        private static Chart DeserializeFromJsonReader(JsonTextReader jsonReader)
        {
            try
            {
                var serializer = JsonSerializer.Create(JsonDefaults.DeserializeSettings);
                var chart =
                    serializer.Deserialize<Chart>(jsonReader)
                    ?? throw new InvalidOperationException(
                        "Failed to deserialize Chart from JSON: result is null"
                    );
                chart.BpmList.ComputeTimes();
                return chart;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize Chart from JSON: {ex.Message}",
                    ex
                );
            }
        }

        /// <summary>
        /// 序列化 Chart 为 JSON 字符串
        /// </summary>
        /// <param name="format">是否格式化输出</param>
        /// <returns>JSON 字符串</returns>
        [PublicAPI]
        public string ExportToJson(bool format = false)
        {
            return JsonConvert.SerializeObject(
                this,
                format ? Formatting.Indented : Formatting.None
            );
        }

        /// <summary>
        /// 异步序列化 Chart 为 JSON 字符串
        /// </summary>
        /// <param name="format">是否格式化输出</param>
        /// <returns>JSON 字符串</returns>
        [PublicAPI]
        public Task<string> ExportToJsonAsync(bool format = false) =>
            Task.FromResult(ExportToJson(format));

        /// <summary>
        /// 序列化 Chart 为 JSON 并写入流
        /// </summary>
        /// <param name="stream">目标流</param>
        /// <param name="format">是否格式化输出</param>
        public void ExportToJsonStream(Stream stream, bool format = false)
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
        /// 异步序列化 Chart 为 JSON 并写入流
        /// </summary>
        /// <param name="stream">目标流</param>
        /// <param name="format">是否格式化输出</param>
        public async Task ExportToJsonStreamAsync(Stream stream, bool format = false)
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
            using var jsonWriter = new JsonTextWriter(streamWriter) { CloseOutput = false };
            serializer.Serialize(jsonWriter, this);
            jsonWriter.Flush();
            await streamWriter.FlushAsync();
        }

        /// <summary>
        /// 异步从 JSON 字符串反序列化为 Chart 对象
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>Chart 对象</returns>
        [PublicAPI]
        public static Task<Chart> LoadFromJsonAsync(string json) =>
            Task.FromResult(LoadFromJson(json));

        /// <summary>
        /// 从文件路径加载 Chart
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>Chart 对象</returns>
        public static Chart LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Chart file not found: {filePath}");

            var json = File.ReadAllText(filePath, JsonDefaults.NoBomUtf8);
            return LoadFromJson(json);
        }

        /// <summary>
        /// 异步从文件路径加载 Chart
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>Chart 对象</returns>
        public static async Task<Chart> LoadFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Chart file not found: {filePath}");

            var json = await File.ReadAllTextAsync(filePath, JsonDefaults.NoBomUtf8);
            return await LoadFromJsonAsync(json);
        }
    }
}
