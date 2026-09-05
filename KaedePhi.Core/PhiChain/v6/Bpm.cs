using System;
using System.Collections.Generic;
using System.Linq;
using KaedePhi.Core.Common;
using KaedePhi.Core.PhiChain.v6.JsonConverter;
using Newtonsoft.Json;

namespace KaedePhi.Core.PhiChain.v6
{
    public sealed class BpmPoint
    {
        [JsonProperty("beat")]
        public Beat Beat { get; set; } = new(new[] { 0, 0, 1 });

        [JsonProperty("bpm")]
        public float Bpm { get; set; } = 120f;

        [JsonIgnore]
        public float Time { get; internal set; }

        /// <summary>
        /// 深克隆当前 BpmPoint 对象
        /// </summary>
        public BpmPoint Clone()
        {
            return new BpmPoint
            {
                Beat = new Beat((int[])Beat),
                Bpm = Bpm,
                Time = Time,
            };
        }
    }

    [JsonConverter(typeof(BpmListJsonConverter))]
    public sealed class BpmList : List<BpmPoint>
    {
        public BpmList()
        {
            Add(new BpmPoint());
        }

        public BpmList(IEnumerable<BpmPoint> points)
            : base(points)
        {
            if (Count == 0)
            {
                Add(new BpmPoint());
            }

            ComputeTimes();
        }

        public void ComputeTimes()
        {
            var time = 0f;
            var lastBeat = 0f;
            var lastBpm = -1f;
            for (var i = 0; i < Count; i++)
            {
                var point = this[i];
                if (Math.Abs(lastBpm - -1f) > float.Epsilon)
                {
                    time += (point.Beat - lastBeat) * (60f / lastBpm);
                }

                lastBeat = point.Beat;
                lastBpm = point.Bpm;
                point.Time = time;
            }
        }

        /// <summary>
        /// 深克隆当前 BpmList 对象
        /// </summary>
        public BpmList Clone()
        {
            var cloned = new BpmList(this.Select(p => p.Clone()));
            cloned.ComputeTimes();
            return cloned;
        }
    }
}
