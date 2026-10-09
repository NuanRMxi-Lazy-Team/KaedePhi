using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    /// <summary>
    /// 表示噪域的缩放关键帧；缩放值向后插值，锚点在帧间保持左侧关键帧的位置。
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
        /// 此关键帧的 x、y 方向缩放倍率；(1, 1) 表示原始大小，首个关键帧之前默认为 (1, 1)。
        /// </summary>
        [JsonProperty("scale")]
        public PositionUnit Scale { get; set; }

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
        /// 获取指定音乐时间的缩放中心坐标；帧间保持左侧关键帧的锚点。
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
            return progress < 1f ? startAnchor : Anchor;
        }
    }
}
