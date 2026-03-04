using System.Collections.Generic;
using System.Linq;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.PhiEdit
{
    public partial class Chart
    {
        /// <summary>
        /// 谱面偏移，单位为毫秒
        /// </summary>
        public int Offset = 0;

        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static class CoordinateSystem
        {
            public const float MaxX = 1024f;
            public const float MinX = -1024f;
            public const float MaxY = 700f;
            public const float MinY = -700f;
            public const CoordinateSystemAnchor Anchor = CoordinateSystemAnchor.ScreenCenter;
            public const bool ClockwiseRotation = false;
        }

        /// <summary>
        /// 判定线列表
        /// </summary>
        public List<JudgeLine> JudgeLineList = new List<JudgeLine>();

        /// <summary>
        /// BPM列表
        /// </summary>
        public List<Bpm> BpmList = new List<Bpm>();

        public Chart Clone()
        {
            var clonedChart = new Chart
            {
                Offset = Offset,
                BpmList = BpmList.Select(b => b.Clone()).ToList(),
                JudgeLineList = JudgeLineList.Select(jl => jl.Clone()).ToList()
            };
            return clonedChart;
        }
    }
}