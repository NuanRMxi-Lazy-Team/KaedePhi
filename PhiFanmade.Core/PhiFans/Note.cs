using Newtonsoft.Json;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.PhiFans
{
    public class Note
    {
            [JsonProperty("type")] public NoteType Type { get; set; }= NoteType.Tap;
            [JsonProperty("beat")] public Beat Beat { get; set; }= new Beat(0);
            [JsonProperty("positionX")] public float PositionX;
            [JsonProperty("speed")] public float Speed { get; set; } = 1f;
            [JsonProperty("isAbove")] public bool IsAbove { get; set; }= true;
            [JsonProperty("holdEndBeat")] public Beat HoldEndBeat { get; set; }= new Beat(0);
        }
        
        public enum NoteType
        {
            Tap = 1,
            Hold = 3,
            Flick = 4,
            Drag = 2
        }
}