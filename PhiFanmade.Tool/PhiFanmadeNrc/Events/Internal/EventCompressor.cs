namespace PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;

/// <summary>
/// NRC 事件压缩器：合并变化率相近的相邻线性事件，以及移除无意义的默认值事件。
/// </summary>
internal static class EventCompressor
{
    private static void ValidateParams<T>(double tolerance)
    {
        if (tolerance is > 100 or < 0)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be between 0 and 100.");
        if (typeof(T) != typeof(int) && typeof(T) != typeof(float) && typeof(T) != typeof(double))
            throw new NotSupportedException("EventListCompress only supports int, float, and double types.");
    }

    /// <summary>
    /// 判断两段线性事件能否合并（归一化垂直距离算法）。
    /// </summary>
    private static bool TryMergeSqrt<T>(Nrc.Event<T> last, Nrc.Event<T> cur, double relTol)
    {
        var startBeat = (double)last.StartBeat;
        var midBeat = (double)last.EndBeat;
        var endBeat = (double)cur.EndBeat;
        var startValue = Convert.ToDouble(last.StartValue);
        var midValueEnd = Convert.ToDouble(last.EndValue);
        var midValueStart = Convert.ToDouble(cur.StartValue);
        var endValue = Convert.ToDouble(cur.EndValue);

        var scale = Math.Max(Math.Max(Math.Abs(startValue), Math.Abs(midValueEnd)),
            Math.Max(Math.Abs(endValue), 1e-3));

        if (Math.Abs(midValueEnd - midValueStart) / scale > relTol)
            return false;

        var totalBeatSpan = endBeat - startBeat;
        if (totalBeatSpan < 1e-12) return true;

        var normalizedMidBeat = (midBeat - startBeat) / totalBeatSpan;
        var normalizedValueDelta = (endValue - startValue) / scale;
        var normalizedMidValue = (midValueEnd - startValue) / scale;
        var linearDeviation = normalizedMidValue - normalizedValueDelta * normalizedMidBeat;
        var mergedLineLength = Math.Sqrt(1.0 + normalizedValueDelta * normalizedValueDelta);
        return Math.Abs(linearDeviation) / mergedLineLength <= relTol;
    }

    /// <summary>
    /// 判断两段线性事件能否合并（归一化斜率差算法）。
    /// </summary>
    private static bool TryMergeSlope<T>(Nrc.Event<T> last, Nrc.Event<T> cur, double relTol)
    {
        var startBeat = (double)last.StartBeat;
        var midBeat = (double)last.EndBeat;
        var endBeat = (double)cur.EndBeat;
        var startValue = Convert.ToDouble(last.StartValue);
        var midValueEnd = Convert.ToDouble(last.EndValue);
        var midValueStart = Convert.ToDouble(cur.StartValue);
        var endValue = Convert.ToDouble(cur.EndValue);

        var scale = Math.Max(Math.Max(Math.Abs(startValue), Math.Abs(midValueEnd)),
            Math.Max(Math.Abs(endValue), 1e-3));

        if (Math.Abs(midValueEnd - midValueStart) / scale > relTol)
            return false;

        var totalBeatSpan = endBeat - startBeat;
        if (totalBeatSpan < 1e-12) return true;

        var firstSegmentDuration = midBeat - startBeat;
        var secondSegmentDuration = endBeat - midBeat;
        var firstSlope = firstSegmentDuration < 1e-12 ? 0.0 : (midValueEnd - startValue) / firstSegmentDuration / scale;
        var secondSlope = secondSegmentDuration < 1e-12
            ? 0.0
            : (endValue - midValueStart) / secondSegmentDuration / scale;
        return Math.Abs(firstSlope - secondSlope) <= relTol;
    }

    /// <summary>
    /// 压缩事件列表，合并相连的线性事件。
    /// 使用归一化 (拍, 值) 空间中的垂直距离度量误差：
    /// 将两段合并为一段后，在原交界点处计算归一化垂直距离是否在容差之内。
    /// 与原来的斜率比较方法相比，本算法对长段误差更敏感，且不受坐标系 X/Y 轴缩放影响。
    /// </summary>
    /// <param name="events">事件列表</param>
    /// <param name="tolerance">容差百分比，越大拟合精细度越低</param>
    internal static List<Nrc.Event<T>> EventListCompressSqrt<T>(
        List<Nrc.Event<T>>? events, double tolerance)
    {
        ValidateParams<T>(tolerance);
        if (events == null || events.Count == 0) return [];

        var compressed = new List<Nrc.Event<T>> { events[0] };
        var relTol = tolerance / 100.0;

        for (var i = 1; i < events.Count; i++)
        {
            var lastEvent = compressed[^1];
            var currentEvent = events[i];

            if (lastEvent.Easing == 1 && currentEvent.Easing == 1 &&
                lastEvent.EndBeat == currentEvent.StartBeat &&
                TryMergeSqrt(lastEvent, currentEvent, relTol))
            {
                lastEvent.EndBeat = currentEvent.EndBeat;
                lastEvent.EndValue = currentEvent.EndValue;
                continue;
            }

            compressed.Add(currentEvent);
        }

        return compressed;
    }

    /// <summary>
    /// 压缩事件列表，合并相连的线性事件。
    /// 使用归一化斜率差度量误差：比较两段的归一化斜率之差是否在容差之内。
    /// 适用于空间不敏感的数据（如透明度），不依赖垂直距离计算，无需开方。
    /// </summary>
    /// <param name="events">事件列表</param>
    /// <param name="tolerance">容差百分比，越大拟合精细度越低</param>
    internal static List<Nrc.Event<T>> EventListCompressSlope<T>(
        List<Nrc.Event<T>>? events, double tolerance)
    {
        ValidateParams<T>(tolerance);
        if (events == null || events.Count == 0) return [];

        var compressed = new List<Nrc.Event<T>> { events[0] };
        var relTol = tolerance / 100.0;

        for (var i = 1; i < events.Count; i++)
        {
            var lastEvent = compressed[^1];
            var currentEvent = events[i];

            if (lastEvent.Easing == 1 && currentEvent.Easing == 1 &&
                lastEvent.EndBeat == currentEvent.StartBeat &&
                TryMergeSlope(lastEvent, currentEvent, relTol))
            {
                lastEvent.EndBeat = currentEvent.EndBeat;
                lastEvent.EndValue = currentEvent.EndValue;
                continue;
            }

            compressed.Add(currentEvent);
        }

        return compressed;
    }

    /// <summary>
    /// 移除无用事件（起始值和结束值都为默认值的事件）。
    /// </summary>
    internal static List<Nrc.Event<T>>? RemoveUselessEvent<T>(List<Nrc.Event<T>>? events)
    {
        var eventsCopy = events?.Select(e => e.Clone()).ToList();
        if (eventsCopy is { Count: 1 } &&
            EqualityComparer<T>.Default.Equals(eventsCopy[0].StartValue, default) &&
            EqualityComparer<T>.Default.Equals(eventsCopy[0].EndValue, default))
        {
            eventsCopy.RemoveAt(0);
        }

        return eventsCopy;
    }
}