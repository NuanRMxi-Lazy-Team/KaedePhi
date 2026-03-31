using System;

namespace PhiFanmade.Core.PhiEdit
{
    /// <summary>
    /// <inheritdoc cref="BpmItem"/>
    /// </summary>
    [Obsolete("Please use BpmItem instead of Bpm")]
    public class Bpm : BpmItem
    {
    }


    /// <summary>
    /// 单个BPM变化点
    /// </summary>
    public class BpmItem
    {
        private float _bpm = 120f;

        [Obsolete("Please use Bpm instead of BeatPerMinute")]
        public float BeatPerMinute => Bpm;

        public float Bpm
        {
            get => _bpm;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Bpm must be greater than zero.");
                _bpm = value;
            }
        }

        public float StartBeat { get; set; }

        public override string ToString()
        {
            return $"bp {StartBeat} {Bpm}";
        }

        public BpmItem Clone()
        {
            return new BpmItem
            {
                Bpm = Bpm,
                StartBeat = StartBeat
            };
        }
    }
}