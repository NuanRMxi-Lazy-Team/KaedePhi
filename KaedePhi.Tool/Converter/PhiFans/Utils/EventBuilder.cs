using KaedePhi.Core.Common;
using KaedePhi.Core.PhiFans;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Event.KaedePhi;
using KaedePhi.Tool.Layer.KaedePhi;
using PfEvent = KaedePhi.Core.PhiFans.Event;

namespace KaedePhi.Tool.Converter.PhiFans.Utils;

internal static class EventBuilder
{
    private const double BeatEpsilon = 1e-7;
    private const float ValueEpsilon = 1e-5f;

    internal static void RemoveInstantEvents(KpcEvents.EventLayer layer)
    {
        layer.AlphaEvents?.RemoveAll(evt => evt.StartBeat == evt.EndBeat);
        layer.MoveXEvents?.RemoveAll(evt => evt.StartBeat == evt.EndBeat);
        layer.MoveYEvents?.RemoveAll(evt => evt.StartBeat == evt.EndBeat);
        layer.RotateEvents?.RemoveAll(evt => evt.StartBeat == evt.EndBeat);
        layer.SpeedEvents?.RemoveAll(evt => evt.StartBeat == evt.EndBeat);
    }

    internal static (
        List<KpcEvents.Event<T>> Events,
        HashSet<Beat> ExactStepBeats
    ) ResolveChannelComposition<T>(
        List<KpcEvents.Event<T>>? configuredEvents,
        IReadOnlyList<KpcEvents.EventLayer> sourceLayers,
        Func<KpcEvents.EventLayer, List<KpcEvents.Event<T>>?> selectEvents,
        bool linearOnly,
        double cutLength
    )
        where T : notnull
    {
        var sourceEventLists = sourceLayers.Select(layer => selectEvents(layer) ?? []).ToList();
        var sourceStartBeats = sourceEventLists
            .SelectMany(events => events)
            .Select(evt => evt.StartBeat)
            .ToHashSet();
        var intervals = CollectUnsupportedOverlapIntervals(sourceEventLists, linearOnly);
        var resolved = configuredEvents?.ConvertAll(evt => evt.Clone()) ?? [];
        if (intervals.Count > 0)
        {
            resolved = SpliceComposedIntervals(
                resolved,
                sourceEventLists,
                sourceStartBeats,
                intervals,
                linearOnly,
                cutLength
            );
        }

        var stepBeats = CollectStepTransitionBeats(sourceEventLists);
        if (stepBeats.Count > 0)
        {
            resolved = ComposeStepTimeline(
                resolved,
                sourceEventLists,
                sourceStartBeats,
                stepBeats,
                linearOnly,
                cutLength
            );
        }

        resolved = resolved.OrderBy(evt => evt.StartBeat).ThenBy(evt => evt.EndBeat).ToList();
        return (resolved, stepBeats);
    }

    private static List<(Beat Start, Beat End)> CollectUnsupportedOverlapIntervals<T>(
        IReadOnlyList<List<KpcEvents.Event<T>>> sourceEventLists,
        bool linearOnly
    )
        where T : notnull
    {
        var indexedEvents = new List<(int LayerIndex, KpcEvents.Event<T> Event)>();
        for (var layerIndex = 0; layerIndex < sourceEventLists.Count; layerIndex++)
        {
            foreach (var evt in sourceEventLists[layerIndex])
            {
                if (evt.StartBeat < evt.EndBeat)
                    indexedEvents.Add((layerIndex, evt));
            }
        }

        indexedEvents = indexedEvents
            .OrderBy(item => item.Event.StartBeat)
            .ThenBy(item => item.Event.EndBeat)
            .ToList();
        var intervals = new List<(Beat Start, Beat End)>();
        for (var componentStart = 0; componentStart < indexedEvents.Count; )
        {
            var componentEndBeat = indexedEvents[componentStart].Event.EndBeat;
            var componentEnd = componentStart + 1;
            while (
                componentEnd < indexedEvents.Count
                && indexedEvents[componentEnd].Event.StartBeat < componentEndBeat
            )
            {
                if (indexedEvents[componentEnd].Event.EndBeat > componentEndBeat)
                    componentEndBeat = indexedEvents[componentEnd].Event.EndBeat;
                componentEnd++;
            }

            if (
                ComponentHasUnsupportedCrossLayerOverlap(
                    indexedEvents,
                    componentStart,
                    componentEnd,
                    linearOnly
                )
            )
            {
                intervals.Add((indexedEvents[componentStart].Event.StartBeat, componentEndBeat));
            }

            componentStart = componentEnd;
        }

        return intervals;
    }

