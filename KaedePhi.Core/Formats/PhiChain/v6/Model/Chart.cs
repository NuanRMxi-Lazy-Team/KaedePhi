using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.PhiChain.v6.Model
{
    public sealed partial class Chart
    {
        [JsonProperty("format")] public ulong Format { get; set; } = Constants.CurrentFormat;

        [JsonProperty("offset")] public float Offset { get; set; }

        [JsonProperty("bpm_list")] public BpmList BpmList { get; set; } = new();

        [JsonProperty("lines")] public List<SerializedLine> Lines { get; set; } = new();

        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static Common.CoordinateSystem CoordinateSystem { get; } = new()
        {
            MaxX = 675f,
            MinX = -675f,
            MaxY = 450f,
            MinY = -450f,
            ClockwiseRotation = false,
        };

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