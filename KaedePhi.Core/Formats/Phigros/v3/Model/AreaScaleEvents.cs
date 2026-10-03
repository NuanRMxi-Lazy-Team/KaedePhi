using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    /// <summary>
    /// 表示方块区域的缩放关键帧，属性值由上一关键帧插值至此事件的目标值。
    /// </summary>
    public class AreaScaleEvent
    {
        /// <summary>
        /// 缩放中心坐标，以谱面渲染范围左下角为原点，坐标可超出 0 到 1。
        /// </summary>
        [JsonProperty("anchor")]
        public PositionUnit Anchor { get; set; }

        /// <summary>
        /// 关键帧时间，单位为音乐时间秒。
        /// </summary>
        [JsonProperty("time")]
        public float Time { get; set; }

        /// <summary>
        /// x、y 方向的缩放倍率；(1, 1) 表示原始大小，首个关键帧之前默认为 (1, 1)。
        /// </summary>
        [JsonProperty("scale")]
        public PositionUnit Scale { get; set; }

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
        /// 获取指定音乐时间的缩放倍率。
        /// </summary>
        /// <param name="time">当前音乐时间，单位为秒。</param>
        /// <param name="previousEventTime">前一个关键帧时间，单位为秒。</param>
        /// <param name="startScale">前一个关键帧的缩放倍率。</param>
        /// <returns>指定时间的缩放倍率。</returns>
        public PositionUnit GetScaleAtTime(
            float time,
            float previousEventTime,
            PositionUnit startScale
        )
        {
            var progress = AreaEase.GetProgress(previousEventTime, Time, time);
            return AreaEase.Interpolate(startScale, Scale, progress, EaseTypeX, EaseTypeY);
        }

        /// <summary>
        /// 获取指定音乐时间的缩放中心坐标。
        /// </summary>
        /// <param name="time">当前音乐时间，单位为秒。</param>
        /// <param name="previousEventTime">前一关键帧时间，单位为秒。</param>
        /// <param name="startAnchor">前一关键帧的缩放中心坐标。</param>
        /// <returns>指定时间的缩放中心坐标。</returns>
        public PositionUnit GetAnchorAtTime(
            float time,
            float previousEventTime,
            PositionUnit startAnchor
        )
        {
            var progress = AreaEase.GetProgress(previousEventTime, Time, time);
            return AreaEase.Interpolate(startAnchor, Anchor, progress, EaseTypeX, EaseTypeY);
        }
    }
}
