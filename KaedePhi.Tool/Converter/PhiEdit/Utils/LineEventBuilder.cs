using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Event.KaedePhi;
using KaedePhi.Tool.Layer.KaedePhi;
using KpcEasing = KaedePhi.Core.KaedePhi.Easing;
using KpcEventLayer = KaedePhi.Core.KaedePhi.Events.EventLayer;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

/// <summary>
/// KPC 事件到 PE Frame/Event 的构建器。
/// </summary>
public class LineEventBuilder
{
    private readonly KpcToPhiEditConvertOptions _options;
    private readonly Action<string>? _warnLogger;
    private readonly EventCutter<int> _eventCutterInt;
    private readonly EventCompressor<int> _eventCompressorInt;
    private readonly EventCutter<float> _eventCutterFloat;
    private readonly EventCutter<double> _eventCutterDouble;
    private readonly LayerProcessor _layerProcessor = new();
    private readonly Dictionary<Type, object> _eventCutters = new();

    public LineEventBuilder(KpcToPhiEditConvertOptions options, Action<string>? warnLogger = null)
    {
        _options = options;
        _warnLogger = warnLogger;
        _eventCutterInt = new EventCutter<int>();
        _eventCompressorInt = new EventCompressor<int>();
        _eventCutterFloat = new EventCutter<float>();
        _eventCutterDouble = new EventCutter<double>();
    }

    /// <summary>
    /// 将 KPC 事件层映射为 PE 的线事件结构。
    /// </summary>
    public void ConvertLineEvents(Pe.JudgeLine target, List<KpcEventLayer> layers)
    {
        if (layers.Count == 0)
            return;

        var primaryLayer = layers[0];
        for (var i = 1; i < layers.Count; i++)
        {
            if (!HasAnyEventData(layers[i]))
                continue;
            Warn("JudgeLine 存在多个事件层；PE 仅支持单层，将自动合并为一层");
            if (_options.MultiLayerMerge.ClassicMode)
            {
                if (_options.MultiLayerMerge.Compress)
                {
                    var layer = _layerProcessor.LayerMerge(
                        layers,
                        _options.MultiLayerMerge.Precision
                    );
                    _layerProcessor.LayerEventsCompress(layer, _options.MultiLayerMerge.Tolerance);
                    primaryLayer = layer;
                }
                else
                    primaryLayer = _layerProcessor.LayerMerge(
                        layers,
                        _options.MultiLayerMerge.Precision
                    );
            }
            else
                primaryLayer = _layerProcessor.LayerMergePlus(
                    layers,
                    _options.MultiLayerMerge.Precision,
                    _options.MultiLayerMerge.Tolerance
                );

            break;
        }

        ConvertMoveEvents(target, primaryLayer);
        ConvertScalarEvents(
            target.RotateFrames,
            target.RotateEvents,
            primaryLayer.RotateEvents,
            value => Transform.TransformToPeAngle(value),
            "旋转"
        );
        ConvertAlphaEvents(target, primaryLayer.AlphaEvents);
        ConvertSpeedFrames(target, primaryLayer.SpeedEvents);
    }

    /// <summary>
    /// 转换 Alpha 事件：PE 的 cf 不支持缓动，需先按单事件切段并压缩，再写入线性事件。
    /// </summary>
    public void ConvertAlphaEvents(Pe.JudgeLine target, List<KpcEvents.Event<int>>? sourceEvents)
    {
        if (sourceEvents == null || sourceEvents.Count == 0)
            return;

        WarnIfEventPayloadUnsupported(sourceEvents, "不透明度");

        var ordered = sourceEvents
            .OrderBy(e => (double)e.StartBeat)
            .SelectMany(srcEvent =>
            {
                var sliced = _eventCutterInt.CutEventToLinear(
                    srcEvent,
                    1d / _options.Alpha.CutPrecision
                );
                return _options.Alpha.CutCompress
                    ? _eventCompressorInt.EventListCompressSlope(
                        sliced,
                        _options.Alpha.CutTolerance
                    )
                    : sliced;
            })
            .ToList();

        var previousEndBeat = float.MinValue;
        var previousEndValue = float.NaN;

        foreach (var ev in ordered)
        {
            var startBeat = (float)(double)ev.StartBeat;
            var endBeat = (float)(double)ev.EndBeat;
            var startValue = ToSingle(ev.StartValue);
            var endValue = ToSingle(ev.EndValue);

            var disconnected = Math.Abs(previousEndBeat - startBeat) > Constants.FloatEpsilon;
            var changed =
                float.IsNaN(previousEndValue)
                || Math.Abs(previousEndValue - startValue) > Constants.FloatEpsilon;
            if (disconnected || changed)
            {
                target.AlphaFrames.Add(new Pe.Frame { Beat = startBeat, Value = startValue });
            }

            target.AlphaEvents.Add(
                new Pe.Event
                {
                    StartBeat = startBeat,
                    EndBeat = endBeat,
                    EasingType = new Pe.Easing(1),
                    EndValue = endValue,
                }
            );

            previousEndBeat = endBeat;
            previousEndValue = endValue;
        }
    }

