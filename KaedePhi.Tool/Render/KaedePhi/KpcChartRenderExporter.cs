using KaedePhi.Tool.Common;
using SkiaSharp;
using Chart = KaedePhi.Core.KaedePhi.Chart;

namespace KaedePhi.Tool.Render.KaedePhi;

/// <summary>
/// KPC 谱面渲染导出器：将谱面各判定线、各事件层渲染为 PNG 图片并写入目录。
/// </summary>
public class KpcChartRenderExporter : LoggableBase, IChartRenderExporter<Chart, KpcRenderOptions>
{
    /// <inheritdoc/>
    public IReadOnlyList<string> ExportChart(
        Chart chart,
        string outputDir,
        KpcRenderOptions opts,
        int? lineIndex = null,
        int? layerIndex = null,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        ChartProcessingValidator.ValidateRender(chart, opts, lineIndex, layerIndex);
        Directory.CreateDirectory(outputDir);
        var written = new List<string>();

        var lineStart = lineIndex ?? 0;
        var lineEnd = lineIndex.HasValue ? lineIndex.Value + 1 : chart.JudgeLineList.Count;
        var totalLines = lineEnd - lineStart;
        var completedLines = 0;

        for (var li = lineStart; li < lineEnd; li++)
        {
            ct.ThrowIfCancellationRequested();
            if (li >= chart.JudgeLineList.Count)
                break;
            var line = chart.JudgeLineList[li];
            var layers = line.EventLayers;
            if (layers.Count == 0)
            {
                completedLines++;
                continue;
            }

            var safeName = SanitizeFileName(line.Name);

            var layerStart = layerIndex ?? 0;
            var layerEnd = layerIndex.HasValue ? layerIndex.Value + 1 : line.EventLayers.Count;
            var totalLayers = layerEnd - layerStart;
            var completedLayers = 0;

            for (var ei = layerStart; ei < layerEnd; ei++)
            {
                ct.ThrowIfCancellationRequested();
                if (ei >= line.EventLayers.Count)
                    break;
                var eventLayer = line.EventLayers[ei];
                if ((object?)eventLayer is null)
                {
                    completedLayers++;
                    continue;
                }

                LogInfo($"渲染 [{li}]{safeName} 第 {ei} 层...");

                using var bitmap = KpcEventLayerRenderer.RenderEventLayer(eventLayer, opts);
                var filename = $"{safeName}_L{li}_layer{ei}.png";
                var filePath = Path.Combine(outputDir, filename);

                SaveBitmap(bitmap, filePath, ct);
                written.Add(filePath);
                LogInfo($"  已写入: {filePath}");

                completedLayers++;
                var lineProgress = (double)completedLines / totalLines;
                var layerProgress = (double)completedLayers / totalLayers / totalLines;
                progress?.Report(
                    new ToolProgress(lineProgress + layerProgress, $"[{li}]{safeName} layer{ei}")
                );
            }

            completedLines++;
        }

        progress?.Report(new ToolProgress(1.0));
        return written;
    }

    private static void SaveBitmap(SKBitmap bitmap, string filePath, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var temporaryPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (
                var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    useAsync: false
                )
            )
            {
                data.SaveTo(stream);
                stream.Flush(true);
            }
            ct.ThrowIfCancellationRequested();
            File.Move(temporaryPath, filePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars).Trim('.', ' ');
    }
}
