using KaedePhi.Core.Common;
using KaedePhi.Core.PhiFans;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiFans.Model;
using KaedePhi.Tool.Event.KaedePhi;
using KaedePhi.Tool.Layer.KaedePhi;
using KpcNoteType = KaedePhi.Core.Common.NoteType;
using PfEvent = KaedePhi.Core.PhiFans.Event;
using PfNoteType = KaedePhi.Core.PhiFans.NoteType;

namespace KaedePhi.Tool.Converter.PhiFans;

/// <summary>
/// PhiFans 格式转换器。
/// </summary>
public class PhiFansConverter
    : LoggableBase,
        IChartConverter<Chart, Unit?, KpcToPhiFansConvertOptions>,
        ICancellableChartConverter
{
    private CancellationToken _ct;

    // 此值为粗略估算，并非严谨计算后得出的内容，请知悉。
    private const float SpeedRatio = 7.15f;
    private const double BeatEpsilon = 1e-7;
    private const float ValueEpsilon = 1e-5f;

    /// <inheritdoc/>
    public void SetCancellationToken(CancellationToken ct) => _ct = ct;

    #region ToKpc：PhiFans → KPC

    /// <summary>
    /// 将 PhiFans 格式转换为 KPC 内部格式。
    /// </summary>
    /// <param name="source">PhiFans 谱面</param>
    /// <param name="_">未使用</param>
    /// <returns>KPC 谱面</returns>
    public Kpc.Chart ToKpc(Chart source, Unit? _)
    {
        ArgumentNullException.ThrowIfNull(source);
        _ct.ThrowIfCancellationRequested();
        var converted = new Kpc.Chart
        {
            BpmList = source.BpmList.ConvertAll(ConvertBpmItem),
            Meta = ConvertMeta(source.Info, source.Offset),
            JudgeLineList = ConvertLinesWithCancellation(source.JudgeLineList),
        };
        return ChartProcessingValidator.NormalizeAndValidateNoteEndBeats(converted);
    }

    private List<Kpc.JudgeLine> ConvertLinesWithCancellation(List<Line> lines)
    {
        var result = new List<Kpc.JudgeLine>(lines.Count);
        foreach (var t in lines)
        {
            _ct.ThrowIfCancellationRequested();
            result.Add(ConvertLine(t));
        }
        return result;
    }

    /// <summary>
    /// 将 KPC 内部格式转换为 PhiFans 格式。
    /// </summary>
    /// <param name="input">KPC 谱面</param>
    /// <param name="options">输出转换选项</param>
    /// <returns>PhiFans 谱面</returns>
    public Chart FromKpc(Kpc.Chart input, KpcToPhiFansConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);
        ConversionOptionsValidator.Validate(options);
        var normalized = ChartProcessingValidator.NormalizeAndValidateNoteEndBeats(input);
        ChartProcessingValidator.ValidateJudgeLineHierarchy(normalized.JudgeLineList);
        _ct.ThrowIfCancellationRequested();

        var lines = new List<Line>(normalized.JudgeLineList.Count);
        foreach (var line in normalized.JudgeLineList)
        {
            _ct.ThrowIfCancellationRequested();
            lines.Add(ConvertLine(line, options));
        }

        return new Chart
        {
            Offset = normalized.Meta.Offset,
            Info = ConvertMeta(normalized.Meta),
            BpmList = normalized.BpmList.ConvertAll(ConvertBpmItem),
            JudgeLineList = lines,
        };
    }

    #endregion

    #region BPM 转换

    private static Kpc.BpmItem ConvertBpmItem(Bpm src) =>
        new() { Bpm = src.BeatPerMinute, StartBeat = new Beat((int[])src.StartBeat) };

    private static Bpm ConvertBpmItem(Kpc.BpmItem src) =>
        new() { BeatPerMinute = src.Bpm, StartBeat = new Beat((int[])src.StartBeat) };

    #endregion

    #region Meta 转换

    private static Kpc.Meta ConvertMeta(Info info, int offset) =>
        new()
        {
            Name = info.Name,
            Composer = info.Artist,
            Artist = info.Illustration,
            Level = info.Level,
            Author = info.Designer,
            Offset = offset,
        };

    private static Info ConvertMeta(Kpc.Meta src) =>
        new()
        {
            Name = src.Name,
            Artist = src.Composer,
            Illustration = src.Artist,
            Level = src.Level,
            Designer = src.Author,
        };

    #endregion

    #region 判定线转换

    private static Kpc.JudgeLine ConvertLine(Line src)
    {
        var line = new Kpc.JudgeLine { Notes = src.NoteList.ConvertAll(ConvertNoteToKpc) };

        var layer = new KpcEvents.EventLayer();
        var props = src.Props;

        if (props.Speed.Count > 0)
            layer.SpeedEvents = ConvertPhifansEventsToFloat(props.Speed, v => v * SpeedRatio);

        if (props.PositionX.Count > 0)
            layer.MoveXEvents = ConvertPhifansEventsToDouble(
                props.PositionX,
                v => v / Chart.CoordinateSystem.MaxX
            );

        if (props.PositionY.Count > 0)
            layer.MoveYEvents = ConvertPhifansEventsToDouble(
                props.PositionY,
                v => v / Chart.CoordinateSystem.MaxY
            );

        if (props.Rotate.Count > 0)
            layer.RotateEvents = ConvertPhifansEventsToDouble(
                props.Rotate,
                v => CoordinateGeometry.ToKpcAngle(v, CoordinateProfile.PhiFansProfile)
            );

        if (props.Alpha.Count > 0)
            layer.AlphaEvents = ConvertPhifansEventsToInt(props.Alpha, v => (int)v);

        line.EventLayers = [layer];
        return line;
    }

    private static Line ConvertLine(Kpc.JudgeLine src, KpcToPhiFansConvertOptions options)
    {
        var line = new Line { NoteList = src.Notes.ConvertAll(ConvertNoteFromKpc) };
        var sourceLayers = src.EventLayers.ConvertAll(layer => layer.Clone());
        foreach (var sourceLayer in sourceLayers)
            sourceLayer.Sort();
        var layers = sourceLayers.ConvertAll(layer => layer.Clone());
        foreach (var mergeLayer in layers)
            RemoveInstantEvents(mergeLayer);
        var layerProcessor = new LayerProcessor();
        KpcEvents.EventLayer layer;
        if (options.MultiLayerMerge.ClassicMode)
        {
            layer = layerProcessor.LayerMerge(layers, options.MultiLayerMerge.Precision);
            if (options.MultiLayerMerge.Compress)
                layerProcessor.LayerEventsCompress(layer, options.MultiLayerMerge.Tolerance);
        }
        else
        {
            layer = layerProcessor.LayerMergePlus(
                layers,
                options.MultiLayerMerge.Precision,
                options.MultiLayerMerge.Tolerance
            );
        }

        var cutLength = 1d / options.Cutting.UnsupportedEasingPrecision;
        var (alphaEvents, alphaStepBeats) = ResolveChannelComposition(
            layer.AlphaEvents,
            sourceLayers,
            sourceLayer => sourceLayer.AlphaEvents,
            false,
            cutLength
        );
        var (moveXEvents, moveXStepBeats) = ResolveChannelComposition(
            layer.MoveXEvents,
            sourceLayers,
            sourceLayer => sourceLayer.MoveXEvents,
            false,
            cutLength
        );
        var (moveYEvents, moveYStepBeats) = ResolveChannelComposition(
            layer.MoveYEvents,
            sourceLayers,
            sourceLayer => sourceLayer.MoveYEvents,
            false,
            cutLength
        );
        var (rotateEvents, rotateStepBeats) = ResolveChannelComposition(
            layer.RotateEvents,
            sourceLayers,
            sourceLayer => sourceLayer.RotateEvents,
            false,
            cutLength
        );
        var (speedEvents, speedStepBeats) = ResolveChannelComposition(
            layer.SpeedEvents,
            sourceLayers,
            sourceLayer => sourceLayer.SpeedEvents,
            true,
            cutLength
        );
        layer.AlphaEvents = alphaEvents;
        layer.MoveXEvents = moveXEvents;
        layer.MoveYEvents = moveYEvents;
        layer.RotateEvents = rotateEvents;
        layer.SpeedEvents = speedEvents;

        if (layer.AlphaEvents is not null)
            foreach (var e in ExpandUnsupportedEvents(layer.AlphaEvents, cutLength, false))
                ConvertKpcEventToPhiFans(e, line.Props.Alpha, v => (float)v, MapKpcEasingToPp);

        if (layer.MoveXEvents is not null)
            foreach (var e in ExpandUnsupportedEvents(layer.MoveXEvents, cutLength, false))
                ConvertKpcEventToPhiFans(
                    e,
                    line.Props.PositionX,
                    v => (float)(v * 100.0),
                    MapKpcEasingToPp
                );

        if (layer.MoveYEvents is not null)
            foreach (var e in ExpandUnsupportedEvents(layer.MoveYEvents, cutLength, false))
                ConvertKpcEventToPhiFans(
                    e,
                    line.Props.PositionY,
                    v => (float)(v * 100.0),
                    MapKpcEasingToPp
                );

        if (layer.RotateEvents is not null)
            foreach (var e in ExpandUnsupportedEvents(layer.RotateEvents, cutLength, false))
                ConvertKpcEventToPhiFans(
                    e,
                    line.Props.Rotate,
                    v =>
                        (float)
                            CoordinateGeometry.ToTargetAngle(
                                v,
                                CoordinateProfile.PhiFansProfile
                            ),
                    MapKpcEasingToPp
                );

        if (layer.SpeedEvents is not null)
            foreach (var e in ExpandUnsupportedEvents(layer.SpeedEvents, cutLength, true))
                ConvertKpcEventToPhiFans(e, line.Props.Speed, v => v / SpeedRatio, _ => 0);

        var paddingPrecision = options.DiscontinuityBeatPrecision;
        FixDiscontinuityGaps(line.Props.Alpha, paddingPrecision, alphaStepBeats);
        FixDiscontinuityGaps(line.Props.PositionX, paddingPrecision, moveXStepBeats);
        FixDiscontinuityGaps(line.Props.PositionY, paddingPrecision, moveYStepBeats);
        FixDiscontinuityGaps(line.Props.Rotate, paddingPrecision, rotateStepBeats);
        FixDiscontinuityGaps(line.Props.Speed, paddingPrecision, speedStepBeats);

        return line;
    }

    private static void RemoveInstantEvents(KpcEvents.EventLayer layer)
    {
        layer.AlphaEvents?.RemoveAll(evt => evt.StartBeat == evt.EndBeat);
        layer.MoveXEvents?.RemoveAll(evt => evt.StartBeat == evt.EndBeat);
        layer.MoveYEvents?.RemoveAll(evt => evt.StartBeat == evt.EndBeat);
        layer.RotateEvents?.RemoveAll(evt => evt.StartBeat == evt.EndBeat);
        layer.SpeedEvents?.RemoveAll(evt => evt.StartBeat == evt.EndBeat);
    }

    private static (
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
        var sourceEventLists = sourceLayers
            .Select(layer => selectEvents(layer) ?? [])
            .ToList();
        var intervals = CollectUnsupportedOverlapIntervals(sourceEventLists, linearOnly);
        var resolved = configuredEvents?.ConvertAll(evt => evt.Clone()) ?? [];
        if (intervals.Count > 0)
        {
            resolved = SpliceComposedIntervals(
                resolved,
                sourceEventLists,
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

        var intervals = new List<(Beat Start, Beat End)>();
        foreach (var root in indexedEvents)
        {
            if (
                CanMapDirectly(root.Event, linearOnly)
                || !indexedEvents.Any(candidate =>
                    candidate.LayerIndex != root.LayerIndex
                    && EventsOverlap(root.Event, candidate.Event)
                )
            )
                continue;

            var start = root.Event.StartBeat;
            var end = root.Event.EndBeat;
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var candidate in indexedEvents)
                {
                    if (candidate.Event.StartBeat >= end || candidate.Event.EndBeat <= start)
                        continue;
                    if (candidate.Event.StartBeat < start)
                    {
                        start = candidate.Event.StartBeat;
                        changed = true;
                    }
                    if (candidate.Event.EndBeat > end)
                    {
                        end = candidate.Event.EndBeat;
                        changed = true;
                    }
                }
            }

            intervals.Add((start, end));
        }

        return MergeIntervals(intervals);
    }

    private static bool EventsOverlap<T>(
        KpcEvents.Event<T> left,
        KpcEvents.Event<T> right
    )
        where T : notnull =>
        left.StartBeat < right.EndBeat && right.StartBeat < left.EndBeat;

    private static List<(Beat Start, Beat End)> MergeIntervals(
        List<(Beat Start, Beat End)> intervals
    )
    {
        var ordered = intervals.OrderBy(interval => interval.Start).ToList();
        var merged = new List<(Beat Start, Beat End)>();
        foreach (var interval in ordered)
        {
            if (merged.Count == 0 || interval.Start >= merged[^1].End)
            {
                merged.Add(interval);
                continue;
            }

            var previous = merged[^1];
            merged[^1] = (
                previous.Start,
                interval.End > previous.End ? interval.End : previous.End
            );
        }

        return merged;
    }

    private static List<KpcEvents.Event<T>> SpliceComposedIntervals<T>(
        List<KpcEvents.Event<T>> configuredEvents,
        IReadOnlyList<List<KpcEvents.Event<T>>> sourceEventLists,
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
                CreateLinearEvent(
                    start,
                    end,
                    evt.GetValueAtBeat(start),
                    evt.GetValueAtBeat(end)
                ),
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
                CreateLinearEvent(
                    beat,
                    next,
                    evt.GetValueAtBeat(beat),
                    evt.GetValueAtBeat(next)
                )
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
            foreach (var evt in events)
            {
                AddBoundaryIfInside(boundaries, evt.StartBeat, start, end);
                AddBoundaryIfInside(boundaries, evt.EndBeat, start, end);
                if (evt.StartBeat >= evt.EndBeat || CanMapDirectly(evt, linearOnly))
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
            var endValue = HasEventStartingAt(sourceEventLists, segmentEnd)
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
                    groupEnd < events.Count
                    && events[groupEnd].StartBeat == events[index].StartBeat
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

    private static bool HasEventStartingAt<T>(
        IReadOnlyList<List<KpcEvents.Event<T>>> sourceEventLists,
        Beat beat
    )
        where T : notnull =>
        sourceEventLists.Any(events => events.Any(evt => evt.StartBeat == beat));

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
            KpcEvents.Event<T>? dominant = null;
            foreach (var evt in events)
            {
                if (evt.StartBeat >= beat)
                    break;
                dominant = evt;
            }

            if (dominant is null)
                continue;
            var value = beat <= dominant.EndBeat
                ? dominant.GetValueAtBeat(beat)
                : dominant.EndValue;
            sum = NumericHelper.Add(sum, value);
        }

        return sum;
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

    private static IEnumerable<KpcEvents.Event<T>> ExpandUnsupportedEvents<T>(
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

    #endregion

    #region Note 转换

    private static Kpc.Note ConvertNoteToKpc(Note src)
    {
        if (
            src.Type == PfNoteType.Hold
            && (!src.HasExplicitHoldEndBeat || src.HoldEndBeat <= src.Beat)
        )
            throw new FormatException("PhiFans Hold 音符缺少有效的结束拍。");

        return new Kpc.Note
        {
            Type = MapPpNoteTypeToKpc(src.Type),
            StartBeat = new Beat((int[])src.Beat),
            PositionX = src.PositionX / 100.0,
            SpeedMultiplier = src.Speed,
            Above = src.IsAbove,
            EndBeat = new Beat(
                (int[])(src.Type == PfNoteType.Hold ? src.HoldEndBeat : src.Beat)
            ),
        };
    }

    private static Note ConvertNoteFromKpc(Kpc.Note src) =>
        new()
        {
            Type = MapKpcNoteTypeToPp(src.Type),
            Beat = new Beat((int[])src.StartBeat),
            PositionX = (float)(src.PositionX * 100.0),
            Speed = src.SpeedMultiplier,
            IsAbove = src.Above,
            HoldEndBeat = new Beat((int[])src.EndBeat),
        };

    /// <summary>
    /// 将 PhiFans NoteType (Tap=1, Drag=2, Hold=3, Flick=4) 映射为 KPC NoteType (Tap=1, Hold=2, Flick=3, Drag=4)。
    /// </summary>
    private static KpcNoteType MapPpNoteTypeToKpc(PfNoteType pfType) =>
        pfType switch
        {
            PfNoteType.Drag => KpcNoteType.Drag,
            PfNoteType.Hold => KpcNoteType.Hold,
            PfNoteType.Flick => KpcNoteType.Flick,
            _ => KpcNoteType.Tap,
        };

    private static PfNoteType MapKpcNoteTypeToPp(KpcNoteType kpcType) =>
        kpcType switch
        {
            KpcNoteType.Drag => PfNoteType.Drag,
            KpcNoteType.Hold => PfNoteType.Hold,
            KpcNoteType.Flick => PfNoteType.Flick,
            _ => PfNoteType.Tap,
        };

    #endregion

    #region 缓动映射

    /// <summary>
    /// 将 PhiFans 缓动编号映射为 KPC 缓动编号。
    /// </summary>
    private static int MapPpEasingToKpc(int pfEasing) =>
        pfEasing switch
        {
            0 => 1,
            1 => 2,
            2 => 3,
            3 => 4,
            4 => 5,
            5 => 6,
            6 => 7,
            7 => 8,
            8 => 9,
            9 => 10,
            10 => 11,
            11 => 12,
            12 => 13,
            13 => 14,
            14 => 15,
            15 => 16,
            16 => 17,
            17 => 18,
            18 => 19,
            19 => 20,
            20 => 21,
            21 => 22,
            22 => 23,
            23 => 24,
            24 => 25,
            25 => 26,
            26 => 27,
            27 => 28,
            28 => 29,
            29 => 30,
            30 => 31,
            _ => 1,
        };

    /// <summary>
    /// 将 KPC 缓动编号映射为 PhiFans 缓动编号。
    /// </summary>
    private static int MapKpcEasingToPp(int kpcEasing) =>
        kpcEasing switch
        {
            1 => 0,
            2 => 1,
            3 => 2,
            4 => 3,
            5 => 4,
            6 => 5,
            7 => 6,
            8 => 7,
            9 => 8,
            10 => 9,
            11 => 10,
            12 => 11,
            13 => 12,
            14 => 13,
            15 => 14,
            16 => 15,
            17 => 16,
            18 => 17,
            19 => 18,
            20 => 19,
            21 => 20,
            22 => 21,
            23 => 22,
            24 => 23,
            25 => 24,
            26 => 25,
            27 => 26,
            28 => 27,
            29 => 28,
            30 => 29,
            31 => 30,
            _ => throw new ArgumentOutOfRangeException(
                nameof(kpcEasing),
                kpcEasing,
                "PhiFans 不支持该缓动编号。"
            ),
        };

    #endregion

    #region 事件转换：PhiFans 增量编码 → KPC 区间编码

    private static List<KpcEvents.Event<double>> ConvertPhifansEventsToDouble(
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
                        Easing = new Kpc.Easing(MapPpEasingToKpc((int)item.Easing)),
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

    private static List<KpcEvents.Event<float>> ConvertPhifansEventsToFloat(
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

    private static List<KpcEvents.Event<int>> ConvertPhifansEventsToInt(
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
                        Easing = new Kpc.Easing(MapPpEasingToKpc((int)item.Easing)),
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

    #endregion

    #region 事件转换：KPC 区间编码 → PhiFans 增量编码

    private static void ConvertKpcEventToPhiFans<T>(
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
    private static void FixDiscontinuityGaps(
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

                if (Math.Abs(prev.Value - curr.Value) <= ValueEpsilon)
                {
                    // 后事件沿用前事件的结束节点，删除重复的非连续起点，避免重置连续链。
                    events.RemoveAt(i);
                }
                else if (exactStepBeats?.Contains(curr.Beat) == true)
                {
                    continue;
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

    #endregion
}
