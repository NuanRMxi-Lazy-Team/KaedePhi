using KaedePhi.Core.Formats.RePhiEdit.Serialization.JsonConverter;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.RePhiEdit
{
    [JsonConverter(typeof(EasingJsonConverter))]
    public class Easing
    {
        /// <summary>
        /// 线性缓动（编号1）。
        /// </summary>
        public static Easing Linear { get; } = new(1);

        public Easing(int easingNumber)
        {
            _easingNumber = easingNumber;
        }

        [JsonIgnore]
        private readonly int _easingNumber;

        public float Interpolate(float minLim, float maxLim, float start, float end, float t)
        {
            var easedTime = Easings.Evaluate(_easingNumber, minLim, maxLim, t);
            // 插值后返回
            return (float)(start + (end - start) * easedTime);
        }

        public double Interpolate(float minLim, float maxLim, double start, double end, double t)
        {
            var easedTime = Easings.Evaluate(_easingNumber, minLim, maxLim, t);
            // 插值后返回
            return start + (end - start) * easedTime;
        }

        public int Interpolate(float minLim, float maxLim, int start, int end, float t)
        {
            var easedTime = Easings.Evaluate(_easingNumber, minLim, maxLim, t);
            // 插值后返回
            return (int)(start + (end - start) * easedTime);
        }

        public byte Interpolate(float minLim, float maxLim, byte start, byte end, float t)
        {
            var easedTime = Easings.Evaluate(_easingNumber, minLim, maxLim, t);
            // 插值后返回
            return (byte)(start + (end - start) * easedTime);
        }

        // 以int访问时，返回缓动编号
        public static implicit operator int(Easing easing) => easing._easingNumber;
    }
}
