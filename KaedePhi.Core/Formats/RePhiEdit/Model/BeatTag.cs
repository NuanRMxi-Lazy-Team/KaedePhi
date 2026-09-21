using KaedePhi.Core.Primitives;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.RePhiEdit.Model
{
    public class BeatTag
    {
        /// <summary>
        /// 标签名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = "Wow";

        /// <summary>
        /// 标签所在拍
        /// </summary>
        [JsonProperty("time")]
        public Beat Time { get; set; }
    }
}
