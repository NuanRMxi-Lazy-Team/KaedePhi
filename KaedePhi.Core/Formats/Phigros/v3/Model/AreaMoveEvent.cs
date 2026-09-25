using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    public class AreaMoveEvent
    {
        [JsonProperty("endPosition")] public PositionUnit EndPosition { get; set; }
        [JsonProperty("time")] public float Time { get; set; }

        /// <summary>
        /// 尚无可对照表
        /// </summary>
        [JsonProperty("easeTypeX")]
        public int EaseTypeX { get; set; }

        /// <summary>
        /// 尚无可对照表
        /// </summary>
        [JsonProperty("easeTypeY")]
        public int EaseTypeY { get; set; }
    }
}