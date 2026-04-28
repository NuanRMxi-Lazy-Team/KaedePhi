using System;
using KaedePhi.Core.Common;
using Newtonsoft.Json;

namespace KaedePhi.Core.RePhiEdit
{
    public class BpmItem
    {
        private float _beatPerMinute = 120f;

        [JsonProperty("bpm")]
        public float Bpm
        {
            get => _beatPerMinute;
            set { _beatPerMinute = value; }
        }

        [JsonIgnore]
        [Obsolete("拍与时间容易产生歧义，未来将会改为StartBeat", false)]
        public Beat StartTime
        {
            get => StartBeat;
            set => StartBeat = value;
        }

        [JsonProperty("startTime")] public Beat StartBeat = new Beat(BeatArray);
        private static readonly int[] BeatArray = { 0, 0, 1 };

        public BpmItem Clone()
        {
            return new BpmItem()
            {
                Bpm = Bpm,
                StartBeat = new Beat((int[])StartBeat)
            };
        }
    }

    [Obsolete("由于Bpm类名容易产生争议，请改用BpmItem", false)]
    public class Bpm : BpmItem
    {
    }
}