using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.PhiFanmadeNrc
{
    public class BpmItem
    {
        public float BeatPerMinute = 120f;
        public float Bpm => BeatPerMinute;
        public Beat StartBeat = new Beat(new[] { 0, 0, 1 });
        public BpmItem Clone()
        {
            return new BpmItem()
            {
                BeatPerMinute = BeatPerMinute,
                StartBeat = new Beat((int[])StartBeat)
            };
        }
    }
}