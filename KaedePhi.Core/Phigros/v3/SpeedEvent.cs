using Newtonsoft.Json;

namespace KaedePhi.Core.Phigros.v3
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

#pragma warning disable CS0618
        /// <summary>
        /// 将旧版速度事件类自动映射为结构体。
        /// </summary>
        /// <param name="legacy">旧版速度事件实例</param>
        /// <returns>映射后的结构体</returns>
        public static implicit operator SpeedEvent(LegacySpeedEvent legacy) =>
            new(legacy.StartTime, legacy.EndTime, legacy.Value);

        /// <summary>
        /// 将结构体映射回旧版速度事件类（仅供旧接口调用）。
        /// </summary>
        /// <param name="speedEvent">结构体速度事件</param>
        /// <returns>旧版速度事件实例</returns>
        public static implicit operator LegacySpeedEvent(SpeedEvent speedEvent) =>
            new()
            {
                StartTime = speedEvent.StartTime,
                EndTime = speedEvent.EndTime,
                Value = speedEvent.Value,
            };
#pragma warning restore CS0618
    }
}
