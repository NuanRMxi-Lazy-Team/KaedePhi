using static KaedePhi.Core.Utils.Easings;

namespace KaedePhi.Core.KaedePhi
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
            var span = progressEnd - progressStart;
            return System.Math.Abs(span) <= 1e-12d ? t : (progress - progressStart) / span;
        }

        /// <summary>
        /// 使用指定编号的缓动函数在给定区间计算进度。
        /// </summary>
        /// <param name="easingType">缓动函数编号。</param>
        /// <param name="start">缓动区间左端点。</param>
        /// <param name="end">缓动区间右端点。</param>
        /// <param name="t">区间内的线性进度。</param>
        /// <returns>归一化后的缓动进度。</returns>
        public static double Evaluate(int easingType, double start, double end, double t)
        {
            EasingFunction function = easingType switch
            {
                1 => Linear,
                // 正弦
                2 => EaseInSine,
                3 => EaseOutSine,
                4 => EaseInOutSine,
                // 二次
                5 => EaseInQuad,
                6 => EaseOutQuad,
                7 => EaseInOutQuad,
                // 三次
                8 => EaseInCubic,
                9 => EaseOutCubic,
                10 => EaseInOutCubic,
                // 四次
                11 => EaseInQuart,
                12 => EaseOutQuart,
                13 => EaseInOutQuart,
                // 五次
                14 => EaseInQuint,
                15 => EaseOutQuint,
                16 => EaseInOutQuint,
                // 指数
                17 => EaseInExpo,
                18 => EaseOutExpo,
                19 => EaseInOutExpo,
                // 圆形
                20 => EaseInCirc,
                21 => EaseOutCirc,
                22 => EaseInOutCirc,
                // 回弹
                23 => EaseInBack,
                24 => EaseOutBack,
                25 => EaseInOutBack,
                // 弹性
                26 => EaseInElastic,
                27 => EaseOutElastic,
                28 => EaseInOutElastic,
                // 弹跳
                29 => EaseInBounce,
                30 => EaseOutBounce,
                31 => EaseInOutBounce,
                // 兜底
                _ => Linear,
            };

            return Evaluate(function, start, end, t);
        }
    }
}
