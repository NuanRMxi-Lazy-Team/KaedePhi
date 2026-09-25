using System.Collections.Generic;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    public partial class Chart
    {
        /// <summary>
        /// 格式版本号
        /// </summary>
        [JsonProperty("formatVersion")]
        public uint FormatVersion { get; set; } = 3;

        /// <summary>
        /// 谱面偏移，单位为秒
        /// </summary>
        [JsonProperty("offset")]
        public float Offset { get; set; }

        /// <summary>
        /// 判定线列表
        /// </summary>
        [JsonProperty("judgeLineList")]
        public List<JudgeLine> JudgeLineList { get; set; } = new();
        
        /// <summary>
        /// 尚不明确的字段，在行为确定之前，无任何有效注解可以提供
        /// </summary>
        public List<BlockArea> BlockAreaList { get; set; } = new();

        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static Common.CoordinateSystem CoordinateSystem { get; } = new()
        {
            MaxX = 1f,
            MinX = 0f,
            MaxY = 1f,
            MinY = 0f,
            ClockwiseRotation = false
        };
    }
}