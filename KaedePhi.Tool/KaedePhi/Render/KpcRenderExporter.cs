using SkiaSharp;
using Chart = KaedePhi.Core.KaedePhi.Chart;

namespace KaedePhi.Tool.KaedePhi.Render;

/// <summary>
/// 将 NRC 谱面各判定线、各事件层渲染为 PNG 图片并写入目录。
/// </summary>
public static class KpcRenderExporter
{
    /// <summary>
    /// 渲染整个谱面（或指定判定线 / 事件层）并写入 PNG 文件。
    /// </summary>
    /// <param name="chart">NRC 谱面对象</param>
    /// <param name="outputDir">输出目录（不存在时自动创建）</param>
    /// <param name="opts">渲染配置</param>
    /// <param name="lineIndex">若指定，则只渲染该索引的判定线</param>
    /// <param name="layerIndex">若指定，则只渲染该索引的事件层（需同时指定 lineIndex）</param>
    /// <returns>所有已写入文件的路径列表</returns>
    public static IReadOnlyList<string> ExportChart(
        Chart chart,
        string outputDir,
        RenderOptions opts,
        int? lineIndex = null,
        int? layerIndex = null)
    {
        Directory.CreateDirectory(outputDir);
        var written = new List<string>();

        int lineStart = lineIndex ?? 0;
        int lineEnd = lineIndex.HasValue ? lineIndex.Value + 1 : chart.JudgeLineList.Count;

        for (int li = lineStart; li < lineEnd; li++)
        {
            if (li >= chart.JudgeLineList.Count) break;
            var line = chart.JudgeLineList[li];
            if (line.EventLayers == null || line.EventLayers.Count == 0) continue;

            // 使用判定线名称（去掉非法文件名字符）
            string safeName = SanitizeFileName(line.Name ?? $"line{li}");

            int layerStart = layerIndex ?? 0;
            int layerEnd = layerIndex.HasValue ? layerIndex.Value + 1 : line.EventLayers.Count;

            for (int ei = layerStart; ei < layerEnd; ei++)
            {
                if (ei >= line.EventLayers.Count) break;
                var eventLayer = line.EventLayers[ei];
                if (eventLayer == null) continue;

                KpcToolLog.OnInfo($"渲染 [{li}]{safeName} 第 {ei} 层...");

                using var bitmap = KpcEventChannelRenderer.RenderEventLayer(eventLayer, opts);
                string filename = $"{safeName}_L{li}_layer{ei}.png";
                string filePath = Path.Combine(outputDir, filename);

                SaveBitmap(bitmap, filePath);
                written.Add(filePath);
                KpcToolLog.OnInfo($"  已写入: {filePath}");
            }
        }

        return written;
    }

    // ── 私有辅助 ──────────────────────────────────────────────────────────────

    private static void SaveBitmap(SKBitmap bitmap, string filePath)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(filePath);
        data.SaveTo(stream);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars).Trim('.', ' ');
    }
}


