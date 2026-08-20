using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using KaedePhi.Tool.Event.KaedePhi;
using KaedePhi.Tool.Layer.KaedePhi;
using KpcEventLayer = KaedePhi.Core.KaedePhi.Events.EventLayer;
using PhigrosEvent = KaedePhi.Core.Phigros.v3.Event;
using PhigrosSpeedEvent = KaedePhi.Core.Phigros.v3.SpeedEvent;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

/// <summary>
/// KPC 事件到 PhigrosV3 事件的构建器。
/// </summary>
public class PhigrosV3EventBuilder
{
    private const double AlphaMax = 255d;
    private const float BeatToPhigrosTime = 32f;
    private const float TailEventEndTime = 1_000_000_000f;

    private readonly KpcToPhigrosV3ConvertOptions _options;
    private readonly Action<string>? _warnLogger;
    private readonly EventCutter<double> _eventCutterDouble = new();
    private readonly EventCutter<int> _eventCutterInt = new();
    private readonly EventCutter<float> _eventCutterFloat = new();
    private readonly LayerProcessor _layerProcessor = new();

    public PhigrosV3EventBuilder(KpcToPhigrosV3ConvertOptions options, Action<string>? warnLogger)
    {
        _options = options;
        _warnLogger = warnLogger;
    }

    public void ConvertLineEvents(Core.Phigros.v3.JudgeLine target, List<KpcEventLayer> layers)
    {
        ConvertLineEvents(target, ResolvePrimaryLayer(layers));
    }

    internal KpcEventLayer ResolvePrimaryLayer(List<KpcEventLayer> layers)
    {
        if (layers.Count == 0)
            return new KpcEventLayer();

        KpcEventLayer primaryLayer;
        if (layers.Skip(1).Any(HasAnyEventData))
        {
            if (_options.MultiLayerMerge.ClassicMode)
                primaryLayer = _layerProcessor.LayerMerge(
                    [.. layers],
                    _options.MultiLayerMerge.Precision
                );
            else
                primaryLayer = _layerProcessor.LayerMergePlus(
                    [.. layers],
                    _options.MultiLayerMerge.Precision,
                    _options.MultiLayerMerge.Tolerance
                );
        }
        else
            primaryLayer = layers[0].Clone();

        primaryLayer.Sort();
        return primaryLayer;
    }

    internal void ConvertLineEvents(Core.Phigros.v3.JudgeLine target, KpcEventLayer primaryLayer)
    {
        ConvertMoveEvents(target, primaryLayer);
        ConvertScalarEvents(
            target.JudgeLineRotateEvents,
            primaryLayer.RotateEvents,
            Transform.ToPhigrosV3Angle
        );
        ConvertAlphaEvents(target, primaryLayer.AlphaEvents);
        ConvertSpeedEvents(target, primaryLayer.SpeedEvents);
    }

    #region 移动事件

    private void ConvertMoveEvents(Core.Phigros.v3.JudgeLine target, KpcEventLayer layer)
    {
        var xEvents = layer.MoveXEvents ?? [];
        var yEvents = layer.MoveYEvents ?? [];
        if (xEvents.Count == 0 && yEvents.Count == 0)
            return;

        var cutLength = 1d / _options.Cutting.MisalignedXyEventPrecision;
        var cutX = xEvents
            .SelectMany(e => _eventCutterDouble.CutEventToLinear(e, cutLength))
            .ToList();
        var cutY = yEvents
            .SelectMany(e => _eventCutterDouble.CutEventToLinear(e, cutLength))
            .ToList();

        var allEvents = MergeAndFill(cutX, cutY, 0d);
        foreach (var (start, end, xStart, xEnd, yStart, yEnd) in allEvents)
        {
            target.JudgeLineMoveEvents.Add(
                new PhigrosEvent
                {
                    StartTime = start * BeatToPhigrosTime,
                    EndTime = end * BeatToPhigrosTime,
                    Start = Transform.ToPhigrosV3X(xStart),
                    End = Transform.ToPhigrosV3X(xEnd),
                    Start2 = Transform.ToPhigrosV3Y(yStart),
                    End2 = Transform.ToPhigrosV3Y(yEnd),
                }
            );
        }

        if (target.JudgeLineMoveEvents.Count <= 0)
            return;
        var last = target.JudgeLineMoveEvents[^1];
        target.JudgeLineMoveEvents.Add(
            new PhigrosEvent
            {
                StartTime = last.EndTime,
                EndTime = TailEventEndTime,
                Start = last.End,
                End = last.End,
                Start2 = last.End2,
                End2 = last.End2,
            }
        );
    }

