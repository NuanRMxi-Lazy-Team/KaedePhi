namespace PhiFanmade.Core.PhiFanmadeNrc
{
    /// <summary>
    /// 谱面元数据
    /// </summary>
    public class Meta
    {
        /// <summary>
        /// 曲绘的相对路径
        /// </summary>
        public string Background { get; set; } = "0.jpg"; // 曲绘

        /// <summary>
        /// 谱师
        /// </summary>
        public string Author { get; set; } = "PhiFanmadeCore"; // 谱师

        /// <summary>
        /// 谱面音乐作者
        /// </summary>
        public string Composer { get; set; } = "Unknown"; // 曲师

        /// <summary>
        /// 谱面曲绘作者
        /// </summary>
        public string Artist { get; set; } = "Unknown"; // 曲绘画师

        /// <summary>
        /// 谱面难度
        /// </summary>
        public string Level { get; set; } = "NR  Lv.17"; // 难度

        /// <summary>
        /// 谱面名称
        /// </summary>
        public string Name { get; set; } = "PhiFanmadeCore by NuanR_Star Ciallo Team"; // 曲名

        /// <summary>
        /// 谱面偏移，单位为毫秒
        /// </summary>
        public int Offset { get; set; } = 0; // 音乐偏移

        /// <summary>
        /// 音乐的相对路径
        /// </summary>
        public string Song { get; set; } = "0.wav"; // 音乐

        public Meta Clone()
        {
            // 这个没必要自己实现，直接MemberwiseClone就行
            return (Meta)MemberwiseClone();
        }
    }
}