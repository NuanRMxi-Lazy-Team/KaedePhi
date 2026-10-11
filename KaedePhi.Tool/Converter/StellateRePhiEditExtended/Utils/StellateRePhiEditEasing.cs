using PhiEditEasings = KaedePhi.Core.Formats.PhiEdit.Model.Easings;
using RpeEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<float>;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;

/// <summary>
/// 提供 StellateRePhiEditExtended 缓动类型映射和曲线求值功能。
/// </summary>
public static class StellateRePhiEditEasing
{
    /// <summary>
    /// 将缓动编号规范到支持的取值范围。
    /// </summary>
    /// <param name="easingType">缓动编号</param>
    public static int NormalizeType(int easingType)
    {
        if (easingType is <= -1 and >= -28)
            easingType = -easingType;
        return easingType is >= 1 and <= 28 ? easingType : 1;
    }

    /// <summary>
    /// 判断事件是否包含有效的贝塞尔控制点。
    /// </summary>
    /// <param name="evt">待检查的 RePhiEdit 事件</param>
    public static bool IsBezierEvent(RpeEvent evt) =>
        evt is { IsBezier: true, BezierPoints.Length: 4 };

    // 将 RPE 缓动编号转换为 Phigros 噪域缓动编号。
    /// <summary>
    /// 将 RePhiEdit 缓动编号映射为噪域缓动编号。
    /// </summary>
    /// <param name="easingType">RePhiEdit 缓动编号</param>
    /// <param name="areaEaseType">映射后的噪域缓动编号；未映射时为负数</param>
    public static bool TryMapToBlockArea(int easingType, out int areaEaseType)
    {
        areaEaseType = NormalizeType(easingType) switch
        {
            1 => 0,
            5 => 1,
            4 => 2,
            7 => 3,
            9 => 4,
            8 => 5,
            12 => 6,
            11 => 7,
            10 => 8,
            13 => 9,
            15 => 10,
            14 => 11,
            _ => -1,
        };
        return areaEaseType >= 0;
    }

    /// <summary>
    /// 根据事件缓动设置计算指定进度的值。
    /// </summary>
    /// <param name="evt">包含缓动设置的 RePhiEdit 事件</param>
    /// <param name="progress">待求值的进度</param>
    /// <param name="escape">是否将区间外进度按端点规则处理</param>
    public static double Evaluate(RpeEvent evt, double progress, bool escape)
    {
        if (IsBezierEvent(evt))
            return BezierEase(progress, evt.BezierPoints, escape);

        return RpeEase(
            progress,
            NormalizeType((int)evt.Easing),
            evt.EasingLeft,
            evt.EasingRight,
            escape
        );
    }

    // RPE 的区间退化与端点外推规则不同于 Core 的通用归一化处理。
    private static double RpeEase(
        double progress,
        int easingType,
        double left,
        double right,
        bool escape
    )
    {
        if (progress <= 0d && escape)
            return 0d;
        if (progress >= 1d)
            return 1d;
        if (left == 0d && right == 1d)
            return RpeEase(progress, easingType, escape);
        if (right == left)
            return double.NaN;

        var leftValue = RpeEase(left, easingType, true);
        var rightValue = RpeEase(right, easingType, true);
        if (leftValue == rightValue)
            return double.NaN;

        progress = left + (right - left) * progress;
        var value = RpeEase(progress, easingType, escape);
        return !double.IsFinite(value) ? value : (value - leftValue) / (rightValue - leftValue);
    }

    private static double RpeEase(double progress, int easingType, bool escape = true)
    {
        if (progress <= 0d && escape)
            return 0d;
        if (progress >= 1d)
            return 1d;

        // RPE 的指数端点处理、回退系数和弹性周期与 Core 通用曲线不同。
        return easingType switch
        {
            16 => 1d - Math.Pow(2d, -10d * progress),
            17 => Math.Pow(2d, 10d * (progress - 1d)),
            23 => progress < 0.5d
                ? (21.61264d * Math.Pow(progress, 3d) - 6.80632d * Math.Pow(progress, 2d)) / 2d
                : (
                    2d
                    + 2.70158d * Math.Pow(progress * 2d - 2d, 3d)
                    + 1.70158d * Math.Pow(progress * 2d - 2d, 2d)
                ) / 2d,
            24 => Math.Pow(2d, -10d * progress)
                * Math.Sin((10d * progress - 0.75d) * 2.094395d)
                + 1d,
            25 => -Math.Pow(2d, 10d * (progress - 1d))
                * Math.Sin((10d * progress - 10.75d) * 2.094395d),
            >= 1 and <= 28 => PhiEditEasings.GetFunction(easingType)(progress),
            _ => progress,
        };
    }

    // Core 贝塞尔工具会截断进度；此处需要保留 RPE 曲线的负进度外推。
    private static double BezierEase(double x, float[] points, bool escape)
    {
        if (x <= 0d && escape)
            return 0d;
        if (x >= 1d)
            return 1d;

        const int maxIterations = 10;
        const double epsilon = 0.0000001d;
        var t = 0.5d;
        var closestY = 0d;
        var minDistance = 2d;

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var currentX = 3d * (1d - t) * (1d - t) * t * points[0]
                + 3d * (1d - t) * t * t * points[2]
                + t * t * t;
            var currentY = 3d * (1d - t) * (1d - t) * t * points[1]
                + 3d * (1d - t) * t * t * points[3]
                + t * t * t;
            var distance = Math.Abs(currentX - x);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestY = currentY;
            }
            if (distance < epsilon)
                return currentY;

            var dx = 3d * (1d - t - 2d * t) * (1d - t) * points[0]
                + 3d * ((1d - t) * 2d - t) * t * points[2]
                + 3d * t * t;
            if (Math.Abs(dx) < 1e-12d)
                break;
            t -= (currentX - x) / dx;
        }

        return closestY;
    }
}
