using Newtonsoft.Json;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.PhiFans
{
    public class Event
    {
        [JsonProperty("beat")] public Beat Beat { get; set; } = new Beat(0);
        [JsonProperty("value")] public float Value { get; set; }
        [JsonProperty("continuous")] public bool Continuous { get; set; }
        [JsonProperty("easing")] public int Easing { get; set; }
    }
}