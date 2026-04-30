using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.KaedePhi.Events;
using BpmItem = KaedePhi.Core.KaedePhi.BpmItem;
using Chart = KaedePhi.Core.KaedePhi.Chart;
using Easing = KaedePhi.Core.KaedePhi.Easing;
using EventLayer = KaedePhi.Core.KaedePhi.EventLayer;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;
using Meta = KaedePhi.Core.KaedePhi.Meta;
using Note = KaedePhi.Core.KaedePhi.Note;
using NoteType = KaedePhi.Core.KaedePhi.NoteType;

namespace KaedePhi.Tool.PhiEdit.Converters;

/// <summary>
/// PhiEdit 格式 → KPC 格式转换器。
/// </summary>
[Obsolete("请改用 KaedePhi.Tool.Converter.PhiEdit.PhiEditConverter.ToKpc()")]
public class PeToKpc
{
    /// <summary>
    /// 在末尾额外补齐的拍点长度，避免最后一个关键帧/事件在 Kpc 中没有后续区间可承载。
    /// </summary>
    private const double TrailingBeatPadding = 1d / 64d;

    /// <summary>
    /// 非事件起点 Frame 在 Kpc 中保留的最小可编辑区间长度（拍）。
    /// </summary>
    private const double FrameEditableSliceBeat = 0.0125d;

    /// <summary>
    /// 拍点比较容差。
    /// </summary>
    private const double BeatComparisonEpsilon = 1e-6d;

    /// <summary>
    /// PhiEdit 坐标系配置，用于将 PE 坐标统一映射到 Kpc 归一化坐标系。
    /// </summary>
    private static readonly CoordinateProfile PeCoordinateProfile = new(
        Pe.Chart.CoordinateSystem.MinX,
        Pe.Chart.CoordinateSystem.MaxX,
        Pe.Chart.CoordinateSystem.MinY,
        Pe.Chart.CoordinateSystem.MaxY,
        Pe.Chart.CoordinateSystem.ClockwiseRotation);

    /// <summary>
    /// 偏移的偏移常数。
    /// </summary>
    private const int OffsetOffset = 175;

    public Chart Import(Pe.Chart source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new Chart
        {
            BpmList = source.BpmList?.ConvertAll(ConvertBpmItem) ?? [],
            Meta = ConvertMeta(source),
            JudgeLineList = ConvertJudgeLines(source.JudgeLineList)
        };
    }

    /// <summary>
    /// 将 PhiEdit 缓动编号映射为 Kpc 缓动编号。
    /// </summary>
    /// <param name="pe">PhiEdit 缓动编号。</param>
    /// <returns>对应的 Kpc 缓动编号；未知编号时回退为线性。</returns>
    private static int MapEasingNumber(int pe) => pe switch
    {
        1 => 1, 2 => 3, 3 => 2, 4 => 6, 5 => 5, 6 => 4, 7 => 7,
        8 => 9, 9 => 8, 10 => 12, 11 => 11, 12 => 10, 13 => 13,
        14 => 15, 15 => 14, 16 => 18, 17 => 17, 18 => 21, 19 => 20,
        20 => 24, 21 => 23, 22 => 22, 23 => 25, 24 => 27, 25 => 26,
        26 => 30, 27 => 29, 28 => 31, 29 => 28, _ => 1
    };

    /// <summary>将 PhiEdit 缓动对象转换为 Kpc 缓动对象。</summary>
    private static Easing ConvertEasing(Pe.Easing src) => new(MapEasingNumber((int)src));

    /// <summary>将 PhiEdit X 坐标转换为 Kpc X 坐标。</summary>
    private static double TransformX(float x) => CoordinateGeometry.ToKpcX(x, PeCoordinateProfile);

    /// <summary>将 PhiEdit Y 坐标转换为 Kpc Y 坐标。</summary>
    private static double TransformY(float y) => CoordinateGeometry.ToKpcY(y, PeCoordinateProfile);

