#pragma warning disable CS0618

using KaedePhi.Tool.Compatibility;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;

namespace KaedePhi.Tool.Event.KaedePhi;

/// <summary>
/// 已弃用的 KPC 事件拟合器，行为与 <see cref="Intermediate.EventFit{TPayload}"/> 一致。
/// </summary>
/// <typeparam name="TPayload">事件值类型。</typeparam>
[Obsolete("已弃用：请迁移至 KaedePhi.Tool.Event.Intermediate.EventFit{TPayload}。")]
public class EventFit<TPayload>
    : Intermediate.EventFit<TPayload>,
        IEventFit<KpcEvents.Event<TPayload>>
    where TPayload : notnull
{
    /// <summary>
    /// 将 KPC 事件列表中连续的线性事件序列拟合为带缓动函数的事件。
    /// </summary>
    /// <param name="events">待处理的事件列表，允许为 null 或空列表。</param>
    /// <param name="tolerance">容差百分比，取值范围 [0, 100]。</param>
    /// <returns>拟合后的事件列表。</returns>
    [Obsolete("已弃用：请迁移至 EventFit{TPayload}.FitEvents。")]
    public List<KpcEvents.Event<TPayload>> FitEvents(
        List<KpcEvents.Event<TPayload>>? events,
        double tolerance
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.FitEvents(KpcCompatibilityMapper.ToIntermediate(events), tolerance)
        )!;
}
