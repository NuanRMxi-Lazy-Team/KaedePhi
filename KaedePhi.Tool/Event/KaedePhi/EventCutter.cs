#pragma warning disable CS0618

using KaedePhi.Tool.Compatibility;
using KpcCommon = KaedePhi.Core.Common;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;

namespace KaedePhi.Tool.Event.KaedePhi;

/// <summary>
/// 已弃用的 KPC 事件切割器，行为与 <see cref="Intermediate.EventCutter{TPayload}"/> 一致。
/// </summary>
/// <typeparam name="TPayload">事件值类型。</typeparam>
[Obsolete("已弃用：请迁移至 KaedePhi.Tool.Event.Intermediate.EventCutter{TPayload}。")]
public class EventCutter<TPayload>
    : Intermediate.EventCutter<TPayload>,
        IEventCutter<KpcEvents.Event<TPayload>, KpcCommon.Beat>
    where TPayload : notnull
{
    /// <summary>
    /// 将 KPC 事件列表中，在指定节拍范围内切割为等长事件。
    /// </summary>
    /// <param name="events">事件列表。</param>
    /// <param name="startBeat">起始拍。</param>
    /// <param name="endBeat">结束拍。</param>
    /// <param name="cutLength">切割长度（拍）。</param>
    /// <returns>处理后的事件列表。</returns>
    [Obsolete("已弃用：请迁移至 EventCutter{TPayload}.CutEventsInRange。")]
    public List<KpcEvents.Event<TPayload>> CutEventsInRange(
        List<KpcEvents.Event<TPayload>> events,
        KpcCommon.Beat startBeat,
        KpcCommon.Beat endBeat,
        KpcCommon.Beat cutLength
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.CutEventsInRange(
                KpcCompatibilityMapper.ToIntermediate(events)!,
                KpcCompatibilityMapper.ToShared(startBeat),
                KpcCompatibilityMapper.ToShared(endBeat),
                KpcCompatibilityMapper.ToShared(cutLength)
            )
        )!;

    /// <summary>
    /// 将 KPC 事件列表中，在指定节拍范围内切割为等长事件。
    /// </summary>
    /// <param name="events">事件列表。</param>
    /// <param name="startBeat">起始拍。</param>
    /// <param name="endBeat">结束拍。</param>
    /// <param name="cutLength">切割长度（拍）。</param>
    /// <returns>处理后的事件列表。</returns>
    [Obsolete("已弃用：请迁移至 EventCutter{TPayload}.CutEventsInRange。")]
    public List<KpcEvents.Event<TPayload>> CutEventsInRange(
        List<KpcEvents.Event<TPayload>> events,
        KpcCommon.Beat startBeat,
        KpcCommon.Beat endBeat,
        double cutLength
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.CutEventsInRange(
                KpcCompatibilityMapper.ToIntermediate(events)!,
                KpcCompatibilityMapper.ToShared(startBeat),
                KpcCompatibilityMapper.ToShared(endBeat),
                cutLength
            )
        )!;

    /// <summary>
    /// 将 KPC 事件切割为多个等长事件。
    /// </summary>
    /// <param name="evt">事件。</param>
    /// <param name="cutLength">切割长度（拍）。</param>
    /// <returns>处理后的事件列表。</returns>
    [Obsolete("已弃用：请迁移至 EventCutter{TPayload}.CutEventToLinear。")]
    public List<KpcEvents.Event<TPayload>> CutEventToLinear(
        KpcEvents.Event<TPayload> evt,
        KpcCommon.Beat cutLength
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.CutEventToLinear(
                KpcCompatibilityMapper.ToIntermediate(evt),
                KpcCompatibilityMapper.ToShared(cutLength)
            )
        )!;

    /// <summary>
    /// 将 KPC 事件切割为多个等长事件。
    /// </summary>
    /// <param name="evt">事件。</param>
    /// <param name="cutLength">切割长度（拍）。</param>
    /// <returns>处理后的事件列表。</returns>
    [Obsolete("已弃用：请迁移至 EventCutter{TPayload}.CutEventToLinear。")]
    public List<KpcEvents.Event<TPayload>> CutEventToLinear(
        KpcEvents.Event<TPayload> evt,
        double cutLength
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.CutEventToLinear(KpcCompatibilityMapper.ToIntermediate(evt), cutLength)
        )!;
}