    private static bool ComponentHasUnsupportedCrossLayerOverlap<T>(
        IReadOnlyList<(int LayerIndex, KpcEvents.Event<T> Event)> events,
        int startIndex,
        int endIndex,
        bool linearOnly
    )
        where T : notnull
    {
        var active = new PriorityQueue<(int LayerIndex, bool Unsupported), Beat>();
        var activeByLayer = new Dictionary<int, int>();
        var unsupportedByLayer = new Dictionary<int, int>();
        var activeCount = 0;
        var unsupportedCount = 0;
        for (var index = startIndex; index < endIndex; index++)
        {
            var current = events[index];
            while (
                active.TryPeek(out var expired, out var expiredEnd)
                && expiredEnd <= current.Event.StartBeat
            )
            {
                active.Dequeue();
                activeByLayer[expired.LayerIndex]--;
                activeCount--;
                if (expired.Unsupported)
                {
                    unsupportedByLayer[expired.LayerIndex]--;
                    unsupportedCount--;
                }
            }

            var unsupported = !CanMapDirectly(current.Event, linearOnly);
            var sameLayerActive = activeByLayer.GetValueOrDefault(current.LayerIndex);
            var sameLayerUnsupported = unsupportedByLayer.GetValueOrDefault(current.LayerIndex);
            if (
                (unsupported && activeCount > sameLayerActive)
                || unsupportedCount > sameLayerUnsupported
            )
                return true;

            active.Enqueue((current.LayerIndex, unsupported), current.Event.EndBeat);
            activeByLayer[current.LayerIndex] = sameLayerActive + 1;
            activeCount++;
            if (unsupported)
            {
                unsupportedByLayer[current.LayerIndex] = sameLayerUnsupported + 1;
                unsupportedCount++;
            }
        }

        return false;
    }

    private static List<KpcEvents.Event<T>> SpliceComposedIntervals<T>(
        List<KpcEvents.Event<T>> configuredEvents,
        IReadOnlyList<List<KpcEvents.Event<T>>> sourceEventLists,
        IReadOnlySet<Beat> sourceStartBeats,
        IReadOnlyList<(Beat Start, Beat End)> intervals,
        bool linearOnly,
        double cutLength
    )
        where T : notnull
    {
        var result = new List<KpcEvents.Event<T>>();
        foreach (var evt in configuredEvents)
        {
            if (evt.StartBeat == evt.EndBeat)
            {
                if (!intervals.Any(interval => IsBeatInInterval(evt.StartBeat, interval)))
                    result.Add(evt.Clone());
                continue;
            }

            foreach (var fragment in SubtractIntervals(evt.StartBeat, evt.EndBeat, intervals))
            {
                if (fragment.Start == evt.StartBeat && fragment.End == evt.EndBeat)
                    result.Add(evt.Clone());
                else
                    result.AddRange(
                        ComposeConfiguredFragment(evt, fragment.Start, fragment.End, cutLength)
                    );
            }
        }

        foreach (var interval in intervals)
        {
            result.AddRange(
                ComposeInterval(
                    sourceEventLists,
                    sourceStartBeats,
                    interval.Start,
                    interval.End,
                    linearOnly,
                    cutLength
                )
            );
        }

        return result.OrderBy(evt => evt.StartBeat).ThenBy(evt => evt.EndBeat).ToList();
    }

