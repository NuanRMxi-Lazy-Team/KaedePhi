using Newtonsoft.Json;

namespace PhiFanmade.Core.Phigros.v3
{
    public class Event
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
        /// 开始数值
        /// </summary>
        [JsonProperty("start")]
        public float Start;
        /// <summary>
        /// 结束数值
        /// </summary>
        [JsonProperty("end")]
        public float End;
        /// <summary>
        /// 开始数值2（通常用在移动事件中，一般值Y轴坐标）
        /// </summary>
        [JsonProperty("start2")]
        public float Start2;
        /// <summary>
        /// 结束数值2（通常用在移动事件中，一般值Y轴坐标）
        /// </summary>
        [JsonProperty("end2")]
        public float End2;
    }
}