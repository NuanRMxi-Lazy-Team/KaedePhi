using System;
using System.Globalization;

namespace KaedePhi.Core.PhiEdit
{
    /// <summary>
    /// 单个BPM变化点
    /// </summary>
    public class BpmItem
    {
        private float _bpm = 120f;

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
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", "bp", StartBeat, Bpm);
        }

        public BpmItem Clone()
        {
            return new BpmItem { Bpm = Bpm, StartBeat = StartBeat };
        }
    }
}
