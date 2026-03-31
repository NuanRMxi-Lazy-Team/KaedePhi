using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhiFanmade.Core.PhiFans
{
    public class Chart
    {
        /// <summary>
        /// 谱面基本信息
        /// </summary>
        [JsonProperty("info")]
        public Info Info { get; set; } = new Info();

        /// <summary>
        /// 谱面偏移
        /// </summary>
        [JsonProperty("offset")] public int Offset;

        /// <summary>
        /// 谱面BPM列表
        /// </summary>
        [JsonProperty("bpm")]
        public List<Bpm> BpmList { get; set; } = new List<Bpm>();

        /// <summary>
        /// 谱面判定线列表
        /// </summary>
        [JsonProperty("lines")]
        public List<Line> JudgeLineList { get; set; } = new List<Line>();

        /// <summary>
        /// 坐标系范围
        /// </summary>
        public static class CoordinateSystem
        {
            public const float MaxX = 100f;
            public const float MinX = -100f;
            public const float MaxY = 100f;
            public const float MinY = -100f;
        }
    }
}