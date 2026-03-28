using PhiFanmade.Core.Common;

namespace PhiFanmade.Tool.RePhiEdit.Events.Internal;

/// <summary>RPE 事件合并器：固定采样与自适应采样两种策略。</summary>
internal static class EventMerger
{
    internal static List<Rpe.Event<T>> EventListMerge<T>(
        List<Rpe.Event<T>>? toEvents, List<Rpe.Event<T>>? fromEvents,
        double precision = 64d, double tolerance = 5d, bool compress = true)
    {
        if (TryGetMergeEarlyReturn(toEvents, fromEvents, out var earlyReturn)) return earlyReturn;
        if (toEvents == null || fromEvents == null) return [];
        EnsureSupportedNumericType<T>();

        var toEventsCopy   = CloneEventList(toEvents);
        var fromEventsCopy = CloneEventList(fromEvents);
        if (!HasOverlap(toEventsCopy, fromEventsCopy))
            return MergeWithoutOverlap(toEventsCopy, fromEventsCopy);

        return MergeWithOverlapFixedSampling(toEvents, toEventsCopy, fromEventsCopy, precision, tolerance, compress);
    }

    internal static List<Rpe.Event<T>> EventMergePlus<T>(
        List<Rpe.Event<T>>? toEvents, List<Rpe.Event<T>>? fromEvents,
        double precision = 64d, double tolerance = 5d)
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

