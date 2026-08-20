using static KaedePhi.Core.Utils.Easings;

namespace KaedePhi.Core.RePhiEdit
{
    public static class Easings
    {
        // 在任意起点和终点之间评估缓动
        private static double Evaluate(EasingFunction function, double start, double end, double t)
        {
            // 代码来自 PhiZone Player
            var progress = function(start + (end - start) * t);
            var progressStart = function(start);
            var progressEnd = function(end);
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
            return Evaluate(PhiEdit.Easings.GetFunction(easingType), start, end, t);
        }
    }
}
