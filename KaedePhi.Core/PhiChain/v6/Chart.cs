using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace KaedePhi.Core.PhiChain.v6
{
    [System.Obsolete("已弃用：请迁移至 KaedePhi.Core.Formats.PhiChain.v6 命名空间下的同名类型。")]
    public sealed partial class Chart
    {
        [JsonProperty("format")]
        public ulong Format { get; set; } = Constants.CurrentFormat;

        [JsonProperty("offset")]
        public float Offset { get; set; }

        [JsonProperty("bpm_list")]
        public BpmList BpmList { get; set; } = new();

        [JsonProperty("lines")]
        public List<SerializedLine> Lines { get; set; } = new();

        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static class CoordinateSystem
        {
            public const float MaxX = 675f;
            public const float MinX = -675f;
            public const float MaxY = 450f;
            public const float MinY = -450f;
            public const bool ClockwiseRotation = false;
        }

        /// <summary>
        /// 深克隆当前 Chart 对象
        /// </summary>
        public Chart Clone()
        {
            return new Chart
            {
                Format = Format,
                Offset = Offset,
                BpmList = BpmList.Clone(),
                Lines = Lines.Select(l => l.Clone()).ToList(),
            };
        }
    }
}
