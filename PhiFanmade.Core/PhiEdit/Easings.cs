using JetBrains.Annotations;
using static PhiFanmade.Core.Utils.Easings;

namespace PhiFanmade.Core.PhiEdit
{
    public static class Easings
    {
        // Method to evaluate easing between any start and end point
        [PublicAPI]
        public static double Evaluate(EasingFunction function, double t)
        {
            return function(t);
        }

        // Overload, using int to specify the corresponding EasingFunction
        public static double Evaluate(int easingType, double t)
        {
            EasingFunction function = easingType switch
            {
                1 => Linear,
                2 => EaseOutSine,
                3 => EaseInSine,
                4 => EaseOutQuad,
                5 => EaseInQuad,
                6 => EaseInOutSine,
                7 => EaseInOutQuad,
                8 => EaseOutCubic,
                9 => EaseInCubic,
                10 => EaseOutQuart,
                11 => EaseInQuart,
                12 => EaseInOutCubic,
                13 => EaseInOutQuart,
                14 => EaseOutQuint,
                15 => EaseInQuint,
                16 => EaseOutExpo,
                17 => EaseInExpo,
                18 => EaseOutCirc,
                19 => EaseInCirc,
                20 => EaseOutBack,
                21 => EaseInBack,
                22 => EaseInOutCirc,
                23 => EaseInOutBack,
                24 => EaseOutElastic,
                25 => EaseInElastic,
                26 => EaseOutBounce,
                27 => EaseInBounce,
                28 => EaseInOutBounce,
                29 => EaseInOutElastic,
                _ => Linear
            };

            return Evaluate(function, t);
        }
    }

    public class Easing
    {
        private int _easingNumber;
        private readonly EasingFunction _function;

        public Easing(int easingNumber)
        {
            _easingNumber = easingNumber;
            // 缓存缓动函数，避免每次 Do 都走 switch
            _function = t => Easings.Evaluate(easingNumber, t);
        }

        /// <summary>对 [start, end] 区间在 t 处进行插值</summary>
        public float Interpolate(float start, float end, float t)
        {
            var easedTime = _function(t);
            return (float)(start + (end - start) * easedTime);
        }

        /// <inheritdoc cref="Interpolate(double,double,double)"/>
        public double Interpolate(double start, double end, double t)
        {
            var easedTime = _function(t);
            return start + (end - start) * easedTime;
        }

        public static implicit operator int(Easing easing) => easing._easingNumber;
    }
}