    private static List<(
        float start,
        float end,
        double xStart,
        double xEnd,
        double yStart,
        double yEnd
    )> MergeAndFill(
        List<KpcEvents.Event<double>> xEvents,
        List<KpcEvents.Event<double>> yEvents,
        double defaultValue
    )
    {
        var boundaries = new SortedSet<float> { 0f };
        foreach (var ev in xEvents)
        {
            boundaries.Add((float)(double)ev.StartBeat);
            boundaries.Add((float)(double)ev.EndBeat);
        }
        foreach (var ev in yEvents)
        {
            boundaries.Add((float)(double)ev.StartBeat);
            boundaries.Add((float)(double)ev.EndBeat);
        }

        var result = new List<(float, float, double, double, double, double)>();
        var boundaryList = boundaries.ToList();
        var lastX = defaultValue;
        var lastY = defaultValue;

        for (var i = 0; i < boundaryList.Count - 1; i++)
        {
            var start = boundaryList[i];
            var end = boundaryList[i + 1];
            if (end - start <= Constants.FloatEpsilon)
                continue;

            // 二分查找：找到 StartBeat <= start 的最靠右事件，再验证是否覆盖该区间
            var xEv = BinaryFindEventCovering(xEvents, start, end);
            var yEv = BinaryFindEventCovering(yEvents, start, end);

            var xStart = xEv?.GetValueAtBeat(new Beat(start)) ?? lastX;
            var xEnd = xEv?.GetValueAtBeat(new Beat(end)) ?? lastX;
            var yStart = yEv?.GetValueAtBeat(new Beat(start)) ?? lastY;
            var yEnd = yEv?.GetValueAtBeat(new Beat(end)) ?? lastY;

            result.Add((start, end, xStart, xEnd, yStart, yEnd));

            if (xEv != null)
                lastX = xEnd;
            if (yEv != null)
                lastY = yEnd;
        }

        return result;
    }

    /// <summary>
    /// 在按 StartBeat 升序排列的列表中，二分查找覆盖区间
    /// [<paramref name="start"/>, <paramref name="end"/>] 的事件。
    /// 若不存在则返回 <c>null</c>。
    /// </summary>
    private static KpcEvents.Event<T>? BinaryFindEventCovering<T>(
        List<KpcEvents.Event<T>> sortedEvents,
        float start,
        float end
    )
        where T : notnull
    {
        // 找到 StartBeat <= start + epsilon 的最靠右的候选项
        int lo = 0,
            hi = sortedEvents.Count - 1,
            candidate = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if ((float)(double)sortedEvents[mid].StartBeat <= start + Constants.FloatEpsilon)
            {
                candidate = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }
        if (candidate == -1)
            return null;
        var ev = sortedEvents[candidate];
        return (float)(double)ev.EndBeat >= end - Constants.FloatEpsilon ? ev : null;
    }

    #endregion

    #region 标量事件（旋转）

    private void ConvertScalarEvents(
        List<PhigrosEvent> target,
        List<KpcEvents.Event<double>>? sourceEvents,
        Func<double, float> valueTransform
    )
    {
        if (sourceEvents is not { Count: > 0 })
            return;

        var cutLength = 1d / _options.Cutting.EasingPrecision;
        var cutEvents = sourceEvents
            .SelectMany(e => _eventCutterDouble.CutEventToLinear(e, cutLength))
            .ToList();
        var filled = FillGaps(cutEvents, 0d);

        target.AddRange(
            from ev in filled
            let startBeat = (float)(double)ev.StartBeat
            let endBeat = (float)(double)ev.EndBeat
            where endBeat > startBeat
            select new PhigrosEvent
            {
                StartTime = startBeat * BeatToPhigrosTime,
                EndTime = endBeat * BeatToPhigrosTime,
                Start = valueTransform(ev.StartValue),
                End = valueTransform(ev.EndValue),
            }
        );

        if (target.Count <= 0)
            return;
        var last = target[^1];
        target.Add(
            new PhigrosEvent
            {
                StartTime = last.EndTime,
                EndTime = TailEventEndTime,
                Start = last.End,
                End = last.End,
            }
        );
    }

    #endregion

    #region 不透明度事件

    private void ConvertAlphaEvents(
        Core.Phigros.v3.JudgeLine target,
        List<KpcEvents.Event<int>>? sourceEvents
    )
    {
        if (sourceEvents is not { Count: > 0 })
            return;

        var cutLength = 1d / _options.Alpha.CutPrecision;
        var cutEvents = sourceEvents
            .SelectMany(e => _eventCutterInt.CutEventToLinear(e, cutLength))
            .ToList();
        var filled = FillGaps(cutEvents, 0);
        if (filled.Count > 0 && (double)filled[0].StartBeat > Constants.FloatEpsilon)
        {
            filled.Insert(
                0,
                new KpcEvents.Event<int>
                {
                    StartBeat = new Beat(0d),
                    EndBeat = filled[0].StartBeat,
                    StartValue = 0,
                    EndValue = 0,
                }
            );
        }

        foreach (var ev in filled)
        {
            var startBeat = (float)(double)ev.StartBeat;
            var endBeat = (float)(double)ev.EndBeat;
            if (endBeat <= startBeat)
                continue;

            target.JudgeLineDisappearEvents.Add(
                new PhigrosEvent
                {
                    StartTime = startBeat * BeatToPhigrosTime,
                    EndTime = endBeat * BeatToPhigrosTime,
                    Start = ClampAlpha(ev.StartValue),
                    End = ClampAlpha(ev.EndValue),
                }
            );
        }

        if (target.JudgeLineDisappearEvents.Count <= 0)
            return;
        var last = target.JudgeLineDisappearEvents[^1];
        target.JudgeLineDisappearEvents.Add(
            new PhigrosEvent
            {
                StartTime = last.EndTime,
                EndTime = TailEventEndTime,
                Start = last.End,
                End = last.End,
            }
        );
    }

