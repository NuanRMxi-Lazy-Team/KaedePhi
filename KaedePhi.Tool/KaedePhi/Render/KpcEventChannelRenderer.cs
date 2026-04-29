using KaedePhi.Core.Common;
using SkiaSharp;
using EventLayer = KaedePhi.Core.KaedePhi.EventLayer;

namespace KaedePhi.Tool.KaedePhi.Render;

/// <summary>
/// 将 NRC EventLayer 中各通道事件渲染为图片的静态工具。
/// </summary>
[Obsolete("请改用 KaedePhi.Tool.Render.KaedePhi.KpcEventChannelRenderer")]
public static class KpcEventChannelRenderer
{
    /// <summary>渲染单个 EventLayer 的所有通道，返回 SKBitmap。</summary>
    [Obsolete("请改用 KaedePhi.Tool.Render.KaedePhi.KpcEventChannelRenderer.RenderEventLayer")]
    public static SKBitmap RenderEventLayer(EventLayer layer, RenderOptions opts)
        => global::KaedePhi.Tool.Render.KaedePhi.KpcEventChannelRenderer.RenderEventLayer(layer, opts.ToNewOptions());
}
