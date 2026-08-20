using System;
using System.Runtime.Serialization;
using KaedePhi.Core.Phigros.v3.JsonConverter;
using Newtonsoft.Json;

namespace KaedePhi.Core.Phigros.v3
{
    public class Note
    {
        private float _holdTime;
        private bool _hasExplicitHoldTime;

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
                        $"TimeAsBeat must be a multiple of 1/32, but got {value} (time={time})"
                    );
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
        public float HoldTime
        {
            get => _holdTime;
            set
            {
                _holdTime = value;
                _hasExplicitHoldTime = true;
            }
        }

        [JsonIgnore]
        internal bool HasExplicitHoldTime => _hasExplicitHoldTime;

        /// <summary>
        /// 非 Hold 音符表示速度倍率；Hold 音符表示结束时判定线的确切速度。
        /// </summary>
        [JsonProperty("speed")]
        public float Speed { get; set; }

        /// <summary>
        /// floorPosition，恒定为0，我懒得算。
        /// </summary>
        [JsonProperty("floorPosition")]
        public float FloorPosition { get; set; }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context) => _hasExplicitHoldTime = false;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Type == NoteType.Hold && (!_hasExplicitHoldTime || HoldTime <= 0f))
                throw new JsonSerializationException("Hold 音符缺少有效的持续时间。");
        }
    }

    public enum NoteType
    {
        Tap = 1,
        Drag = 2,
        Hold = 3,
        Flick = 4,
    }
}