    private static float ClampAlpha(int alpha) => (float)Math.Clamp(alpha / AlphaMax, 0d, 1d);

    #endregion

    #region 速度事件

    private void ConvertSpeedEvents(
        Core.Phigros.v3.JudgeLine target,
        List<KpcEvents.Event<float>>? sourceEvents
    )
    {
        if (sourceEvents is not { Count: > 0 })
            return;

        var cutLength = 1d / _options.Speed.CutPrecision;
        var cutEvents = sourceEvents
            .SelectMany(e => _eventCutterFloat.CutEventToLinear(e, cutLength))
            .ToList();
        var filled = FillGaps(cutEvents, 1f);
        var hasConvertedEvent = false;
        var tailValue = 0f;

        foreach (var ev in filled)
        {
            var startBeat = (float)(double)ev.StartBeat;
            var endBeat = (float)(double)ev.EndBeat;
            if (endBeat <= startBeat)
                continue;

            target.SpeedEvents.Add(
                new PhigrosSpeedEvent
                {
                    StartTime = startBeat * BeatToPhigrosTime,
                    EndTime = endBeat * BeatToPhigrosTime,
                    Value = ev.StartValue / (float)Constants.SpeedValueRatio,
                }
            );
            hasConvertedEvent = true;
            tailValue = ev.EndValue / (float)Constants.SpeedValueRatio;
        }

        if (!hasConvertedEvent)
            return;
        var last = target.SpeedEvents[^1];
        target.SpeedEvents.Add(
            new PhigrosSpeedEvent
            {
                StartTime = last.EndTime,
                EndTime = TailEventEndTime,
                Value = tailValue,
            }
        );
    }

    #endregion

    #region 辅助方法

    private static List<KpcEvents.Event<T>> FillGaps<T>(
        List<KpcEvents.Event<T>> events,
        T defaultValue
    )
        where T : notnull
    {
        if (events.Count == 0)
            return events;

        // CutEventToLinear 的输出已按拍数有序；仅在必要时排序（O(n log n) 保底）。
        var sorted = IsSortedByStartBeat(events)
            ? events
            : [.. events.OrderBy(e => (double)e.StartBeat)];

        var result = new List<KpcEvents.Event<T>>(sorted.Count * 2);
        var lastEndValue = defaultValue;
        var lastEndBeat = 0f;

        foreach (var ev in sorted)
        {
            var startBeat = (float)(double)ev.StartBeat;
            var endBeat = (float)(double)ev.EndBeat;

            if (startBeat > lastEndBeat + Constants.FloatEpsilon && result.Count > 0)
            {
                result.Add(
                    new KpcEvents.Event<T>
                    {
                        StartBeat = new Beat(lastEndBeat),
                        EndBeat = new Beat(startBeat),
                        StartValue = lastEndValue,
                        EndValue = lastEndValue,
                    }
                );
            }

            result.Add(ev);
            lastEndValue = ev.EndValue;
            lastEndBeat = endBeat;
        }

        return result;
    }

    /// <summary>
    /// O(n) 检查——若 <paramref name="events"/> 已按 <c>StartBeat</c> 升序排列则返回
    /// <c>true</c>，避免在常规路径下执行 O(n log n) 的 <c>OrderBy</c>。
    /// </summary>
    private static bool IsSortedByStartBeat<T>(List<KpcEvents.Event<T>> events)
        where T : notnull
    {
        for (var i = 1; i < events.Count; i++)
        {
            if (events[i].StartBeat < (double)events[i - 1].StartBeat - Constants.FloatEpsilon)
                return false;
        }
        return true;
    }

    private static bool HasAnyEventData(KpcEventLayer layer) =>
        (layer.MoveXEvents?.Count ?? 0) > 0
        || (layer.MoveYEvents?.Count ?? 0) > 0
        || (layer.RotateEvents?.Count ?? 0) > 0
        || (layer.AlphaEvents?.Count ?? 0) > 0
        || (layer.SpeedEvents?.Count ?? 0) > 0;

    private void Warn(string message) => _warnLogger?.Invoke(message);

    #endregion
}
