using Newtonsoft.Json;
using System.Collections.Generic;

namespace PhiFanmade.Core.PhiFans
{
    public class Line
    {
        /// <summary>
        /// 判定线事件组
        /// </summary>
        [JsonProperty("props")]
        public Props Props { get; set; } = new Props();

        /// <summary>
        /// 判定线音符列表
        /// </summary>
        [JsonProperty("notes")]
        public List<Note> NoteList { get; set; } = new List<Note>();
    }
}