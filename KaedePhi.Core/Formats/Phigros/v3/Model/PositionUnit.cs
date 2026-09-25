using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    public struct PositionUnit
    {
        [JsonProperty("x")]
        public float X { get; set; }
        
        [JsonProperty("y")]
        public float Y { get; set; }
    }
}