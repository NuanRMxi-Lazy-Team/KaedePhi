#pragma warning disable CS0618

using KaedePhi.Tool.Common;
using KaedePhi.Tool.Compatibility;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;

namespace KaedePhi.Tool.Event.KaedePhi;

/// <summary>
/// 已弃用的 KPC 事件压缩器，行为与 <see cref="Intermediate.EventCompressor{TPayload}"/> 一致。
/// </summary>
/// <typeparam name="TPayload">事件值类型。</typeparam>
[Obsolete("已弃用：请迁移至 KaedePhi.Tool.Event.Intermediate.EventCompressor{TPayload}。")]
public class EventCompressor<TPayload>
    : Intermediate.EventCompressor<TPayload>,
        IEventCompressor<KpcEvents.Event<TPayload>>
    where TPayload : notnull
{
    /// <summary>
    /// 使用归一化垂直距离算法对 KPC 事件列表进行压缩。
    /// </summary>
    /// <param name="events">事件列表。</param>
    /// <param name="tolerance">容差百分比。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>压缩后的事件列表。</returns>
    [Obsolete("已弃用：请迁移至 EventCompressor{TPayload}.EventListCompressSqrt。")]
    public List<KpcEvents.Event<TPayload>> EventListCompressSqrt(
        List<KpcEvents.Event<TPayload>>? events,
        double tolerance,
        IProgress<ToolProgress>? progress = null
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.EventListCompressSqrt(
                KpcCompatibilityMapper.ToIntermediate(events),
                tolerance,
                progress
            )
        )!;

    /// <summary>
    /// 使用归一化斜率算法对 KPC 事件列表进行压缩。
    /// </summary>
    /// <param name="events">事件列表。</param>
    /// <param name="tolerance">容差百分比。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>压缩后的事件列表。</returns>
    [Obsolete("已弃用：请迁移至 EventCompressor{TPayload}.EventListCompressSlope。")]
    public List<KpcEvents.Event<TPayload>> EventListCompressSlope(
        List<KpcEvents.Event<TPayload>>? events,
        double tolerance,
        IProgress<ToolProgress>? progress = null
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.EventListCompressSlope(
                KpcCompatibilityMapper.ToIntermediate(events),
                tolerance,
                progress
            )
        )!;

    /// <summary>
    /// 移除列表中唯一且为默认值的事件。
    /// </summary>
    /// <param name="events">事件列表。</param>
    /// <returns>处理后的事件列表。</returns>
    [Obsolete("已弃用：请迁移至 EventCompressor{TPayload}.RemoveUselessEvent。")]
    public List<KpcEvents.Event<TPayload>> RemoveUselessEvent(
        List<KpcEvents.Event<TPayload>>? events
    ) => KpcCompatibilityMapper.ToKpc(base.RemoveUselessEvent(KpcCompatibilityMapper.ToIntermediate(events)))!;
}
