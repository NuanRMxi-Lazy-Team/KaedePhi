using System.Text;
using Newtonsoft.Json;

namespace KaedePhi.Core.Primitives.Serialization
{
    /// <summary>
    /// 共享的 JSON 序列化配置，避免每次调用重复创建无状态对象。
    /// </summary>
    internal static class JsonDefaults
    {
        /// <summary>不带 BOM 的 UTF8 编码（单例复用）</summary>
        internal static readonly UTF8Encoding NoBomUtf8 = new(
            encoderShouldEmitUTF8Identifier: false
        );

        /// <summary>用于反序列化的默认设置 (MaxDepth=64)</summary>
        internal static readonly JsonSerializerSettings DeserializeSettings = new()
        {
            MaxDepth = 64,
        };

        /// <summary>创建序列化用的 JsonSerializer 实例（带 MaxDepth 保护）</summary>
        internal static JsonSerializer CreateSerializer(Formatting formatting) =>
            new() { Formatting = formatting, MaxDepth = 64 };
    }
}