    private static List<KpcEvents.Event<T>> ComposeConfiguredFragment<T>(
        KpcEvents.Event<T> evt,
        Beat start,
        Beat end,
        double cutLength
    )
        where T : notnull
    {
        if (CanMapDirectly(evt, true))
        {
            return
            [
                CreateLinearEvent(start, end, evt.GetValueAtBeat(start), evt.GetValueAtBeat(end)),
            ];
        }

        var result = new List<KpcEvents.Event<T>>();
        var step = new Beat(cutLength);
        for (var beat = start; beat < end; )
        {
            var next = beat + step;
            if (next > end)
                next = end;
            if (next <= beat)
                throw new InvalidOperationException("切片步长无法推进配置事件位置。");
            result.Add(
                CreateLinearEvent(beat, next, evt.GetValueAtBeat(beat), evt.GetValueAtBeat(next))
            );
            beat = next;
        }

        return result;
    }

    private static bool IsBeatInInterval(Beat beat, (Beat Start, Beat End) interval) =>
        beat >= interval.Start && beat <= interval.End;

    private static List<(Beat Start, Beat End)> SubtractIntervals(
        Beat start,
        Beat end,
        IReadOnlyList<(Beat Start, Beat End)> intervals
    )
    {
        var fragments = new List<(Beat Start, Beat End)>();
        var cursor = start;
        foreach (var interval in intervals)
        {
            if (interval.End <= cursor || interval.Start >= end)
                continue;
            if (interval.Start > cursor)
                fragments.Add((cursor, interval.Start < end ? interval.Start : end));
            if (interval.End > cursor)
                cursor = interval.End;
            if (cursor >= end)
                break;
        }

        if (cursor < end)
            fragments.Add((cursor, end));
        return fragments;
    }

    private static List<KpcEvents.Event<T>> ComposeInterval<T>(
        IReadOnlyList<List<KpcEvents.Event<T>>> sourceEventLists,
        IReadOnlySet<Beat> sourceStartBeats,
        Beat start,
        Beat end,
        bool linearOnly,
        double cutLength
    )
        where T : notnull
    {
        if (start >= end)
            return [];

        var boundaries = new SortedSet<Beat> { start, end };
        var step = new Beat(cutLength);
        for (var beat = start; beat < end; )
        {
            var next = beat + step;
            if (next > end)
                next = end;
            if (next <= beat)
                throw new InvalidOperationException("切片步长无法推进组合事件位置。");
            boundaries.Add(next);
            beat = next;
        }

        var cutter = new EventCutter<T>();
        foreach (var events in sourceEventLists)
        {
            foreach (var evt in EnumerateEventsForInterval(events, start, end))
            {
                AddBoundaryIfInside(boundaries, evt.StartBeat, start, end);
                AddBoundaryIfInside(boundaries, evt.EndBeat, start, end);
                if (
                    evt.StartBeat >= evt.EndBeat
                    || evt.StartBeat >= end
                    || evt.EndBeat <= start
                    || CanMapDirectly(evt, linearOnly)
                )
                    continue;
                foreach (var segment in cutter.CutEventToLinear(evt, cutLength))
                {
                    AddBoundaryIfInside(boundaries, segment.StartBeat, start, end);
                    AddBoundaryIfInside(boundaries, segment.EndBeat, start, end);
                }
            }
        }

        var ordered = boundaries.ToList();
        var result = new List<KpcEvents.Event<T>>(ordered.Count - 1);
        for (var index = 0; index < ordered.Count - 1; index++)
        {
            var segmentStart = ordered[index];
            var segmentEnd = ordered[index + 1];
            var endValue = sourceStartBeats.Contains(segmentEnd)
                ? SumBeforeBeat(sourceEventLists, segmentEnd)
                : SumAtBeat(sourceEventLists, segmentEnd);
            result.Add(
                CreateLinearEvent(
                    segmentStart,
                    segmentEnd,
                    SumAtBeat(sourceEventLists, segmentStart),
                    endValue
                )
            );
        }

        return result;
    }

