using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.PhiChain.v6.Model
{
    public sealed class Line
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "Unnamed Line";
    }
}
