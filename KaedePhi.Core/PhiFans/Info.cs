using Newtonsoft.Json;

namespace KaedePhi.Core.PhiFans
{
    public class Info
    {
        [JsonProperty("name")] public string Name { get; set; } = ""; // 曲名
        [JsonProperty("artist")] public string Artist { get; set; } = ""; // 曲师
        [JsonProperty("illustration")] public string Illustration { get; set; } = ""; // 插画
        [JsonProperty("level")] public string Level { get; set; } = ""; // 等级
        [JsonProperty("designer")] public string Designer { get; set; } = ""; // 谱师
    }
}