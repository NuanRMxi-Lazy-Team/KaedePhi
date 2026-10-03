using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    /// <summary>
    /// 表示方块区域的移动关键帧，目标坐标由上一关键帧的位置插值而来。
    /// </summary>
    public class AreaMoveEvent
    {
        /// <summary>
        /// 方块中心的目标坐标，以谱面渲染范围左下角为原点；首个关键帧之前的位置为区域中心。
        /// </summary>
        [JsonProperty("endPosition")]
        public PositionUnit EndPosition { get; set; }

        /// <summary>
        /// 关键帧时间，单位为音乐时间秒。
        /// </summary>
        [JsonProperty("time")]
        public float Time { get; set; }

        /// <summary>
        /// x 方向的缓动类型，由 <see cref="AreaEase"/> 表示，编号对应 <see cref="AreaEaseType"/>。
        /// </summary>
        [JsonProperty("easeTypeX")]
        public AreaEase EaseTypeX { get; set; }

        /// <summary>
        /// y 方向的缓动类型，由 <see cref="AreaEase"/> 表示，编号对应 <see cref="AreaEaseType"/>。
        /// </summary>
        [JsonProperty("easeTypeY")]
        public AreaEase EaseTypeY { get; set; }

        /// <summary>
        /// 获取指定音乐时间的方块中心坐标。
        /// </summary>
        /// <param name="time">当前音乐时间，单位为秒。</param>
        /// <param name="previousEventTime">前一个关键帧时间，单位为秒。</param>
        /// <param name="startPosition">前一个关键帧的方块中心坐标。</param>
        /// <returns>指定时间的方块中心坐标。</returns>
        public PositionUnit GetPositionAtTime(
            float time,
            float previousEventTime,
            PositionUnit startPosition
        )
        {
            var progress = AreaEase.GetProgress(previousEventTime, Time, time);
            return AreaEase.Interpolate(startPosition, EndPosition, progress, EaseTypeX, EaseTypeY);
        }
    }
}
