using System.Collections.Generic;

namespace KaedePhi.Core.Intermediate.Model
{
    /// <summary>
    /// 用于内部跨格式转换的谱面中间表示（IR），也可供开发者直接使用和扩展。
    /// </summary>
    public partial class Chart
    {
        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static Common.CoordinateSystem CoordinateSystem { get; } = new()
        {
            MaxX = 1f,
            MinX = -1f,
            MaxY = 1f,
            MinY = -1f,
            ClockwiseRotation = false
        };

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