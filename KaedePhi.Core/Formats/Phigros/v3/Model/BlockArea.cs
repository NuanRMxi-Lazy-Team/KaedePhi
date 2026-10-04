using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    /// <summary>
    /// 表示 Phigros v3 谱面中的噪域及其显示时序和变换事件。
    /// </summary>
    public class BlockArea
    {
        /// <summary>
        /// 噪域的一个对角坐标，以谱面渲染范围左下角为原点，坐标可超出 0 到 1。
        /// </summary>
        [JsonProperty("topRightPercentage")]
        public PositionUnit TopRightPercentage { get; set; }

        /// <summary>
        /// 噪域的另一个对角坐标，以谱面渲染范围左下角为原点，坐标可超出 0 到 1。
        /// </summary>
        [JsonProperty("bottomLeftPercentage")]
        public PositionUnit BottomLeftPercentage { get; set; }

        /// <summary>
        /// 获取由两个对角坐标确定的噪域中心。
        /// </summary>
        [JsonIgnore]
        public PositionUnit Center =>
            new()
            {
                X = (TopRightPercentage.X + BottomLeftPercentage.X) / 2f,
                Y = (TopRightPercentage.Y + BottomLeftPercentage.Y) / 2f,
            };

        /// <summary>
        /// 噪域开始显示的音乐时间，单位为秒。
        /// </summary>
        [JsonProperty("appearTime")]
        public float AppearTime { get; set; }

        /// <summary>
        /// 噪域进入激活状态的音乐时间，单位为秒。
        /// </summary>
        [JsonProperty("enableTime")]
        public float EnableTime { get; set; }

        /// <summary>
        /// 噪域离开激活状态的音乐时间，单位为秒。
        /// </summary>
        [JsonProperty("disableTime")]
        public float DisableTime { get; set; }

        /// <summary>
        /// 噪域停止显示的音乐时间，单位为秒。
        /// </summary>
        [JsonProperty("disappearTime")]
        public float DisappearTime { get; set; }

        /// <summary>
        /// 指示该噪域是否为减算区域。
        /// </summary>
        [JsonProperty("isSubtract")]
        public bool IsSubtract { get; set; }

        /// <summary>
        /// 噪域的旋转关键帧列表。
        /// </summary>
        [JsonProperty("rotateEvents")]
        public List<AreaRotateEvent> RotateEvents { get; set; } = new();

        /// <summary>
        /// 噪域的移动关键帧列表。
        /// </summary>
        [JsonProperty("moveEvents")]
        public List<AreaMoveEvent> MoveEvents { get; set; } = new();

        /// <summary>
        /// 噪域的缩放关键帧列表。
        /// </summary>
        [JsonProperty("scaleEvents")]
        public List<AreaScaleEvent> ScaleEvents { get; set; } = new();

        /// <summary>
        /// 根据音乐时间确定噪域当前所处的显示阶段。
        /// </summary>
        /// <param name="time">当前音乐时间，单位为秒。</param>
        /// <returns>噪域在指定时间所处的阶段。</returns>
        /// <exception cref="ArgumentOutOfRangeException">时间不是有限数值。</exception>
        public BlockAreaPhase GetPhase(float time)
        {
            if (float.IsNaN(time) || float.IsInfinity(time))
                throw new ArgumentOutOfRangeException(nameof(time), "时间必须是有限数值。");

            if (time < AppearTime)
                return BlockAreaPhase.HiddenBefore;
            if (time >= DisappearTime)
                return BlockAreaPhase.HiddenAfter;
            if (time >= EnableTime && time < DisableTime)
                return BlockAreaPhase.Active;
            if (time >= EnableTime - ReadyDurationSeconds && time < EnableTime)
                return BlockAreaPhase.Ready;
            return BlockAreaPhase.Disabled;
        }

        /// <summary>
        /// 激活前的预备阶段时长，单位为秒。
        /// </summary>
        public const float ReadyDurationSeconds = 0.5f;

        /// <summary>
        /// 原版噪域进入未激活显示状态时的渐显时长，单位为秒。
        /// </summary>
        public const float DisabledShowDurationSeconds = 0.5f;
    }
}
