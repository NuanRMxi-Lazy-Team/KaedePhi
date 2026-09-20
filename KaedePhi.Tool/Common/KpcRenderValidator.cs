#pragma warning disable CS0618

using KaedePhi.Tool.Compatibility;
using KaedePhi.Tool.Render.KaedePhi;
using Kpc = KaedePhi.Core.KaedePhi;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;

namespace KaedePhi.Tool.Common;

/// <summary>
/// 已弃用的 KPC 渲染参数校验入口，行为与 <see cref="IrRenderValidator"/> 一致。
/// </summary>
[Obsolete("已弃用：请迁移至 KaedePhi.Tool.Common.IrRenderValidator。")]
public static class KpcRenderValidator
{
    /// <summary>渲染位图的最大像素数。</summary>
    public const long MaximumRenderPixels = IrRenderValidator.MaximumRenderPixels;

    /// <summary>
    /// 校验 KPC 谱面与渲染配置。
    /// </summary>
    /// <param name="chart">待渲染的 KPC 谱面。</param>
    /// <param name="options">渲染配置。</param>
    /// <param name="lineIndex">若指定，则只渲染该索引的判定线。</param>
    /// <param name="layerIndex">若指定，则只渲染该索引的事件层。</param>
    [Obsolete("已弃用：请迁移至 IrRenderValidator.Validate。")]
    public static void Validate(
        Kpc.Chart chart,
        KpcRenderOptions options,
        int? lineIndex = null,
        int? layerIndex = null
    ) =>
        IrRenderValidator.Validate(
            KpcCompatibilityMapper.ToIntermediate(chart),
            options,
            lineIndex,
            layerIndex
        );

    /// <summary>
    /// 校验渲染配置。
    /// </summary>
    /// <param name="options">渲染配置。</param>
    [Obsolete("已弃用：请迁移至 IrRenderValidator.ValidateOptions。")]
    public static void ValidateOptions(KpcRenderOptions options) =>
        IrRenderValidator.ValidateOptions(options);

    /// <summary>
    /// 校验单个事件层与渲染配置。
    /// </summary>
    /// <param name="layer">待渲染的事件层。</param>
    /// <param name="options">渲染配置。</param>
    [Obsolete("已弃用：请迁移至 IrRenderValidator.ValidateEventLayer。")]
    public static void ValidateEventLayer(KpcEvents.EventLayer layer, KpcRenderOptions options) =>
        IrRenderValidator.ValidateEventLayer(KpcCompatibilityMapper.ToIntermediate(layer), options);
}