    private static IEnumerable<KpcEvents.Event<T>> EnumerateEventsForInterval<T>(
        List<KpcEvents.Event<T>> events,
        Beat start,
        Beat end
    )
        where T : notnull
    {
        var firstIndex = FindFirstEventAtOrAfterBeat(events, start);
        if (firstIndex > 0)
        {
            var previous = events[firstIndex - 1];
            if (previous.StartBeat < end && previous.EndBeat > start)
                yield return previous;
        }

        for (var index = firstIndex; index < events.Count && events[index].StartBeat < end; index++)
        {
            yield return events[index];
        }
    }

    private static void AddBoundaryIfInside(
        SortedSet<Beat> boundaries,
        Beat beat,
        Beat start,
        Beat end
    )
    {
        if (beat > start && beat < end)
            boundaries.Add(beat);
    }

    private static HashSet<Beat> CollectStepTransitionBeats<T>(
        IReadOnlyList<List<KpcEvents.Event<T>>> sourceEventLists
    )
        where T : notnull
    {
        var transitions = new HashSet<Beat>();
        foreach (var events in sourceEventLists)
        {
            for (var index = 0; index < events.Count; )
            {
                var groupEnd = index + 1;
                while (
                    groupEnd < events.Count && events[groupEnd].StartBeat == events[index].StartBeat
                )
                {
                    groupEnd++;
                }

                var effective = events[groupEnd - 1];
                if (effective.StartBeat == effective.EndBeat)
                {
                    transitions.Add(effective.StartBeat);
                    if (groupEnd < events.Count)
                        transitions.Add(events[groupEnd].StartBeat);
                }

                index = groupEnd;
            }
        }

        return transitions;
    }

    private static List<KpcEvents.Event<T>> ComposeStepTimeline<T>(
        List<KpcEvents.Event<T>> configuredEvents,
        IReadOnlyList<List<KpcEvents.Event<T>>> sourceEventLists,
        IReadOnlySet<Beat> sourceStartBeats,
        HashSet<Beat> stepBeats,
        bool linearOnly,
        double cutLength
    )
        where T : notnull
    {
        var orderedSteps = stepBeats.OrderBy(beat => beat).ToList();
        var result = new List<KpcEvents.Event<T>>();
        foreach (var evt in configuredEvents.Where(evt => evt.StartBeat < evt.EndBeat))
        {
            var interiorSteps = orderedSteps
                .Where(beat => beat > evt.StartBeat && beat < evt.EndBeat)
                .ToList();
            if (interiorSteps.Count == 0)
            {
                var clone = evt.Clone();
                clone.StartValue = SumAtBeat(sourceEventLists, clone.StartBeat);
                clone.EndValue = stepBeats.Contains(clone.EndBeat)
                    ? SumBeforeBeat(sourceEventLists, clone.EndBeat)
                    : SumAtBeat(sourceEventLists, clone.EndBeat);
                result.Add(clone);
                continue;
            }

            var boundaries = new List<Beat> { evt.StartBeat };
            boundaries.AddRange(interiorSteps);
            boundaries.Add(evt.EndBeat);
            for (var index = 0; index < boundaries.Count - 1; index++)
            {
                var fragmentStart = boundaries[index];
                var fragmentEnd = boundaries[index + 1];
                if (CanMapDirectly(evt, true))
                {
                    result.Add(
                        CreateLinearEvent(
                            fragmentStart,
                            fragmentEnd,
                            SumAtBeat(sourceEventLists, fragmentStart),
                            stepBeats.Contains(fragmentEnd)
                                ? SumBeforeBeat(sourceEventLists, fragmentEnd)
                                : SumAtBeat(sourceEventLists, fragmentEnd)
                        )
                    );
                }
                else
                {
                    result.AddRange(
                        ComposeInterval(
                            sourceEventLists,
                            sourceStartBeats,
                            fragmentStart,
                            fragmentEnd,
                            linearOnly,
                            cutLength
                        )
                    );
                }
            }
        }

        foreach (var stepBeat in orderedSteps)
        {
            if (result.Any(evt => evt.StartBeat == stepBeat))
                continue;
            result.Add(CreateInstantEvent(stepBeat, SumAtBeat(sourceEventLists, stepBeat)));
        }

        return result.OrderBy(evt => evt.StartBeat).ThenBy(evt => evt.EndBeat).ToList();
    }

