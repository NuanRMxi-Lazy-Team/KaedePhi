using static KaedePhi.Core.Utils.Easings;

namespace KaedePhi.Core.PhiFans
{
    public static class Easings
    {
        // 在任意起点和终点之间评估缓动
        private static double Evaluate(EasingFunction function, double start, double end, double t)
        {
            // 代码来自 PhiZone Player
            double progress = function(start + (end - start) * t);
            double progressStart = function(start);
            double progressEnd = function(end);
            return (progress - progressStart) / (progressEnd - progressStart);
        }

        // 使用 int 指定对应的缓动函数
        public static double Evaluate(int easingType, double start, double end, double t)
        {
            EasingFunction function = easingType switch
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

            return Evaluate(function, start, end, t);
        }
    }
}
