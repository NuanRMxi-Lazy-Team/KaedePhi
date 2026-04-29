namespace KaedePhi.Tool.Render;

/// <summary>
/// 谱面渲染导出器：将谱面各判定线、各事件层渲染为图片并写入目录。
/// </summary>
/// <typeparam name="TChart">谱面类型。</typeparam>
/// <typeparam name="TRenderOptions">渲染配置类型。</typeparam>
public interface IChartRenderExporter<in TChart, in TRenderOptions>
{
    /// <summary>
    /// 渲染整个谱面（或指定判定线 / 事件层）并写入图片文件。
    /// </summary>
    /// <param name="chart">谱面对象。</param>
    /// <param name="outputDir">输出目录（不存在时自动创建）。</param>
    /// <param name="opts">渲染配置。</param>
    /// <param name="lineIndex">若指定，则只渲染该索引的判定线。</param>
    /// <param name="layerIndex">若指定，则只渲染该索引的事件层（需同时指定 <paramref name="lineIndex"/>）。</param>
    /// <returns>所有已写入文件的路径列表。</returns>
    IReadOnlyList<string> ExportChart(
        TChart chart,
        string outputDir,
        TRenderOptions opts,
        int? lineIndex = null,
        int? layerIndex = null);
}
