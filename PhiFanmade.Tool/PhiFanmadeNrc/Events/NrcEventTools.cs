using PhiFanmade.Core.Common;
using PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;

namespace PhiFanmade.Tool.PhiFanmadeNrc.Events;

/// <summary>
/// NRC 格式事件操作工具。提供事件列表的压缩与合并功能。
/// </summary>
public static class NrcEventTools
{
    /// <summary>
    /// 在指定拍范围内将事件按固定拍长切割。
    /// </summary>
    public static List<Nrc.Event<T>> CutEventsInRange<T>(
        List<Nrc.Event<T>> events,
        Beat startBeat,
        Beat endBeat,
        Beat cutLength)
        => EventCutter.CutEventsInRange(events, startBeat, endBeat, cutLength);

    /// <summary>
    /// 在指定拍范围内将事件按固定拍长切割。
    /// </summary>
    public static List<Nrc.Event<T>> CutEventsInRange<T>(
        List<Nrc.Event<T>> events,
        Beat startBeat,
        Beat endBeat,
        double cutLength)
        => EventCutter.CutEventsInRange(events, startBeat, endBeat, cutLength);

    /// <summary>
    /// 将单个事件切割为固定拍长线性事件
    /// </summary>
    public static List<Nrc.Event<T>> CutEventToLiner<T>(
        Nrc.Event<T> evt,
        Beat cutLength)
        => EventCutter.CutEventToLiner(evt, cutLength);

    /// <summary>
    /// 将单个事件切割为固定拍长线性事件
    /// </summary>
    public static List<Nrc.Event<T>> CutEventToLiner<T>(
        Nrc.Event<T> evt,
        double cutLength)
        => EventCutter.CutEventToLiner(evt, cutLength);

    /// <summary>
    /// 对事件列表做缓动拟合；仅会拟合连续线性事件，原有非线性事件会被保留。
    /// </summary>
    public static List<Nrc.Event<T>> EventListFit<T>(
        List<Nrc.Event<T>> events,
        double tolerance = 5d)
        => EventFit.EventListFit(events, tolerance);

    /// <summary>
    /// 对事件列表做缓动拟合（多核版）；maxDegreeOfParallelism 为并行线程数。
    /// </summary>
    public static List<Nrc.Event<T>> EventListFit<T>(
        List<Nrc.Event<T>> events,
        double tolerance,
        int? maxDegreeOfParallelism)
        => EventFit.EventListFit(events, tolerance, maxDegreeOfParallelism);

    /// <summary>
    /// 对事件列表做缓动拟合（异步版）。
    /// </summary>
    public static Task<List<Nrc.Event<T>>> EventListFitAsync<T>(
        List<Nrc.Event<T>> events,
        double tolerance = 5d,
        int? maxDegreeOfParallelism = null,
        CancellationToken cancellationToken = default)
        => EventFit.EventListFitAsync(events, tolerance, maxDegreeOfParallelism, cancellationToken);


    /// <summary>根据容差压缩事件列表，合并变化率相近的相邻线性事件。</summary>
    public static List<Nrc.Event<T>> EventListCompress<T>(
        List<Nrc.Event<T>> events, double tolerance = 5)
        => EventCompressor.EventListCompress(events, tolerance);

    /// <summary>
    /// 将两个事件列表合并（固定采样策略）。有重叠区间时按等长切片逐段相加，可选压缩。
    /// </summary>
    public static List<Nrc.Event<T>> EventListMerge<T>(
        List<Nrc.Event<T>> toEvents, List<Nrc.Event<T>> fromEvents,
        double precision = 64d, double tolerance = 5d, bool compress = true)
        => EventMerger.EventListMerge(toEvents, fromEvents, precision, tolerance, compress);

    /// <summary>
    /// 将两个事件列表合并（自适应采样策略）。性能更优，天然压缩，不支持禁用压缩。
    /// </summary>
    public static List<Nrc.Event<T>> EventMergePlus<T>(
        List<Nrc.Event<T>> toEvents, List<Nrc.Event<T>> fromEvents,
        double precision = 64d, double tolerance = 5d)
        => EventMerger.EventMergePlus(toEvents, fromEvents, precision, tolerance);
}