        return MergeWithOverlapAdaptiveSampling(toEvents, toEventsCopy, fromEventsCopy, precision, tolerance);
    }

    private static bool TryGetMergeEarlyReturn<T>(
        List<Rpe.Event<T>>? toEvents, List<Rpe.Event<T>>? fromEvents, out List<Rpe.Event<T>> result)
    {
        if (toEvents == null || toEvents.Count == 0)
        { result = fromEvents == null || fromEvents.Count == 0 ? [] : fromEvents.Select(e => e.Clone()).ToList(); return true; }
        if (fromEvents == null || fromEvents.Count == 0)
        { result = toEvents.Select(e => e.Clone()).ToList(); return true; }
        result = []; return false;
    }

    private static void EnsureSupportedNumericType<T>()
    {
        if (typeof(T) != typeof(int) && typeof(T) != typeof(float) && typeof(T) != typeof(double))
            throw new NotSupportedException("EventMerge only supports int, float, and double types.");
    }

    private static List<Rpe.Event<T>> CloneEventList<T>(List<Rpe.Event<T>> events)
        => events.Select(e => e.Clone()).ToList();

    private static void SortByStartBeat<T>(List<Rpe.Event<T>> events)
        => events.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));

    private static bool HasOverlap<T>(List<Rpe.Event<T>> toEvents, List<Rpe.Event<T>> fromEvents)
        => fromEvents.Any(fe => toEvents.Any(te => fe.StartBeat < te.EndBeat && fe.EndBeat > te.StartBeat));

    private static List<Rpe.Event<T>> MergeWithoutOverlap<T>(
        List<Rpe.Event<T>> toEventsCopy, List<Rpe.Event<T>> fromEventsCopy)
    {
        var newEvents = new List<Rpe.Event<T>>();
        foreach (var toEvent in toEventsCopy)
        {
            var prevForm   = fromEventsCopy.FindLast(e => e.EndBeat <= toEvent.StartBeat);
            var formOffset = prevForm.EndValue ?? default;
            newEvents.Add(new Rpe.Event<T>
            {
                StartBeat = toEvent.StartBeat, EndBeat = toEvent.EndBeat,
                StartValue = (dynamic?)toEvent.StartValue + (dynamic?)formOffset,
                EndValue   = (dynamic?)toEvent.EndValue   + (dynamic?)formOffset,
                BezierPoints = toEvent.BezierPoints, Easing = toEvent.Easing,
                EasingLeft = toEvent.EasingLeft, EasingRight = toEvent.EasingRight, IsBezier = toEvent.IsBezier,
            });
        }
        newEvents.AddRange(from formEvent in fromEventsCopy
            let prevTo       = toEventsCopy.FindLast(e => e.EndBeat <= formEvent.StartBeat)
            let toEventValue = prevTo.EndValue ?? default
            select new Rpe.Event<T>
            {
                StartBeat = formEvent.StartBeat, EndBeat = formEvent.EndBeat,
                StartValue = (dynamic?)formEvent.StartValue + (dynamic?)toEventValue,
                EndValue   = (dynamic?)formEvent.EndValue   + (dynamic?)toEventValue,
                BezierPoints = formEvent.BezierPoints, Easing = formEvent.Easing,
                EasingLeft = formEvent.EasingLeft, EasingRight = formEvent.EasingRight, IsBezier = formEvent.IsBezier,
            });
        SortByStartBeat(newEvents);
        return newEvents;
    }

    // ─── 重叠区间 ────────────────────────────────────────────────────────────

    private static List<(Beat Start, Beat End)> BuildOverlapIntervals<T>(
        List<Rpe.Event<T>> toEvents, List<Rpe.Event<T>> fromEvents)
    {
        var overlapIntervals = new List<(Beat Start, Beat End)>();
        {
            foreach (var fe in fromEvents)
            {
                foreach (var te in toEvents)
                {
                    if (!TryGetOverlapBounds(fe, te, out var start, out var end)) continue;
                    if (overlapIntervals.Any(iv => iv.Start == start && iv.End == end)) continue;
                    AddOrMergeOverlapInterval(overlapIntervals, start, end);
                }
            }
        }
        SortIntervals(overlapIntervals);
        return overlapIntervals;
    }

    private static bool TryGetOverlapBounds<T>(Rpe.Event<T> fe, Rpe.Event<T> te, out Beat start, out Beat end)
    {
        if (fe.StartBeat >= te.EndBeat || fe.EndBeat <= te.StartBeat)
        { start = new Beat(0d); end = new Beat(0d); return false; }
        start = fe.StartBeat < te.StartBeat ? fe.StartBeat : te.StartBeat;
        end   = fe.EndBeat   > te.EndBeat   ? fe.EndBeat   : te.EndBeat;
        return true;
    }

    private static void AddOrMergeOverlapInterval(
        List<(Beat Start, Beat End)> overlapIntervals, Beat start, Beat end)
    {
        if (!overlapIntervals.Any(iv => start < iv.End && end > iv.Start))
        { overlapIntervals.Add((start, end)); return; }
        for (var i = 0; i < overlapIntervals.Count; i++)
        {
            var iv = overlapIntervals[i];
            if (!(start < iv.End && end > iv.Start)) continue;
            var ns = start < iv.Start ? start : iv.Start;
            var ne = end   > iv.End   ? end   : iv.End;
            overlapIntervals[i] = (ns, ne); start = ns; end = ne;
        }
    }

    private static void SortIntervals(List<(Beat Start, Beat End)> list)
        => list.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.End.CompareTo(b.End));

    // ─── 固定采样 ────────────────────────────────────────────────────────────

    private static List<Rpe.Event<T>> MergeWithOverlapFixedSampling<T>(
        List<Rpe.Event<T>> toEventsForOffsetLookup,
        List<Rpe.Event<T>> toEventsCopy, List<Rpe.Event<T>> fromEventsCopy,
        double precision, double tolerance, bool compress)
    {
        var overlapIntervals = BuildOverlapIntervals(toEventsCopy, fromEventsCopy);
        var newEvents = BuildBaseEventsOutsideOverlap(toEventsCopy, fromEventsCopy, toEventsForOffsetLookup, overlapIntervals);
        var cutLength = new Beat(1d / precision);
        var (cutTo, cutFrom) = CutAndRemoveOverlapEvents(toEventsCopy, fromEventsCopy, overlapIntervals, cutLength);
        newEvents.AddRange(MergeCutOverlapSegments(toEventsCopy, fromEventsCopy, cutTo, cutFrom, overlapIntervals, cutLength));
        if (compress)
        {
            var floatEvents = newEvents as List<Rpe.Event<float>> ?? throw new InvalidCastException(nameof(newEvents));
            newEvents = EventCompressor.EventListCompress(floatEvents, tolerance).Select(e => (Rpe.Event<T>)(object)e).ToList();
        }
        SortByStartBeat(newEvents);
        return newEvents;
    }

    private static List<Rpe.Event<T>> BuildBaseEventsOutsideOverlap<T>(
        List<Rpe.Event<T>> toEventsCopy, List<Rpe.Event<T>> fromEventsCopy,
        List<Rpe.Event<T>> toEventsForOffsetLookup, List<(Beat Start, Beat End)> overlapIntervals)
    {
        bool IsInOverlap(Rpe.Event<T> evt) => overlapIntervals.Any(iv => evt.StartBeat < iv.End && evt.EndBeat > iv.Start);
        var newEvents = (from toEvent in toEventsCopy where !IsInOverlap(toEvent)
            let prevForm   = fromEventsCopy.FindLast(e => e.EndBeat <= toEvent.StartBeat)
            let formOffset = prevForm.EndValue ?? default
            select new Rpe.Event<T>
            {
                StartBeat = toEvent.StartBeat, EndBeat = toEvent.EndBeat,
                StartValue = (dynamic)toEvent.StartValue + (dynamic)formOffset,
                EndValue   = (dynamic)toEvent.EndValue   + (dynamic)formOffset,
                BezierPoints = toEvent.BezierPoints, Easing = toEvent.Easing,
                EasingLeft = toEvent.EasingLeft, EasingRight = toEvent.EasingRight, IsBezier = toEvent.IsBezier,
            }).ToList();
        newEvents.AddRange(from formEvent in fromEventsCopy where !IsInOverlap(formEvent)
            let prevTo       = toEventsForOffsetLookup.FindLast(e => e.EndBeat <= formEvent.StartBeat)
            let toEventValue = prevTo.EndValue ?? default
            select new Rpe.Event<T>
            {
                StartBeat = formEvent.StartBeat, EndBeat = formEvent.EndBeat,
                StartValue = (dynamic)formEvent.StartValue + (dynamic)toEventValue,
                EndValue   = (dynamic)formEvent.EndValue   + (dynamic)toEventValue,
                BezierPoints = formEvent.BezierPoints, Easing = formEvent.Easing,
                EasingLeft = formEvent.EasingLeft, EasingRight = formEvent.EasingRight, IsBezier = formEvent.IsBezier,
            });
        return newEvents;
    }

    private static (List<Rpe.Event<T>> CutTo, List<Rpe.Event<T>> CutFrom) CutAndRemoveOverlapEvents<T>(
        List<Rpe.Event<T>> toEventsCopy, List<Rpe.Event<T>> fromEventsCopy,
        List<(Beat Start, Beat End)> overlapIntervals, Beat cutLength)
    {
        var cutTo = new List<Rpe.Event<T>>(); var cutFrom = new List<Rpe.Event<T>>();
        foreach (var (start, end) in overlapIntervals)
        {
            cutTo.AddRange(EventCutter.CutEventsInRange(toEventsCopy,   start, end, cutLength));
            cutFrom.AddRange(EventCutter.CutEventsInRange(fromEventsCopy, start, end, cutLength));
            toEventsCopy.RemoveAll(e   => e.StartBeat < end && e.EndBeat > start);
            fromEventsCopy.RemoveAll(e => e.StartBeat < end && e.EndBeat > start);
        }
        return (cutTo, cutFrom);
    }

    private static List<Rpe.Event<T>> MergeCutOverlapSegments<T>(
        List<Rpe.Event<T>> toEventsCopy, List<Rpe.Event<T>> fromEventsCopy,
        List<Rpe.Event<T>> cutTo, List<Rpe.Event<T>> cutFrom,
        List<(Beat Start, Beat End)> overlapIntervals, Beat cutLength)
    {
        var allCutEvents = new List<Rpe.Event<T>>();
        foreach (var (start, end) in overlapIntervals)
        {
            var prevTo   = toEventsCopy.FindLast(e => e.EndBeat <= start);
            var prevForm = fromEventsCopy.FindLast(e => e.EndBeat <= start);
            allCutEvents.AddRange(MergeSingleOverlapInterval(
                cutTo, cutFrom, start, end, cutLength,
                prevTo   != null ? prevTo.EndValue   : default,
                prevForm != null ? prevForm.EndValue : default));
        }
        return allCutEvents;
    }

    private static List<Rpe.Event<T>> MergeSingleOverlapInterval<T>(
        List<Rpe.Event<T>> cutTo, List<Rpe.Event<T>> cutFrom,
        Beat start, Beat end, Beat cutLength, T? toLastEnd, T? fromLastEnd)
    {
        var merged = new List<Rpe.Event<T>>(); var currentBeat = start;
        while (currentBeat < end)
        {
            var nextBeat  = currentBeat + cutLength;
            var toEvent   = cutTo.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);
            var formEvent = cutFrom.FirstOrDefault(e => e.StartBeat == currentBeat && e.EndBeat == nextBeat);
            var toS   = toEvent   != null ? toEvent.StartValue   : toLastEnd;
            var fromS = formEvent != null ? formEvent.StartValue : fromLastEnd;
            var toE   = toEvent   != null ? toEvent.EndValue     : toLastEnd;
            var fromE = formEvent != null ? formEvent.EndValue   : fromLastEnd;
            merged.Add(new Rpe.Event<T>
            {
                StartBeat = currentBeat, EndBeat = nextBeat,
                StartValue = (dynamic?)toS + (dynamic?)fromS,
                EndValue   = (dynamic?)toE + (dynamic?)fromE,
            });
            if (toEvent   != null) toLastEnd   = toEvent.EndValue;
            if (formEvent != null) fromLastEnd = formEvent.EndValue;
            currentBeat = nextBeat;
        }
        return merged;
    }

    // 自适应采样

    private static List<Rpe.Event<T>> MergeWithOverlapAdaptiveSampling<T>(
        List<Rpe.Event<T>> toEventsForOffsetLookup,
        List<Rpe.Event<T>> toEventsCopy, List<Rpe.Event<T>> fromEventsCopy,
        double precision, double tolerance)
    {
        var overlapIntervals = BuildOverlapIntervals(toEventsCopy, fromEventsCopy);
        var newEvents = BuildBaseEventsOutsideOverlap(toEventsCopy, fromEventsCopy, toEventsForOffsetLookup, overlapIntervals);
        newEvents.AddRange(MergeAdaptiveIntervals(toEventsCopy, fromEventsCopy, overlapIntervals, precision, tolerance));
        if (typeof(T) == typeof(float))
        {
            var floatEvents = newEvents as List<Rpe.Event<float>>;
            newEvents = EventCompressor.EventListCompress(floatEvents, tolerance).Select(e => (Rpe.Event<T>)(object)e).ToList();
        }
        SortByStartBeat(newEvents);
        return newEvents;
    }

    private static List<Rpe.Event<T>> MergeAdaptiveIntervals<T>(
        List<Rpe.Event<T>> toEventsCopy, List<Rpe.Event<T>> fromEventsCopy,
        List<(Beat Start, Beat End)> overlapIntervals, double precision, double tolerance)
    {
        var cutLength = new Beat(1d / precision); var result = new List<Rpe.Event<T>>();
        foreach (var (start, end) in overlapIntervals)
            result.AddRange(MergeAdaptiveSingleInterval(toEventsCopy, fromEventsCopy, start, end, cutLength, tolerance));
        return result;
    }

    private static List<Rpe.Event<T>> MergeAdaptiveSingleInterval<T>(
        List<Rpe.Event<T>> toEventsCopy, List<Rpe.Event<T>> fromEventsCopy,
        Beat start, Beat end, Beat cutLength, double tolerance)
    {
        var result      = new List<Rpe.Event<T>>(); var currentBeat = start;
        var lastToValue   = GetPreviousEndValue(toEventsCopy, start);
        var lastFormValue = GetPreviousEndValue(fromEventsCopy, start);
        var toEventAtCurrent   = GetActiveEventAtBeat(toEventsCopy, currentBeat);
        var formEventAtCurrent = GetActiveEventAtBeat(fromEventsCopy, currentBeat);
        var toValAtCurrent   = toEventAtCurrent   != null ? toEventAtCurrent.GetValueAtBeat(currentBeat)   : lastToValue;
        var formValAtCurrent = formEventAtCurrent != null ? formEventAtCurrent.GetValueAtBeat(currentBeat) : lastFormValue;
        var segmentStart = start; var segmentStartToValue = toValAtCurrent; var segmentStartFormValue = formValAtCurrent;
        var segmentStartSum = AddValues(segmentStartToValue, segmentStartFormValue);

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
                AddSegmentEvent(result, segmentStart, currentBeat, segmentStartToValue, segmentStartFormValue, toValAtCurrent, formValAtCurrent);
                segmentStart = currentBeat; segmentStartToValue = toValAtCurrent; segmentStartFormValue = formValAtCurrent;
                segmentStartSum = AddValues(toValAtCurrent, formValAtCurrent);
            }

            var (toValueAtNext, toValUpdate)     = GetNextBeatValues(toEventsCopy,   toEventAtCurrent,   toEventAtNext,   nextBeat);
            var (formValueAtNext, formValUpdate)  = GetNextBeatValues(fromEventsCopy, formEventAtCurrent, formEventAtNext, nextBeat);
            var sumAtNext = AddValues(toValueAtNext, formValueAtNext);
            var sumAtEnd  = AddValues(GetValueAtBeatOrPreviousEnd(toEventsCopy, end), GetValueAtBeatOrPreviousEnd(fromEventsCopy, end));

            if (crossEvent || ShouldSplitAdaptiveSegment(segmentStart, nextBeat, end, segmentStartSum, sumAtNext, sumAtEnd, tolerance))
            {
                AddSegmentEvent(result, segmentStart, nextBeat, segmentStartToValue, segmentStartFormValue, toValueAtNext, formValueAtNext);
                segmentStart = nextBeat; segmentStartToValue = toValUpdate; segmentStartFormValue = formValUpdate;
                segmentStartSum = AddValues(toValUpdate, formValUpdate);
            }

            toEventAtCurrent = toEventAtNext; formEventAtCurrent = formEventAtNext;
            toValAtCurrent = toValUpdate; formValAtCurrent = formValUpdate;
            currentBeat = nextBeat;
        }
        return result;
    }

    private static Rpe.Event<T>? GetActiveEventAtBeat<T>(List<Rpe.Event<T>> events, Beat beat)
        => events.Where(e => e.StartBeat <= beat && e.EndBeat >= beat).MaxBy(e => e.StartBeat);

    private static T? GetPreviousEndValue<T>(List<Rpe.Event<T>> events, Beat beat)
    { var prev = events.FindLast(e => e.EndBeat <= beat); return prev != null ? prev.EndValue : default; }

    private static T? GetValueAtBeatOrPreviousEnd<T>(List<Rpe.Event<T>> events, Beat beat)
    { var active = GetActiveEventAtBeat(events, beat); return active != null ? active.GetValueAtBeat(beat) : GetPreviousEndValue(events, beat); }

    private static (T? Outgoing, T? Incoming) GetNextBeatValues<T>(
        List<Rpe.Event<T>> events, Rpe.Event<T>? eventAtCurrent, Rpe.Event<T>? eventAtNext, Beat nextBeat)
    {
        var prevEnd  = GetPreviousEndValue(events, nextBeat);
        var outgoing = (eventAtCurrent != null && eventAtCurrent.EndBeat >= nextBeat) ? eventAtCurrent.GetValueAtBeat(nextBeat) : prevEnd;
        var incoming = eventAtNext != null ? eventAtNext.GetValueAtBeat(nextBeat) : prevEnd;
        return (outgoing, incoming);
    }

    private static bool ShouldSplitAdaptiveSegment<T>(
        Beat segmentStart, Beat nextBeat, Beat intervalEnd,
        T? segmentStartSum, T? sumAtNext, T? sumAtEnd, double tolerance)
    {
        var progress  = nextBeat == intervalEnd ? 1.0 : (double)(nextBeat - segmentStart) / (double)(intervalEnd - segmentStart);
        var startNum  = ToDouble(segmentStartSum);
        var nextNum   = ToDouble(sumAtNext);
        var endNum    = ToDouble(sumAtEnd);
        var predicted = startNum + (endNum - startNum) * progress;
        var error     = Math.Abs(nextNum - predicted);
        var threshold = tolerance / 100.0 * ((Math.Abs(startNum) + Math.Abs(nextNum)) / 2.0 + 1e-9);
        return error > threshold || nextBeat >= intervalEnd;
    }

    private static double ToDouble(dynamic? value)
    {
        if (value == null)
            throw new InvalidOperationException("Unexpected null numeric value.");

        return (double)value;
    }

    private static T AddValues<T>(T? left, T? right) => (dynamic?)left + (dynamic?)right;

    private static void AddSegmentEvent<T>(List<Rpe.Event<T>> target, Beat startBeat, Beat endBeat,
        T? startToValue, T? startFormValue, T? endToValue, T? endFormValue)
    {
        target.Add(new Rpe.Event<T>
        {
            StartBeat  = startBeat, EndBeat = endBeat,
            StartValue = AddValues(startToValue, startFormValue),
            EndValue   = AddValues(endToValue,   endFormValue),
        });
    }
}


