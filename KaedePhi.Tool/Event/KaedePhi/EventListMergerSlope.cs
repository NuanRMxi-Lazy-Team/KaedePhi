#pragma warning disable CS0618

using KaedePhi.Tool.Compatibility;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;

namespace KaedePhi.Tool.Event.KaedePhi;

/// <summary>
/// 已弃用的 KPC 斜率自适应事件列表合并器，行为与 <see cref="Intermediate.EventListMergerSlope{TPayload}"/> 一致。
/// </summary>
/// <typeparam name="TPayload">事件值类型。</typeparam>
[Obsolete("已弃用：请迁移至 KaedePhi.Tool.Event.Intermediate.EventListMergerSlope{TPayload}。")]
public class EventListMergerSlope<TPayload>
    : Intermediate.EventListMergerSlope<TPayload>,
        IEventListMerger<KpcEvents.Event<TPayload>>
    where TPayload : notnull
{
    /// <summary>
    /// 将来源 KPC 事件列表叠加到目标列表上，返回合并后的新事件列表。
    /// </summary>
    /// <param name="toEvents">目标轨道事件列表。</param>
    /// <param name="fromEvents">来源轨道事件列表。</param>
    /// <param name="precision">重叠区段的切片精度（每拍切片数）。</param>
    /// <returns>叠加后的新事件列表，已按起始拍升序排序。</returns>
    [Obsolete("已弃用：请迁移至 EventListMergerSlope{TPayload}.EventListMerge。")]
    public List<KpcEvents.Event<TPayload>> EventListMerge(
        List<KpcEvents.Event<TPayload>>? toEvents,
        List<KpcEvents.Event<TPayload>>? fromEvents,
        double precision
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.EventListMerge(
                KpcCompatibilityMapper.ToIntermediate(toEvents),
                KpcCompatibilityMapper.ToIntermediate(fromEvents),
                precision
            )
        )!;

    /// <summary>
    /// 将来源 KPC 事件列表自适应叠加到目标列表上，返回合并后的新事件列表。
    /// </summary>
    /// <param name="toEvents">目标轨道事件列表。</param>
    /// <param name="fromEvents">来源轨道事件列表。</param>
    /// <param name="precision">自适应采样的最大步数上限。</param>
    /// <param name="tolerance">误差容差百分比。</param>
    /// <returns>叠加后的新事件列表，已按起始拍升序排序。</returns>
    [Obsolete("已弃用：请迁移至 EventListMergerSlope{TPayload}.EventListMerge。")]
    public List<KpcEvents.Event<TPayload>> EventListMerge(
        List<KpcEvents.Event<TPayload>>? toEvents,
        List<KpcEvents.Event<TPayload>>? fromEvents,
        double precision,
        double tolerance
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.EventListMerge(
                KpcCompatibilityMapper.ToIntermediate(toEvents),
                KpcCompatibilityMapper.ToIntermediate(fromEvents),
                precision,
                tolerance
            )
        )!;
}
