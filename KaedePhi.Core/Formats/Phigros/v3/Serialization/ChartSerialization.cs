using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using KaedePhi.Core.Formats.Phigros.v3.Model;
using KaedePhi.Core.Primitives.Serialization;

namespace KaedePhi.Core.Formats.Phigros.v3.Serialization
{
    /// <summary>
    /// 提供 Phigros v3 谱面 JSON 的序列化与反序列化功能。
    /// </summary>
    public static class ChartSerialization
    {
        /// <summary>
        /// 序列化谱面
        /// </summary>
        /// <param name="chart">待序列化的谱面。</param>
        /// <param name="format">是否需要格式化</param>
        /// <returns>Json</returns>
        [PublicAPI]
        public static string ExportToJson(this Chart chart, bool format)
            => JsonCodec.Serialize(chart, format);

        /// <summary>
        /// 将谱面序列化为Json并写入流
        /// </summary>
        /// <param name="chart">待序列化的谱面。</param>
        /// <param name="stream">流</param>
        /// <param name="format">是否需要格式化</param>
        public static void ExportToJsonStream(this Chart chart, Stream stream, bool format)
            => JsonCodec.Serialize(stream, chart, format);

        /// <summary>
        /// 异步将谱面序列化为Json并写入流
        /// </summary>
        /// <param name="chart">待序列化的谱面。</param>
        /// <param name="stream">流</param>
        /// <param name="format">是否需要格式化</param>
        public static Task ExportToJsonStreamAsync(
            this Chart chart,
            Stream stream,
            bool format
        ) => JsonCodec.SerializeAsync(stream, chart, format);

        /// <summary>
        /// 异步序列化为Json
        /// </summary>
        /// <param name="chart">待序列化的谱面。</param>
        /// <param name="format">是否需要格式化</param>
        /// <returns>json</returns>
        public static Task<string> ExportToJsonAsync(this Chart chart, bool format) =>
            Task.FromResult(chart.ExportToJson(format));

        /// <summary>
        /// 从Json反序列化
        /// </summary>
        /// <param name="json">谱面Json数据</param>
        /// <returns>谱面对象</returns>
        /// <exception cref="InvalidOperationException">谱面json数据无法正确序列化</exception>
        [PublicAPI]
        public static Chart LoadFromJson(string json) =>
            JsonCodec.Deserialize<Chart>(json)
            ?? throw new InvalidOperationException("Failed to deserialize Chart from JSON.");

        /// <summary>
        /// 异步从Json反序列化
        /// </summary>
        /// <param name="json">谱面Json数据</param>
        /// <returns>谱面</returns>
        public static Task<Chart> LoadFromJsonAsync(string json) =>
            Task.FromResult(LoadFromJson(json));

        /// <summary>
        /// 从流反序列化
        /// </summary>
        /// <param name="stream">流</param>
        /// <returns>谱面</returns>
        /// <exception cref="InvalidOperationException">反序列化失败</exception>
        public static Chart LoadFromStream(Stream stream)
        {
            return JsonCodec.Deserialize<Chart>(stream)
                ?? throw new InvalidOperationException("Failed to deserialize Chart from stream.");
        }

        /// <summary>
        /// 从流反序列化
        /// </summary>
        /// <param name="stream">流</param>
        /// <returns>谱面</returns>
        /// <exception cref="InvalidOperationException">反序列化失败</exception>
        public static Task<Chart> LoadFromStreamAsync(Stream stream)
        {
            var chart = JsonCodec.Deserialize<Chart>(stream)
                ?? throw new InvalidOperationException("Failed to deserialize Chart from stream.");
            return Task.FromResult(chart);
        }
    }
}
