using Newtonsoft.Json;

namespace PhiFanmade.Core.Phigros.v3
{
    public class SpeedEvent
    {
        /// <summary>
        /// 事件开始时间
        /// </summary>
        [JsonProperty("startTime")]
        public float StartTime;
        /// <summary>
        /// 事件结束时间
        /// </summary>
        [JsonProperty("endTime")]
        public float EndTime;
        /// <summary>
        /// 事件值
        /// </summary>
        [JsonProperty("value")]
        public float Value;
    }
}