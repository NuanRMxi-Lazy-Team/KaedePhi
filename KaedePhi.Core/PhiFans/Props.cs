using System.Collections.Generic;
using Newtonsoft.Json;

namespace KaedePhi.Core.PhiFans
{
    public class Props
    {
        [JsonProperty("speed")] public List<Event> Speed { get; set; }= new List<Event>();
        [JsonProperty("positionX")] public List<Event> PositionX { get; set; }= new List<Event>();
        [JsonProperty("positionY")] public List<Event> PositionY { get; set; }= new List<Event>();
        [JsonProperty("rotate")] public List<Event> Rotate { get; set; }= new List<Event>();
        [JsonProperty("alpha")] public List<Event> Alpha { get; set; }= new List<Event>();
    }
}