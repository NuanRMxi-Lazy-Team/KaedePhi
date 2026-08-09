using System.Collections.Generic;
using Newtonsoft.Json;

namespace KaedePhi.Core.PhiFans
{
    public partial class Chart
    {
        /// <summary>
        /// 谱面基本信息
        /// </summary>
        [JsonProperty("info")]
        public Info Info { get; set; } = new();

        /// <summary>
        /// 谱面偏移
        /// </summary>
        [JsonProperty("offset")]
        public int Offset { get; set; }

        /// <summary>
        /// 谱面BPM列表
        /// </summary>
        [JsonProperty("bpm")]
        public List<Bpm> BpmList { get; set; } = new();

        /// <summary>
        /// 谱面判定线列表
        /// </summary>
        [JsonProperty("lines")]
        public List<Line> JudgeLineList { get; set; } = new();

        /// <summary>
        /// 坐标系范围
        /// </summary>
        public static class CoordinateSystem
        {
            /// <summary>X 轴最大值</summary>
            public const float MaxX = 100f;

            /// <summary>X 轴最小值</summary>
            public const float MinX = -100f;

            /// <summary>Y 轴最大值</summary>
            public const float MaxY = 100f;

            /// <summary>Y 轴最小值</summary>
            public const float MinY = -100f;

            /// <summary>旋转方向是否为顺时针</summary>
            public const bool ClockwiseRotation = true;
        }
    }
}
