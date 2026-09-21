using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    /// <summary>
    /// 速度事件，描述一段时间的速度变化。
    /// 已由引用类型改为结构体，降低谱面处理时的内存开销与 GC 压力；
    /// 旧的 <see cref="LegacySpeedEvent"/> 类仍保留兼容，可通过隐式转换自动映射到本结构体。
    /// </summary>
    public readonly struct SpeedEvent
    {
        /// <summary>
        /// 事件开始时间
        /// </summary>
        [JsonProperty("startTime")]
        public float StartTime { get; init; }

        /// <summary>
        /// 事件结束时间
        /// </summary>
        [JsonProperty("endTime")]
        public float EndTime { get; init; }

        /// <summary>
        /// 事件值
        /// </summary>
        [JsonProperty("value")]
        public float Value { get; init; }

        /// <summary>
        /// 创建速度事件。
        /// </summary>
        /// <param name="startTime">事件开始时间</param>
        /// <param name="endTime">事件结束时间</param>
        /// <param name="value">事件值</param>
        public SpeedEvent(float startTime, float endTime, float value)
        {
            StartTime = startTime;
            EndTime = endTime;
            Value = value;
        }
    }
}