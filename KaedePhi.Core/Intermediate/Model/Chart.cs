using System.Collections.Generic;

namespace KaedePhi.Core.Intermediate
{
    /// <summary>
    /// 用于内部跨格式转换的谱面中间表示（IR），也可供开发者直接使用和扩展。
    /// </summary>
    public partial class Chart
    {
        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static class CoordinateSystem
        {
            /// <summary>X 轴最大值</summary>
            public const float MaxX = 1f;

            /// <summary>X 轴最小值</summary>
            public const float MinX = -1f;

            /// <summary>Y 轴最大值</summary>
            public const float MaxY = 1f;

            /// <summary>Y 轴最小值</summary>
            public const float MinY = -1f;

            /// <summary>旋转方向是否为顺时针</summary>
            public const bool ClockwiseRotation = false;
        }

        /// <summary>
        /// BPM列表
        /// </summary>
        public List<BpmItem> BpmList { get; set; } = new();

        /// <summary>
        /// 元数据
        /// </summary>
        public Meta Meta { get; set; } = new();

        /// <summary>
        /// 判定线列表
        /// </summary>
        public List<JudgeLine> JudgeLineList { get; set; } = new();
    }
}
