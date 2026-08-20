using System.Runtime.Serialization;
using KaedePhi.Core.Common;
using Newtonsoft.Json;

namespace KaedePhi.Core.PhiChain.v6
{
    public sealed class CurveNoteTrack
    {
        private Beat? _holdBeat;
        private bool _hasExplicitHoldBeat;

        [JsonProperty("from")]
        public int From { get; set; }

        [JsonProperty("to")]
        public int To { get; set; }

        [JsonProperty("kind")]
        public NoteType NoteType { get; set; } = NoteType.Drag;

        [JsonProperty("hold_beat", NullValueHandling = NullValueHandling.Ignore)]
        public Beat? HoldBeat
        {
            get => _holdBeat;
            set
            {
                _holdBeat = value;
                _hasExplicitHoldBeat = true;
            }
        }

        [JsonIgnore]
        internal bool HasExplicitHoldBeat => _hasExplicitHoldBeat;

        [JsonProperty("density")]
        public uint Density { get; set; } = 16;

        [JsonProperty("curve")]
        public Easing Curve { get; set; } = Easing.Linear;

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context) => _hasExplicitHoldBeat = false;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (
                NoteType == NoteType.Hold
                && (!_hasExplicitHoldBeat || HoldBeat is null || HoldBeat <= new Beat(0))
            )
                throw new JsonSerializationException("Hold 曲线音符缺少有效的持续拍。");
        }

        /// <summary>
        /// 深克隆当前 CurveNoteTrack 对象
        /// </summary>
        public CurveNoteTrack Clone()
        {
            return new CurveNoteTrack
            {
                From = From,
                To = To,
                NoteType = NoteType,
                HoldBeat = HoldBeat != null ? new Beat((int[])HoldBeat) : null,
                Density = Density,
                Curve = Curve.Clone(),
                _hasExplicitHoldBeat = _hasExplicitHoldBeat,
            };
        }
    }
}
