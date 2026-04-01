using System;
using Newtonsoft.Json;

namespace PhiFanmade.Core.Phigros.v3
{
    public class Note
    {
        /// <summary>
        /// 音符类型
        /// </summary>
        [JsonProperty("type")]
        [JsonConverter(typeof(NoteTypeConverter))]
        public NoteType Type { get; set; }

        /// <summary>
        /// 音符判定时间，单位为1.875 / bpm
        /// </summary>
        [JsonProperty("time")]
        public int Time { get; set; }

        [JsonIgnore]
        public float TimeAsBeat
        {
            get => (float)Time / 32;
            set
            {
                var time = value * 32d;
                // 小数部分必须为0，并且考虑浮点精度，如果小数部分不为0，那么报错
                if (Math.Abs(time - Math.Round(time)) > 1e-6)
                {
                    throw new InvalidOperationException(
                        $"TimeAsBeat must be a multiple of 1/32, but got {value} (time={time})");
                }

                Time = Convert.ToInt32(time);
            }
        }

        /// <summary>
        /// 相对于判定线中心的X坐标位置
        /// </summary>
        [JsonProperty("positionX")]
        public float PositionX { get; set; }

        /// <summary>
        /// 仅 Hold 音符有效，表示持续时间，单位为1.875 / bpm
        /// </summary>
        [JsonProperty("holdTime")]
        public float HoldTime { get; set; }

        /// <summary>
        /// 速度倍率
        /// </summary>
        [JsonProperty("speed")]
        public float Speed { get; set; }
    }

    public enum NoteType
    {
        Tap = 1,
        Drag = 2,
        Hold = 3,
        Flick = 4
    }
}