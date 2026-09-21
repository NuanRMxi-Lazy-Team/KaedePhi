using System.Runtime.Serialization;
using KaedePhi.Core.Formats.PhiChain.v6.Serialization.JsonConverter;
using KaedePhi.Core.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KaedePhi.Core.Formats.PhiChain.v6.Model
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NoteType
    {
        [EnumMember(Value = "tap")]
        Tap,

        [EnumMember(Value = "drag")]
        Drag,

        [EnumMember(Value = "hold")]
        Hold,

        [EnumMember(Value = "flick")]
        Flick,
    }

    [JsonConverter(typeof(NoteJsonConverter))]
    public sealed class Note
    {
        [JsonIgnore]
        public NoteType Type { get; set; } = NoteType.Tap;

        [JsonIgnore]
        public Beat HoldBeat { get; set; } = new(new[] { 0, 0, 1 });

        [JsonProperty("above")]
        public bool Above { get; set; }

        [JsonProperty("beat")]
        public Beat Beat { get; set; } = new(new[] { 0, 0, 1 });

        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("speed")]
        public float Speed { get; set; } = 1f;

        /// <summary>
        /// 深克隆当前 Note 对象
        /// </summary>
        public Note Clone()
        {
            return new Note
            {
                Type = Type,
                HoldBeat = new Beat((int[])HoldBeat),
                Above = Above,
                Beat = new Beat((int[])Beat),
                X = X,
                Speed = Speed,
            };
        }
    }
}
