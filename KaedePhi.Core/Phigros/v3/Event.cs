using Newtonsoft.Json;

namespace KaedePhi.Core.Phigros.v3
{
    /// <summary>
    /// 判定线事件，描述一段时间内判定线属性（移动、旋转、透明度）的变化。
    /// 已由引用类型改为结构体，降低谱面处理时的内存开销与 GC 压力；
    /// 旧的 <see cref="LegacyEvent"/> 类仍保留兼容，可通过隐式转换自动映射到本结构体。
    /// </summary>
    public readonly struct Event
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
        /// 开始数值
        /// </summary>
        [JsonProperty("start")]
        public float Start { get; init; }

        /// <summary>
        /// 结束数值
        /// </summary>
        [JsonProperty("end")]
        public float End { get; init; }

        /// <summary>
        /// 开始数值2（通常用在移动事件中，一般值Y轴坐标）
        /// </summary>
        [JsonProperty("start2")]
        public float Start2 { get; init; }

        /// <summary>
        /// 结束数值2（通常用在移动事件中，一般值Y轴坐标）
        /// </summary>
        [JsonProperty("end2")]
        public float End2 { get; init; }

        /// <summary>
        /// 创建判定线事件。
        /// </summary>
        /// <param name="startTime">事件开始时间</param>
        /// <param name="endTime">事件结束时间</param>
        /// <param name="start">开始数值</param>
        /// <param name="end">结束数值</param>
        /// <param name="start2">开始数值2</param>
        /// <param name="end2">结束数值2</param>
        public Event(
            float startTime,
            float endTime,
            float start,
            float end,
            float start2,
            float end2
        )
        {
            StartTime = startTime;
            EndTime = endTime;
            Start = start;
            End = end;
            Start2 = start2;
            End2 = end2;
        }

#pragma warning disable CS0618
        /// <summary>
        /// 将旧版事件类自动映射为结构体。
        /// </summary>
        /// <param name="legacy">旧版事件实例</param>
        /// <returns>映射后的结构体</returns>
        public static implicit operator Event(LegacyEvent legacy) =>
            new(
                legacy.StartTime,
                legacy.EndTime,
                legacy.Start,
                legacy.End,
                legacy.Start2,
                legacy.End2
            );

        /// <summary>
        /// 将结构体映射回旧版事件类（仅供旧接口调用）。
        /// </summary>
        /// <param name="evt">结构体事件</param>
        /// <returns>旧版事件实例</returns>
        public static implicit operator LegacyEvent(Event evt) =>
            new()
            {
                StartTime = evt.StartTime,
                EndTime = evt.EndTime,
                Start = evt.Start,
                End = evt.End,
                Start2 = evt.Start2,
                End2 = evt.End2,
            };
#pragma warning restore CS0618
    }
}
