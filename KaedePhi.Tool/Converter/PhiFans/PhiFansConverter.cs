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
        var layers = sourceLayers.ConvertAll(layer => layer.Clone());
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
        RestoreUnsupportedComposition(
            layer,
            sourceLayers,
            layerProcessor,
            options.Cutting.UnsupportedEasingPrecision,
            cutLength
        );
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
        FixDiscontinuityGaps(line.Props.Alpha, paddingPrecision);
        FixDiscontinuityGaps(line.Props.PositionX, paddingPrecision);
        FixDiscontinuityGaps(line.Props.PositionY, paddingPrecision);
        FixDiscontinuityGaps(line.Props.Rotate, paddingPrecision);
        FixDiscontinuityGaps(line.Props.Speed, paddingPrecision);

        return line;
    }

    private static void RestoreUnsupportedComposition(
        KpcEvents.EventLayer mergedLayer,
        List<KpcEvents.EventLayer> sourceLayers,
        LayerProcessor layerProcessor,
        double precision,
        double cutLength
    )
    {
        var restoreAlpha = HasUnsupportedOverlap(sourceLayers, layer => layer.AlphaEvents, false);
        var restoreMoveX = HasUnsupportedOverlap(sourceLayers, layer => layer.MoveXEvents, false);
        var restoreMoveY = HasUnsupportedOverlap(sourceLayers, layer => layer.MoveYEvents, false);
        var restoreRotate = HasUnsupportedOverlap(sourceLayers, layer => layer.RotateEvents, false);
        var restoreSpeed = HasUnsupportedOverlap(sourceLayers, layer => layer.SpeedEvents, true);
        if (!restoreAlpha && !restoreMoveX && !restoreMoveY && !restoreRotate && !restoreSpeed)
            return;

        var fallbackLayers = sourceLayers.ConvertAll(layer => layer.Clone());
        foreach (var fallbackLayer in fallbackLayers)
        {
            fallbackLayer.AlphaEvents = PrepareEventsForMerge(
                fallbackLayer.AlphaEvents,
                cutLength,
                false
            );
            fallbackLayer.MoveXEvents = PrepareEventsForMerge(
                fallbackLayer.MoveXEvents,
                cutLength,
                false
            );
            fallbackLayer.MoveYEvents = PrepareEventsForMerge(
                fallbackLayer.MoveYEvents,
                cutLength,
                false
            );
            fallbackLayer.RotateEvents = PrepareEventsForMerge(
                fallbackLayer.RotateEvents,
                cutLength,
                false
            );
            fallbackLayer.SpeedEvents = PrepareEventsForMerge(
                fallbackLayer.SpeedEvents,
                cutLength,
                true
            );
        }

        var fallback = layerProcessor.LayerMerge(fallbackLayers, precision);
        if (restoreAlpha)
            mergedLayer.AlphaEvents = fallback.AlphaEvents;
        if (restoreMoveX)
            mergedLayer.MoveXEvents = fallback.MoveXEvents;
        if (restoreMoveY)
            mergedLayer.MoveYEvents = fallback.MoveYEvents;
        if (restoreRotate)
            mergedLayer.RotateEvents = fallback.RotateEvents;
        if (restoreSpeed)
            mergedLayer.SpeedEvents = fallback.SpeedEvents;
    }

    private static bool HasUnsupportedOverlap<T>(
        IReadOnlyList<KpcEvents.EventLayer> layers,
        Func<KpcEvents.EventLayer, List<KpcEvents.Event<T>>?> selectEvents,
        bool linearOnly
    )
        where T : notnull
    {
        for (var layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
            var events = selectEvents(layers[layerIndex]);
            if (events is null)
                continue;

            foreach (var evt in events)
            {
                if (CanMapDirectly(evt, linearOnly))
                    continue;

                for (var otherLayerIndex = 0; otherLayerIndex < layers.Count; otherLayerIndex++)
                {
                    if (otherLayerIndex == layerIndex)
                        continue;
                    var otherEvents = selectEvents(layers[otherLayerIndex]);
                    if (
                        otherEvents?.Any(other =>
                            evt.StartBeat < other.EndBeat && other.StartBeat < evt.EndBeat
                        ) == true
                    )
                        return true;
                }
            }
        }

        return false;
    }

    private static List<KpcEvents.Event<T>>? PrepareEventsForMerge<T>(
        List<KpcEvents.Event<T>>? events,
        double cutLength,
        bool linearOnly
    )
        where T : notnull
    {
        return events is null
            ? null
            : ExpandUnsupportedEvents(events, cutLength, linearOnly)
                .Select(evt => evt.Clone())
                .ToList();
    }

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
    private static void FixDiscontinuityGaps(List<PfEvent> events, int paddingPrecision)
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
