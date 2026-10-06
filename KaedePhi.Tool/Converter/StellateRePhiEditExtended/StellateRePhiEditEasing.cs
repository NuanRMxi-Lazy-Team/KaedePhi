using RpeEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<float>;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended;

internal static class StellateRePhiEditEasing
{
    private const double Pi = 3.1415926535897932384626433832795d;

    internal static int NormalizeType(int easingType)
    {
        if (easingType is <= -1 and >= -28)
            easingType = -easingType;
        return easingType is >= 1 and <= 28 ? easingType : 1;
    }

    internal static bool IsBezierEvent(RpeEvent evt) =>
        evt.IsBezier && evt.BezierPoints is { Length: 4 };

    internal static bool TryMapToBlockArea(int easingType, out int areaEaseType)
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

    internal static double Evaluate(RpeEvent evt, double progress, bool escape)
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

        return easingType switch
        {
            1 => progress,
            2 => Math.Sin(progress * Pi / 2d),
            3 => 1d - Math.Sin(progress * Pi / 2d + Pi / 2d),
            4 => 1d - Math.Pow(progress - 1d, 2d),
            5 => Math.Pow(progress, 2d),
            6 => progress < 0.5d
                ? 0.5d - Math.Sin(progress * Pi + Pi / 2d) / 2d
                : Math.Sin((progress - 0.5d) * Pi) / 2d + 0.5d,
            7 => progress < 0.5d
                ? Math.Pow(progress * 2d, 2d) / 2d
                : (2d - Math.Pow(2d * progress - 2d, 2d)) / 2d,
            8 => 1d + Math.Pow(progress - 1d, 3d),
            9 => Math.Pow(progress, 3d),
            10 => 1d - Math.Pow(progress - 1d, 4d),
            11 => Math.Pow(progress, 4d),
            12 => progress < 0.5d
                ? Math.Pow(progress * 2d, 3d) / 2d
                : (2d + Math.Pow(2d * progress - 2d, 3d)) / 2d,
            13 => progress < 0.5d
                ? Math.Pow(progress * 2d, 4d) / 2d
                : (2d - Math.Pow(2d * progress - 2d, 4d)) / 2d,
            14 => 1d + Math.Pow(progress - 1d, 5d),
            15 => Math.Pow(progress, 5d),
            16 => 1d - Math.Pow(2d, -10d * progress),
            17 => Math.Pow(2d, 10d * (progress - 1d)),
            18 => progress is >= 0d and <= 2d
                ? Math.Sqrt(1d - Math.Pow(progress - 1d, 2d))
                : double.NaN,
            19 => progress is >= -1d and <= 1d
                ? 1d - Math.Sqrt(1d - progress * progress)
                : double.NaN,
            20 => 1d
                + 2.70158d * Math.Pow(progress - 1d, 3d)
                + 1.70158d * Math.Pow(progress - 1d, 2d),
            21 => 2.70158d * Math.Pow(progress, 3d) - 1.70158d * Math.Pow(progress, 2d),
            22 => progress is >= -0.5d and <= 1.5d
                ? progress < 0.5d
                    ? 0.5d - Math.Sqrt(1d - progress * progress * 4d) / 2d
                    : 0.5d + Math.Sqrt(1d - Math.Pow(progress * 2d - 2d, 2d)) / 2d
                : double.NaN,
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
            26 => EaseOutBounce(progress),
            27 => 1d - EaseOutBounce(1d - progress),
            28 => progress < 0.5d
                ? (1d - EaseOutBounce(1d - 2d * progress)) / 2d
                : (1d + EaseOutBounce(2d * progress - 1d)) / 2d,
            _ => progress,
        };
    }

    private static double EaseOutBounce(double progress)
    {
        if (progress < 1d / 2.75d)
            return 7.5625d * progress * progress;
        if (progress < 2d / 2.75d)
        {
            progress -= 1.5d / 2.75d;
            return 7.5625d * progress * progress + 0.75d;
        }
        if (progress < 2.5d / 2.75d)
        {
            progress -= 2.25d / 2.75d;
            return 7.5625d * progress * progress + 0.9375d;
        }

        progress -= 2.625d / 2.75d;
        return 7.5625d * progress * progress + 0.984375d;
    }

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
