using SkiaSharp;
using Chart = KaedePhi.Core.KaedePhi.Chart;

namespace KaedePhi.Tool.KaedePhi.Render;

/// <summary>
/// 将 NRC 谱面各判定线、各事件层渲染为 PNG 图片并写入目录。
/// </summary>
[Obsolete("请改用 KaedePhi.Tool.Render.KaedePhi.KpcChartRenderExporter")]
public static class KpcRenderExporter
{
    /// <summary>
    /// 渲染整个谱面（或指定判定线 / 事件层）并写入 PNG 文件。
    /// </summary>
    [Obsolete("请改用 KaedePhi.Tool.Render.KaedePhi.KpcChartRenderExporter.ExportChart")]
    public static IReadOnlyList<string> ExportChart(
        Chart chart,
        string outputDir,
        RenderOptions opts,
        int? lineIndex = null,
        int? layerIndex = null)
    {
        var exporter = new global::KaedePhi.Tool.Render.KaedePhi.KpcChartRenderExporter();
        return exporter.ExportChart(chart, outputDir, opts.ToNewOptions(), lineIndex, layerIndex);
    }
}
