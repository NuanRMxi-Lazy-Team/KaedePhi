using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    /// <summary>
    /// 表示方块区域的旋转关键帧，属性值由上一关键帧插值至此事件的目标值。
    /// </summary>
    public class AreaRotateEvent
    {
        /// <summary>
        /// 旋转中心坐标，以谱面渲染范围左下角为原点，坐标可超出 0 到 1。
        /// </summary>
        [JsonProperty("anchor")]
        public PositionUnit Anchor { get; set; }

        /// <summary>
        /// 关键帧时间，单位为音乐时间秒。
        /// </summary>
        [JsonProperty("time")]
        public float Time { get; set; }

        /// <summary>
        /// 缓动类型，由 <see cref="AreaEase"/> 表示，编号对应 <see cref="AreaEaseType"/>。
        /// </summary>
        [JsonProperty("easeType")]
        public AreaEase EaseType { get; set; }

        /// <summary>
        /// 目标旋转角度，单位为度；首个关键帧之前的旋转角度为 0。
        /// </summary>
        [JsonProperty("rotation")]
        public float Rotation { get; set; }

        /// <summary>
        /// 获取指定音乐时间的旋转角度。
        /// </summary>
        /// <param name="time">当前音乐时间，单位为秒。</param>
        /// <param name="previousEventTime">前一个关键帧时间，单位为秒。</param>
        /// <param name="startRotation">前一个关键帧的旋转角度。</param>
        /// <returns>指定时间的旋转角度。</returns>
        public float GetRotationAtTime(float time, float previousEventTime, float startRotation)
        {
            var progress = AreaEase.GetProgress(previousEventTime, Time, time);
            return EaseType.Interpolate(startRotation, Rotation, progress);
        }

        /// <summary>
        /// 获取指定音乐时间的旋转中心坐标。
        /// </summary>
        /// <param name="time">当前音乐时间，单位为秒。</param>
        /// <param name="previousEventTime">前一个关键帧时间，单位为秒。</param>
        /// <param name="startAnchor">前一个关键帧的旋转中心坐标。</param>
        /// <returns>指定时间的旋转中心坐标。</returns>
        public PositionUnit GetAnchorAtTime(
            float time,
            float previousEventTime,
            PositionUnit startAnchor
        )
        {
            var progress = AreaEase.GetProgress(previousEventTime, Time, time);
            return AreaEase.Interpolate(startAnchor, Anchor, progress, EaseType, EaseType);
        }
    }
}
