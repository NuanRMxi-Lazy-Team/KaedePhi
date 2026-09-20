#pragma warning disable CS0618

using KaedePhi.Tool.Compatibility;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;
using SkiaSharp;

namespace KaedePhi.Tool.Render.KaedePhi;

/// <summary>
/// 已弃用的 KPC 事件层渲染入口，行为与 <see cref="Intermediate.IrEventLayerRenderer"/> 一致。
/// </summary>
[Obsolete("已弃用：请迁移至 KaedePhi.Tool.Render.Intermediate.IrEventLayerRenderer。")]
public static class KpcEventLayerRenderer
{
    /// <summary>
    /// 将单个 KPC 事件层渲染为位图。
    /// </summary>
    /// <param name="layer">待渲染的事件层。</param>
    /// <param name="opts">渲染配置。</param>
    /// <returns>渲染结果位图。</returns>
    [Obsolete("已弃用：请迁移至 IrEventLayerRenderer.RenderEventLayer。")]
    public static SKBitmap RenderEventLayer(KpcEvents.EventLayer layer, KpcRenderOptions opts) =>
        Intermediate.IrEventLayerRenderer.RenderEventLayer(
            KpcCompatibilityMapper.ToIntermediate(layer),
            opts
        );
}
