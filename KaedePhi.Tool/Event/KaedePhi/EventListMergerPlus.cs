using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Event.KaedePhi;

/// <summary>
/// 事件列表合并器（自适应采样）。
/// <para>
/// 继承 <see cref="EventListMerger{TPayload}"/> 的共享基础设施与固定采样合并，
/// 新增自适应采样合并入口 <see cref="EventListMerge(System.Collections.Generic.List{global::KaedePhi.Core.KaedePhi.Events.Event{TPayload}},System.Collections.Generic.List{global::KaedePhi.Core.KaedePhi.Events.Event{TPayload}},double,double)"/>：
/// 仅在两轨道的事件边界处或叠加值偏离线性近似超过容差时才插入新分段，
/// 在保证精度的前提下减少冗余事件数量。
/// </para>
/// <para>
/// 子类（如 <see cref="EventListMergerSlope{TPayload}"/>、<see cref="EventListMergerSqrt{TPayload}"/>）
/// 可覆盖 <see cref="ShouldSplitAdaptiveSegment"/> 切换至其他分段判定算法。
/// </para>
/// </summary>
/// <typeparam name="TPayload">事件值类型（<see langword="int"/>、<see langword="float"/> 或 <see langword="double"/>）。</typeparam>
public class EventListMergerPlus<TPayload> : EventListMerger<TPayload>
    where TPayload : notnull
{
    #region 入口

    /// <summary>
    /// 自适应采样合并：以事件边界为强制切割点，仅在误差超过容差时插入新分段。
    /// 相较基类的固定采样可减少冗余事件数量。
    /// </summary>
    public List<KpcEvents.Event<TPayload>> EventListMerge(
        List<KpcEvents.Event<TPayload>>? toEvents,
        List<KpcEvents.Event<TPayload>>? fromEvents,
        double precision,
        double tolerance
    )
    {
        ChartProcessingValidator.ValidatePrecision(precision);
        ChartProcessingValidator.ValidateTolerance(tolerance);
        if (TryGetMergeEarlyReturn(toEvents, fromEvents, out var earlyReturn))
            return earlyReturn;
        if (toEvents == null || fromEvents == null)
            return [];

        EnsureSupportedNumericType();

        var toEventsCopy = CloneEventList(toEvents);
        var fromEventsCopy = CloneEventList(fromEvents);
        SortByStartBeat(toEventsCopy);
        SortByStartBeat(fromEventsCopy);

        if (!HasOverlap(toEventsCopy, fromEventsCopy))
            return MergeWithoutOverlap(toEventsCopy, fromEventsCopy);

        return MergeWithOverlapAdaptiveSampling(
            toEvents,
            toEventsCopy,
            fromEventsCopy,
            precision,
            tolerance
        );
    }

    #endregion

    #region 自适应采样路径

    /// <summary>
    /// 通过自适应采样合并重叠区间，减少冗余切片。
    /// </summary>
    private List<KpcEvents.Event<TPayload>> MergeWithOverlapAdaptiveSampling(
        List<KpcEvents.Event<TPayload>> toEventsForOffsetLookup,
        List<KpcEvents.Event<TPayload>> toEventsCopy,
        List<KpcEvents.Event<TPayload>> fromEventsCopy,
        double precision,
        double tolerance
    )
    {
        var overlapIntervals = BuildOverlapIntervals(toEventsCopy, fromEventsCopy);
        var newEvents = BuildBaseEventsOutsideOverlap(
            toEventsCopy,
            fromEventsCopy,
            toEventsForOffsetLookup,
            overlapIntervals
        );

        newEvents.AddRange(
            MergeAdaptiveIntervals(
                toEventsCopy,
                fromEventsCopy,
                overlapIntervals,
                precision,
                tolerance
            )
        );

        SortByStartBeat(newEvents);
        return newEvents;
    }

    /// <summary>
    /// 逐个重叠区间执行自适应合并。
    /// </summary>
    private List<KpcEvents.Event<TPayload>> MergeAdaptiveIntervals(
        List<KpcEvents.Event<TPayload>> toEventsCopy,
        List<KpcEvents.Event<TPayload>> fromEventsCopy,
        List<(Beat Start, Beat End)> overlapIntervals,
        double precision,
        double tolerance
    )
    {
        var cutLength = new Beat(1d / precision);
        var result = new List<KpcEvents.Event<TPayload>>();
        foreach (var (start, end) in overlapIntervals)
        {
            result.AddRange(
                MergeAdaptiveSingleInterval(
                    toEventsCopy,
                    fromEventsCopy,
                    start,
                    end,
                    cutLength,
                    tolerance
                )
            );
        }

        return result;
    }

    /// <summary>
    /// 在单个区间内按误差阈值动态分段，生成近似线性片段。
    /// <para>
    /// 先收集区间内所有事件的起止拍作为强制切割关键帧（与
    /// FatherUnbindHelpers.CollectKeyBeats 等价），再逐子区间调用
    /// <see cref="AdaptiveSampleSubInterval"/>。
    /// 关键帧之间的子区间长度等于单个事件的持续时间，使得容差评估中的
    /// 进度参数 p = (nextBeat − segStart) / (subEnd − segStart)
    /// 与 Unbind 链路保持同量级，从根本上消除"远端参考点"导致的灵敏度衰减。
    /// </para>
    /// </summary>
    private List<KpcEvents.Event<TPayload>> MergeAdaptiveSingleInterval(
        List<KpcEvents.Event<TPayload>> toEventsCopy,
        List<KpcEvents.Event<TPayload>> fromEventsCopy,
        Beat start,
        Beat end,
        Beat cutLength,
        double tolerance
    )
    {
        var result = new List<KpcEvents.Event<TPayload>>();

        var keyBeats = CollectMergeKeyBeats(toEventsCopy, fromEventsCopy, start, end);

        for (var ki = 0; ki < keyBeats.Count - 1; ki++)
        {
            var subStart = keyBeats[ki];
            var subEnd = keyBeats[ki + 1];
            if (subStart >= subEnd)
                continue;

            result.AddRange(
                AdaptiveSampleSubInterval(
                    toEventsCopy,
                    fromEventsCopy,
                    subStart,
                    subEnd,
                    cutLength,
                    tolerance
                )
            );
        }

        return result;
    }

    /// <summary>
    /// 对两相邻事件边界之间的子区间执行自适应采样。
    /// 容差评估参考端点固定为 <paramref name="subEnd"/>（子区间终点），
    /// 与 FatherUnbindHelpers.AdaptiveSampleInterval 结构完全对齐。
    /// </summary>
    private List<KpcEvents.Event<TPayload>> AdaptiveSampleSubInterval(
        List<KpcEvents.Event<TPayload>> toEventsCopy,
        List<KpcEvents.Event<TPayload>> fromEventsCopy,
        Beat subStart,
        Beat subEnd,
        Beat cutLength,
        double tolerance
    )
    {
        var result = new List<KpcEvents.Event<TPayload>>();

        var lastToValue = GetPreviousEndValue(toEventsCopy, subStart);
        var lastFormValue = GetPreviousEndValue(fromEventsCopy, subStart);

        var toEventAtCurrent = GetActiveEventAtBeat(toEventsCopy, subStart);
        var formEventAtCurrent = GetActiveEventAtBeat(fromEventsCopy, subStart);
        var toValAtCurrent =
            toEventAtCurrent != null ? toEventAtCurrent.GetValueAtBeat(subStart) : lastToValue;
        var formValAtCurrent =
            formEventAtCurrent != null
                ? formEventAtCurrent.GetValueAtBeat(subStart)
                : lastFormValue;

        var segmentStart = subStart;
        var segmentStartToValue = toValAtCurrent;
        var segmentStartFormValue = formValAtCurrent;
        var segmentStartSum = AddValues(toValAtCurrent, formValAtCurrent);

        var subEndSum = AddValues(
            GetValueAtBeatOrPreviousEnd(toEventsCopy, subEnd),
            GetValueAtBeatOrPreviousEnd(fromEventsCopy, subEnd)
        );

        for (var cur = subStart; cur < subEnd; )
        {
            var nextBeat = cur + cutLength;
            if (nextBeat > subEnd)
                nextBeat = subEnd;
            var isLast = nextBeat >= subEnd;

            var toEventAtNext = GetActiveEventAtBeat(toEventsCopy, nextBeat);
            var formEventAtNext = GetActiveEventAtBeat(fromEventsCopy, nextBeat);

            var (toValueAtNext, toValUpdate) = GetNextBeatValues(
                toEventsCopy,
                toEventAtCurrent,
                toEventAtNext,
                nextBeat
            );
            var (formValueAtNext, formValUpdate) = GetNextBeatValues(
                fromEventsCopy,
                formEventAtCurrent,
                formEventAtNext,
                nextBeat
            );

            var sumAtNext = AddValues(toValueAtNext, formValueAtNext);

            if (
                isLast
                || ShouldSplitAdaptiveSegment(
                    segmentStart,
                    nextBeat,
                    subEnd,
                    segmentStartSum,
                    sumAtNext,
                    subEndSum,
                    tolerance
                )
            )
            {
                AddSegmentEvent(
                    result,
                    segmentStart,
                    nextBeat,
                    segmentStartToValue,
                    segmentStartFormValue,
                    toValueAtNext,
                    formValueAtNext
                );
                segmentStart = nextBeat;
                segmentStartToValue = toValUpdate;
                segmentStartFormValue = formValUpdate;
                segmentStartSum = AddValues(toValUpdate, formValUpdate);
            }

            toEventAtCurrent = toEventAtNext;
            formEventAtCurrent = formEventAtNext;
            cur = nextBeat;
        }

        return result;
    }

    #endregion

    #region 自适应采样工具方法

    /// <summary>
    /// 收集两组事件在区间内的所有起止拍，作为自适应采样的强制切割关键帧。
    /// </summary>
    private static List<Beat> CollectMergeKeyBeats(
        List<KpcEvents.Event<TPayload>> toEvents,
        List<KpcEvents.Event<TPayload>> fromEvents,
        Beat start,
        Beat end
    )
    {
        var beats = new List<Beat> { start, end };
        foreach (var e in toEvents)
        {
            if (e.StartBeat > start && e.StartBeat < end)
                beats.Add(e.StartBeat);
            if (e.EndBeat > start && e.EndBeat < end)
                beats.Add(e.EndBeat);
        }

        foreach (var e in fromEvents)
        {
            if (e.StartBeat > start && e.StartBeat < end)
                beats.Add(e.StartBeat);
            if (e.EndBeat > start && e.EndBeat < end)
                beats.Add(e.EndBeat);
        }

        return beats.Distinct().OrderBy(b => b).ToList();
    }

    /// <summary>
    /// 二分查找在指定拍处活跃的事件（最后一个 StartBeat &lt;= beat 且 EndBeat &gt;= beat 的事件）。
    /// 要求 events 已按 StartBeat 排序。O(log n) 复杂度。
    /// </summary>
    private static KpcEvents.Event<TPayload>? GetActiveEventAtBeat(
        List<KpcEvents.Event<TPayload>> events,
        Beat beat
    )
    {
        var lo = 0;
        var hi = events.Count - 1;
        KpcEvents.Event<TPayload>? result = null;

        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var evt = events[mid];

            if (evt.StartBeat <= beat)
            {
                if (evt.EndBeat >= beat)
                    result = evt;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return result;
    }

    private static TPayload? GetPreviousEndValue(List<KpcEvents.Event<TPayload>> events, Beat beat)
    {
        var prev = events.FindLast(e => e.EndBeat <= beat);
        return prev != null ? prev.EndValue : default;
    }

    private static TPayload? GetValueAtBeatOrPreviousEnd(
        List<KpcEvents.Event<TPayload>> events,
        Beat beat
    )
    {
        var active = GetActiveEventAtBeat(events, beat);
        return active != null ? active.GetValueAtBeat(beat) : GetPreviousEndValue(events, beat);
    }

    private static (TPayload? Outgoing, TPayload? Incoming) GetNextBeatValues(
        List<KpcEvents.Event<TPayload>> events,
        KpcEvents.Event<TPayload>? eventAtCurrent,
        KpcEvents.Event<TPayload>? eventAtNext,
        Beat nextBeat
    )
    {
        var prevEnd = GetPreviousEndValue(events, nextBeat);
        var outgoing =
            (eventAtCurrent is not null && eventAtCurrent.EndBeat >= nextBeat)
                ? eventAtCurrent.GetValueAtBeat(nextBeat)
                : prevEnd;
        var incoming = eventAtNext is not null ? eventAtNext.GetValueAtBeat(nextBeat) : prevEnd;
        return (outgoing, incoming);
    }

    /// <summary>
    /// 判断当前自适应分段是否需要切分（默认实现：子区间 swing 为比例尺 + 欧氏垂直距离）。
    /// <para>
    /// 子类可通过覆盖本方法切换至其他算法（如 EventListMergerSlope 的斜率差方案
    /// 或 EventListMergerSqrt 的子段 swing 方案）。
    /// </para>
    /// </summary>
    protected virtual bool ShouldSplitAdaptiveSegment(
        Beat segmentStart,
        Beat nextBeat,
        Beat intervalEnd,
        TPayload? segmentStartSum,
        TPayload? sumAtNext,
        TPayload? sumAtEnd,
        double tolerance
    )
    {
        if (nextBeat >= intervalEnd)
            return true;
        if (nextBeat <= segmentStart)
            return false;

        var dtTotal = (double)(intervalEnd - segmentStart);
        var dtLocal = (double)(nextBeat - segmentStart);
        if (dtTotal <= 1e-12 || dtLocal <= 1e-12)
            return false;

        var p = Math.Clamp(dtLocal / dtTotal, 0.0, 1.0);

        var startNum = ToDouble(segmentStartSum);
        var nextNum = ToDouble(sumAtNext);
        var endNum = ToDouble(sumAtEnd);

        var scale = Math.Max(Math.Abs(endNum - startNum), 1e-3);

        var dvNorm = (endNum - startNum) / scale;
        var byNorm = (nextNum - startNum) / scale;
        var det = byNorm - dvNorm * p;
        var len = Math.Sqrt(1.0 + dvNorm * dvNorm);
        var normalizedDist = Math.Abs(det) / len;

        return normalizedDist > Math.Max(0d, tolerance) / 100.0;
    }

    protected static double ToDouble(TPayload? value)
    {
        return value is null
            ? throw new InvalidOperationException("Unexpected null numeric value.")
            : NumericHelper.ToDouble(value);
    }

    private static TPayload AddValues(TPayload? left, TPayload? right) =>
        NumericHelper.Add(left, right);

    private static void AddSegmentEvent(
        List<KpcEvents.Event<TPayload>> target,
        Beat startBeat,
        Beat endBeat,
        TPayload? startToValue,
        TPayload? startFormValue,
        TPayload? endToValue,
        TPayload? endFormValue
    )
    {
        target.Add(
            new KpcEvents.Event<TPayload>
            {
                StartBeat = startBeat,
                EndBeat = endBeat,
                StartValue = AddValues(startToValue, startFormValue),
                EndValue = AddValues(endToValue, endFormValue),
            }
        );
    }

    #endregion
}
