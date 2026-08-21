using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Event.KaedePhi;

/// <summary>
/// KPC 事件压缩器：合并变化率相近的相邻线性事件，以及移除无意义的默认值事件。
/// </summary>
public class EventCompressor<TPayload> : LoggableBase, IEventCompressor<KpcEvents.Event<TPayload>>
    where TPayload : notnull
{
    private static void ValidateParams(double tolerance)
    {
        if (tolerance is > 100 or < 0)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "容差必须在 0 到 100 之间。");
        if (
            typeof(TPayload) != typeof(int)
            && typeof(TPayload) != typeof(float)
            && typeof(TPayload) != typeof(double)
        )
            throw new NotSupportedException("EventListCompress 仅支持 int、float 和 double 类型。");
    }

    /// <summary>
    /// 判断两段线性事件能否合并（归一化垂直距离算法）。
    /// </summary>
    private static bool TryMergeSqrt(
        KpcEvents.Event<TPayload> last,
        KpcEvents.Event<TPayload> cur,
        double relTol
    )
    {
        var startBeat = (double)last.StartBeat;
        var midBeat = (double)last.EndBeat;
        var endBeat = (double)cur.EndBeat;
        var startValue = NumericHelper.ToDouble(last.StartValue);
        var midValueEnd = NumericHelper.ToDouble(last.EndValue);
        var midValueStart = NumericHelper.ToDouble(cur.StartValue);
        var endValue = NumericHelper.ToDouble(cur.EndValue);
        var rangeFirst = Math.Abs(midValueEnd - startValue);
        var rangeSecond = Math.Abs(endValue - midValueStart);
        var scale = Math.Max(Math.Max(rangeFirst, rangeSecond), 1e-3);

        if (Math.Abs(midValueEnd - midValueStart) / scale > relTol)
            return false;

        var totalBeatSpan = endBeat - startBeat;
        if (totalBeatSpan < 1e-12)
            return true;

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
    private static bool TryMergeSlope(
        KpcEvents.Event<TPayload> last,
        KpcEvents.Event<TPayload> cur,
        double relTol
    )
    {
        var startBeat = (double)last.StartBeat;
        var midBeat = (double)last.EndBeat;
        var endBeat = (double)cur.EndBeat;
        var startValue = NumericHelper.ToDouble(last.StartValue);
        var midValueEnd = NumericHelper.ToDouble(last.EndValue);
        var midValueStart = NumericHelper.ToDouble(cur.StartValue);
        var endValue = NumericHelper.ToDouble(cur.EndValue);

        // 同 TryMergeSqrt：以两个子段各自运动范围的最大值为归一化尺度，
        // 避免跨零点信号因 rangeTotal 虚增而被过度合并。
        var rangeFirst = Math.Abs(midValueEnd - startValue);
        var rangeSecond = Math.Abs(endValue - midValueStart);
        var scale = Math.Max(Math.Max(rangeFirst, rangeSecond), 1e-3);

        if (Math.Abs(midValueEnd - midValueStart) / scale > relTol)
            return false;

        var totalBeatSpan = endBeat - startBeat;
        if (totalBeatSpan < 1e-12)
            return true;

        var firstSegmentDuration = midBeat - startBeat;
        var secondSegmentDuration = endBeat - midBeat;
        var firstSlope =
            firstSegmentDuration < 1e-12
                ? 0.0
                : (midValueEnd - startValue) / firstSegmentDuration / scale;
        var secondSlope =
            secondSegmentDuration < 1e-12
                ? 0.0
                : (endValue - midValueStart) / secondSegmentDuration / scale;
        return Math.Abs(firstSlope - secondSlope) <= relTol;
    }

    /// <summary>
    /// 判断事件是否为可安全压缩的完整线性事件。
    /// </summary>
    private static bool IsEligibleForTransform(KpcEvents.Event<TPayload> evt)
    {
        return !evt.IsBezier && (int)evt.Easing == 1 && evt.Font is null;
    }

    /// <inheritdoc/>
    public List<KpcEvents.Event<TPayload>> EventListCompressSqrt(
        List<KpcEvents.Event<TPayload>>? events,
        double tolerance,
        IProgress<ToolProgress>? progress = null
    )
    {
        ValidateParams(tolerance);
        if (events == null || events.Count == 0)
            return [];

        var compressed = new List<KpcEvents.Event<TPayload>> { events[0].Clone() };
        var relTol = tolerance / 100.0;

        for (var i = 1; i < events.Count; i++)
        {
            var lastEvent = compressed[^1];
            var currentEvent = events[i];

            if (
                IsEligibleForTransform(lastEvent)
                && IsEligibleForTransform(currentEvent)
                && lastEvent.EndBeat == currentEvent.StartBeat
                && TryMergeSqrt(lastEvent, currentEvent, relTol)
            )
            {
                lastEvent.EndBeat = currentEvent.EndBeat;
                lastEvent.EndValue = currentEvent.EndValue;
                continue;
            }

            compressed.Add(currentEvent.Clone());
            progress?.Report(new ToolProgress((double)i / events.Count));
        }

        progress?.Report(new ToolProgress(1.0));
        return compressed;
    }

    /// <inheritdoc/>
    public List<KpcEvents.Event<TPayload>> EventListCompressSlope(
        List<KpcEvents.Event<TPayload>>? events,
        double tolerance,
        IProgress<ToolProgress>? progress = null
    )
    {
        ValidateParams(tolerance);
        if (events == null || events.Count == 0)
            return [];

        var compressed = new List<KpcEvents.Event<TPayload>> { events[0].Clone() };
        var relTol = tolerance / 100.0;

        for (var i = 1; i < events.Count; i++)
        {
            var lastEvent = compressed[^1];
            var currentEvent = events[i];

            if (
                IsEligibleForTransform(lastEvent)
                && IsEligibleForTransform(currentEvent)
                && lastEvent.EndBeat == currentEvent.StartBeat
                && TryMergeSlope(lastEvent, currentEvent, relTol)
            )
            {
                lastEvent.EndBeat = currentEvent.EndBeat;
                lastEvent.EndValue = currentEvent.EndValue;
                continue;
            }

            compressed.Add(currentEvent.Clone());
            progress?.Report(new ToolProgress((double)i / events.Count));
        }

        progress?.Report(new ToolProgress(1.0));
        return compressed;
    }

    /// <summary>
    /// 移除无用事件（起始值和结束值都为默认值的事件）。
    /// </summary>
    public List<KpcEvents.Event<TPayload>> RemoveUselessEvent(
        List<KpcEvents.Event<TPayload>>? events
    )
    {
        if (events == null || events.Count == 0)
            return [];

        // 只有当列表只有一个事件且该事件为默认值时才需要修改
        if (
            events.Count == 1
            && EqualityComparer<TPayload>.Default.Equals(events[0].StartValue, default)
            && EqualityComparer<TPayload>.Default.Equals(events[0].EndValue, default)
        )
        {
            return [];
        }

        return [.. events.Select(evt => evt.Clone())];
    }
}
