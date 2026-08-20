using KaedePhi.Core.Common;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KpcEasing = KaedePhi.Core.KaedePhi.Easing;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

/// <summary>
/// PE Frame/Event 模型到 KPC Event 模型的插值构建器。
/// </summary>
public class PhiEditFrameEventBuilder
{
    private readonly double _trailingBeatPadding;
    private readonly double _frameDurationBeat;
    private const double BeatComparisonEpsilon = 1e-6d;

    /// <summary>
    /// 根据转换选项初始化构建器实例。
    /// </summary>
    /// <param name="options">PE 到 KPC 转换选项</param>
    public PhiEditFrameEventBuilder(PhiEditToKpcConvertOptions options)
    {
        _frameDurationBeat = options.FrameDurationBeat;
        _trailingBeatPadding = options.TrailingBeatPadding;
    }

    /// <summary>
    /// 计算单条判定线的时间范围上界，并额外补充一个微小区间。
    /// </summary>
    /// <param name="src">源 PE 判定线</param>
    /// <returns>该判定线所有元素中最大拍点值加上尾部补偿量</returns>
    public double GetJudgeLineHorizonBeat(Pe.JudgeLine src)
    {
        var maxBeat = 0d;
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.NoteList.Select(note => (double)note.EndBeat)));
        maxBeat = Math.Max(
            maxBeat,
            GetMaxBeat(src.NoteList.Select(note => (double)note.StartBeat))
        );
        maxBeat = Math.Max(
            maxBeat,
            GetMaxBeat(src.SpeedFrames.Select(frame => (double)frame.Beat))
        );
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.MoveFrames.Select(frame => (double)frame.Beat)));
        maxBeat = Math.Max(
            maxBeat,
            GetMaxBeat(src.RotateFrames.Select(frame => (double)frame.Beat))
        );
        maxBeat = Math.Max(
            maxBeat,
            GetMaxBeat(src.AlphaFrames.Select(frame => (double)frame.Beat))
        );
        maxBeat = Math.Max(
            maxBeat,
            GetMaxBeat(src.MoveEvents.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat }))
        );
        maxBeat = Math.Max(
            maxBeat,
            GetMaxBeat(src.RotateEvents.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat }))
        );
        maxBeat = Math.Max(
            maxBeat,
            GetMaxBeat(src.AlphaEvents.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat }))
        );
        return maxBeat + _trailingBeatPadding;
    }

    /// <summary>
    /// 构建 Move 轴（X 或 Y）事件列表。
    /// </summary>
    /// <param name="frames">源 PE MoveFrame 列表</param>
    /// <param name="events">源 PE MoveEvent 列表</param>
    /// <param name="horizonBeat">判定线时间范围上界</param>
    /// <param name="selector">从 (X, Y) 元组中提取目标轴分量的选择器</param>
    /// <param name="valueTransformer">将 float 源值转换为目标坐标系 double 值的函数</param>
    /// <returns>转换后的 KPC Move 轴事件列表；若无有效事件则返回 null</returns>
    public List<KpcEvents.Event<double>>? BuildMoveAxisEvents(
        List<Pe.MoveFrame>? frames,
        List<Pe.MoveEvent>? events,
        double horizonBeat,
        Func<(float X, float Y), float> selector,
        Func<float, double> valueTransformer
    )
    {
        var orderedFrames = frames?.OrderBy(frame => frame.Beat).ToList() ?? [];
        var orderedEvents = events?.OrderBy(ev => ev.StartBeat).ToList() ?? [];
        var orderedEventsByEnd = orderedEvents.OrderBy(ev => ev.EndBeat).ToList();
        var boundaries = BuildBoundariesWithFrameSlices(
            orderedFrames.Select(frame => (double)frame.Beat),
            orderedEvents.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat }),
            orderedEvents.Select(ev => (double)ev.StartBeat),
            horizonBeat
        );

        if (boundaries.Count < 2)
            return null;

        var result = new List<KpcEvents.Event<double>>(boundaries.Count - 1);
        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var startBeat = boundaries[i];
            var endBeat = boundaries[i + 1];
            if (endBeat <= startBeat)
                continue;

            var frameAtBoundary = FindMoveFrameAtBeat(orderedFrames, startBeat);
            if (frameAtBoundary is not null && !IsMoveEventStartBeat(orderedEvents, startBeat))
            {
                var convertedValue = valueTransformer(
                    selector((frameAtBoundary.Value.XValue, frameAtBoundary.Value.YValue))
                );
                result.Add(CreateConstantEvent(startBeat, endBeat, convertedValue));
                continue;
            }

            var sampleBeat = GetMidBeat(startBeat, endBeat);
            var activeEvent = FindActiveMoveEvent(orderedEvents, sampleBeat);
            if (activeEvent is null)
                continue;
            var eventStartSource = ResolveMoveEventStartValue(
                activeEvent,
                orderedFrames,
                orderedEvents,
                orderedEventsByEnd
            );
            result.Add(
                new KpcEvents.Event<double>
                {
                    StartBeat = new Beat(startBeat),
                    EndBeat = new Beat(endBeat),
                    Easing = EasingConverter.ConvertEasing(activeEvent.EasingType),
                    EasingLeft = GetEventBoundary(
                        activeEvent.StartBeat,
                        activeEvent.EndBeat,
                        startBeat
                    ),
                    EasingRight = GetEventBoundary(
                        activeEvent.StartBeat,
                        activeEvent.EndBeat,
                        endBeat
                    ),
                    StartValue = valueTransformer(
                        InterpolateMoveValue(activeEvent, startBeat, eventStartSource, selector)
                    ),
                    EndValue = valueTransformer(
                        InterpolateMoveValue(activeEvent, endBeat, eventStartSource, selector)
                    ),
                }
            );
        }

        return result.Count == 0 ? null : result;
    }

    /// <summary>
    /// 构建标量通道（旋转、不透明度、速度）事件列表。
    /// </summary>
    /// <param name="frames">源 PE Frame 列表</param>
    /// <param name="events">源 PE Event 列表</param>
    /// <param name="horizonBeat">判定线时间范围上界</param>
    /// <param name="valueTransformer">将 float 源值转换为目标类型 T 的函数</param>
    /// <returns>转换后的 KPC 标量事件列表；若无有效事件则返回 null</returns>
    public List<KpcEvents.Event<T>>? BuildScalarEvents<T>(
        List<Pe.Frame>? frames,
        List<Pe.Event>? events,
        double horizonBeat,
        Func<float, T> valueTransformer
    )
        where T : notnull
    {
        var orderedFrames = frames?.OrderBy(frame => frame.Beat).ToList() ?? [];
        var orderedEvents = events?.OrderBy(ev => ev.StartBeat).ToList() ?? [];
        var orderedEventsByEnd = orderedEvents.OrderBy(ev => ev.EndBeat).ToList();
        var boundaries = BuildBoundariesWithFrameSlices(
            orderedFrames.Select(frame => (double)frame.Beat),
            orderedEvents.SelectMany(ev => new double[] { ev.StartBeat, ev.EndBeat }),
            orderedEvents.Select(ev => (double)ev.StartBeat),
            horizonBeat
        );

        if (boundaries.Count < 2)
            return null;

        var result = new List<KpcEvents.Event<T>>(boundaries.Count - 1);
        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var startBeat = boundaries[i];
            var endBeat = boundaries[i + 1];
            if (endBeat <= startBeat)
                continue;

            var frameAtBoundary = FindScalarFrameAtBeat(orderedFrames, startBeat);
            if (frameAtBoundary is not null && !IsScalarEventStartBeat(orderedEvents, startBeat))
            {
                result.Add(
                    CreateConstantEvent(startBeat, endBeat, valueTransformer(frameAtBoundary.Value.Value))
                );
                continue;
            }

            var sampleBeat = GetMidBeat(startBeat, endBeat);
            var activeEvent = FindActiveScalarEvent(orderedEvents, sampleBeat);
            if (activeEvent is null)
                continue;
            var eventStartSource = ResolveScalarEventStartValue(
                activeEvent,
                orderedFrames,
                orderedEvents,
                orderedEventsByEnd
            );
            result.Add(
                new KpcEvents.Event<T>
                {
                    StartBeat = new Beat(startBeat),
                    EndBeat = new Beat(endBeat),
                    Easing = EasingConverter.ConvertEasing(activeEvent.EasingType),
                    EasingLeft = GetEventBoundary(
                        activeEvent.StartBeat,
                        activeEvent.EndBeat,
                        startBeat
                    ),
                    EasingRight = GetEventBoundary(
                        activeEvent.StartBeat,
                        activeEvent.EndBeat,
                        endBeat
                    ),
                    StartValue = valueTransformer(
                        InterpolateScalarValue(activeEvent, startBeat, eventStartSource)
                    ),
                    EndValue = valueTransformer(
                        InterpolateScalarValue(activeEvent, endBeat, eventStartSource)
                    ),
                }
            );
        }

        return result.Count == 0 ? null : result;
    }

    #region Helpers

    /// <summary>
    /// 从拍点序列中取得最大值；序列为空时返回 0。
    /// </summary>
    /// <param name="beats">拍点序列</param>
    /// <returns>序列中的最大拍点值，或 0</returns>
    private static double GetMaxBeat(IEnumerable<double>? beats) =>
        beats?.DefaultIfEmpty(0d).Max() ?? 0d;

    /// <summary>
    /// 计算两个拍点之间的中点拍位。
    /// </summary>
    /// <param name="startBeat">起始拍点</param>
    /// <param name="endBeat">结束拍点</param>
    /// <returns>区间中点对应的拍点值</returns>
    private static double GetMidBeat(double startBeat, double endBeat) =>
        startBeat + (endBeat - startBeat) / 2d;

    /// <summary>
    /// 判断两个拍点值在误差范围内是否相等。
    /// </summary>
    /// <param name="leftBeat">左侧拍点</param>
    /// <param name="rightBeat">右侧拍点</param>
    /// <returns>差值绝对值不超过比较精度时返回 true</returns>
    private static bool IsSameBeat(double leftBeat, double rightBeat) =>
        Math.Abs(leftBeat - rightBeat) <= BeatComparisonEpsilon;

    /// <summary>
    /// 将帧边界与事件边界合并为有序边界列表，并追加时间范围上界。
    /// </summary>
    /// <param name="frameBoundaries">来自帧的拍点集合</param>
    /// <param name="eventBoundaries">来自事件起止拍点的集合</param>
    /// <param name="horizonBeat">时间范围上界</param>
    /// <returns>去重排序后的边界拍点列表</returns>
    private List<double> BuildBoundaries(
        IEnumerable<double> frameBoundaries,
        IEnumerable<double> eventBoundaries,
        double horizonBeat
    )
    {
        var boundaries = new SortedSet<double>();
        foreach (var beat in frameBoundaries)
            boundaries.Add(beat);
        foreach (var beat in eventBoundaries)
            boundaries.Add(beat);
        if (boundaries.Count == 0)
            return [];

        boundaries.Add(Math.Max(horizonBeat, boundaries.Max + _trailingBeatPadding));
        return boundaries.ToList();
    }

    /// <summary>
    /// 在 <see cref="BuildBoundaries"/> 的基础上，为每个不与事件起始拍重合的帧额外插入
    /// 一个偏移切片边界，确保帧值区间被正确分段。
    /// </summary>
    /// <param name="frameBoundaries">来自帧的拍点集合</param>
    /// <param name="eventBoundaries">来自事件起止拍点的集合</param>
    /// <param name="eventStartBoundaries">事件起始拍点集合，用于判断帧是否与事件重合</param>
    /// <param name="horizonBeat">时间范围上界</param>
    /// <returns>含帧切片偏移的去重排序边界列表</returns>
    private List<double> BuildBoundariesWithFrameSlices(
        IEnumerable<double> frameBoundaries,
        IEnumerable<double> eventBoundaries,
        IEnumerable<double> eventStartBoundaries,
        double horizonBeat
    )
    {
        var frameList = frameBoundaries.ToList();
        var eventStartList = eventStartBoundaries.OrderBy(beat => beat).ToList();
        var boundaries = BuildBoundaries(frameList, eventBoundaries, horizonBeat);
        if (boundaries.Count == 0)
            return boundaries;

        var expandedBoundaries = new SortedSet<double>(boundaries);
        foreach (
            var frameBeat in frameList.Where(frameBeat => !ContainsBeat(eventStartList, frameBeat))
        )
        {
            expandedBoundaries.Add(frameBeat + _frameDurationBeat);
        }

        return expandedBoundaries.ToList();
    }

    /// <summary>
    /// 在有序列表中二分查找最后一个拍点值不超过目标拍点的元素索引。
    /// </summary>
    /// <param name="items">有序元素列表</param>
    /// <param name="beat">目标拍点</param>
    /// <param name="beatSelector">从元素中提取拍点值的委托</param>
    /// <returns>满足条件的最大索引；不存在时返回 -1</returns>
    private static int FindLastIndexAtOrBeforeBeat<T>(
        List<T> items,
        double beat,
        Func<T, double> beatSelector
    )
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

    /// <summary>
    /// 在已排序的拍点列表中判断指定拍点是否存在。
    /// </summary>
    /// <param name="sortedBeats">已升序排列的拍点列表</param>
    /// <param name="beat">待查拍点</param>
    /// <returns>列表中存在与目标拍点误差范围内相等的值时返回 true</returns>
    private static bool ContainsBeat(List<double> sortedBeats, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(sortedBeats, beat, value => value);
        return idx >= 0 && IsSameBeat(sortedBeats[idx], beat);
    }

    /// <summary>
    /// 将绝对拍点映射为事件区间内的归一化缓动位置（0 到 1）。
    /// </summary>
    /// <param name="eventStartBeat">事件起始拍点</param>
    /// <param name="eventEndBeat">事件结束拍点</param>
    /// <param name="beat">目标绝对拍点</param>
    /// <returns>归一化后的缓动边界值；事件时长为零时返回 1</returns>
    private static float GetEventBoundary(float eventStartBeat, float eventEndBeat, double beat)
    {
        var duration = eventEndBeat - eventStartBeat;
        if (Math.Abs(duration) < 1e-6f)
            return 1f;
        return (float)((beat - eventStartBeat) / duration);
    }

    /// <summary>
    /// 创建一个起止值相同的常量事件。
    /// </summary>
    /// <param name="startBeat">事件起始拍点</param>
    /// <param name="endBeat">事件结束拍点</param>
    /// <param name="value">常量值</param>
    /// <returns>线性缓动、起止值均为 <paramref name="value"/> 的 KPC 事件</returns>
    private static KpcEvents.Event<T> CreateConstantEvent<T>(
        double startBeat,
        double endBeat,
        T value
    )
        where T : notnull =>
        new()
        {
            StartBeat = new Beat(startBeat),
            EndBeat = new Beat(endBeat),
            Easing = new KpcEasing(1),
            EasingLeft = 0f,
            EasingRight = 1f,
            StartValue = value,
            EndValue = value,
        };

    /// <summary>
    /// 在已按起始拍点排序的 MoveEvent 列表中，查找在指定拍点处活跃的事件。
    /// </summary>
    /// <param name="events">按起始拍点升序排列的 MoveEvent 列表</param>
    /// <param name="beat">目标采样拍点</param>
    /// <returns>覆盖该拍点的最近 MoveEvent；不存在时返回 null</returns>
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

    /// <summary>
    /// 在已按起始拍点排序的标量 Event 列表中，查找在指定拍点处活跃的事件。
    /// </summary>
    /// <param name="events">按起始拍点升序排列的标量 Event 列表</param>
    /// <param name="beat">目标采样拍点</param>
    /// <returns>覆盖该拍点的最近标量 Event；不存在时返回 null</returns>
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

    /// <summary>
    /// 在给定边界拍点之前，综合帧与已结束事件，推断该时刻之后应使用的 Move 位置值。
    /// </summary>
    /// <param name="frames">MoveFrame 列表（已排序）</param>
    /// <param name="events">按结束拍点排序的 MoveEvent 列表</param>
    /// <param name="boundaryBeat">边界拍点</param>
    /// <returns>边界拍点后的 (X, Y) 位置；无任何前驱时返回 (0, 0)</returns>
    private static (float X, float Y) ResolveMoveValueAfterBoundary(
        List<Pe.MoveFrame> frames,
        List<Pe.MoveEvent> events,
        double boundaryBeat
    )
    {
        var previousFrameIndex = FindLastIndexAtOrBeforeBeat(
            frames,
            boundaryBeat,
            frame => frame.Beat
        );
        var previousEventIndex = FindLastIndexAtOrBeforeBeat(
            events,
            boundaryBeat,
            ev => ev.EndBeat
        );

        Pe.MoveFrame? previousFrame = null;
        if (previousFrameIndex >= 0)
            previousFrame = frames[previousFrameIndex];
        var previousEvent = previousEventIndex >= 0 ? events[previousEventIndex] : null;

        if (
            previousEvent is not null
            && (previousFrame is null || previousEvent.EndBeat > previousFrame.Value.Beat)
        )
            return (previousEvent.EndXValue, previousEvent.EndYValue);

        return previousFrame is not null
            ? (previousFrame.Value.XValue, previousFrame.Value.YValue)
            : (0f, 0f);
    }

    /// <summary>
    /// 在给定边界拍点之前，综合帧与已结束事件，推断该时刻之后应使用的标量值。
    /// </summary>
    /// <param name="frames">Frame 列表（已排序）</param>
    /// <param name="events">按结束拍点排序的标量 Event 列表</param>
    /// <param name="boundaryBeat">边界拍点</param>
    /// <returns>边界拍点后的标量值；无任何前驱时返回 0</returns>
    private static float ResolveScalarValueAfterBoundary(
        List<Pe.Frame> frames,
        List<Pe.Event> events,
        double boundaryBeat
    )
    {
        var previousFrameIndex = FindLastIndexAtOrBeforeBeat(
            frames,
            boundaryBeat,
            frame => frame.Beat
        );
        var previousEventIndex = FindLastIndexAtOrBeforeBeat(
            events,
            boundaryBeat,
            ev => ev.EndBeat
        );

        Pe.Frame? previousFrame = null;
        if (previousFrameIndex >= 0)
            previousFrame = frames[previousFrameIndex];
        var previousEvent = previousEventIndex >= 0 ? events[previousEventIndex] : null;

        if (
            previousEvent is not null
            && (previousFrame is null || previousEvent.EndBeat > previousFrame.Value.Beat)
        )
            return previousEvent.EndValue;

        // Nullable<Frame> 的 HasValue 判断后取出其内层 Value 属性
        return previousFrame.HasValue ? previousFrame.Value.Value : 0f;
    }

    /// <summary>
    /// 解析 MoveEvent 在其起始拍点处的初始位置值。
    /// </summary>
    /// <param name="ev">需要解析起始值的 MoveEvent</param>
    /// <param name="frames">MoveFrame 列表（已按拍点升序排列）</param>
    /// <param name="orderedEvents">按起始拍点升序排列的 MoveEvent 列表</param>
    /// <param name="eventsByEnd">按结束拍点升序排列的 MoveEvent 列表</param>
    /// <returns>该事件在起始拍点处的 (X, Y) 初始值</returns>
    private static (float X, float Y) ResolveMoveEventStartValue(
        Pe.MoveEvent ev,
        List<Pe.MoveFrame> frames,
        List<Pe.MoveEvent> orderedEvents,
        List<Pe.MoveEvent> eventsByEnd
    )
    {
        // 查找事件索引：直接搜索而不是线性遍历
        var evIndex = 0;
        for (var i = 0; i < orderedEvents.Count; i++)
        {
            if (!ReferenceEquals(orderedEvents[i], ev))
                continue;
            evIndex = i;
            break;
        }

        return ResolveMoveEventStartValueImpl(ev, evIndex, frames, orderedEvents, eventsByEnd);
    }

    /// <summary>
    /// <see cref="ResolveMoveEventStartValue"/> 的递归实现，通过已知索引避免重复线性搜索。
    /// </summary>
    /// <param name="ev">需要解析起始值的 MoveEvent</param>
    /// <param name="evIndex">ev 在 <paramref name="orderedEvents"/> 中的索引</param>
    /// <param name="frames">MoveFrame 列表（已排序）</param>
    /// <param name="orderedEvents">按起始拍点升序排列的 MoveEvent 列表</param>
    /// <param name="eventsByEnd">按结束拍点升序排列的 MoveEvent 列表</param>
    /// <returns>该事件在起始拍点处的 (X, Y) 初始值</returns>
    private static (float X, float Y) ResolveMoveEventStartValueImpl(
        Pe.MoveEvent ev,
        int evIndex,
        List<Pe.MoveFrame> frames,
        List<Pe.MoveEvent> orderedEvents,
        List<Pe.MoveEvent> eventsByEnd
    )
    {
        // 优先：ev.StartBeat 处有精确帧
        var frameAtStart = FindMoveFrameAtBeat(frames, ev.StartBeat);
        if (frameAtStart is not null)
            return (frameAtStart.Value.XValue, frameAtStart.Value.YValue);

        // 查找前驱主导事件：从 evIndex 向前查找
        Pe.MoveEvent? precedingDominant = null;
        var precedingDominantIndex = -1;
        for (var i = evIndex - 1; i >= 0; i--)
        {
            if (!(orderedEvents[i].StartBeat <= ev.StartBeat + BeatComparisonEpsilon))
                continue;
            precedingDominant = orderedEvents[i];
            precedingDominantIndex = i;
            break;
        }

        if (
            precedingDominant is null
            || !(precedingDominant.EndBeat >= ev.StartBeat - BeatComparisonEpsilon)
        )
            return ResolveMoveValueAfterBoundary(frames, eventsByEnd, ev.StartBeat);
        // 前驱事件在 ev 开始时仍活跃（重叠）→ 递归计算其在该拍点的值
        var innerStartSource = ResolveMoveEventStartValueImpl(
            precedingDominant,
            precedingDominantIndex,
            frames,
            orderedEvents,
            eventsByEnd
        );
        return (
            InterpolateMoveValue(precedingDominant, ev.StartBeat, innerStartSource, v => v.X),
            InterpolateMoveValue(precedingDominant, ev.StartBeat, innerStartSource, v => v.Y)
        );
    }

    /// <summary>
    /// 解析标量 Event 在其起始拍点处的初始值。
    /// </summary>
    /// <param name="ev">需要解析起始值的标量 Event</param>
    /// <param name="frames">Frame 列表（已按拍点升序排列）</param>
    /// <param name="orderedEvents">按起始拍点升序排列的标量 Event 列表</param>
    /// <param name="eventsByEnd">按结束拍点升序排列的标量 Event 列表</param>
    /// <returns>该事件在起始拍点处的标量初始值</returns>
    private static float ResolveScalarEventStartValue(
        Pe.Event ev,
        List<Pe.Frame> frames,
        List<Pe.Event> orderedEvents,
        List<Pe.Event> eventsByEnd
    )
    {
        // 查找事件索引：直接搜索而不是线性遍历
        var evIndex = 0;
        for (var i = 0; i < orderedEvents.Count; i++)
        {
            if (!ReferenceEquals(orderedEvents[i], ev))
                continue;
            evIndex = i;
            break;
        }

        return ResolveScalarEventStartValueImpl(ev, evIndex, frames, orderedEvents, eventsByEnd);
    }

    /// <summary>
    /// <see cref="ResolveScalarEventStartValue"/> 的递归实现，通过已知索引避免重复线性搜索。
    /// </summary>
    /// <param name="ev">需要解析起始值的标量 Event</param>
    /// <param name="evIndex">ev 在 <paramref name="orderedEvents"/> 中的索引</param>
    /// <param name="frames">Frame 列表（已排序）</param>
    /// <param name="orderedEvents">按起始拍点升序排列的标量 Event 列表</param>
    /// <param name="eventsByEnd">按结束拍点升序排列的标量 Event 列表</param>
    /// <returns>该事件在起始拍点处的标量初始值</returns>
    private static float ResolveScalarEventStartValueImpl(
        Pe.Event ev,
        int evIndex,
        List<Pe.Frame> frames,
        List<Pe.Event> orderedEvents,
        List<Pe.Event> eventsByEnd
    )
    {
        // 优先：ev.StartBeat 处有精确帧
        var frameAtStart = FindScalarFrameAtBeat(frames, ev.StartBeat);
        if (frameAtStart is not null)
            return frameAtStart.Value.Value;

        // 查找前驱主导事件：从 evIndex 向前查找
        Pe.Event? precedingDominant = null;
        var precedingDominantIndex = -1;
        for (var i = evIndex - 1; i >= 0; i--)
        {
            if (!(orderedEvents[i].StartBeat <= ev.StartBeat + (float)BeatComparisonEpsilon))
                continue;
            precedingDominant = orderedEvents[i];
            precedingDominantIndex = i;
            break;
        }

        if (
            precedingDominant is null
            || !(precedingDominant.EndBeat >= ev.StartBeat - (float)BeatComparisonEpsilon)
        )
            return ResolveScalarValueAfterBoundary(frames, eventsByEnd, ev.StartBeat);
        // 前驱事件在 ev 开始时仍活跃（重叠）→ 递归计算其在该拍点的值
        var innerStart = ResolveScalarEventStartValueImpl(
            precedingDominant,
            precedingDominantIndex,
            frames,
            orderedEvents,
            eventsByEnd
        );
        return InterpolateScalarValue(precedingDominant, ev.StartBeat, innerStart);
    }

    /// <summary>
    /// 判断指定拍点是否为某个 MoveEvent 的精确起始拍点。
    /// </summary>
    /// <param name="events">按起始拍点升序排列的 MoveEvent 列表</param>
    /// <param name="beat">待检测拍点</param>
    /// <returns>存在起始拍点与 <paramref name="beat"/> 在误差范围内相等的事件时返回 true</returns>
    private static bool IsMoveEventStartBeat(List<Pe.MoveEvent> events, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(events, beat, ev => ev.StartBeat);
        return idx >= 0 && IsSameBeat(events[idx].StartBeat, beat);
    }

    /// <summary>
    /// 判断指定拍点是否为某个标量 Event 的精确起始拍点。
    /// </summary>
    /// <param name="events">按起始拍点升序排列的标量 Event 列表</param>
    /// <param name="beat">待检测拍点</param>
    /// <returns>存在起始拍点与 <paramref name="beat"/> 在误差范围内相等的事件时返回 true</returns>
    private static bool IsScalarEventStartBeat(List<Pe.Event> events, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(events, beat, ev => ev.StartBeat);
        return idx >= 0 && IsSameBeat(events[idx].StartBeat, beat);
    }

    /// <summary>
    /// 在 MoveFrame 列表中查找拍点与目标拍点精确匹配的帧。
    /// </summary>
    /// <param name="frames">按拍点升序排列的 MoveFrame 列表</param>
    /// <param name="beat">目标拍点</param>
    /// <returns>精确匹配的 MoveFrame；不存在时返回 null</returns>
    private static Pe.MoveFrame? FindMoveFrameAtBeat(List<Pe.MoveFrame> frames, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(frames, beat, frame => frame.Beat);
        if (idx >= 0 && IsSameBeat(frames[idx].Beat, beat))
            return frames[idx];
        return null;
    }

    /// <summary>
    /// 在标量 Frame 列表中查找拍点与目标拍点精确匹配的帧。
    /// </summary>
    /// <param name="frames">按拍点升序排列的标量 Frame 列表</param>
    /// <param name="beat">目标拍点</param>
    /// <returns>精确匹配的 Frame；不存在时返回 null</returns>
    private static Pe.Frame? FindScalarFrameAtBeat(List<Pe.Frame> frames, double beat)
    {
        var idx = FindLastIndexAtOrBeforeBeat(frames, beat, frame => frame.Beat);
        if (idx >= 0 && IsSameBeat(frames[idx].Beat, beat))
            return frames[idx];
        return null;
    }

    /// <summary>
    /// 根据事件缓动曲线，对 MoveEvent 在指定拍点处的单轴值进行插值。
    /// </summary>
    /// <param name="ev">源 MoveEvent</param>
    /// <param name="beat">目标拍点</param>
    /// <param name="intervalStartSource">本次插值区间的起始 (X, Y) 值</param>
    /// <param name="selector">从 (X, Y) 元组中提取目标轴分量的选择器</param>
    /// <returns>目标拍点处插值后的单轴 float 值</returns>
    private static float InterpolateMoveValue(
        Pe.MoveEvent ev,
        double beat,
        (float X, float Y) intervalStartSource,
        Func<(float X, float Y), float> selector
    )
    {
        return selector(
            Math.Abs(ev.EndBeat - ev.StartBeat) < 1e-6f
                ? (ev.EndXValue, ev.EndYValue)
                : ev.GetValueAtBeat((float)beat, intervalStartSource.X, intervalStartSource.Y)
        );
    }

    /// <summary>
    /// 根据事件缓动曲线，对标量 Event 在指定拍点处的值进行插值。
    /// </summary>
    /// <param name="ev">源标量 Event</param>
    /// <param name="beat">目标拍点</param>
    /// <param name="intervalStartSource">本次插值区间的起始标量值</param>
    /// <returns>目标拍点处插值后的 float 值</returns>
    private static float InterpolateScalarValue(Pe.Event ev, double beat, float intervalStartSource)
    {
        return Math.Abs(ev.EndBeat - ev.StartBeat) < 1e-6f
            ? ev.EndValue
            : ev.GetValueAtBeat((float)beat, intervalStartSource);
    }

    #endregion
}
