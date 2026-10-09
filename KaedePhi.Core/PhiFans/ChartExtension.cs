using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using KaedePhi.Core.Common;
using JsonCodec = KaedePhi.Core.Primitives.Serialization.JsonCodec;

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
            => JsonCodec.Serialize(this, format);

        /// <summary>
        /// 将谱面序列化为 JSON 并写入流。
        /// </summary>
        /// <param name="stream">目标流</param>
        /// <param name="format">是否需要格式化</param>
        public void ExportToJsonStream(Stream stream, bool format)
            => JsonCodec.Serialize(stream, this, format);

        /// <summary>
        /// 异步将谱面序列化为 JSON 并写入流。
        /// </summary>
        /// <param name="stream">目标流</param>
        /// <param name="format">是否需要格式化</param>
        public Task ExportToJsonStreamAsync(Stream stream, bool format) =>
            JsonCodec.SerializeAsync(stream, this, format);

        /// <summary>
        /// 异步序列化为 JSON。
        /// </summary>
        /// <param name="format">是否需要格式化</param>
        /// <returns>JSON 字符串</returns>
        public Task<string> ExportToJsonAsync(bool format) => Task.FromResult(ExportToJson(format));

        /// <summary>
        /// 从 JSON 反序列化谱面。
        /// </summary>
        /// <param name="json">谱面 JSON 数据</param>
        /// <returns>谱面对象</returns>
        /// <exception cref="InvalidOperationException">反序列化失败</exception>
        [PublicAPI]
        public static Chart LoadFromJson(string json)
        {
            return JsonCodec.Deserialize<Chart>(json)
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
            Task.FromResult(LoadFromJson(json));

        /// <summary>
        /// 从流反序列化谱面。
        /// </summary>
        /// <param name="stream">流</param>
        /// <returns>谱面</returns>
        /// <exception cref="InvalidOperationException">反序列化失败</exception>
        public static Chart LoadFromStream(Stream stream)
        {
            return JsonCodec.Deserialize<Chart>(stream)
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
            var chart = JsonCodec.Deserialize<Chart>(stream)
                ?? throw new InvalidOperationException(
                    "Failed to deserialize PhiFans Chart from stream."
                );
            return Task.FromResult(chart);
        }
    }
}
