using JetBrains.Annotations;
using static KaedePhi.Core.Utils.Easings;

namespace KaedePhi.Core.PhiFans
{
    public static class Easings
    {
        /// <summary>
        /// 根据 PhiFans 缓动编号获取对应的缓动函数。
        /// PhiFans 没有缓动截取（minLim/maxLim）概念，因此插值仅需归一化时间。
        /// </summary>
        public static EasingFunction GetFunction(int easingType) =>
            easingType switch
            {
                0 => Linear,
                // 正弦
                1 => EaseInSine,
                2 => EaseOutSine,
                3 => EaseInOutSine,
                // 二次
                4 => EaseInQuad,
                5 => EaseOutQuad,
                6 => EaseInOutQuad,
                // 三次
                7 => EaseInCubic,
                8 => EaseOutCubic,
                9 => EaseInOutCubic,
                // 四次
                10 => EaseInQuart,
                11 => EaseOutQuart,
                12 => EaseInOutQuart,
                // 五次
                13 => EaseInQuint,
                14 => EaseOutQuint,
                15 => EaseInOutQuint,
                // 指数
                16 => EaseInExpo,
                17 => EaseOutExpo,
                18 => EaseInOutExpo,
                // 圆形
                19 => EaseInCirc,
                20 => EaseOutCirc,
                21 => EaseInOutCirc,
                // 回弹
                22 => EaseInBack,
                23 => EaseOutBack,
                24 => EaseInOutBack,
                // 弹性
                25 => EaseInElastic,
                26 => EaseOutElastic,
                27 => EaseInOutElastic,
                // 弹跳
                28 => EaseInBounce,
                29 => EaseOutBounce,
                30 => EaseInOutBounce,
                // 兜底
                _ => Linear,
            };

        // 在归一化时间 t 处评估缓动
        [PublicAPI]
        public static double Evaluate(EasingFunction function, double t) => function(t);

        // 使用 int 指定对应的缓动函数
        public static double Evaluate(int easingType, double t) =>
            Evaluate(GetFunction(easingType), t);
    }
}
