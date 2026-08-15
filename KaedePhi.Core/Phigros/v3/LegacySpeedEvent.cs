using System;
using Newtonsoft.Json;

namespace KaedePhi.Core.Phigros.v3
{
    /// <summary>
    /// 旧版速度事件类型（引用类型实现），仅保留用于兼容迁移期。
    /// 请改用 <see cref="SpeedEvent"/> 结构体以降低内存开销；
    /// 本类型实例可通过隐式转换自动映射到 <see cref="SpeedEvent"/>，将在后续版本一次性移除。
    /// </summary>
    [Obsolete(
        "SpeedEvent 已改为结构体以节省内存开销，请改用 SpeedEvent 结构体。"
            + "旧类型仍可通过隐式转换自动映射，本类型将在后续版本移除。"
    )]
    public sealed class LegacySpeedEvent
    {
        /// <summary>
        /// 事件开始时间
        /// </summary>
        [JsonProperty("startTime")]
        public float StartTime { get; set; }

        /// <summary>
        /// 事件结束时间
        /// </summary>
        [JsonProperty("endTime")]
        public float EndTime { get; set; }

        /// <summary>
        /// 事件值
        /// </summary>
        [JsonProperty("value")]
        public float Value { get; set; }
    }
}