    /// <summary>将 PhiEdit 角度转换为 Kpc 角度方向。</summary>
    private static double TransformAngle(float angle) => CoordinateGeometry.ToKpcAngle(angle, PeCoordinateProfile);

    /// <summary>
    /// 转换单个 BPM 点。
    /// </summary>
    /// <param name="src">PhiEdit BPM 点。</param>
    /// <returns>Kpc BPM 点。</returns>
    private static BpmItem ConvertBpmItem(Pe.BpmItem src) => new()
    {
        Bpm = src.Bpm,
        StartBeat = new Beat(src.StartBeat)
    };

    /// <summary>
    /// 生成 Kpc 元数据。PhiEdit 缺失的大部分元信息保持 Kpc 默认值，仅覆盖可直接映射项。
    /// </summary>
    /// <param name="src">PhiEdit 谱面。</param>
    /// <returns>Kpc 元数据。</returns>
    private static Meta ConvertMeta(Pe.Chart src) => new()
    {
        Offset = src.Offset - OffsetOffset // WTF
    };

    /// <summary>
    /// 转换全部判定线。
    /// </summary>
    /// <param name="judgeLines">PhiEdit 判定线列表。</param>
    /// <returns>转换后的 Kpc 判定线列表；输入为空时返回空列表。</returns>
    private static List<JudgeLine> ConvertJudgeLines(List<Pe.JudgeLine>? judgeLines)
    {
        if (judgeLines == null || judgeLines.Count == 0) return [];

        var result = new List<JudgeLine>(judgeLines.Count);
        result.AddRange(judgeLines.Select(ConvertJudgeLine));
        return result;
    }

    /// <summary>
    /// 转换单条判定线，并合成为单事件层的 Kpc 判定线。
    /// </summary>
    /// <param name="src">PhiEdit 判定线。</param>
    /// <param name="index">判定线索引，用于生成默认名称。</param>
    /// <returns>转换后的 Kpc 判定线。</returns>
    private static JudgeLine ConvertJudgeLine(Pe.JudgeLine src, int index)
    {
        var horizonBeat = GetJudgeLineHorizonBeat(src);
        var eventLayer = ConvertEventLayer(src, horizonBeat);
        eventLayer.Anticipation();

        return new JudgeLine
        {
            Name = $"PeJudgeLine_{index}",
            Notes = src.NoteList?.ConvertAll(ConvertNote) ?? [],
            EventLayers = [eventLayer]
        };
    }

    /// <summary>
    /// 转换单个音符。
    /// </summary>
    /// <param name="src">PhiEdit 音符。</param>
    /// <returns>Kpc 音符。</returns>
    private static Note ConvertNote(Pe.Note src) => new()
    {
        Above = src.Above,
        StartBeat = new Beat(src.StartBeat),
        EndBeat = new Beat(src.EndBeat),
        IsFake = src.IsFake,
        PositionX = TransformX(src.PositionX) + Chart.CoordinateSystem.MaxX,
        WidthRatio = src.WidthRatio,
        SpeedMultiplier = src.SpeedMultiplier,
        Type = (NoteType)(int)src.Type
    };

    /// <summary>
    /// 将 PhiEdit 判定线上的各通道帧/事件规范化为 Kpc 事件层。
    /// </summary>
    /// <param name="src">PhiEdit 判定线。</param>
    /// <param name="horizonBeat">用于补尾区间的终止拍点。</param>
    /// <returns>构建后的 Kpc 事件层。</returns>
    private static EventLayer ConvertEventLayer(Pe.JudgeLine src, double horizonBeat) => new()
    {
        MoveXEvents = BuildMoveAxisEvents(src.MoveFrames, src.MoveEvents, horizonBeat, point => point.X, TransformX),
        MoveYEvents = BuildMoveAxisEvents(src.MoveFrames, src.MoveEvents, horizonBeat, point => point.Y, TransformY),
        RotateEvents = BuildScalarEvents(src.RotateFrames, src.RotateEvents, horizonBeat, TransformAngle),
        AlphaEvents = BuildScalarEvents(src.AlphaFrames, src.AlphaEvents, horizonBeat,
            value => Math.Clamp((int)Math.Round(value), 0, 255)),
        SpeedEvents = BuildScalarEvents(src.SpeedFrames, [], horizonBeat, value => (float)(value / (14d / 9d)))
    };

