using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    /// <summary>
    /// 表示噪域的移动关键帧，位置值作为当前帧状态并向后插值到下一关键帧。
    /// </summary>
    public class AreaMoveEvent
    {
        /// <summary>
        /// 此关键帧中噪域中心的绝对坐标，以谱面渲染范围左下角为原点；首个关键帧之前的位置为噪域中心。
        /// </summary>
        [JsonProperty("endPosition")]
        public PositionUnit EndPosition { get; set; }

        /// <summary>
        /// 关键帧时间，单位为音乐时间秒。
        /// </summary>
        [JsonProperty("time")]
        public float Time { get; set; }

        /// <summary>
        /// 从此关键帧向下一关键帧过渡时 x 方向的缓动类型，由 <see cref="AreaEase"/> 表示。
        /// </summary>
        [JsonProperty("easeTypeX")]
        public AreaEase EaseTypeX { get; set; }

        /// <summary>
        /// 从此关键帧向下一关键帧过渡时 y 方向的缓动类型，由 <see cref="AreaEase"/> 表示。
        /// </summary>
        [JsonProperty("easeTypeY")]
        public AreaEase EaseTypeY { get; set; }

        /// <summary>
        /// 获取指定音乐时间的噪域中心坐标。
        /// </summary>
        /// <param name="time">当前音乐时间，单位为秒。</param>
        /// <param name="previousEventTime">前一个关键帧时间，单位为秒。</param>
        /// <param name="startPosition">前一个关键帧的噪域中心坐标。</param>
        /// <returns>指定时间的噪域中心坐标。</returns>
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
