using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.PhiChain.v6
{
    public sealed class SerializedLine
    {
        // Rust uses serde(flatten) for line; v6 currently only contains name.
        [JsonProperty("name")]
        public string Name { get; set; } = "PhiChain Line";

        [JsonIgnore]
        public Line Line
        {
            get => new() { Name = Name };
            set => Name = value.Name;
        }

        [JsonProperty("notes")]
        public List<Note> Notes { get; set; } = new();

        [JsonProperty("events")]
        public List<LineEvent> Events { get; set; } = new();

        [JsonProperty("children")]
        public List<SerializedLine> Children { get; set; } = new();

        [JsonProperty("curve_note_tracks")]
        public List<CurveNoteTrack> CurveNoteTracks { get; set; } = new();

        /// <summary>
        /// 深克隆当前 SerializedLine 对象
        /// </summary>
        public SerializedLine Clone()
        {
            return new SerializedLine
            {
                Name = Name,
                Notes = Notes.Select(n => n.Clone()).ToList(),
                Events = Events.Select(e => e.Clone()).ToList(),
                Children = Children.Select(c => c.Clone()).ToList(),
                CurveNoteTracks = CurveNoteTracks.Select(t => t.Clone()).ToList(),
            };
        }
    }
}
