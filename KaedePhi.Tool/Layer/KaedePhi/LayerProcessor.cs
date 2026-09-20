#pragma warning disable CS0618

using KaedePhi.Tool.Common;
using KaedePhi.Tool.Compatibility;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;

namespace KaedePhi.Tool.Layer.KaedePhi;

/// <summary>
/// 已弃用的 KPC 谱面事件层处理器，行为与 <see cref="Intermediate.LayerProcessor"/> 一致。
/// </summary>
[Obsolete("已弃用：请迁移至 KaedePhi.Tool.Layer.Intermediate.LayerProcessor。")]
public class LayerProcessor
    : Intermediate.LayerProcessor,
        ILayerProcessor<KpcEvents.EventLayer>
{
    /// <summary>
    /// 将多个事件层合并为单层（固定采样）。
    /// </summary>
    /// <param name="layers">待合并的事件层列表。</param>
    /// <param name="precision">每拍内的采样步数。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>合并后的单个事件层。</returns>
    [Obsolete("已弃用：请迁移至 LayerProcessor.LayerMerge。")]
    public KpcEvents.EventLayer LayerMerge(
        List<KpcEvents.EventLayer> layers,
        double precision,
        IProgress<ToolProgress>? progress = null
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.LayerMerge(KpcCompatibilityMapper.ToIntermediate(layers), precision, progress)
        );

    /// <summary>
    /// 将多个事件层合并为单层（自适应采样）。
    /// </summary>
    /// <param name="layers">待合并的事件层列表。</param>
    /// <param name="precision">自适应采样的最大步数上限。</param>
    /// <param name="tolerance">误差容差百分比。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>合并后的单个事件层。</returns>
    [Obsolete("已弃用：请迁移至 LayerProcessor.LayerMergePlus。")]
    public KpcEvents.EventLayer LayerMergePlus(
        List<KpcEvents.EventLayer> layers,
        double precision,
        double tolerance,
        IProgress<ToolProgress>? progress = null
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.LayerMergePlus(
                KpcCompatibilityMapper.ToIntermediate(layers),
                precision,
                tolerance,
                progress
            )
        );

    /// <summary>
    /// 将单个事件层中各通道事件按指定精度切割为等长段。
    /// </summary>
    /// <param name="layer">待切割的事件层。</param>
    /// <param name="precision">每拍内的切割步数。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>切割后的事件层。</returns>
    [Obsolete("已弃用：请迁移至 LayerProcessor.CutLayerEvents。")]
    public KpcEvents.EventLayer CutLayerEvents(
        KpcEvents.EventLayer? layer,
        double precision,
        IProgress<ToolProgress>? progress = null
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.CutLayerEvents(
                layer is null ? null : KpcCompatibilityMapper.ToIntermediate(layer),
                precision,
                progress
            )
        );

    /// <summary>
    /// 将多个事件层中各通道事件按指定精度切割为等长段。
    /// </summary>
    /// <param name="layers">待切割的事件层列表。</param>
    /// <param name="precision">每拍内的切割步数。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>切割后的事件层列表。</returns>
    [Obsolete("已弃用：请迁移至 LayerProcessor.CutLayerEvents。")]
    public List<KpcEvents.EventLayer> CutLayerEvents(
        List<KpcEvents.EventLayer> layers,
        double precision,
        IProgress<ToolProgress>? progress = null
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.CutLayerEvents(
                KpcCompatibilityMapper.ToIntermediate(layers),
                precision,
                progress
            )
        );

    /// <summary>
    /// 压缩事件层中各通道的事件列表，原地修改传入的事件层。
    /// </summary>
    /// <param name="layer">待压缩的事件层（原地修改）。</param>
    /// <param name="tolerance">容差百分比。</param>
    /// <param name="progress">进度回调。</param>
    [Obsolete("已弃用：请迁移至 LayerProcessor.LayerEventsCompress。")]
    public void LayerEventsCompress(
        KpcEvents.EventLayer layer,
        double tolerance,
        IProgress<ToolProgress>? progress = null
    )
    {
        var mapped = KpcCompatibilityMapper.ToIntermediate(layer);
        base.LayerEventsCompress(mapped, tolerance, progress);
        var result = KpcCompatibilityMapper.ToKpc(mapped);
        layer.MoveXEvents = result.MoveXEvents;
        layer.MoveYEvents = result.MoveYEvents;
        layer.RotateEvents = result.RotateEvents;
        layer.AlphaEvents = result.AlphaEvents;
        layer.SpeedEvents = result.SpeedEvents;
    }
}
