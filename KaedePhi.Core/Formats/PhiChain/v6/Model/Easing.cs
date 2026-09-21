using KaedePhi.Core.Formats.PhiChain.v6.Serialization.JsonConverter;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.PhiChain.v6.Model
{
    public enum EasingKind
    {
        Linear,
        EaseInSine,
        EaseOutSine,
        EaseInOutSine,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        EaseInQuart,
        EaseOutQuart,
        EaseInOutQuart,
        EaseInQuint,
        EaseOutQuint,
        EaseInOutQuint,
        EaseInExpo,
        EaseOutExpo,
        EaseInOutExpo,
        EaseInCirc,
        EaseOutCirc,
        EaseInOutCirc,
        EaseInBack,
        EaseOutBack,
        EaseInOutBack,
        EaseInElastic,
        EaseOutElastic,
        EaseInOutElastic,
        EaseInBounce,
        EaseOutBounce,
        EaseInOutBounce,
        Custom,
        Steps,
        Elastic,
    }

    [JsonConverter(typeof(EasingJsonConverter))]
    public sealed class Easing
    {
        public static Easing Linear => new() { EasingType = EasingKind.Linear };

        [JsonIgnore]
        public EasingKind EasingType { get; set; } = EasingKind.Linear;

        [JsonIgnore]
        public float X1 { get; set; }

        [JsonIgnore]
        public float Y1 { get; set; }

        [JsonIgnore]
        public float X2 { get; set; }

        [JsonIgnore]
        public float Y2 { get; set; }

        [JsonIgnore]
        public int Count { get; set; }

        [JsonIgnore]
        public float Omega { get; set; }

        /// <summary>
        /// 深克隆当前 Easing 对象
        /// </summary>
        public Easing Clone()
        {
            return new Easing
            {
                EasingType = EasingType,
                X1 = X1,
                Y1 = Y1,
                X2 = X2,
                Y2 = Y2,
                Count = Count,
                Omega = Omega,
            };
        }
    }
}
