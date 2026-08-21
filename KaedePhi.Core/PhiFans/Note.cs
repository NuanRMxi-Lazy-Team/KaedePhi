using System.Runtime.Serialization;
using KaedePhi.Core.Common;
using Newtonsoft.Json;

namespace KaedePhi.Core.PhiFans
{
    public class Note
    {
        private Beat _holdEndBeat = new(0);
        private bool _hasExplicitHoldEndBeat;

        [JsonProperty("type")]
        public NoteType Type { get; set; } = NoteType.Tap;

        [JsonProperty("beat")]
        public Beat Beat { get; set; } = new(0);

        [JsonProperty("positionX")]
        public float PositionX { get; set; }

        [JsonProperty("speed")]
        public float Speed { get; set; } = 1f;

        [JsonProperty("isAbove")]
        public bool IsAbove { get; set; } = true;

        [JsonProperty("holdEndBeat")]
        public Beat HoldEndBeat
        {
            get => _holdEndBeat;
            set
            {
                _holdEndBeat = value;
                _hasExplicitHoldEndBeat = true;
            }
        }

        [JsonIgnore]
        internal bool HasExplicitHoldEndBeat => _hasExplicitHoldEndBeat;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context) => _hasExplicitHoldEndBeat = false;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Type == NoteType.Hold && (!_hasExplicitHoldEndBeat || HoldEndBeat <= Beat))
                throw new JsonSerializationException("Hold 音符缺少有效的结束拍。");
        }
    }

    public enum NoteType
    {
        Tap = 1,
        Hold = 3,
        Flick = 4,
        Drag = 2,
    }
}
