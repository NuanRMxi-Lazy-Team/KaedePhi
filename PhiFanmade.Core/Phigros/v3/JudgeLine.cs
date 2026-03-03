using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhiFanmade.Core.Phigros.v3
{
    public class JudgeLine
    {
        /// <summary>
        /// 判定线的BPM
        /// </summary>
        [JsonProperty("bpm")] public float Bpm = 120;
        /// <summary>
        /// 从判定线上方下落的音符列表
        /// </summary>
        [JsonProperty("notesAbove")] public List<Note> NotesAbove = new List<Note>();

        /// <summary>
        /// 从判定线下方下落的音符列表
        /// </summary>
        [JsonProperty("notesBelow")] public List<Note> NotesBelow = new List<Note>();

        /// <summary>
        /// 速度事件列表
        /// </summary>
        [JsonProperty("speedEvents")]
        public List<SpeedEvent> SpeedEvents = new List<SpeedEvent>();

        /// <summary>
        /// 判定线移动事件
        /// </summary>
        [JsonProperty("judgeLineMoveEvents")]
        public List<Event> JudgeLineMoveEvents = new List<Event>();
        /// <summary>
        /// 判定线旋转事件，旋转方向为顺时针
        /// </summary>
        [JsonProperty("judgeLineRotateEvents")]
        public List<Event> JudgeLineRotateEvents = new List<Event>();
        /// <summary>
        /// 判定线透明度事件，数值范围为0~1，数值越大透明度越高
        /// </summary>
        [JsonProperty("judgeLineDisappearEvents")]
        public List<Event> JudgeLineDisappearEvents = new List<Event>();
    }
}