    private static T SumAtBeat<T>(
        IReadOnlyList<List<KpcEvents.Event<T>>> sourceEventLists,
        Beat beat
    )
        where T : notnull
    {
        var sum = default(T)!;
        foreach (var events in sourceEventLists)
        {
            if (events.Count > 0)
                sum = NumericHelper.Add(sum, KpcEvents.EventLayer.GetValueAtBeat(events, beat));
        }

        return sum;
    }

    private static T SumBeforeBeat<T>(
        IReadOnlyList<List<KpcEvents.Event<T>>> sourceEventLists,
        Beat beat
    )
        where T : notnull
    {
        var sum = default(T)!;
        foreach (var events in sourceEventLists)
        {
            var dominant = FindLastEventBeforeBeat(events, beat);
            if (dominant is null)
                continue;
            var value =
                beat <= dominant.EndBeat ? dominant.GetValueAtBeat(beat) : dominant.EndValue;
            sum = NumericHelper.Add(sum, value);
        }

        return sum;
    }

    private static KpcEvents.Event<T>? FindLastEventBeforeBeat<T>(
        List<KpcEvents.Event<T>> events,
        Beat beat
    )
        where T : notnull
    {
        var candidate = FindFirstEventAtOrAfterBeat(events, beat) - 1;
        return candidate >= 0 ? events[candidate] : null;
    }