    /// <summary>
    /// 计算单条判定线的时间范围上界，并额外补齐一个微小区间。
    /// </summary>
    /// <param name="src">PhiEdit 判定线。</param>
    /// <returns>用于事件构建的尾部拍点。</returns>
    private static double GetJudgeLineHorizonBeat(Pe.JudgeLine src)
    {
        var maxBeat = 0d;
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.NoteList?.Select(note => (double)note.EndBeat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.NoteList?.Select(note => (double)note.StartBeat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.SpeedFrames?.Select(frame => (double)frame.Beat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.MoveFrames?.Select(frame => (double)frame.Beat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.RotateFrames?.Select(frame => (double)frame.Beat)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.AlphaFrames?.Select(frame => (double)frame.Beat)));
        maxBeat = Math.Max(maxBeat,
            GetMaxBeat(src.MoveEvents?.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat })));
        maxBeat = Math.Max(maxBeat,
            GetMaxBeat(src.RotateEvents?.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat })));
        maxBeat = Math.Max(maxBeat,
            GetMaxBeat(src.AlphaEvents?.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat })));
        return maxBeat + TrailingBeatPadding;
    }

    /// <summary>
    /// 获取拍点集合中的最大值；为空时返回 <c>0</c>。
    /// </summary>
    /// <param name="beats">拍点集合。</param>
    /// <returns>最大拍点。</returns>
    private static double GetMaxBeat(IEnumerable<double>? beats) => beats?.DefaultIfEmpty(0d).Max() ?? 0d;

    /// <summary>
    /// 构建 Move 轴（X 或 Y）事件列表。
    /// </summary>
    private static List<Kpc.Event<double>>? BuildMoveAxisEvents(
        List<Pe.MoveFrame>? frames,
        List<Pe.MoveEvent>? events,
        double horizonBeat,
        Func<(float X, float Y), float> selector,
        Func<float, double> valueTransformer)
    {
        var orderedFrames = frames?.OrderBy(frame => frame.Beat).ToList() ?? [];
        var orderedEvents = events?.OrderBy(ev => ev.StartBeat).ToList() ?? [];
        var orderedEventsByEnd = orderedEvents.OrderBy(ev => ev.EndBeat).ToList();
        var boundaries = BuildBoundariesWithFrameSlices(
            orderedFrames.Select(frame => (double)frame.Beat),
            orderedEvents.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat }),
            orderedEvents.Select(ev => (double)ev.StartBeat),
            horizonBeat);

        if (boundaries.Count < 2) return null;

        var result = new List<Kpc.Event<double>>(boundaries.Count - 1);
        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var startBeat = boundaries[i];
            var endBeat = boundaries[i + 1];
            if (endBeat <= startBeat) continue;

            var frameAtBoundary = FindMoveFrameAtBeat(orderedFrames, startBeat);
            if (frameAtBoundary != null && !IsMoveEventStartBeat(orderedEvents, startBeat))
            {
                var convertedValue = valueTransformer(selector((frameAtBoundary.XValue, frameAtBoundary.YValue)));
                result.Add(CreateConstantEvent(startBeat, endBeat, convertedValue));
                continue;
            }

            var sampleBeat = GetMidBeat(startBeat, endBeat);
            var activeEvent = FindActiveMoveEvent(orderedEvents, sampleBeat);
            if (activeEvent != null)
            {
                var eventStartSource = ResolveMoveEventStartValue(activeEvent, orderedFrames, orderedEventsByEnd);
                result.Add(new Kpc.Event<double>
                {
                    StartBeat = new Beat(startBeat),
                    EndBeat = new Beat(endBeat),
                    Easing = ConvertEasing(activeEvent.EasingType),
                    EasingLeft = GetEventBoundary(activeEvent.StartBeat, activeEvent.EndBeat, startBeat),
                    EasingRight = GetEventBoundary(activeEvent.StartBeat, activeEvent.EndBeat, endBeat),
                    StartValue =
                        valueTransformer(InterpolateMoveValue(activeEvent, startBeat, eventStartSource, selector)),
                    EndValue = valueTransformer(InterpolateMoveValue(activeEvent, endBeat, eventStartSource,
                        selector))
                });
            }
        }

        return result.Count == 0 ? null : KpcEventTools.EventListCompressSqrt(result, 0d);
    }

    /// <summary>
    /// 构建标量通道（旋转、透明度、速度）事件列表。
    /// </summary>
    private static List<Kpc.Event<T>>? BuildScalarEvents<T>(
        List<Pe.Frame>? frames,
        List<Pe.Event>? events,
        double horizonBeat,
        Func<float, T> valueTransformer)
    {
        var orderedFrames = frames?.OrderBy(frame => frame.Beat).ToList() ?? [];
        var orderedEvents = events?.OrderBy(ev => ev.StartBeat).ToList() ?? [];
        var orderedEventsByEnd = orderedEvents.OrderBy(ev => ev.EndBeat).ToList();
        var boundaries = BuildBoundariesWithFrameSlices(
            orderedFrames.Select(frame => (double)frame.Beat),
            orderedEvents.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat }),
            orderedEvents.Select(ev => (double)ev.StartBeat),
            horizonBeat);

        if (boundaries.Count < 2) return null;

        var result = new List<Kpc.Event<T>>(boundaries.Count - 1);
        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var startBeat = boundaries[i];
            var endBeat = boundaries[i + 1];
            if (endBeat <= startBeat) continue;

            var frameAtBoundary = FindScalarFrameAtBeat(orderedFrames, startBeat);
            if (frameAtBoundary != null && !IsScalarEventStartBeat(orderedEvents, startBeat))
            {
                result.Add(CreateConstantEvent(startBeat, endBeat, valueTransformer(frameAtBoundary.Value)));
                continue;
            }

            var sampleBeat = GetMidBeat(startBeat, endBeat);
            var activeEvent = FindActiveScalarEvent(orderedEvents, sampleBeat);
            if (activeEvent != null)
            {
                var eventStartSource = ResolveScalarEventStartValue(activeEvent, orderedFrames, orderedEventsByEnd);
                result.Add(new Kpc.Event<T>
                {
                    StartBeat = new Beat(startBeat),
                    EndBeat = new Beat(endBeat),
                    Easing = ConvertEasing(activeEvent.EasingType),
                    EasingLeft = GetEventBoundary(activeEvent.StartBeat, activeEvent.EndBeat, startBeat),
                    EasingRight = GetEventBoundary(activeEvent.StartBeat, activeEvent.EndBeat, endBeat),
                    StartValue = valueTransformer(InterpolateScalarValue(activeEvent, startBeat, eventStartSource)),
                    EndValue = valueTransformer(InterpolateScalarValue(activeEvent, endBeat, eventStartSource))
                });
            }
        }

        return result.Count == 0 ? null : result;
    }

    /// <summary>
    /// 合并帧边界与事件边界，并补齐尾部边界。
    /// </summary>
    private static List<double> BuildBoundaries(
        IEnumerable<double> frameBoundaries,
        IEnumerable<double> eventBoundaries,
        double horizonBeat)
    {
        var boundaries = new SortedSet<double>();
        foreach (var beat in frameBoundaries) boundaries.Add(beat);
        foreach (var beat in eventBoundaries) boundaries.Add(beat);
        if (boundaries.Count == 0) return [];

        boundaries.Add(Math.Max(horizonBeat, boundaries.Max + TrailingBeatPadding));
        return boundaries.ToList();
    }

    /// <summary>
    /// 在基础边界上为非事件起点的 Frame 追加短切片边界，用于保留可编辑性。
    /// </summary>
    private static List<double> BuildBoundariesWithFrameSlices(
        IEnumerable<double> frameBoundaries,
        IEnumerable<double> eventBoundaries,
        IEnumerable<double> eventStartBoundaries,
        double horizonBeat)
    {
        var frameList = frameBoundaries.ToList();
        var eventStartList = eventStartBoundaries.OrderBy(beat => beat).ToList();
        var boundaries = BuildBoundaries(frameList, eventBoundaries, horizonBeat);
        if (boundaries.Count == 0) return boundaries;

        var expandedBoundaries = new SortedSet<double>(boundaries);
        foreach (var frameBeat in frameList)
        {
            if (ContainsBeat(eventStartList, frameBeat)) continue;
            expandedBoundaries.Add(frameBeat + FrameEditableSliceBeat);
        }

        return expandedBoundaries.ToList();
    }

    private static double GetMidBeat(double startBeat, double endBeat) => startBeat + (endBeat - startBeat) / 2d;

    private static bool IsSameBeat(double leftBeat, double rightBeat)
        => Math.Abs(leftBeat - rightBeat) <= BeatComparisonEpsilon;

    private static int FindLastIndexAtOrBeforeBeat<T>(List<T> items, double beat, Func<T, double> beatSelector)
    {
        var lo = 0;
        var hi = items.Count - 1;
        var result = -1;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var midBeat = beatSelector(items[mid]);
            if (midBeat <= beat + BeatComparisonEpsilon)
            {
                result = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return result;
    }

    private static bool ContainsBeat(List<double> sortedBeats, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(sortedBeats, beat, value => value);
        return idx >= 0 && IsSameBeat(sortedBeats[idx], beat);
    }

    private static float GetEventBoundary(float eventStartBeat, float eventEndBeat, double beat)
    {
        var duration = eventEndBeat - eventStartBeat;
        if (Math.Abs(duration) < 1e-6f) return 1f;
        return (float)((beat - eventStartBeat) / duration);
    }

    private static Kpc.Event<T> CreateConstantEvent<T>(double startBeat, double endBeat, T value) => new()
    {
        StartBeat = new Beat(startBeat),
        EndBeat = new Beat(endBeat),
        Easing = new Easing(1),
        EasingLeft = 0f,
        EasingRight = 1f,
        StartValue = value,
        EndValue = value
    };

    private static Pe.MoveEvent? FindActiveMoveEvent(List<Pe.MoveEvent> events, double beat)
    {
        var lo = 0;
        var hi = events.Count - 1;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            if (events[mid].StartBeat <= beat)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        if (hi >= 0 && events[hi].EndBeat >= beat)
            return events[hi];
        return null;
    }

    private static Pe.Event? FindActiveScalarEvent(List<Pe.Event> events, double beat)
    {
        var lo = 0;
        var hi = events.Count - 1;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            if (events[mid].StartBeat <= beat)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        if (hi >= 0 && events[hi].EndBeat >= beat)
            return events[hi];
        return null;
    }

    private static (float X, float Y) ResolveMoveValueAfterBoundary(
        List<Pe.MoveFrame> frames,
        List<Pe.MoveEvent> events,
        double boundaryBeat)
    {
        var previousFrameIndex = FindLastIndexAtOrBeforeBeat(frames, boundaryBeat, frame => frame.Beat);
        var previousEventIndex = FindLastIndexAtOrBeforeBeat(events, boundaryBeat, ev => ev.EndBeat);

        var previousFrame = previousFrameIndex >= 0 ? frames[previousFrameIndex] : null;
        var previousEvent = previousEventIndex >= 0 ? events[previousEventIndex] : null;

        if (previousEvent != null && (previousFrame == null || previousEvent.EndBeat > previousFrame.Beat))
            return (previousEvent.EndXValue, previousEvent.EndYValue);

        if (previousFrame != null)
            return (previousFrame.XValue, previousFrame.YValue);

        return (0f, 0f);
    }

    private static float ResolveScalarValueAfterBoundary(
        List<Pe.Frame> frames,
        List<Pe.Event> events,
        double boundaryBeat)
    {
        var previousFrameIndex = FindLastIndexAtOrBeforeBeat(frames, boundaryBeat, frame => frame.Beat);
        var previousEventIndex = FindLastIndexAtOrBeforeBeat(events, boundaryBeat, ev => ev.EndBeat);

        var previousFrame = previousFrameIndex >= 0 ? frames[previousFrameIndex] : null;
        var previousEvent = previousEventIndex >= 0 ? events[previousEventIndex] : null;

        if (previousEvent != null && (previousFrame == null || previousEvent.EndBeat > previousFrame.Beat))
            return previousEvent.EndValue;

        if (previousFrame != null)
            return previousFrame.Value;

        return 0f;
    }

    private static (float X, float Y) ResolveMoveEventStartValue(
        Pe.MoveEvent ev,
        List<Pe.MoveFrame> frames,
        List<Pe.MoveEvent> events)
    {
        var frameAtStart = FindMoveFrameAtBeat(frames, ev.StartBeat);
        if (frameAtStart != null)
            return (frameAtStart.XValue, frameAtStart.YValue);

        return ResolveMoveValueAfterBoundary(frames, events, ev.StartBeat);
    }

    private static float ResolveScalarEventStartValue(Pe.Event ev, List<Pe.Frame> frames, List<Pe.Event> events)
    {
        var frameAtStart = FindScalarFrameAtBeat(frames, ev.StartBeat);
        if (frameAtStart != null)
            return frameAtStart.Value;

        return ResolveScalarValueAfterBoundary(frames, events, ev.StartBeat);
    }

    private static bool IsMoveEventStartBeat(List<Pe.MoveEvent> events, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(events, beat, ev => ev.StartBeat);
        return idx >= 0 && IsSameBeat(events[idx].StartBeat, beat);
    }

    private static bool IsScalarEventStartBeat(List<Pe.Event> events, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(events, beat, ev => ev.StartBeat);
        return idx >= 0 && IsSameBeat(events[idx].StartBeat, beat);
    }

    private static Pe.MoveFrame? FindMoveFrameAtBeat(List<Pe.MoveFrame> frames, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(frames, beat, frame => frame.Beat);
        return idx >= 0 && IsSameBeat(frames[idx].Beat, beat) ? frames[idx] : null;
    }

    private static Pe.Frame? FindScalarFrameAtBeat(List<Pe.Frame> frames, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(frames, beat, frame => frame.Beat);
        return idx >= 0 && IsSameBeat(frames[idx].Beat, beat) ? frames[idx] : null;
    }

    private static float InterpolateMoveValue(
        Pe.MoveEvent ev,
        double beat,
        (float X, float Y) intervalStartSource,
        Func<(float X, float Y), float> selector)
    {
        if (Math.Abs(ev.EndBeat - ev.StartBeat) < 1e-6f)
            return selector((ev.EndXValue, ev.EndYValue));

        return selector(ev.GetValueAtBeat((float)beat, intervalStartSource.X, intervalStartSource.Y));
    }

    private static float InterpolateScalarValue(Pe.Event ev, double beat, float intervalStartSource)
    {
        if (Math.Abs(ev.EndBeat - ev.StartBeat) < 1e-6f)
            return ev.EndValue;

        return ev.GetValueAtBeat((float)beat, intervalStartSource);
    }
}