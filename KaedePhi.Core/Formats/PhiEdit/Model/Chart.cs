using System.Collections.Generic;
using System.Linq;

namespace KaedePhi.Core.Formats.PhiEdit.Model
{
    public partial class Chart
    {
        /// <summary>
        /// 谱面偏移，单位为毫秒
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static Common.CoordinateSystem CoordinateSystem { get; } = new()
        {
            MaxX = 2048f,
            MinX = 0f,
            MaxY = 1400f,
            MinY = 0f,
            ClockwiseRotation = true,
        };

        /// <summary>
        /// 判定线列表
        /// </summary>
        public List<JudgeLine> JudgeLineList { get; set; } = new();

        /// <summary>
        /// BPM列表
        /// </summary>
        public List<BpmItem> BpmList { get; set; } = new();

        public Chart Clone()
        {
            var clonedChart = new Chart
            {
                Offset = Offset,
                BpmList = BpmList.Select(b => b.Clone()).ToList(),
                JudgeLineList = JudgeLineList.Select(jl => jl.Clone()).ToList(),
            };
            return clonedChart;
        }
    }
}