    /// <summary>
    /// 转换 Speed 事件：PE 无速度事件，仅导出帧。
    /// </summary>
    public void ConvertSpeedFrames(Pe.JudgeLine target, List<KpcEvents.Event<float>>? sourceEvents)
    {
        if (sourceEvents == null || sourceEvents.Count == 0)
            return;

        WarnIfEventPayloadUnsupported(sourceEvents, "速度");

        foreach (var srcEvent in sourceEvents.OrderBy(e => (double)e.StartBeat))
        {
            var slices = _eventCutterFloat.CutEventToLinear(
                srcEvent,
                1d / _options.Speed.CutPrecision
            );
            for (var i = 0; i < slices.Count; i++)
            {
                var slice = slices[i];
                var useStartValue = i < 2;
                var beat = useStartValue
                    ? (float)(double)slice.StartBeat
                    : (float)(double)slice.EndBeat;
                var value =
                    Transform.TransformToPeSpeed(
                        useStartValue ? ToSingle(slice.StartValue) : ToSingle(slice.EndValue)
                    );

                target.SpeedFrames.Add(new Pe.Frame { Beat = beat, Value = value });
            }
        }
    }

    /// <summary>
    /// 转换 MoveX/MoveY 事件为 PE MoveFrame 与 MoveEvent。
    /// </summary>
    public void ConvertMoveEvents(Pe.JudgeLine target, KpcEventLayer layer)
    {
        var xEvents = ExpandEventsForUnsupportedEasing(layer.MoveXEvents ?? [], "移动X");
        var yEvents = ExpandEventsForUnsupportedEasing(layer.MoveYEvents ?? [], "移动Y");
        if (xEvents.Count == 0 && yEvents.Count == 0)
            return;

        WarnIfEventPayloadUnsupported(xEvents, "移动X");
        WarnIfEventPayloadUnsupported(yEvents, "移动Y");

        var boundaries = CollectBoundaries(xEvents, yEvents);
        if (boundaries.Count < 2)
            return;

        var lastX = 0d;
        var lastY = 0d;

        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var start = boundaries[i];
            var end = boundaries[i + 1];
            if (end - start <= Constants.FloatEpsilon)
                continue;
            ProcessMoveInterval(target, xEvents, yEvents, start, end, ref lastX, ref lastY);
        }
    }

    /// <summary>
    /// 转换 double 标量事件通道（如 Rotate）。
    /// </summary>
    public void ConvertScalarEvents(
        List<Pe.Frame> targetFrames,
        List<Pe.Event>? targetEvents,
        List<KpcEvents.Event<double>>? sourceEvents,
        Func<float, float> valueTransform,
        string channelName
    )
    {
        ConvertScalarEventsInternal(
            targetFrames,
            targetEvents,
            sourceEvents,
            valueTransform,
            channelName
        );
    }

    #region Move Event Helpers

    private void ProcessMoveInterval(
        Pe.JudgeLine target,
        List<KpcEvents.Event<double>> xEvents,
        List<KpcEvents.Event<double>> yEvents,
        float start,
        float end,
        ref double lastX,
        ref double lastY
    )
    {
        var activeX = FindActiveEvent(xEvents, new Beat(start));
        var activeY = FindActiveEvent(yEvents, new Beat(start));

        if (activeX == null && activeY == null)
            return;

        // 只有两轴都精确覆盖当前区间且缓动相同时，才能直接映射
        var xAligned = IsExactlyCovering(activeX, start, end);
        var yAligned = IsExactlyCovering(activeY, start, end);
        var canDirectMap =
            xAligned
            && yAligned
            && activeX != null
            && activeY != null
            && (int)activeX.Easing == (int)activeY.Easing;

        if (canDirectMap)
        {
            EmitAlignedMoveSegment(target, start, end, activeX, activeY, ref lastX, ref lastY);
        }
        else
        {
            if (!xAligned || !yAligned)
                WarnMoveSegmentMisalignment(activeX, activeY, xAligned, yAligned, start, end);
            EmitCutMoveSegments(target, xEvents, yEvents, start, end, ref lastX, ref lastY);
        }
    }

    private void EmitAlignedMoveSegment(
        Pe.JudgeLine target,
        float start,
        float end,
        KpcEvents.Event<double>? activeX,
        KpcEvents.Event<double>? activeY,
        ref double lastX,
        ref double lastY
    )
    {
        // 两轴都精确覆盖且缓动相同，直接取 StartValue/EndValue
        var startXv =
            activeX?.StartValue ?? throw new InvalidOperationException("activeX 不应为 null");
        var endXv = activeX.EndValue;
        var startYv =
            activeY?.StartValue ?? throw new InvalidOperationException("activeY 不应为 null");
        var endYv = activeY.EndValue;
        var easing = SafeConvertEasingToInt(activeX.Easing, $"移动@{start:F3}");

        target.MoveFrames.Add(
            new Pe.MoveFrame
            {
                Beat = start,
                XValue = Transform.TransformToPeX(startXv),
                YValue = Transform.TransformToPeY(startYv),
            }
        );
        target.MoveEvents.Add(
            new Pe.MoveEvent
            {
                StartBeat = start,
                EndBeat = end,
                EasingType = easing,
                EndXValue = Transform.TransformToPeX(endXv),
                EndYValue = Transform.TransformToPeY(endYv),
            }
        );
        lastX = endXv;
        lastY = endYv;
    }

    private void WarnMoveSegmentMisalignment(
        KpcEvents.Event<double>? activeX,
        KpcEvents.Event<double>? activeY,
        bool xAligned,
        bool yAligned,
        float start,
        float end
    )
    {
        switch (xAligned)
        {
            case false when !yAligned:
                Warn($"Move 区间 [{start:F3}, {end:F3}] X/Y 事件均未完整覆盖此区间，将切段线性化");
                break;
            case false:
                Warn($"Move 区间 [{start:F3}, {end:F3}] X 事件跨越 Y 边界（未对齐），将切段线性化");
                break;
            default:
            {
                if (!yAligned)
                    Warn(
                        $"Move 区间 [{start:F3}, {end:F3}] Y 事件跨越 X 边界（未对齐），将切段线性化"
                    );
                else
                {
                    var xEasingNum = activeX != null ? (int)activeX.Easing : 0;
                    var yEasingNum = activeY != null ? (int)activeY.Easing : 0;
                    Warn(
                        $"Move 区间 [{start:F3}, {end:F3}] X/Y 缓动类型不一致（X={xEasingNum}, Y={yEasingNum}），将切段线性化"
                    );
                }

                break;
            }
        }
    }

    private static bool IsExactlyCovering(KpcEvents.Event<double>? ev, float start, float end) =>
        ev != null
        && Math.Abs((double)ev.StartBeat - start) <= Constants.FloatEpsilon
        && Math.Abs((double)ev.EndBeat - end) <= Constants.FloatEpsilon;

    private void EmitCutMoveSegments(
        Pe.JudgeLine target,
        List<KpcEvents.Event<double>> xEvents,
        List<KpcEvents.Event<double>> yEvents,
        float start,
        float end,
        ref double lastX,
        ref double lastY
    )
    {
        var startBeat = new Beat(start);
        var endBeat = new Beat(end);
        var cutLength = 1d / _options.Cutting.MisalignedXyEventPrecision;

        var cutX = _eventCutterDouble.CutEventsInRange(xEvents, startBeat, endBeat, cutLength);
        var cutY = _eventCutterDouble.CutEventsInRange(yEvents, startBeat, endBeat, cutLength);

        var subBoundaries = CollectBoundaries(cutX, cutY);
        subBoundaries.Add(start);
        subBoundaries.Add(end);
        subBoundaries = subBoundaries.Distinct().OrderBy(v => v).ToList();

        for (var i = 0; i < subBoundaries.Count - 1; i++)
        {
            var segStart = subBoundaries[i];
            var segEnd = subBoundaries[i + 1];
            if (segEnd - segStart <= Constants.FloatEpsilon)
                continue;

            // CutEventsInRange 对每个原始事件独立按 cutLength 切割，cutX 与 cutY 的段边界不一定对齐。
            // CollectBoundaries(cutX, cutY) 会产生比任一列表更细的子区间，FindSegment 精确匹配会失败。
            // 改用 FindActiveEvent 在 segStart 处查找活跃段，再插值取区间端点值。
            var xSeg = FindActiveEvent(cutX, new Beat(segStart));
            var ySeg = FindActiveEvent(cutY, new Beat(segStart));

            var startXv = xSeg?.GetValueAtBeat(new Beat(segStart)) ?? lastX;
            var endXv = xSeg?.GetValueAtBeat(new Beat(segEnd)) ?? lastX;
            var startYv = ySeg?.GetValueAtBeat(new Beat(segStart)) ?? lastY;
            var endYv = ySeg?.GetValueAtBeat(new Beat(segEnd)) ?? lastY;

            target.MoveFrames.Add(
                new Pe.MoveFrame
                {
                    Beat = segStart,
                    XValue = Transform.TransformToPeX(startXv),
                    YValue = Transform.TransformToPeY(startYv),
                }
            );
            target.MoveEvents.Add(
                new Pe.MoveEvent
                {
                    StartBeat = segStart,
                    EndBeat = segEnd,
                    EasingType = 1,
                    EndXValue = Transform.TransformToPeX(endXv),
                    EndYValue = Transform.TransformToPeY(endYv),
                }
            );

            if (xSeg != null)
                lastX = endXv;
            if (ySeg != null)
                lastY = endYv;
        }
    }

    #endregion

    #region Scalar Event Helpers

    private void ConvertScalarEventsInternal<T>(
        List<Pe.Frame> targetFrames,
        List<Pe.Event>? targetEvents,
        List<KpcEvents.Event<T>>? sourceEvents,
        Func<float, float> valueTransform,
        string channelName
    )
        where T : notnull
    {
        if (sourceEvents == null || sourceEvents.Count == 0)
            return;
        WarnIfEventPayloadUnsupported(sourceEvents, channelName);

        var ordered = sourceEvents
            .OrderBy(e => (double)e.StartBeat)
            .SelectMany(srcEvent =>
                ExpandUnsupportedEasing(srcEvent, $"{channelName}@{(double)srcEvent.StartBeat:F3}")
            )
            .ToList();

        var previousEndBeat = float.MinValue;
        var previousEndValue = float.NaN;

        foreach (var ev in ordered)
        {
            var startBeat = (float)(double)ev.StartBeat;
            var endBeat = (float)(double)ev.EndBeat;
            var startValue = valueTransform(ToSingle(ev.StartValue));
            var endValue = valueTransform(ToSingle(ev.EndValue));

            var disconnected = Math.Abs(previousEndBeat - startBeat) > Constants.FloatEpsilon;
            var changed =
                float.IsNaN(previousEndValue)
                || Math.Abs(previousEndValue - startValue) > Constants.FloatEpsilon;
            if (disconnected || changed)
            {
                targetFrames.Add(new Pe.Frame { Beat = startBeat, Value = startValue });
            }

            // 头尾值相同时，只需一个起始 Frame 即可表示，无需生成 Event
            if (Math.Abs(startValue - endValue) <= Constants.FloatEpsilon)
            {
                // 已在上方添加过起始 Frame，此处无需额外操作
            }
            else if (targetEvents != null)
            {
                targetEvents.Add(
                    new Pe.Event
                    {
                        StartBeat = startBeat,
                        EndBeat = endBeat,
                        EasingType = EasingConverter.ConvertEasing(ev.Easing),
                        EndValue = endValue,
                    }
                );
            }
            else
            {
                targetFrames.Add(new Pe.Frame { Beat = endBeat, Value = endValue });
            }

            previousEndBeat = endBeat;
            previousEndValue = endValue;
        }
    }

    private List<KpcEvents.Event<double>> ExpandEventsForUnsupportedEasing(
        List<KpcEvents.Event<double>> source,
        string channel
    )
    {
        var expanded = new List<KpcEvents.Event<double>>();
        foreach (var ev in source.OrderBy(e => (double)e.StartBeat))
        {
            expanded.AddRange(ExpandUnsupportedEasing(ev, $"{channel}@{(double)ev.StartBeat:F3}"));
        }

        return expanded;
    }

    private List<KpcEvents.Event<T>> ExpandUnsupportedEasing<T>(
        KpcEvents.Event<T> src,
        string context
    )
        where T : notnull
    {
        try
        {
            if (src.IsBezier)
                throw new EasingConverter.EasingNotSupportedException(-1, isBezier: true);
            _ = EasingConverter.ConvertEasing(src.Easing);
        }
        catch (EasingConverter.EasingNotSupportedException)
        {
            Warn(
                $"{context}：检测到不支持的缓动，将切分为 {(src.EndBeat - src.StartBeat) / _options.Cutting.UnsupportedEasingPrecision} 段线性事件"
            );
            var cutter = GetOrCreateCutter<T>();
            return cutter.CutEventToLinear(src, 1d / _options.Cutting.UnsupportedEasingPrecision);
        }

        // PE 不支持 EasingLeft/EasingRight 截取，需要切割成线性事件
        if (
            Math.Abs(src.EasingLeft) > Constants.FloatEpsilon
            || Math.Abs(src.EasingRight - 1f) > Constants.FloatEpsilon
        )
        {
            Warn(
                $"{context}：PE 不支持 EasingLeft/EasingRight 截取，将切分为 {(src.EndBeat - src.StartBeat) / _options.Cutting.UnsupportedEasingPrecision} 段线性事件"
            );
            var cutter = GetOrCreateCutter<T>();
            return cutter.CutEventToLinear(src, 1d / _options.Cutting.UnsupportedEasingPrecision);
        }

        return [src];
    }

    private EventCutter<T> GetOrCreateCutter<T>()
        where T : notnull
    {
        var type = typeof(T);
        if (!_eventCutters.TryGetValue(type, out var cutter))
        {
            cutter = new EventCutter<T>();
            _eventCutters[type] = cutter;
        }

        return (EventCutter<T>)cutter;
    }

    #endregion

    #region Common Helpers

    private static List<float> CollectBoundaries(params List<KpcEvents.Event<double>>[] eventLists)
    {
        var boundaries = new SortedSet<float>();
        foreach (var list in eventLists)
        {
            foreach (var ev in list)
            {
                boundaries.Add((float)(double)ev.StartBeat);
                boundaries.Add((float)(double)ev.EndBeat);
            }
        }

        return boundaries.ToList();
    }

    private static KpcEvents.Event<double>? FindActiveEvent(
        List<KpcEvents.Event<double>> events,
        Beat beat
    )
    {
        var beatValue = (double)beat;
        // 二分查找：找到最后一个 StartBeat <= beatValue + Constants.FloatEpsilon 的事件
        var lo = 0;
        var hi = events.Count - 1;
        var candidate = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >>> 1;
            if (events[mid].StartBeat <= beatValue + Constants.FloatEpsilon)
            {
                candidate = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }
        if (candidate < 0)
            return null;
        var ev = events[candidate];
        return beatValue < (double)ev.EndBeat - Constants.FloatEpsilon ? ev : null;
    }

    private int SafeConvertEasingToInt(KpcEasing easing, string context)
    {
        try
        {
            return (int)EasingConverter.ConvertEasing(easing);
        }
        catch (EasingConverter.EasingNotSupportedException)
        {
            Warn($"{context}：展开后仍存在不支持的缓动，回退为线性");
            return 1;
        }
    }

    private static float ToSingle<T>(T value) =>
        value switch
        {
            float v => v,
            double v => (float)v,
            int v => v,
            _ => throw new NotSupportedException(
                $"Unsupported scalar event value type: {typeof(T).Name}"
            ),
        };

    private static bool HasAnyEventData(KpcEventLayer layer) =>
        (layer.MoveXEvents?.Count ?? 0) > 0
        || (layer.MoveYEvents?.Count ?? 0) > 0
        || (layer.RotateEvents?.Count ?? 0) > 0
        || (layer.AlphaEvents?.Count ?? 0) > 0
        || (layer.SpeedEvents?.Count ?? 0) > 0;

    private void WarnIfEventPayloadUnsupported<T>(
        IEnumerable<KpcEvents.Event<T>> events,
        string channel
    )
        where T : notnull
    {
        foreach (var e in events)
        {
            if (e.IsBezier)
                Warn($"{channel}：Bezier 事件不受 PE 原生事件模型支持，事件将被自动转换为线性事件");
            if (
                Math.Abs(e.EasingLeft) > Constants.FloatEpsilon
                || Math.Abs(e.EasingRight - 1f) > Constants.FloatEpsilon
            )
                Warn(
                    $"{channel}：PE 不支持 EasingLeft/EasingRight 裁剪，事件将被自动转换为线性事件"
                );
            if (!string.IsNullOrWhiteSpace(e.Font))
                Warn($"{channel}：PE 不支持 Font 字段，字段将被丢弃");
        }
    }

    private void Warn(string message) => _warnLogger?.Invoke(message);

    #endregion
}
