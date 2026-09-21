using KaedePhi.Core.Primitives;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.RePhiEdit.Model
{
    public class BpmItem
    {
        [JsonProperty("bpm")]
        public float Bpm { get; set; } = 120f;

        [JsonProperty("startTime")]
        public Beat StartBeat { get; set; } = new(BeatArray);
        private static readonly int[] BeatArray = { 0, 0, 1 };

        public BpmItem Clone()
        {
            return new BpmItem { Bpm = Bpm, StartBeat = new Beat((int[])StartBeat) };
        }
    }
}
