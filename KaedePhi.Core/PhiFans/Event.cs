using KaedePhi.Core.Common;
using Newtonsoft.Json;

namespace KaedePhi.Core.PhiFans
{
    public class Event
    {
        [JsonProperty("beat")]
        public Beat Beat { get; set; } = new(0);

        [JsonProperty("value")]
        public float Value { get; set; }

        [JsonProperty("continuous")]
        public bool Continuous { get; set; }

        [JsonProperty("easing")]
        public Easing Easing { get; set; } = new(0);
    }
}
