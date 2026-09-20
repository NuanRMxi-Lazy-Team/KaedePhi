using System.Collections.Generic;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.PhiFans
{
    public class Line
    {
        /// <summary>
        /// 判定线事件组
        /// </summary>
        [JsonProperty("props")]
        public Props Props { get; set; } = new();

        /// <summary>
        /// 判定线音符列表
        /// </summary>
        [JsonProperty("notes")]
        public List<Note> NoteList { get; set; } = new();
    }
}
