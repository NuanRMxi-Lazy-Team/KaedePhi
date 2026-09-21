using JetBrains.Annotations;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.RePhiEdit.Model
{
    /// <summary>
    /// 谱面元数据
    /// </summary>
    public class Meta
    {
        /// <summary>
        /// RePhiEdit版本号
        /// </summary>
        [JsonProperty("RPEVersion")]
        public int RpeVersion { get; set; } = 170; // RPE版本

        /// <summary>
        /// 曲绘的相对路径
        /// </summary>
        [JsonProperty("background")]
        public string Background { get; set; } = "0.jpg"; // 曲绘

        /// <summary>
        /// 谱师
        /// </summary>
        [JsonProperty("charter")]
        public string Charter { get; set; } = "KaedePhiCore"; // 谱师

        /// <summary>
        /// 谱面音乐作者
        /// </summary>
        [JsonProperty("composer")]
        public string Composer { get; set; } = "Unknown"; // 曲师

        /// <summary>
        /// 谱面曲绘作者
        /// </summary>
        [JsonProperty("illustration")]
        public string Illustration { get; set; } = "Unknown"; // 曲绘画师

        /// <summary>
        /// 谱面难度
        /// </summary>
        [JsonProperty("level")]
        public string Level { get; set; } = "NR  Lv.17"; // 难度

        /// <summary>
        /// 谱面名称
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = "KaedePhiCore by NuanR_Star Ciallo Team"; // 曲名

        /// <summary>
        /// 谱面偏移，单位为毫秒
        /// </summary>
        [JsonProperty("offset")]
        public int Offset { get; set; } // 音乐偏移

        /// <summary>
        /// 音乐的相对路径
        /// </summary>
        [JsonProperty("song")]
        public string Song { get; set; } = "0.wav"; // 音乐

        /// <summary>
        /// 谱面唯一标识符
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = "0";

        public override string ToString() => ToString(Id + ".json");

        [PublicAPI]
        public string ToString(string fileName)
        {
            return "#\n"
                + $"Name: {Name}\n"
                + $"Path: {Id}\n"
                + $"Song: {Song}\n"
                + $"Picture: {Background}\n"
                + $"Chart: {fileName}\n"
                + $"Level: {Level}\n"
                + $"Composer: {Composer}\n"
                + $"Charter: {Charter}\n";
        }

        public Meta Clone()
        {
            // 这个没必要自己实现，直接MemberwiseClone就行
            return (Meta)MemberwiseClone();
        }
    }
}
