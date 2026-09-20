#pragma warning disable CS0618

using KaedePhi.Tool.Common;
using KaedePhi.Tool.Compatibility;
using Kpc = KaedePhi.Core.KaedePhi;

namespace KaedePhi.Tool.Render.KaedePhi;

/// <summary>
/// 已弃用的 KPC 谱面渲染导出器，行为与 <see cref="Intermediate.IrChartRenderExporter"/> 一致。
/// </summary>
[Obsolete("已弃用：请迁移至 KaedePhi.Tool.Render.Intermediate.IrChartRenderExporter。")]
public class KpcChartRenderExporter
    : Intermediate.IrChartRenderExporter,
        IChartRenderExporter<Kpc.Chart, KpcRenderOptions>
{
    /// <summary>
    /// 渲染整个 KPC 谱面（或指定判定线 / 事件层）并写入图片文件。
    /// </summary>
    /// <param name="chart">KPC 谱面对象。</param>
    /// <param name="outputDir">输出目录（不存在时自动创建）。</param>
    /// <param name="opts">渲染配置。</param>
    /// <param name="lineIndex">若指定，则只渲染该索引的判定线。</param>
    /// <param name="layerIndex">若指定，则只渲染该索引的事件层。</param>
    /// <param name="progress">进度回调。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>所有已写入文件的路径列表。</returns>
    [Obsolete("已弃用：请迁移至 IrChartRenderExporter.ExportChart。")]
    public IReadOnlyList<string> ExportChart(
        Kpc.Chart chart,
        string outputDir,
        KpcRenderOptions opts,
        int? lineIndex = null,
        int? layerIndex = null,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    ) =>
        base.ExportChart(
            KpcCompatibilityMapper.ToIntermediate(chart),
            outputDir,
            opts,
            lineIndex,
            layerIndex,
            progress,
            ct
        );
}
