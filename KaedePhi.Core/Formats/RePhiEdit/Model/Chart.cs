using System.Collections.Generic;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.RePhiEdit
{
    public partial class Chart
    {
        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static class CoordinateSystem
        {
            public const float MaxX = 675f;
            public const float MinX = -675f;
            public const float MaxY = 450f;
            public const float MinY = -450f;
            public const bool ClockwiseRotation = true;
        }

        /// <summary>
        /// BPM列表
        /// </summary>
        [JsonProperty("BPMList")]
        public List<BpmItem> BpmList { get; set; } = new();

        /// <summary>
        /// 元数据
        /// </summary>
        [JsonProperty("META")]
        public Meta Meta { get; set; } = new();

        /// <summary>
        /// 判定线列表
        /// </summary>
        [JsonProperty("judgeLineList")]
        public List<JudgeLine> JudgeLineList { get; set; } = new();

        /// <summary>
        /// 制谱时长（秒）
        /// </summary>
        [JsonProperty("chartTime")]
        public double ChartTime { get; set; }

        /// <summary>
        /// 判定线组
        /// </summary>
        [JsonProperty("judgeLineGroup")]
        public string[] JudgeLineGroup { get; set; } = { "Default" };

        /// <summary>
        /// 多线编辑判定线列表（以空格为分割，或使用x:y选中x~y所有判定线）
        /// </summary>
        [JsonProperty("multiLineString")]
        public string MultiLineString { get; set; } = "1";

        /// <summary>
        /// 多线编辑页面缩放比例
        /// </summary>
        [JsonProperty("multiScale")]
        public float MultiScale { get; set; } = 1.0f;

        /// <summary>
        /// RPE右上角进度条显示的标记
        /// </summary>
        [JsonProperty("timeTags")]
        public List<BeatTag> BeatTags { get; set; } = new();

        /// <summary>
        /// XY事件是否一一对应
        /// </summary>
        // ReSharper disable once StringLiteralTypo
        [JsonProperty("xybind")]
        public bool XyBind { get; set; } = true;
    }
}
