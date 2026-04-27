using KaedePhi.Core.Common;
using Newtonsoft.Json;

namespace KaedePhi.Core.PhiFans
{
    public class Bpm
    {
        [JsonProperty("beat")] public Beat StartBeat { get; set; } = new Beat(0);
        [JsonProperty("bpm")] public float BeatPerMinute { get; set; } = 120;
    }
}