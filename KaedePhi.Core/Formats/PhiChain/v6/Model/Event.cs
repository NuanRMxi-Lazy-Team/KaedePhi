using System.Runtime.Serialization;
using KaedePhi.Core.Formats.PhiChain.v6.Serialization.JsonConverter;
using KaedePhi.Core.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KaedePhi.Core.Formats.PhiChain.v6.Model
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LineEventType
    {
        [EnumMember(Value = "x")]
        X,

        [EnumMember(Value = "y")]
        Y,

        [EnumMember(Value = "rotation")]
        Rotation,

        [EnumMember(Value = "opacity")]
        Opacity,

        [EnumMember(Value = "speed")]
        Speed,
    }

    public enum LineEventValueType
    {
        Transition,
        Constant,
    }

    [JsonConverter(typeof(LineEventValueJsonConverter))]
    public sealed class LineEventValue
    {
        [JsonIgnore]
        public LineEventValueType Type { get; set; } = LineEventValueType.Constant;

        [JsonIgnore]
        public float Start { get; set; }

        [JsonIgnore]
        public float End { get; set; }

        [JsonIgnore]
        public Easing Easing { get; set; } = Easing.Linear;

        [JsonIgnore]
        public float Value { get; set; }

        public static LineEventValue Transition(float start, float end, Easing? easing)
        {
            return new LineEventValue
            {
                Type = LineEventValueType.Transition,
                Start = start,
                End = end,
                Easing = easing ?? Easing.Linear,
            };
        }

        public static LineEventValue Constant(float value)
        {
            return new LineEventValue { Type = LineEventValueType.Constant, Value = value };
        }

        /// <summary>
        /// 深克隆当前 LineEventValue 对象
        /// </summary>
        public LineEventValue Clone()
        {
            return new LineEventValue
            {
                Type = Type,
                Start = Start,
                End = End,
                Easing = Easing.Clone(),
                Value = Value,
            };
        }
    }

    public sealed class LineEvent
    {
        [JsonProperty("kind")]
        public LineEventType Type { get; set; }

        [JsonProperty("start_beat")]
        public Beat StartBeat { get; set; } = new(new[] { 0, 0, 1 });

        [JsonProperty("end_beat")]
        public Beat EndBeat { get; set; } = new(new[] { 1, 0, 1 });

        [JsonProperty("value")]
        public LineEventValue Value { get; set; } = LineEventValue.Constant(0f);

        /// <summary>
        /// 深克隆当前 LineEvent 对象
        /// </summary>
        public LineEvent Clone()
        {
            return new LineEvent
            {
                Type = Type,
                StartBeat = new Beat((int[])StartBeat),
                EndBeat = new Beat((int[])EndBeat),
                Value = Value.Clone(),
            };
        }
    }
}
