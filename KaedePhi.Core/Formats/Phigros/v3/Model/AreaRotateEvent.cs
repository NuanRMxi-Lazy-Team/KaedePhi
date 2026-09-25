using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    public class AreaRotateEvent
    {
        [JsonProperty("anchor")]
        public PositionUnit Anchor { get; set; }
        [JsonProperty("time")]
        public float Time { get; set; }
        /// <summary>
        /// 尚无可对照表
        /// </summary>
        [JsonProperty("easeType")]
        public int EaseType { get; set; }
        [JsonProperty("rotation")]
        public float Rotation { get; set; }
    }
}