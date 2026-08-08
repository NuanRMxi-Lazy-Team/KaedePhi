using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Event.KaedePhi;

public class EventListMerger<TPayload> : LoggableBase, IEventListMerger<KpcEvents.Event<TPayload>>
    where TPayload : notnull
{
    protected static readonly EventCutter<TPayload> Cutter = new();

    #region 入口

    /// <inheritdoc/>
    public List<KpcEvents.Event<TPayload>> EventListMerge(
        List<KpcEvents.Event<TPayload>>? toEvents,
        List<KpcEvents.Event<TPayload>>? fromEvents,
        double precision
    )
    {
        ChartProcessingValidator.ValidatePrecision(precision);
        if (TryGetMergeEarlyReturn(toEvents, fromEvents, out var earlyReturn))
            return earlyReturn;
        if (toEvents is null || fromEvents is null)
            return [];
        EnsureSupportedNumericType();

        var toEventsCopy = CloneEventList(toEvents);
        var fromEventsCopy = CloneEventList(fromEvents);
        SortByStartBeat(toEventsCopy);
        SortByStartBeat(fromEventsCopy);

        return !HasOverlap(toEventsCopy, fromEventsCopy)
            ? MergeWithoutOverlap(toEventsCopy, fromEventsCopy)
            : MergeWithOverlapFixedSampling(
                CloneEventList(toEventsCopy),
                toEventsCopy,
                fromEventsCopy,
                precision
            );
    }

    #endregion

    // 快速返回

    /// <summary>
    /// 在任一输入列表为空时直接给出合并结果，避免进入完整合并流程。
    /// </summary>
    /// <param name="toEvents">目标事件列表。</param>
    /// <param name="fromEvents">来源事件列表。</param>
    /// <param name="result">提前返回时的结果列表。</param>
    /// <returns>若命中提前返回条件则为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
    protected static bool TryGetMergeEarlyReturn(
        List<KpcEvents.Event<TPayload>>? toEvents,
        List<KpcEvents.Event<TPayload>>? fromEvents,
        out List<KpcEvents.Event<TPayload>> result
    )
    {
        if (toEvents is null || toEvents.Count == 0)
        {
            result =
                fromEvents is null || fromEvents.Count == 0
                    ? []
                    : [.. fromEvents.Select(e => e.Clone())];
            SortByStartBeat(result);
            return true;
        }

        if (fromEvents is null || fromEvents.Count == 0)
        {
            result = [.. toEvents.Select(e => e.Clone())];
            SortByStartBeat(result);
            return true;
        }

        result = [];
        return false;
    }

    #region 共用工具

    /// <summary>
    /// 校验事件值类型是否为合并器支持的数值类型。
    /// </summary>
    protected static void EnsureSupportedNumericType()
    {
        if (
            typeof(TPayload) != typeof(int)
            && typeof(TPayload) != typeof(float)
            && typeof(TPayload) != typeof(double)
        )
            throw new NotSupportedException(
                "EventMerge only supports int, float, and double types."
            );
    }

    /// <summary>
    /// 深拷贝事件列表，避免修改调用方数据。
    /// </summary>
    /// <param name="events">待拷贝的事件列表。</param>
    /// <returns>拷贝后的新列表。</returns>
    protected static List<KpcEvents.Event<TPayload>> CloneEventList(
        List<KpcEvents.Event<TPayload>> events
    ) => events.Select(e => e.Clone()).ToList();

    /// <summary>
    /// 按开始拍排序事件列表。
    /// </summary>
    /// <param name="events">待排序的事件列表。</param>
    protected static void SortByStartBeat(List<KpcEvents.Event<TPayload>> events)
    {
        var sorted = events.OrderBy(e => e.StartBeat).ToList();
        events.Clear();
        events.AddRange(sorted);
    }

    /// <summary>
    /// 判断两个已排序事件列表是否存在时间重叠。
    /// 使用双指针法，O(n+m) 复杂度。
    /// </summary>
    /// <param name="toEvents">目标事件列表（需按 StartBeat 排序）。</param>
    /// <param name="fromEvents">来源事件列表（需按 StartBeat 排序）。</param>
    /// <returns>存在任意重叠区间时返回 <see langword="true"/>。</returns>
    protected static bool HasOverlap(
        List<KpcEvents.Event<TPayload>> toEvents,
        List<KpcEvents.Event<TPayload>> fromEvents
    )
    {
        var ti = 0;
        var fi = 0;
        while (ti < toEvents.Count && fi < fromEvents.Count)
        {
            var te = toEvents[ti];
            var fe = fromEvents[fi];
            if (te.StartBeat < fe.EndBeat && fe.StartBeat < te.EndBeat)
                return true;
            if (te.EndBeat <= fe.StartBeat)
                ti++;
            else
                fi++;
        }

        return false;
    }

    #endregion

    #region 非重叠路径

    /// <summary>
    /// 合并无重叠的两组事件，按前序事件终值补偿偏移。
    /// </summary>
    /// <param name="toEventsCopy">目标事件拷贝。</param>
    /// <param name="fromEventsCopy">来源事件拷贝。</param>
    /// <returns>合并后的事件列表。</returns>
    protected static List<KpcEvents.Event<TPayload>> MergeWithoutOverlap(
        List<KpcEvents.Event<TPayload>> toEventsCopy,
        List<KpcEvents.Event<TPayload>> fromEventsCopy
    )
    {
        var newEvents = (
            from toEvent in toEventsCopy
            let prevForm = fromEventsCopy.FindLast(e => e.EndBeat <= toEvent.StartBeat)
            let formOffset = prevForm is null ? default : prevForm.EndValue
            select new KpcEvents.Event<TPayload>
            {
                StartBeat = toEvent.StartBeat,
                EndBeat = toEvent.EndBeat,
                StartValue = NumericHelper.Add(toEvent.StartValue, formOffset),
                EndValue = NumericHelper.Add(toEvent.EndValue, formOffset),
                BezierPoints = toEvent.BezierPoints,
                Easing = toEvent.Easing,
                EasingLeft = toEvent.EasingLeft,
                EasingRight = toEvent.EasingRight,
                IsBezier = toEvent.IsBezier,
            }
        ).ToList();

        newEvents.AddRange(
            from formEvent in fromEventsCopy
            let prevTo = toEventsCopy.FindLast(e => e.EndBeat <= formEvent.StartBeat)
            let toEventValue = prevTo is not null ? prevTo.EndValue : default
            select new KpcEvents.Event<TPayload>
            {
                StartBeat = formEvent.StartBeat,
                EndBeat = formEvent.EndBeat,
                StartValue = NumericHelper.Add(formEvent.StartValue, toEventValue),
                EndValue = NumericHelper.Add(formEvent.EndValue, toEventValue),
                BezierPoints = formEvent.BezierPoints,
                Easing = formEvent.Easing,
                EasingLeft = formEvent.EasingLeft,
                EasingRight = formEvent.EasingRight,
                IsBezier = formEvent.IsBezier,
            }
        );

        SortByStartBeat(newEvents);
        return newEvents;
    }

    #endregion

    #region 重叠区间构建

    /// <summary>
    /// 构建两组事件的重叠区间，并将可连接区间归并。
    /// 使用 HashSet 去重，避免 O(n) 线性查找。
    /// </summary>
    /// <param name="toEvents">目标事件列表。</param>
    /// <param name="fromEvents">来源事件列表。</param>
    /// <returns>按开始拍排序后的重叠区间集合。</returns>
    protected static List<(Beat Start, Beat End)> BuildOverlapIntervals(
        List<KpcEvents.Event<TPayload>> toEvents,
        List<KpcEvents.Event<TPayload>> fromEvents
    )
    {
        var overlapIntervals = new List<(Beat Start, Beat End)>();
        var seen = new HashSet<(double Start, double End)>();
        foreach (var fe in fromEvents)
        {
            foreach (var te in toEvents)
            {
                if (!TryGetOverlapBounds(fe, te, out var start, out var end))
                    continue;
                var key = ((double)start, (double)end);
                if (!seen.Add(key))
                    continue;
                AddOrMergeOverlapInterval(overlapIntervals, start, end);
            }
        }

        SortIntervals(overlapIntervals);
        return overlapIntervals;
    }

    /// <summary>
    /// 计算两个事件的重叠边界。
    /// </summary>
    /// <param name="fe">来源事件。</param>
    /// <param name="te">目标事件。</param>
    /// <param name="start">重叠起始拍。</param>
    /// <param name="end">重叠结束拍。</param>
    /// <returns>存在重叠时返回 <see langword="true"/>。</returns>
    private static bool TryGetOverlapBounds(
        KpcEvents.Event<TPayload> fe,
        KpcEvents.Event<TPayload> te,
        out Beat start,
        out Beat end
    )
    {
        if (fe.StartBeat >= te.EndBeat || fe.EndBeat <= te.StartBeat)
        {
            start = new Beat(0d);
            end = new Beat(0d);
            return false;
        }

        start = fe.StartBeat < te.StartBeat ? fe.StartBeat : te.StartBeat;
        end = fe.EndBeat > te.EndBeat ? fe.EndBeat : te.EndBeat;
        return true;
    }

    /// <summary>
    /// 将新区间加入集合；若与已有区间重叠则进行归并。
    /// </summary>
    /// <param name="overlapIntervals">重叠区间集合。</param>
    /// <param name="start">新区间起始拍。</param>
    /// <param name="end">新区间结束拍。</param>
    private static void AddOrMergeOverlapInterval(
        List<(Beat Start, Beat End)> overlapIntervals,
        Beat start,
        Beat end
    )
    {
        // 检查是否与任何已有区间重叠
        if (!overlapIntervals.Any(iv => start < iv.End && end > iv.Start))
        {
            // 不重叠，直接添加
            overlapIntervals.Add((start, end));
            return;
        }

        // 与已有区间重叠，需要进行归并
        var indicesToRemove = new List<int>();
        var mergedStart = start;
        var mergedEnd = end;

        for (var i = 0; i < overlapIntervals.Count; i++)
        {
            var iv = overlapIntervals[i];
            if (!(mergedStart < iv.End && mergedEnd > iv.Start))
                continue;

            // 扩展合并区间的边界
            if (iv.Start < mergedStart)
                mergedStart = iv.Start;
            if (iv.End > mergedEnd)
                mergedEnd = iv.End;

            // 标记该区间为待删除
            indicesToRemove.Add(i);
        }

        // 从后向前删除，避免索引变化问题
        for (var i = indicesToRemove.Count - 1; i >= 0; i--)
        {
            overlapIntervals.RemoveAt(indicesToRemove[i]);
        }

        // 添加合并后的区间
        overlapIntervals.Add((mergedStart, mergedEnd));
    }

    /// <summary>
    /// 按起止拍对区间进行稳定排序。
    /// </summary>
    /// <param name="overlapIntervals">待排序区间集合。</param>
    private static void SortIntervals(List<(Beat Start, Beat End)> overlapIntervals)
    {
        var sorted = overlapIntervals.OrderBy(x => x.Start).ThenBy(x => x.End).ToList();
        overlapIntervals.Clear();
        overlapIntervals.AddRange(sorted);
    }

    #endregion

    #region 固定采样路径

    /// <summary>
    /// 通过固定步长切片合并重叠区间，并可按容差压缩结果。
    /// </summary>
    /// <param name="toEventsForOffsetLookup">用于查询前序偏移的目标事件原始序列。</param>
    /// <param name="toEventsCopy">目标事件拷贝。</param>
    /// <param name="fromEventsCopy">来源事件拷贝。</param>
    /// <param name="precision">每拍切片精度。</param>
    /// <returns>合并后的事件列表。</returns>
    private static List<KpcEvents.Event<TPayload>> MergeWithOverlapFixedSampling(
        List<KpcEvents.Event<TPayload>> toEventsForOffsetLookup,
        List<KpcEvents.Event<TPayload>> toEventsCopy,
        List<KpcEvents.Event<TPayload>> fromEventsCopy,
        double precision
    )
    {
        var overlapIntervals = BuildOverlapIntervals(toEventsCopy, fromEventsCopy);
        var newEvents = BuildBaseEventsOutsideOverlap(
            toEventsCopy,
            fromEventsCopy,
            toEventsForOffsetLookup,
            overlapIntervals
        );

        var cutLength = new Beat(1d / precision);
        var (cutTo, cutFrom) = CutAndRemoveOverlapEvents(
            toEventsCopy,
            fromEventsCopy,
            overlapIntervals,
            cutLength
        );
        newEvents.AddRange(
            MergeCutOverlapSegments(
                toEventsForOffsetLookup,
                fromEventsCopy,
                cutTo,
                cutFrom,
                overlapIntervals,
                cutLength
            )
        );

        SortByStartBeat(newEvents);
        return newEvents;
    }

    /// <summary>
    /// 构建重叠区间之外的基础事件，并应用另一个轨道的前序偏移。
    /// </summary>
    /// <param name="toEventsCopy">目标事件拷贝。</param>
    /// <param name="fromEventsCopy">来源事件拷贝。</param>
    /// <param name="toEventsForOffsetLookup">用于查询偏移的目标事件序列。</param>
    /// <param name="overlapIntervals">重叠区间集合。</param>
    /// <returns>重叠区间外的合并事件。</returns>
    protected static List<KpcEvents.Event<TPayload>> BuildBaseEventsOutsideOverlap(
        List<KpcEvents.Event<TPayload>> toEventsCopy,
        List<KpcEvents.Event<TPayload>> fromEventsCopy,
        List<KpcEvents.Event<TPayload>> toEventsForOffsetLookup,
        List<(Beat Start, Beat End)> overlapIntervals
    )
    {
        var newEvents = new List<KpcEvents.Event<TPayload>>();

        foreach (var toEvent in toEventsCopy)
        {
            if (!TouchesAnyOverlap(toEvent))
            {
                // 整条事件在重叠区间外，直接输出（原逻辑）
                var prevForm = fromEventsCopy.FindLast(e => e.EndBeat <= toEvent.StartBeat);
                var formOffset = prevForm is not null ? prevForm.EndValue : default;
                newEvents.Add(
                    new KpcEvents.Event<TPayload>
                    {
                        StartBeat = toEvent.StartBeat,
                        EndBeat = toEvent.EndBeat,
                        StartValue = NumericHelper.Add(toEvent.StartValue, formOffset),
                        EndValue = NumericHelper.Add(toEvent.EndValue, formOffset),
                        BezierPoints = toEvent.BezierPoints,
                        Easing = toEvent.Easing,
                        EasingLeft = toEvent.EasingLeft,
                        EasingRight = toEvent.EasingRight,
                        IsBezier = toEvent.IsBezier,
                    }
                );
            }
            else
            {
                // 事件与重叠区间有交叉：补充超出重叠区间的片段。
                // 注意：gap 片段必须使用线性缓动（Easing=1），不能沿用原事件的缓动曲线。
                // 原事件的缓动曲线是针对整个事件区间参数化的；若对子区间应用同一缓动，
                // 子区间内的中间拍数值将与原事件实际数值不一致，产生"残根"错误。
                // GetValueAtBeat 已按照原缓动正确计算了端点值，线性插值端点值即为正确做法。
                foreach (var (gapStart, gapEnd) in GapsOutsideOverlap(toEvent))
                {
                    var prevForm = fromEventsCopy.FindLast(e => e.EndBeat <= gapStart);
                    var formOffset = prevForm is not null ? prevForm.EndValue : default;
                    newEvents.Add(
                        new KpcEvents.Event<TPayload>
                        {
                            StartBeat = gapStart,
                            EndBeat = gapEnd,
                            StartValue = NumericHelper.Add(
                                toEvent.GetValueAtBeat(gapStart),
                                formOffset
                            ),
                            EndValue = NumericHelper.Add(
                                toEvent.GetValueAtBeat(gapEnd),
                                formOffset
                            ),
                            Easing = new Kpc.Easing(1), // 线性，具体看上方注释
                            IsBezier = false,
                        }
                    );
                }
            }
        }

        foreach (var formEvent in fromEventsCopy)
        {
            if (!TouchesAnyOverlap(formEvent))
            {
                var prevTo = toEventsForOffsetLookup.FindLast(e =>
                    e.EndBeat <= formEvent.StartBeat
                );
                var toEventValue = prevTo is not null ? prevTo.EndValue : default;
                newEvents.Add(
                    new KpcEvents.Event<TPayload>
                    {
                        StartBeat = formEvent.StartBeat,
                        EndBeat = formEvent.EndBeat,
                        StartValue = NumericHelper.Add(formEvent.StartValue, toEventValue),
                        EndValue = NumericHelper.Add(formEvent.EndValue, toEventValue),
                        BezierPoints = formEvent.BezierPoints,
                        Easing = formEvent.Easing,
                        EasingLeft = formEvent.EasingLeft,
                        EasingRight = formEvent.EasingRight,
                        IsBezier = formEvent.IsBezier,
                    }
                );
            }
            else
            {
                // 同上：gap 片段使用线性缓动，不沿用原事件缓动曲线。
                foreach (var (gapStart, gapEnd) in GapsOutsideOverlap(formEvent))
                {
                    var prevTo = toEventsForOffsetLookup.FindLast(e => e.EndBeat <= gapStart);
                    var toEventValue = prevTo is not null ? prevTo.EndValue : default;
                    newEvents.Add(
                        new KpcEvents.Event<TPayload>
                        {
                            StartBeat = gapStart,
                            EndBeat = gapEnd,
                            StartValue = NumericHelper.Add(
                                formEvent.GetValueAtBeat(gapStart),
                                toEventValue
                            ),
                            EndValue = NumericHelper.Add(
                                formEvent.GetValueAtBeat(gapEnd),
                                toEventValue
                            ),
                            Easing = new Kpc.Easing(1), // 线性，具体看上方注释
                            IsBezier = false,
                        }
                    );
                }
            }
        }

        return newEvents;

        bool TouchesAnyOverlap(KpcEvents.Event<TPayload> evt) =>
            overlapIntervals.Any(iv => evt.StartBeat < iv.End && evt.EndBeat > iv.Start);

        // 返回一个事件在所有重叠区间之外的时间片段列表（即"空隙"）。
        // 例如事件 [223.5, 225.5]、重叠区间 [223.5, 224.0]，空隙为 [(224.0, 225.5)]。
        List<(Beat Start, Beat End)> GapsOutsideOverlap(KpcEvents.Event<TPayload> evt)
        {
            var gaps = new List<(Beat Start, Beat End)>();
            var cursor = evt.StartBeat;
            // overlapIntervals 已按 Start 排序
            foreach (var iv in overlapIntervals.Where(iv => iv.End > evt.StartBeat))
            {
                // 如果当前区间开始位置在事件范围内，且在游标后面，添加游标到该位置的空隙
                if (iv.Start > cursor && iv.Start < evt.EndBeat)
                    gaps.Add((cursor, iv.Start)); // 空隙在重叠区间左侧
                // 推进游标越过当前重叠区间（仅当该区间与事件相交时）
                if (iv.Start < evt.EndBeat && iv.End > cursor)
                    cursor = iv.End; // 推进游标越过当前重叠区间
                if (cursor >= evt.EndBeat)
                    break;
            }

            if (cursor < evt.EndBeat)
                gaps.Add((cursor, evt.EndBeat)); // 末尾剩余空隙
            return gaps;
        }
    }

    /// <summary>
    /// 在重叠区间内切分两组事件，并从原列表移除已覆盖片段。
    /// </summary>
    /// <param name="toEventsCopy">目标事件拷贝。</param>
    /// <param name="fromEventsCopy">来源事件拷贝。</param>
    /// <param name="overlapIntervals">重叠区间集合。</param>
    /// <param name="cutLength">切片长度。</param>
    /// <returns>目标与来源两组切片事件。</returns>
    private static (
        List<KpcEvents.Event<TPayload>> CutTo,
        List<KpcEvents.Event<TPayload>> CutFrom
    ) CutAndRemoveOverlapEvents(
        List<KpcEvents.Event<TPayload>> toEventsCopy,
        List<KpcEvents.Event<TPayload>> fromEventsCopy,
        List<(Beat Start, Beat End)> overlapIntervals,
        Beat cutLength
    )
    {
        var cutTo = new List<KpcEvents.Event<TPayload>>();
        var cutFrom = new List<KpcEvents.Event<TPayload>>();
        foreach (var (start, end) in overlapIntervals)
        {
            cutTo.AddRange(Cutter.CutEventsInRange(toEventsCopy, start, end, cutLength));
            cutFrom.AddRange(Cutter.CutEventsInRange(fromEventsCopy, start, end, cutLength));
            toEventsCopy.RemoveAll(e => e.StartBeat < end && e.EndBeat > start);
            fromEventsCopy.RemoveAll(e => e.StartBeat < end && e.EndBeat > start);
        }

        return (cutTo, cutFrom);
    }

    /// <summary>
    /// 逐个重叠区间合并切片事件，并处理缺失片段的延续值。
    /// </summary>
    /// <param name="toEventsForOffsetLookup">用于查询目标轨道前序终值的原始序列（未被修改）。</param>
    /// <param name="fromEventsCopy">来源事件拷贝（已被修改，用于前序值回溯）。</param>
    /// <param name="cutTo">目标切片事件。</param>
    /// <param name="cutFrom">来源切片事件。</param>
    /// <param name="overlapIntervals">重叠区间集合。</param>
    /// <param name="cutLength">切片长度。</param>
    /// <returns>重叠区间合并结果。</returns>
    private static List<KpcEvents.Event<TPayload>> MergeCutOverlapSegments(
        List<KpcEvents.Event<TPayload>> toEventsForOffsetLookup,
        List<KpcEvents.Event<TPayload>> fromEventsCopy,
        List<KpcEvents.Event<TPayload>> cutTo,
        List<KpcEvents.Event<TPayload>> cutFrom,
        List<(Beat Start, Beat End)> overlapIntervals,
        Beat cutLength
    )
    {
        var allCutEvents = new List<KpcEvents.Event<TPayload>>();
        foreach (var (start, end) in overlapIntervals)
        {
            var prevTo = toEventsForOffsetLookup.FindLast(e => e.EndBeat <= start);
            var prevForm = fromEventsCopy.FindLast(e => e.EndBeat <= start);
            var toLastEnd = prevTo is not null ? prevTo.EndValue : default;
            var formLastEnd = prevForm is not null ? prevForm.EndValue : default;
            allCutEvents.AddRange(
                MergeSingleOverlapInterval(
                    cutTo,
                    cutFrom,
                    start,
                    end,
                    cutLength,
                    toLastEnd,
                    formLastEnd
                )
            );
        }

        return allCutEvents;
    }

    /// <summary>
    /// 合并单个重叠区间内的切片事件。
    /// </summary>
    /// <param name="cutTo">目标切片事件。</param>
    /// <param name="cutFrom">来源切片事件。</param>
    /// <param name="start">区间起始拍。</param>
    /// <param name="end">区间结束拍。</param>
    /// <param name="cutLength">切片长度。</param>
    /// <param name="toLastEndValue">目标轨道进入区间前的终值。</param>
    /// <param name="formLastEndValue">来源轨道进入区间前的终值。</param>
    /// <returns>该区间的合并事件。</returns>
    private static List<KpcEvents.Event<TPayload>> MergeSingleOverlapInterval(
        List<KpcEvents.Event<TPayload>> cutTo,
        List<KpcEvents.Event<TPayload>> cutFrom,
        Beat start,
        Beat end,
        Beat cutLength,
        TPayload? toLastEndValue,
        TPayload? formLastEndValue
    )
    {
        // 预索引：O(n) 构建，后续 O(1) 查找
        var toIndex = new Dictionary<(double, double), KpcEvents.Event<TPayload>>();
        foreach (var e in cutTo)
            toIndex.TryAdd((e.StartBeat, e.EndBeat), e);
        var fromIndex = new Dictionary<(double, double), KpcEvents.Event<TPayload>>();
        foreach (var e in cutFrom)
            fromIndex.TryAdd((e.StartBeat, e.EndBeat), e);

        var merged = new List<KpcEvents.Event<TPayload>>();
        var currentBeat = start;
        while (currentBeat < end)
        {
            var nextBeat = currentBeat + cutLength;
            if (nextBeat > end)
                nextBeat = end;
            var key = ((double)currentBeat, (double)nextBeat);
            toIndex.TryGetValue(key, out var toEvent);
            fromIndex.TryGetValue(key, out var formEvent);

            var toStart = toEvent is not null ? toEvent.StartValue : toLastEndValue;
            var formStart = formEvent is not null ? formEvent.StartValue : formLastEndValue;
            var toEnd = toEvent is not null ? toEvent.EndValue : toLastEndValue;
            var formEnd = formEvent is not null ? formEvent.EndValue : formLastEndValue;

            merged.Add(
                new KpcEvents.Event<TPayload>
                {
                    StartBeat = currentBeat,
                    EndBeat = nextBeat,
                    StartValue = NumericHelper.Add(toStart, formStart),
                    EndValue = NumericHelper.Add(toEnd, formEnd),
                }
            );

            if (toEvent is not null)
                toLastEndValue = toEvent.EndValue;
            if (formEvent is not null)
                formLastEndValue = formEvent.EndValue;
            currentBeat = nextBeat;
        }

        return merged;
    }

    #endregion
}
