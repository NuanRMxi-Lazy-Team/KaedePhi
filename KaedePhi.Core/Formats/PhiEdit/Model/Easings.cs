using JetBrains.Annotations;
using static KaedePhi.Core.Primitives.Mathematics.Easings;

namespace KaedePhi.Core.Formats.PhiEdit.Model
{
    public static class Easings
    {
        /// <summary>
        /// 根据 PhiEdit 缓动编号获取对应的缓动函数。
        /// </summary>
        public static EasingFunction GetFunction(int easingType) =>
            easingType switch
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
                _ => Linear,
            };

        // 在任意起点和终点之间评估缓动
        [PublicAPI]
        public static double Evaluate(EasingFunction function, double t)
        {
            return function(t);
        }

        // 使用 int 指定对应的缓动函数
        public static double Evaluate(int easingType, double t)
        {
            return Evaluate(GetFunction(easingType), t);
        }
    }
}
