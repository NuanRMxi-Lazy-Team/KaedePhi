using PhiFanmade.Core.Common;

namespace PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;

/// <summary>
/// NRC 事件合并器：将两个事件列表按数值叠加语义合并，支持固定采样与自适应采样两种策略。
/// </summary>
internal static class EventMerger
{
    // ─── 公开入口 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 将两个事件列表合并（固定采样）。有重叠区间时按等长切片逐段相加，可选压缩。
    /// </summary>
    internal static List<Nrc.Event<T>> EventListMerge<T>(
        List<Nrc.Event<T>>? toEvents,
        List<Nrc.Event<T>>? fromEvents,
        double precision = 64d,
        double tolerance = 5d,
        bool compress = true)
    {
        if (TryGetMergeEarlyReturn(toEvents, fromEvents, out var earlyReturn)) return earlyReturn;
        if (toEvents == null || fromEvents == null) return [];

        EnsureSupportedNumericType<T>();

        var toEventsCopy   = CloneEventList(toEvents);
        var fromEventsCopy = CloneEventList(fromEvents);

        if (!HasOverlap(toEventsCopy, fromEventsCopy))
            return MergeWithoutOverlap(toEventsCopy, fromEventsCopy);

        return MergeWithOverlapFixedSampling(
            toEvents, toEventsCopy, fromEventsCopy, precision, tolerance, compress);
    }

    /// <summary>
    /// 将两个事件列表合并（自适应采样）。天然压缩，性能更优。
    /// </summary>
    internal static List<Nrc.Event<T>> EventMergePlus<T>(
        List<Nrc.Event<T>>? toEvents,
        List<Nrc.Event<T>>? fromEvents,
        double precision = 64d,
        double tolerance = 5d)
    {
        if (TryGetMergeEarlyReturn(toEvents, fromEvents, out var earlyReturn)) return earlyReturn;
        if (toEvents == null || fromEvents == null) return [];

        EnsureSupportedNumericType<T>();

        var toEventsCopy   = CloneEventList(toEvents);
        var fromEventsCopy = CloneEventList(fromEvents);
        SortByStartBeat(toEventsCopy);
        SortByStartBeat(fromEventsCopy);

        if (!HasOverlap(toEventsCopy, fromEventsCopy))
            return MergeWithoutOverlap(toEventsCopy, fromEventsCopy);

        return MergeWithOverlapAdaptiveSampling(
            toEvents, toEventsCopy, fromEventsCopy, precision, tolerance);
    }

    // ─── 快速返回 ────────────────────────────────────────────────────────────

    private static bool TryGetMergeEarlyReturn<T>(
        List<Nrc.Event<T>>? toEvents,
        List<Nrc.Event<T>>? fromEvents,
        out List<Nrc.Event<T>> result)
    {
        if (toEvents == null || toEvents.Count == 0)
        {
            result = fromEvents == null || fromEvents.Count == 0
                ? []
                : fromEvents.Select(e => e.Clone()).ToList();
            return true;
        }

        if (fromEvents == null || fromEvents.Count == 0)
        {
            result = toEvents.Select(e => e.Clone()).ToList();
            return true;
        }

        result = [];
        return false;
    }

    // ─── 共用工具 ────────────────────────────────────────────────────────────

    private static void EnsureSupportedNumericType<T>()
    {
        if (typeof(T) != typeof(int) && typeof(T) != typeof(float) && typeof(T) != typeof(double))
            throw new NotSupportedException("EventMerge only supports int, float, and double types.");
    }

    private static List<Nrc.Event<T>> CloneEventList<T>(List<Nrc.Event<T>> events)
        => events.Select(e => e.Clone()).ToList();

