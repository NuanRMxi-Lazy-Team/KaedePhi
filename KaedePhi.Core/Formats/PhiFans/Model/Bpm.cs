using KaedePhi.Core.Primitives;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.PhiFans.Model
{
    public class Bpm
    {
        [JsonProperty("beat")]
        public Beat StartBeat { get; set; } = new(0);

        [JsonProperty("bpm")]
        public float BeatPerMinute { get; set; } = 120;
    }
}
