using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    /// <summary>
    /// 表示噪域使用的二维坐标或缩放倍率。
    /// </summary>
    public struct PositionUnit
    {
        /// <summary>
        /// x 分量；坐标可超出 0 到 1，用于缩放时表示横向倍率。
        /// </summary>
        [JsonProperty("x")]
        public float X { get; set; }

        /// <summary>
        /// y 分量；坐标可超出 0 到 1，用于缩放时表示纵向倍率。
        /// </summary>
        [JsonProperty("y")]
        public float Y { get; set; }
    }
}
