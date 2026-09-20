using System.ComponentModel;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.PhiZonePlayerExtended
{
    public class Note : global::KaedePhi.Core.Formats.RePhiEdit.Note
    {
        /// <summary>
        /// 可以更好地控制音符应显示在哪一层。
        /// 有关默认值，请参阅<see>https://github.com/PhiZone/player#z-indexes</see>
        /// </summary>
        [JsonProperty("zIndex")]
        public float ZIndex { get; set; }

        /// <summary>
        /// 为音符的打击效果设置层级。
        /// </summary>
        [JsonProperty("zIndexHitEffects", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(7)]
        public float ZIndexHitEffects { get; set; } = 7;

        /// <summary>
        /// 与RPE标准不同的是，他们仅仅字段名称不同，作用完全一致，表示判定区域的倍率。
        /// </summary>
        [JsonProperty("judgeSize")]
        public float JudgeSize => JudgeArea;
    }
}
