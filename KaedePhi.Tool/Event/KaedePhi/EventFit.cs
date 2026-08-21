using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Event.KaedePhi;

/// <summary>
/// 将连续的线性事件序列拟合为带缓动函数的单一事件。
/// </summary>
public class EventFit<TPayload> : LoggableBase, IEventFit<KpcEvents.Event<TPayload>>
    where TPayload : notnull
{
    private static readonly int[] AllEasingIds = [.. Enumerable.Range(1, 31)];

    /// <inheritdoc/>
    public List<KpcEvents.Event<TPayload>> FitEvents(
        List<KpcEvents.Event<TPayload>>? events,
        double tolerance
    )
    {
        EnsureSupportedNumericType();

        if (events == null || events.Count == 0)
            return [];

        // 检查是否已排序，避免不必要的列表分配
        var sortedEvents = IsSortedByStartBeat(events)
            ? events
            : events.OrderBy(e => e.StartBeat).ToList();

        return FitEventsCore(sortedEvents, tolerance);
    }

    /// <summary>
    /// 检查事件列表是否已按 StartBeat 升序排列。
    /// </summary>
    private static bool IsSortedByStartBeat(List<KpcEvents.Event<TPayload>> events)
    {
        for (var i = 1; i < events.Count; i++)
        {
            if (events[i].StartBeat < events[i - 1].StartBeat)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 遍历已排序的事件序列，将连续的同方向线性事件分组后贪心拟合；
    /// 常量事件和非线性事件直接原样写入输出。
    /// </summary>
    private static List<KpcEvents.Event<TPayload>> FitEventsCore(
        List<KpcEvents.Event<TPayload>> sortedEvents,
        double tolerance
    )
    {
        var result = new List<KpcEvents.Event<TPayload>>();
        var i = 0;

        while (i < sortedEvents.Count)
        {
            var current = sortedEvents[i];

            // 常量事件或非线性事件不参与拟合，直接输出
            if (IsNumericConstant(current) || !IsLinear(current))
            {
                result.Add(current.Clone());
                i++;
                continue;
            }

            var runEnd = CollectRunEnd(sortedEvents, i);
            FitRunInto(sortedEvents, i, runEnd, result, tolerance);
            i = runEnd;
        }

        return result;
    }

    /// <summary>
    /// 从指定位置起收集一段时间连续、数值连续、方向一致的线性事件序列的结束索引（不含）。
    /// </summary>
    private static int CollectRunEnd(List<KpcEvents.Event<TPayload>> events, int start)
    {
        var firstDir = GetDirection(
            events[start].GetStartValueAsDouble(),
            events[start].GetEndValueAsDouble()
        );

        var nextIndex = start + 1;
        while (nextIndex < events.Count)
        {
            var next = events[nextIndex];
            if (IsNumericConstant(next))
                break;
            if (!IsLinear(next))
                break;
            if (!IsContiguous(events[nextIndex - 1], next))
                break;
            if (GetDirection(next.GetStartValueAsDouble(), next.GetEndValueAsDouble()) != firstDir)
                break;

            nextIndex++;
        }

        return nextIndex;
    }

    /// <summary>
    /// 对 run 中的事件序列进行贪心迭代拟合：
    /// 优先尝试整段拟合，失败后逐步缩短前缀，直到每段都能拟合或退化为单个事件。
    /// 使用索引范围避免 GetRange 分配。
    /// </summary>
    private static void FitRunInto(
        List<KpcEvents.Event<TPayload>> source,
        int runStart,
        int runEnd,
        List<KpcEvents.Event<TPayload>> target,
        double tolerance
    )
    {
        var currentStart = runStart;

        while (currentStart < runEnd)
        {
            var remainingCount = runEnd - currentStart;
            if (remainingCount == 1)
            {
                target.Add(source[currentStart].Clone());
                return;
            }

            // 尝试将当前剩余序列整体拟合为单一缓动事件
            var fitted = TryFitEasing(source, currentStart, runEnd, tolerance);
            if (fitted != null)
            {
                target.Add(fitted);
                return;
            }

            // 整体拟合失败，寻找可拟合的最长前缀
            var found = false;
            for (var splitLen = remainingCount - 1; splitLen >= 2; splitLen--)
            {
                fitted = TryFitEasing(source, currentStart, currentStart + splitLen, tolerance);
                if (fitted is null)
                    continue;
                target.Add(fitted);
                currentStart += splitLen;
                found = true;
                break;
            }

            if (found)
                continue;
            // 无法拟合任何长度 >= 2 的前缀，输出第一个事件并继续处理剩余
            target.Add(source[currentStart].Clone());
            currentStart++;
        }
    }

    /// <summary>
    /// 遍历所有支持的缓动函数，返回第一个在容差范围内能覆盖所有事件边界的拟合结果；
    /// 无法拟合时返回 null。使用索引范围避免 List 分配。
    /// </summary>
    private static KpcEvents.Event<TPayload>? TryFitEasing(
        List<KpcEvents.Event<TPayload>> events,
        int start,
        int end,
        double tolerance
    )
    {
        var count = end - start;
        if (count <= 1)
            return null;

        var first = events[start];
        var last = events[end - 1];
        var startBeat = first.StartBeat;
        var endBeat = last.EndBeat;
        var startValue = first.GetStartValueAsDouble();
        var endValue = last.GetEndValueAsDouble();

        return AllEasingIds
            .Select(easingId =>
                CreateMergedEvent(first, startBeat, startValue, endBeat, endValue, easingId)
            )
            .FirstOrDefault(candidate =>
                FitsWithinTolerance(
                    candidate,
                    events,
                    start,
                    end,
                    tolerance,
                    startBeat,
                    endBeat,
                    startValue,
                    endValue
                )
            );
    }

    /// <summary>
    /// 在所有原始事件的起止边界处采样候选缓动，验证每处的相对误差百分比均不超过容差。
    /// 相对误差（%）= 绝对偏差 / 整段值域跨度 × 100。使用索引范围避免 List 分配。
    /// </summary>
    private static bool FitsWithinTolerance(
        KpcEvents.Event<TPayload> candidate,
        List<KpcEvents.Event<TPayload>> events,
        int start,
        int end,
        double tolerance,
        Beat segStartBeat,
        Beat segEndBeat,
        double segStartValue,
        double segEndValue
    )
    {
        var segSpan = segEndBeat - (double)segStartBeat;
        if (segSpan <= 1e-9)
            return true;

        var valueDelta = segEndValue - segStartValue;
        var valueRange = Math.Abs(valueDelta);

        // 值域近似为零时无法定义相对误差，视为拟合成功
        if (valueRange <= 1e-9)
            return true;

        for (var i = start; i < end; i++)
        {
            var evt = events[i];
            var evtStart = (double)evt.StartBeat;
            var evtEnd = (double)evt.EndBeat;

            var normStart = (evtStart - segStartBeat) / segSpan;
            var normEnd = (evtEnd - segStartBeat) / segSpan;

            var easedStart =
                segStartValue + valueDelta * GetEasingValue(candidate.Easing, normStart);
            var easedEnd = segStartValue + valueDelta * GetEasingValue(candidate.Easing, normEnd);

            var srcStartVal = evt.GetStartValueAsDouble();
            var srcEndVal = evt.GetEndValueAsDouble();

            // 相对误差（%）：绝对偏差 / 值域 × 100 与容差百分比比较
            if (
                Math.Abs(easedStart - srcStartVal) / valueRange * 100.0 > tolerance
                || Math.Abs(easedEnd - srcEndVal) / valueRange * 100.0 > tolerance
            )
                return false;
        }

        return true;
    }

    /// <summary>
    /// 获取指定缓动函数在归一化时间 t 处的输出值（范围 [0, 1]）。
    /// </summary>
    private static double GetEasingValue(Kpc.Easing easing, double t)
    {
        return Kpc.Easings.Evaluate(easing, 0d, 1d, t);
    }

    /// <summary>
    /// 判断两个事件是否在时间上首尾相接且数值连续。
    /// </summary>
    private static bool IsContiguous(
        KpcEvents.Event<TPayload> first,
        KpcEvents.Event<TPayload> second
    )
    {
        return first.EndBeat == second.StartBeat
            && Math.Abs(first.GetEndValueAsDouble() - second.GetStartValueAsDouble()) < 1e-9;
    }

    /// <summary>
    /// 判断事件起止值是否相同（常量事件）。
    /// </summary>
    private static bool IsNumericConstant(KpcEvents.Event<TPayload> evt)
    {
        return Math.Abs(evt.GetStartValueAsDouble() - evt.GetEndValueAsDouble()) < 1e-9;
    }

    /// <summary>
    /// 判断事件是否使用可参与拟合的线性缓动。
    /// </summary>
    private static bool IsLinear(KpcEvents.Event<TPayload> evt)
    {
        return !evt.IsBezier && (int)evt.Easing == 1 && evt.Font is null;
    }

    /// <summary>
    /// 返回数值从 start 到 end 的变化方向：递增为 1，递减为 -1，不变为 0。
    /// </summary>
    private static int GetDirection(double start, double end)
    {
        var diff = end - start;
        if (Math.Abs(diff) < 1e-9)
            return 0;
        return diff > 0 ? 1 : -1;
    }

    /// <summary>
    /// 使用指定缓动函数创建覆盖给定时间区间和值域的新事件。
    /// Font 字段从模板事件继承，其余字段均为合并后的值。
    /// </summary>
    private static KpcEvents.Event<TPayload> CreateMergedEvent(
        KpcEvents.Event<TPayload> template,
        Beat startBeat,
        double startValue,
        Beat endBeat,
        double endValue,
        int easingId
    )
    {
        var evt = new KpcEvents.Event<TPayload>
        {
            StartBeat = new Beat((int[])startBeat),
            EndBeat = new Beat((int[])endBeat),
            Easing = new Kpc.Easing(easingId),
            Font = template.Font,
        };

        if (typeof(TPayload) == typeof(double))
        {
            evt.StartValue = (TPayload)(object)startValue;
            evt.EndValue = (TPayload)(object)endValue;
        }
        else if (typeof(TPayload) == typeof(float))
        {
            evt.StartValue = (TPayload)(object)(float)startValue;
            evt.EndValue = (TPayload)(object)(float)endValue;
        }
        else if (typeof(TPayload) == typeof(int))
        {
            evt.StartValue = (TPayload)(object)(int)startValue;
            evt.EndValue = (TPayload)(object)(int)endValue;
        }

        return evt;
    }

    private static void EnsureSupportedNumericType()
    {
        if (
            typeof(TPayload) != typeof(int)
            && typeof(TPayload) != typeof(float)
            && typeof(TPayload) != typeof(double)
        )
            throw new NotSupportedException("EventFit only supports int, float, and double types.");
    }
}