    private static void SortByStartBeat<T>(List<Nrc.Event<T>> events)
        => events.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));

    private static bool HasOverlap<T>(List<Nrc.Event<T>> toEvents, List<Nrc.Event<T>> fromEvents)
        => fromEvents.Any(fe =>
            toEvents.Any(te => fe.StartBeat < te.EndBeat && fe.EndBeat > te.StartBeat));

    // ─── 无重叠路径 ──────────────────────────────────────────────────────────

    private static List<Nrc.Event<T>> MergeWithoutOverlap<T>(
        List<Nrc.Event<T>> toEventsCopy,
        List<Nrc.Event<T>> fromEventsCopy)
    {
        var newEvents = new List<Nrc.Event<T>>();

        foreach (var toEvent in toEventsCopy)
        {
            var prevForm  = fromEventsCopy.FindLast(e => e.EndBeat <= toEvent.StartBeat);
            var formOffset = prevForm.EndValue ?? default;
            newEvents.Add(new Nrc.Event<T>
            {
                StartBeat   = toEvent.StartBeat,
                EndBeat     = toEvent.EndBeat,
                StartValue  = (dynamic?)toEvent.StartValue + (dynamic?)formOffset,
                EndValue    = (dynamic?)toEvent.EndValue   + (dynamic?)formOffset,
                BezierPoints = toEvent.BezierPoints,
                Easing       = toEvent.Easing,
                EasingLeft   = toEvent.EasingLeft,
                EasingRight  = toEvent.EasingRight,
                IsBezier     = toEvent.IsBezier,
            });
        }

        newEvents.AddRange(from formEvent in fromEventsCopy
            let prevTo      = toEventsCopy.FindLast(e => e.EndBeat <= formEvent.StartBeat)
            let toEventValue = prevTo.EndValue ?? default
            select new Nrc.Event<T>
            {
                StartBeat   = formEvent.StartBeat,
                EndBeat     = formEvent.EndBeat,
                StartValue  = (dynamic?)formEvent.StartValue + (dynamic?)toEventValue,
                EndValue    = (dynamic?)formEvent.EndValue   + (dynamic?)toEventValue,
                BezierPoints = formEvent.BezierPoints,
                Easing       = formEvent.Easing,
                EasingLeft   = formEvent.EasingLeft,
                EasingRight  = formEvent.EasingRight,
                IsBezier     = formEvent.IsBezier,
            });

        SortByStartBeat(newEvents);
        return newEvents;
    }

    // ─── 重叠区间构建 ────────────────────────────────────────────────────────

    private static List<(Beat Start, Beat End)> BuildOverlapIntervals<T>(
        List<Nrc.Event<T>> toEvents,
        List<Nrc.Event<T>> fromEvents)
    {
        var overlapIntervals = new List<(Beat Start, Beat End)>();
        foreach (var fe in fromEvents)
        foreach (var te in toEvents)
        {
            if (!TryGetOverlapBounds(fe, te, out var start, out var end)) continue;
            if (overlapIntervals.Any(iv => iv.Start == start && iv.End == end)) continue;
            AddOrMergeOverlapInterval(overlapIntervals, start, end);
        }

        SortIntervals(overlapIntervals);
        return overlapIntervals;
    }

    private static bool TryGetOverlapBounds<T>(
        Nrc.Event<T> fe, Nrc.Event<T> te, out Beat start, out Beat end)
    {
        if (fe.StartBeat >= te.EndBeat || fe.EndBeat <= te.StartBeat)
        {
            start = new Beat(0d);
            end   = new Beat(0d);
            return false;
        }

        start = fe.StartBeat < te.StartBeat ? fe.StartBeat : te.StartBeat;
        end   = fe.EndBeat   > te.EndBeat   ? fe.EndBeat   : te.EndBeat;
        return true;
    }

    private static void AddOrMergeOverlapInterval(
        List<(Beat Start, Beat End)> overlapIntervals, Beat start, Beat end)
    {
        if (!overlapIntervals.Any(iv => start < iv.End && end > iv.Start))
        {
            overlapIntervals.Add((start, end));
            return;
        }

        for (var i = 0; i < overlapIntervals.Count; i++)
        {
            var iv = overlapIntervals[i];
            if (!(start < iv.End && end > iv.Start)) continue;
            var newStart = start < iv.Start ? start : iv.Start;
            var newEnd   = end   > iv.End   ? end   : iv.End;
            overlapIntervals[i] = (newStart, newEnd);
            start = newStart;
            end   = newEnd;
        }
    }

    private static void SortIntervals(List<(Beat Start, Beat End)> overlapIntervals)
        => overlapIntervals.Sort((a, b)
            => a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.End.CompareTo(b.End));

    // ─── 固定采样路径 ────────────────────────────────────────────────────────

    private static List<Nrc.Event<T>> MergeWithOverlapFixedSampling<T>(
        List<Nrc.Event<T>> toEventsForOffsetLookup,
        List<Nrc.Event<T>> toEventsCopy,
        List<Nrc.Event<T>> fromEventsCopy,
        double precision,
        double tolerance,
        bool compress)
    {
        var overlapIntervals = BuildOverlapIntervals(toEventsCopy, fromEventsCopy);
        var newEvents = BuildBaseEventsOutsideOverlap(
            toEventsCopy, fromEventsCopy, toEventsForOffsetLookup, overlapIntervals);

        var cutLength = new Beat(1d / precision);
        var (cutTo, cutFrom) =
            CutAndRemoveOverlapEvents(toEventsCopy, fromEventsCopy, overlapIntervals, cutLength);
        newEvents.AddRange(
            MergeCutOverlapSegments(toEventsCopy, fromEventsCopy, cutTo, cutFrom, overlapIntervals, cutLength));

        if (compress)
        {
            var floatEvents = newEvents as List<Nrc.Event<float>> ?? throw new InvalidCastException(nameof(newEvents));
            newEvents = EventCompressor.EventListCompress(floatEvents, tolerance)
                .Select(e => (Nrc.Event<T>)(object)e).ToList();
        }

        SortByStartBeat(newEvents);
        return newEvents;
    }

    private static List<Nrc.Event<T>> BuildBaseEventsOutsideOverlap<T>(
        List<Nrc.Event<T>> toEventsCopy,
        List<Nrc.Event<T>> fromEventsCopy,
        List<Nrc.Event<T>> toEventsForOffsetLookup,
        List<(Beat Start, Beat End)> overlapIntervals)
    {
        bool IsInOverlap(Nrc.Event<T> evt)
            => overlapIntervals.Any(iv => evt.StartBeat < iv.End && evt.EndBeat > iv.Start);

        var newEvents = (from toEvent in toEventsCopy
            where !IsInOverlap(toEvent)
            let prevForm   = fromEventsCopy.FindLast(e => e.EndBeat <= toEvent.StartBeat)
            let formOffset = prevForm.EndValue ?? default
            select new Nrc.Event<T>
            {
                StartBeat   = toEvent.StartBeat,
                EndBeat     = toEvent.EndBeat,
                StartValue  = (dynamic)toEvent.StartValue + (dynamic)formOffset,
                EndValue    = (dynamic)toEvent.EndValue   + (dynamic)formOffset,
                BezierPoints = toEvent.BezierPoints,
                Easing       = toEvent.Easing,
                EasingLeft   = toEvent.EasingLeft,
                EasingRight  = toEvent.EasingRight,
                IsBezier     = toEvent.IsBezier,
            }).ToList();

        newEvents.AddRange(from formEvent in fromEventsCopy
            where !IsInOverlap(formEvent)
            let prevTo       = toEventsForOffsetLookup.FindLast(e => e.EndBeat <= formEvent.StartBeat)
            let toEventValue = prevTo.EndValue ?? default
            select new Nrc.Event<T>
            {
                StartBeat   = formEvent.StartBeat,
                EndBeat     = formEvent.EndBeat,
                StartValue  = (dynamic)formEvent.StartValue + (dynamic)toEventValue,
                EndValue    = (dynamic)formEvent.EndValue   + (dynamic)toEventValue,
                BezierPoints = formEvent.BezierPoints,
                Easing       = formEvent.Easing,
                EasingLeft   = formEvent.EasingLeft,
                EasingRight  = formEvent.EasingRight,
                IsBezier     = formEvent.IsBezier,
            });

        return newEvents;
    }

    private static (List<Nrc.Event<T>> CutTo, List<Nrc.Event<T>> CutFrom) CutAndRemoveOverlapEvents<T>(
        List<Nrc.Event<T>> toEventsCopy,
        List<Nrc.Event<T>> fromEventsCopy,
        List<(Beat Start, Beat End)> overlapIntervals,
        Beat cutLength)
    {
        var cutTo   = new List<Nrc.Event<T>>();
        var cutFrom = new List<Nrc.Event<T>>();
        foreach (var (start, end) in overlapIntervals)
        {
            cutTo.AddRange(EventCutter.CutEventsInRange(toEventsCopy,   start, end, cutLength));
            cutFrom.AddRange(EventCutter.CutEventsInRange(fromEventsCopy, start, end, cutLength));
            toEventsCopy.RemoveAll(e => e.StartBeat   < end && e.EndBeat > start);
            fromEventsCopy.RemoveAll(e => e.StartBeat < end && e.EndBeat > start);
        }

        return (cutTo, cutFrom);
    }

    private static List<Nrc.Event<T>> MergeCutOverlapSegments<T>(
        List<Nrc.Event<T>> toEventsCopy,
        List<Nrc.Event<T>> fromEventsCopy,
        List<Nrc.Event<T>> cutTo,
        List<Nrc.Event<T>> cutFrom,
        List<(Beat Start, Beat End)> overlapIntervals,
        Beat cutLength)
    {
        var allCutEvents = new List<Nrc.Event<T>>();
        foreach (var (start, end) in overlapIntervals)
        {
            var prevTo   = toEventsCopy.FindLast(e => e.EndBeat <= start);
            var prevForm = fromEventsCopy.FindLast(e => e.EndBeat <= start);
            var toLastEnd   = prevTo   != null ? prevTo.EndValue   : default;
            var formLastEnd = prevForm != null ? prevForm.EndValue : default;
            allCutEvents.AddRange(
                MergeSingleOverlapInterval(cutTo, cutFrom, start, end, cutLength, toLastEnd, formLastEnd));
        }

        return allCutEvents;
    }

    private static List<Nrc.Event<T>> MergeSingleOverlapInterval<T>(
        List<Nrc.Event<T>> cutTo,
        List<Nrc.Event<T>> cutFrom,
        Beat start, Beat end, Beat cutLength,
        T? toLastEndValue, T? formLastEndValue)
    {
        var merged      = new List<Nrc.Event<T>>();
        var currentBeat = start;
        while (currentBeat < end)
        {
            var nextBeat  = currentBeat + cutLength;
            var toEvent   = cutTo.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);
            var formEvent = cutFrom.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);

            var toStart   = toEvent   != null ? toEvent.StartValue   : toLastEndValue;
            var formStart = formEvent != null ? formEvent.StartValue : formLastEndValue;
            var toEnd     = toEvent   != null ? toEvent.EndValue     : toLastEndValue;
            var formEnd   = formEvent != null ? formEvent.EndValue   : formLastEndValue;

            merged.Add(new Nrc.Event<T>
            {
                StartBeat  = currentBeat,
                EndBeat    = nextBeat,
                StartValue = (dynamic?)toStart   + (dynamic?)formStart,
                EndValue   = (dynamic?)toEnd     + (dynamic?)formEnd,
            });

            if (toEvent   != null) toLastEndValue   = toEvent.EndValue;
            if (formEvent != null) formLastEndValue = formEvent.EndValue;
            currentBeat = nextBeat;
        }

        return merged;
    }

    // ─── 自适应采样路径 ──────────────────────────────────────────────────────

    private static List<Nrc.Event<T>> MergeWithOverlapAdaptiveSampling<T>(
        List<Nrc.Event<T>> toEventsForOffsetLookup,
        List<Nrc.Event<T>> toEventsCopy,
        List<Nrc.Event<T>> fromEventsCopy,
        double precision,
        double tolerance)
    {
        var overlapIntervals = BuildOverlapIntervals(toEventsCopy, fromEventsCopy);
        var newEvents = BuildBaseEventsOutsideOverlap(
            toEventsCopy, fromEventsCopy, toEventsForOffsetLookup, overlapIntervals);

        newEvents.AddRange(
            MergeAdaptiveIntervals(toEventsCopy, fromEventsCopy, overlapIntervals, precision, tolerance));

        if (typeof(T) == typeof(float))
        {
            var floatEvents = newEvents as List<Nrc.Event<float>>;
            newEvents = EventCompressor.EventListCompress(floatEvents, tolerance)
                .Select(e => (Nrc.Event<T>)(object)e).ToList();
        }

        SortByStartBeat(newEvents);
        return newEvents;
    }

    private static List<Nrc.Event<T>> MergeAdaptiveIntervals<T>(
        List<Nrc.Event<T>> toEventsCopy,
        List<Nrc.Event<T>> fromEventsCopy,
        List<(Beat Start, Beat End)> overlapIntervals,
        double precision,
        double tolerance)
    {
        var cutLength = new Beat(1d / precision);
        var result    = new List<Nrc.Event<T>>();
        foreach (var (start, end) in overlapIntervals)
        {
            result.AddRange(
                MergeAdaptiveSingleInterval(toEventsCopy, fromEventsCopy, start, end, cutLength, tolerance));
        }

        return result;
    }

    private static List<Nrc.Event<T>> MergeAdaptiveSingleInterval<T>(
        List<Nrc.Event<T>> toEventsCopy,
        List<Nrc.Event<T>> fromEventsCopy,
        Beat start, Beat end, Beat cutLength, double tolerance)
    {
        var result      = new List<Nrc.Event<T>>();
        var currentBeat = start;

        var lastToValue   = GetPreviousEndValue(toEventsCopy, start);
        var lastFormValue = GetPreviousEndValue(fromEventsCopy, start);

        var toEventAtCurrent   = GetActiveEventAtBeat(toEventsCopy, currentBeat);
        var formEventAtCurrent = GetActiveEventAtBeat(fromEventsCopy, currentBeat);
        var toValAtCurrent   = toEventAtCurrent   != null ? toEventAtCurrent.GetValueAtBeat(currentBeat)   : lastToValue;
        var formValAtCurrent = formEventAtCurrent != null ? formEventAtCurrent.GetValueAtBeat(currentBeat) : lastFormValue;

        var segmentStart      = start;
        var segmentStartToValue   = toValAtCurrent;
        var segmentStartFormValue = formValAtCurrent;
        var segmentStartSum   = AddValues(segmentStartToValue, segmentStartFormValue);

        while (currentBeat < end)
        {
            var nextBeat = currentBeat + cutLength;
            if (nextBeat > end) nextBeat = end;

            var toEventAtNext   = GetActiveEventAtBeat(toEventsCopy, nextBeat);
            var formEventAtNext = GetActiveEventAtBeat(fromEventsCopy, nextBeat);
            var crossEvent = !ReferenceEquals(toEventAtNext, toEventAtCurrent) ||
                             !ReferenceEquals(formEventAtNext, formEventAtCurrent);

            if (crossEvent && currentBeat > segmentStart)
            {
                AddSegmentEvent(result, segmentStart, currentBeat,
                    segmentStartToValue, segmentStartFormValue, toValAtCurrent, formValAtCurrent);
                segmentStart          = currentBeat;
                segmentStartToValue   = toValAtCurrent;
                segmentStartFormValue = formValAtCurrent;
                segmentStartSum       = AddValues(toValAtCurrent, formValAtCurrent);
            }

            var (toValueAtNext, toValUpdate)   = GetNextBeatValues(toEventsCopy, toEventAtCurrent, toEventAtNext, nextBeat);
            var (formValueAtNext, formValUpdate) = GetNextBeatValues(fromEventsCopy, formEventAtCurrent, formEventAtNext, nextBeat);

            var sumAtNext = AddValues(toValueAtNext, formValueAtNext);
            var sumAtEnd  = AddValues(GetValueAtBeatOrPreviousEnd(toEventsCopy, end),
                                      GetValueAtBeatOrPreviousEnd(fromEventsCopy, end));

            if (crossEvent || ShouldSplitAdaptiveSegment(
                    segmentStart, nextBeat, end, segmentStartSum, sumAtNext, sumAtEnd, tolerance))
            {
                AddSegmentEvent(result, segmentStart, nextBeat,
                    segmentStartToValue, segmentStartFormValue, toValueAtNext, formValueAtNext);
                segmentStart          = nextBeat;
                segmentStartToValue   = toValUpdate;
                segmentStartFormValue = formValUpdate;
                segmentStartSum       = AddValues(toValUpdate, formValUpdate);
            }

            toEventAtCurrent   = toEventAtNext;
            formEventAtCurrent = formEventAtNext;
            toValAtCurrent     = toValUpdate;
            formValAtCurrent   = formValUpdate;
            currentBeat        = nextBeat;
        }

        return result;
    }

    // ─── 自适应采样辅助方法 ──────────────────────────────────────────────────

    private static Nrc.Event<T>? GetActiveEventAtBeat<T>(List<Nrc.Event<T>> events, Beat beat)
        => events.Where(e => e.StartBeat <= beat && e.EndBeat >= beat).MaxBy(e => e.StartBeat);

    private static T? GetPreviousEndValue<T>(List<Nrc.Event<T>> events, Beat beat)
    {
        var prev = events.FindLast(e => e.EndBeat <= beat);
        return prev != null ? prev.EndValue : default;
    }

    private static T? GetValueAtBeatOrPreviousEnd<T>(List<Nrc.Event<T>> events, Beat beat)
    {
        var active = GetActiveEventAtBeat(events, beat);
        return active != null ? active.GetValueAtBeat(beat) : GetPreviousEndValue(events, beat);
    }

    private static (T? Outgoing, T? Incoming) GetNextBeatValues<T>(
        List<Nrc.Event<T>> events,
        Nrc.Event<T>? eventAtCurrent,
        Nrc.Event<T>? eventAtNext,
        Beat nextBeat)
    {
        var prevEnd   = GetPreviousEndValue(events, nextBeat);
        var outgoing  = (eventAtCurrent != null && eventAtCurrent.EndBeat >= nextBeat)
            ? eventAtCurrent.GetValueAtBeat(nextBeat)
            : prevEnd;
        var incoming  = eventAtNext != null
            ? eventAtNext.GetValueAtBeat(nextBeat)
            : prevEnd;
        return (outgoing, incoming);
    }

    private static bool ShouldSplitAdaptiveSegment<T>(
        Beat segmentStart, Beat nextBeat, Beat intervalEnd,
        T? segmentStartSum, T? sumAtNext, T? sumAtEnd, double tolerance)
    {
        var segmentProgress = nextBeat == intervalEnd
            ? 1.0
            : (double)(nextBeat - segmentStart) / (double)(intervalEnd - segmentStart);

        var startNum    = ToDouble(segmentStartSum);
        var nextNum     = ToDouble(sumAtNext);
        var endNum      = ToDouble(sumAtEnd);
        var predicted   = startNum + (endNum - startNum) * segmentProgress;
        var error       = Math.Abs(nextNum - predicted);
        var threshold   = tolerance / 100.0 * ((Math.Abs(startNum) + Math.Abs(nextNum)) / 2.0 + 1e-9);

        return error > threshold || nextBeat >= intervalEnd;
    }

    private static double ToDouble(dynamic? value)
    {
        if (value == null)
            throw new InvalidOperationException("Unexpected null numeric value.");
        return (double)value;
    }

    private static T AddValues<T>(T? left, T? right)
        => (dynamic?)left + (dynamic?)right;

    private static void AddSegmentEvent<T>(
        List<Nrc.Event<T>> target, Beat startBeat, Beat endBeat,
        T? startToValue, T? startFormValue, T? endToValue, T? endFormValue)
    {
        target.Add(new Nrc.Event<T>
        {
            StartBeat  = startBeat,
            EndBeat    = endBeat,
            StartValue = AddValues(startToValue, startFormValue),
            EndValue   = AddValues(endToValue,   endFormValue),
        });
    }
}

