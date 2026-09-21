using System.Collections.Generic;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.PhiFans.Model
{
    public class Props
    {
        [JsonProperty("speed")]
        public List<Event> Speed { get; set; } = new();

        [JsonProperty("positionX")]
        public List<Event> PositionX { get; set; } = new();

        [JsonProperty("positionY")]
        public List<Event> PositionY { get; set; } = new();

        [JsonProperty("rotate")]
        public List<Event> Rotate { get; set; } = new();

        [JsonProperty("alpha")]
        public List<Event> Alpha { get; set; } = new();
    }
}
