using System;
using Newtonsoft.Json;

namespace KaedePhi.Core.Phigros.v3
{
    /// <summary>
    /// 旧版判定线事件类型（引用类型实现），仅保留用于兼容迁移期。
    /// 请改用 <see cref="Event"/> 结构体以降低内存开销；
    /// 本类型实例可通过隐式转换自动映射到 <see cref="Event"/>，将在后续版本一次性移除。
    /// </summary>
    [Obsolete(
        "Event 已改为结构体以节省内存开销，请改用 Event 结构体。"
            + "旧类型仍可通过隐式转换自动映射，本类型将在后续版本移除。"
    )]
    public sealed class LegacyEvent
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
        /// 开始数值
        /// </summary>
        [JsonProperty("start")]
        public float Start { get; set; }

        /// <summary>
        /// 结束数值
        /// </summary>
        [JsonProperty("end")]
        public float End { get; set; }

        /// <summary>
        /// 开始数值2（通常用在移动事件中，一般值Y轴坐标）
        /// </summary>
        [JsonProperty("start2")]
        public float Start2 { get; set; }

        /// <summary>
        /// 结束数值2（通常用在移动事件中，一般值Y轴坐标）
        /// </summary>
        [JsonProperty("end2")]
        public float End2 { get; set; }
    }
}