    private static int FindFirstEventAtOrAfterBeat<T>(List<KpcEvents.Event<T>> events, Beat beat)
        where T : notnull
    {
        var low = 0;
        var high = events.Count;
        while (low < high)
        {
            var middle = low + ((high - low) >> 1);
            if (events[middle].StartBeat < beat)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private static KpcEvents.Event<T> CreateLinearEvent<T>(
        Beat start,
        Beat end,
        T startValue,
        T endValue
    )
        where T : notnull =>
        new()
        {
            StartBeat = start,
            EndBeat = end,
            StartValue = startValue,
            EndValue = endValue,
            Easing = Kpc.Easing.Linear,
        };

    private static KpcEvents.Event<T> CreateInstantEvent<T>(Beat beat, T value)
        where T : notnull =>
        new()
        {
            StartBeat = beat,
            EndBeat = beat,
            StartValue = value,
            EndValue = value,
            Easing = Kpc.Easing.Linear,
        };

    internal static IEnumerable<KpcEvents.Event<T>> ExpandUnsupportedEvents<T>(
        IEnumerable<KpcEvents.Event<T>> events,
        double cutLength,
        bool linearOnly
    )
        where T : notnull
    {
        var cutter = new EventCutter<T>();
        foreach (var evt in events)
        {
            if (CanMapDirectly(evt, linearOnly))
            {
                yield return evt;
                continue;
            }

            var segments = cutter.CutEventToLinear(evt, cutLength);
            if (segments.Count > 0)
            {
                foreach (var segment in segments)
                    yield return segment;
                continue;
            }

            if (evt.StartBeat == evt.EndBeat)
                yield return CreateLinearInstantEvent(evt);
        }
    }

    private static KpcEvents.Event<T> CreateLinearInstantEvent<T>(KpcEvents.Event<T> evt)
        where T : notnull
    {
        var instant = evt.Clone();
        instant.StartValue = instant.EndValue;
        instant.Easing = Kpc.Easing.Linear;
        instant.IsBezier = false;
        instant.EasingLeft = 0;
        instant.EasingRight = 1;
        return instant;
    }

    private static bool CanMapDirectly<T>(KpcEvents.Event<T> evt, bool linearOnly)
        where T : notnull
    {
        var easing = (int)evt.Easing;
        return !evt.IsBezier
            && Math.Abs(evt.EasingLeft) <= Constants.FloatEpsilon
            && Math.Abs(evt.EasingRight - 1f) <= Constants.FloatEpsilon
            && (linearOnly ? easing == 1 : easing is >= 1 and <= 31);
    }

    internal static List<KpcEvents.Event<double>> ConvertPhiFansEventsToDouble(
        List<PfEvent> src,
        Func<float, double> valueTransform
    )
    {
        var result = new List<KpcEvents.Event<double>>();
        var i = 0;
        while (i < src.Count)
        {
            var item = src[i];
            if (item.Continuous)
            {
                result.Add(CreateInstantKpcEvent(item.Beat, item.Value, valueTransform));
                i++;
                continue;
            }

            if (i + 1 < src.Count && src[i + 1].Continuous)
            {
                var endItem = src[i + 1];
                result.Add(
                    new KpcEvents.Event<double>
                    {
                        StartBeat = new Beat((int[])item.Beat),
                        EndBeat = new Beat((int[])endItem.Beat),
                        StartValue = valueTransform(item.Value),
                        EndValue = valueTransform(endItem.Value),
                        Easing = new Kpc.Easing(EasingConverter.ToKpc((int)item.Easing)),
                    }
                );
                i += 2;
            }
            else
            {
                result.Add(CreateInstantKpcEvent(item.Beat, item.Value, valueTransform));
                i++;
            }
        }

        return result;
    }

    internal static List<KpcEvents.Event<float>> ConvertPhiFansEventsToFloat(
        List<PfEvent> src,
        Func<float, float> valueTransform
    )
    {
        var result = new List<KpcEvents.Event<float>>();
        var i = 0;
        while (i < src.Count)
        {
            var item = src[i];
            if (item.Continuous)
            {
                result.Add(CreateInstantKpcEvent(item.Beat, item.Value, valueTransform));
                i++;
                continue;
            }

            if (i + 1 < src.Count && src[i + 1].Continuous)
            {
                var endItem = src[i + 1];
                result.Add(
                    new KpcEvents.Event<float>
                    {
                        StartBeat = new Beat((int[])item.Beat),
                        EndBeat = new Beat((int[])endItem.Beat),
                        StartValue = valueTransform(item.Value),
                        EndValue = valueTransform(endItem.Value),
                        Easing = new Kpc.Easing(1),
                    }
                );
                i += 2;
            }
            else
            {
                result.Add(CreateInstantKpcEvent(item.Beat, item.Value, valueTransform));
                i++;
            }
        }

        return result;
    }

    internal static List<KpcEvents.Event<int>> ConvertPhiFansEventsToInt(
        List<PfEvent> src,
        Func<float, int> valueTransform
    )
    {
        var result = new List<KpcEvents.Event<int>>();
        var i = 0;
        while (i < src.Count)
        {
            var item = src[i];
            if (item.Continuous)
            {
                result.Add(CreateInstantKpcEvent(item.Beat, item.Value, valueTransform));
                i++;
                continue;
            }

            if (i + 1 < src.Count && src[i + 1].Continuous)
            {
                var endItem = src[i + 1];
                result.Add(
                    new KpcEvents.Event<int>
                    {
                        StartBeat = new Beat((int[])item.Beat),
                        EndBeat = new Beat((int[])endItem.Beat),
                        StartValue = valueTransform(item.Value),
                        EndValue = valueTransform(endItem.Value),
                        Easing = new Kpc.Easing(EasingConverter.ToKpc((int)item.Easing)),
                    }
                );
                i += 2;
            }
            else
            {
                result.Add(CreateInstantKpcEvent(item.Beat, item.Value, valueTransform));
                i++;
            }
        }

        return result;
    }

    private static KpcEvents.Event<T> CreateInstantKpcEvent<T>(
        Beat beat,
        float value,
        Func<float, T> valueTransform
    )
        where T : notnull
    {
        var v = valueTransform(value);
        return new KpcEvents.Event<T>
        {
            StartBeat = new Beat((int[])beat),
            EndBeat = new Beat((int[])beat),
            StartValue = v,
            EndValue = v,
            Easing = new Kpc.Easing(1),
        };
    }

    internal static void ConvertKpcEventToPhiFans<T>(
        KpcEvents.Event<T> src,
        List<PfEvent> dst,
        Func<T, float> valueTransform,
        Func<int, int> easingMap
    )
        where T : notnull
    {
        var startVal = valueTransform(src.StartValue);
        var endVal = valueTransform(src.EndValue);
        var easing = easingMap((int)src.Easing);

        if (Math.Abs(startVal - endVal) < float.Epsilon)
        {
            dst.Add(
                new PfEvent
                {
                    Beat = new Beat((int[])src.StartBeat),
                    Value = startVal,
                    Continuous = false,
                    Easing = new Easing(easing),
                }
            );
            return;
        }

        dst.Add(
            new PfEvent
            {
                Beat = new Beat((int[])src.StartBeat),
                Value = startVal,
                Continuous = false,
                Easing = new Easing(easing),
            }
        );

        dst.Add(
            new PfEvent
            {
                Beat = new Beat((int[])src.EndBeat),
                Value = endVal,
                Continuous = true,
                Easing = new Easing(easing),
            }
        );
    }

    /// <summary>
    /// 修复节点式格式中的相邻事件：连续区间共用结束节点，值不连续时才创建新的断点节点。
    /// </summary>
    internal static void FixDiscontinuityGaps(
        List<PfEvent> events,
        int paddingPrecision,
        IReadOnlySet<Beat>? exactStepBeats = null
    )
    {
        if (events.Count < 2 || paddingPrecision <= 0)
            return;

        var padding = new Beat([0, 1, paddingPrecision]);
        while (true)
        {
            events.Sort(ComparePhiFansEvents);
            var changed = false;

            for (var i = 1; i < events.Count; i++)
            {
                var prev = events[i - 1];
                var curr = events[i];

                if (!prev.Continuous || curr.Continuous)
                    continue;

                if (Math.Abs((double)prev.Beat - (double)curr.Beat) > BeatEpsilon)
                    continue;

                if (exactStepBeats?.Contains(curr.Beat) == true)
                {
                    if (!prev.Value.Equals(curr.Value))
                        continue;
                    events.RemoveAt(i);
                }
                else if (Math.Abs(prev.Value - curr.Value) <= ValueEpsilon)
                {
                    // 后事件沿用前事件的结束节点，删除重复的非连续起点，避免重置连续链。
                    events.RemoveAt(i);
                }
                else
                {
                    // 直接进行有理数加法，避免精分先转换为浮点数后丢失。
                    curr.Beat += padding;
                }

                changed = true;
                break;
            }

            if (!changed)
                return;
        }
    }

    private static int ComparePhiFansEvents(PfEvent left, PfEvent right)
    {
        var beatComparison = ((double)left.Beat).CompareTo((double)right.Beat);
        if (beatComparison != 0)
            return beatComparison;

        // 同拍时先处理过渡终点，再处理新事件起点，便于判断是否可以共用节点。
        if (left.Continuous == right.Continuous)
            return 0;
        return left.Continuous ? -1 : 1;
    }
}
