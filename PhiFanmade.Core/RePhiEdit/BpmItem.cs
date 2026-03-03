using System;
using Newtonsoft.Json;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.RePhiEdit
{
    public class BpmItem
    {
        [JsonProperty("bpm")] public float BeatPerMinute = 120f;
        [JsonIgnore] public float Bpm => BeatPerMinute;

        [JsonIgnore]
        [Obsolete("拍与时间容易产生歧义，未来将会改为StartBeat",false)]
        public Beat StartTime
        {
            get => StartBeat;
            set => StartBeat = value;
        }

        [JsonProperty("startTime")] public Beat StartBeat = new Beat(new[] { 0, 0, 1 });

        public BpmItem Clone()
        {
            return new BpmItem()
            {
                BeatPerMinute = BeatPerMinute,
                StartBeat = new Beat((int[])StartBeat)
            };
        }
    }

    [Obsolete("由于Bpm类名容易产生争议，请改用BpmItem", false)]
    public class Bpm : BpmItem
    {
    }
}