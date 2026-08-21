using System;
using JetBrains.Annotations;
using KaedePhi.Core.Common;

namespace KaedePhi.Core.KaedePhi
{
    /// <summary>
    /// BPM 节点，定义某一拍点的 BPM 值。
    /// </summary>
    public class BpmItem
    {
        private float _bpm = 120f;

        /// <summary>
        /// BPM 值，必须是有限正数。
        /// </summary>
        public float Bpm
        {
            get => _bpm;
            set
            {
                if (!float.IsFinite(value) || value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(Bpm), "BPM 必须是有限正数。");
                }
                _bpm = value;
            }
        }

        /// <summary>
        /// BPM 生效的起始拍。
        /// </summary>
        [PublicAPI]
        public Beat StartBeat { get; set; } = new(new[] { 0, 0, 1 });

        /// <summary>
        /// 深拷贝 BPM 节点。
        /// </summary>
        /// <returns>BPM 节点副本</returns>
        public BpmItem Clone()
        {
            return new BpmItem { Bpm = Bpm, StartBeat = new Beat((int[])StartBeat) };
        }
    }
}
