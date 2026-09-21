using System.ComponentModel;
using KaedePhi.Core.Formats.PhiZonePlayerExtended.Model.JudgeLines;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.PhiZonePlayerExtended.Model
{
    public class JudgeLine : global::KaedePhi.Core.Formats.RePhiEdit.Model.JudgeLine
    {
        /// <summary>
        /// 用于决定判定线的ScaleX事件对判定线上音符的影响，默认不影响
        /// </summary>
        [JsonProperty("scaleOnNotes", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(ScaleOnNotes.None)]
        public ScaleOnNotes ScaleOnNotes { get; set; } = ScaleOnNotes.None;

        /// <summary>
        /// 决定当UI组件或任何视频附加到该线上时，该线将如何显示。
        /// 颜色事件将覆盖这些选项定义的颜色。默认为Hidden。
        /// </summary>
        [JsonProperty("scaleOnNotesEffect", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(AppearanceOnAttach.Hidden)]
        public AppearanceOnAttach AppearanceOnAttach { get; set; } = AppearanceOnAttach.Hidden;

        /// <summary>
        /// false：将非线性速度缓动函数直接视为随时间变化的高度；
        /// true：对速度缓动函数进行积分以获得高度函数。
        /// </summary>
        [JsonProperty("integrateSpeedEasings")]
        [DefaultValue(true)]
        public bool IntegrateSpeedEasings { get; set; } = true;

        /// <summary>
        /// 它将覆盖zOrder属性，从而可以更好地控制线条应显示在哪一层。
        /// 有关默认值，请参阅<see>https://github.com/PhiZone/player#z-indexes</see>
        /// </summary>
        [JsonProperty("zIndex", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(2)]
        public float ZIndex { get; set; } = 2;
    }
}
