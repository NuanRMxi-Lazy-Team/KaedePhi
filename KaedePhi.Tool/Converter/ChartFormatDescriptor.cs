using KaedePhi.Tool.Common;
using KpcChart = KaedePhi.Core.KaedePhi.Chart;

namespace KaedePhi.Tool.Converter;

/// <summary>
/// 单一谱面格式的导入导出能力描述。由 <see cref="ChartFormatRegistry"/> 统一注册与查询。
/// </summary>
public sealed class ChartFormatDescriptor
{
    /// <summary>格式类型。</summary>
    public required ChartType Type { get; init; }

    /// <summary>该格式的默认文件扩展名（不含点）。</summary>
    public required string FileExtension { get; init; }

    /// <summary>创建该格式导入选项的默认实例，无选项时返回 <see langword="null"/>。</summary>
    public Func<object?>? ImportOptionsFactory { get; init; }

    /// <summary>创建该格式导出选项的默认实例，无选项时返回 <see langword="null"/>。</summary>
    public Func<object?>? ExportOptionsFactory { get; init; }

    /// <summary>将该格式的谱面文本转换为 IR 中间格式。</summary>
    internal Func<
        string,
        object?,
        ChartLogSink,
        CancellationToken,
        Task<Ir.Chart>
    >? Importer { get; init; }

    /// <summary>将该格式的谱面流转换为 IR 中间格式。</summary>
    internal Func<
        Stream,
        object?,
        ChartLogSink,
        CancellationToken,
        Task<Ir.Chart>
    >? StreamImporter { get; init; }

    /// <summary>将 IR 中间格式导出为该格式并写入目标路径。</summary>
    internal Func<
        Ir.Chart,
        string,
        ChartWriteSettings,
        object?,
        ChartLogSink,
        CancellationToken,
        Task
    >? Exporter { get; init; }

    /// <summary>该格式是否支持作为导入源。</summary>
    public bool CanImport => Importer is not null;

    /// <summary>
    /// 该格式是否支持流式导入。
    /// </summary>
    public bool CanStreamImport => StreamImporter is not null;

    /// <summary>该格式是否支持作为导出目标。</summary>
    public bool CanExport => Exporter is not null;

    /// <summary>
    /// 创建该格式的默认导入选项。
    /// </summary>
    /// <returns>选项实例，该格式无导入选项时返回 <see langword="null"/></returns>
    public object? CreateDefaultImportOptions() => ImportOptionsFactory?.Invoke();

    /// <summary>
    /// 创建该格式的默认导出选项。
    /// </summary>
    /// <returns>选项实例，该格式无导出选项时返回 <see langword="null"/></returns>
    public object? CreateDefaultExportOptions() => ExportOptionsFactory?.Invoke();

    /// <summary>
    /// 将该格式的谱面文本转换为 IR 中间格式。
    /// </summary>
    /// <param name="text">谱面原始文本</param>
    /// <param name="importOptions">导入选项，传 <see langword="null"/> 时使用默认值</param>
    /// <param name="log">日志回调集合</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>IR 谱面</returns>
    public async Task<Ir.Chart> ImportIrAsync(
        string text,
        object? importOptions = null,
        ChartLogSink? log = null,
        CancellationToken ct = default
    )
    {
        if (Importer is null)
            throw new NotSupportedException($"{Type} 不支持作为导入源。");
        ArgumentNullException.ThrowIfNull(text);
        var chart = await Importer(text, importOptions, log ?? ChartLogSink.None, ct);
        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(chart);
        return normalized;
    }

    /// <summary>
    /// 从输入流转换为 IR 谱面。
    /// </summary>
    /// <param name="stream">输入流。</param>
    /// <param name="importOptions">导入选项。</param>
    /// <param name="log">日志回调集合。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>IR 谱面。</returns>
    public async Task<Ir.Chart> ImportStreamIrAsync(
        Stream stream,
        object? importOptions = null,
        ChartLogSink? log = null,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (StreamImporter is null)
            throw new NotSupportedException($"{Type} 不支持流式导入。");

        ct.ThrowIfCancellationRequested();
        var chart = await StreamImporter(stream, importOptions, log ?? ChartLogSink.None, ct);
        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(chart);
        return normalized;
    }

    /// <summary>
    /// 将该格式的谱面文本转换为已弃用的 KPC 中间格式。
    /// </summary>
    /// <param name="text">谱面原始文本</param>
    /// <param name="importOptions">导入选项，传 <see langword="null"/> 时使用默认值</param>
    /// <param name="log">日志回调集合</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>KPC 谱面</returns>
    [Obsolete("已弃用：请迁移至 ImportIrAsync。")]
    public async Task<KpcChart> ImportAsync(
        string text,
        object? importOptions = null,
        ChartLogSink? log = null,
        CancellationToken ct = default
    ) =>
        Compatibility.KpcCompatibilityMapper.ToKpc(
            await ImportIrAsync(text, importOptions, log, ct)
        );

    /// <summary>
    /// 从输入流转换为已弃用的 KPC 中间格式。
    /// </summary>
    /// <param name="stream">输入流。</param>
    /// <param name="importOptions">导入选项。</param>
    /// <param name="log">日志回调集合。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>KPC 谱面。</returns>
    [Obsolete("已弃用：请迁移至 ImportStreamIrAsync。")]
    public async Task<KpcChart> ImportStreamAsync(
        Stream stream,
        object? importOptions = null,
        ChartLogSink? log = null,
        CancellationToken ct = default
    ) =>
        Compatibility.KpcCompatibilityMapper.ToKpc(
            await ImportStreamIrAsync(stream, importOptions, log, ct)
        );

    /// <summary>
    /// 将 IR 谱面导出为该格式并写入指定路径。
    /// </summary>
    /// <param name="chart">IR 谱面</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="writeSettings">写入方式设置</param>
    /// <param name="exportOptions">导出选项，传 <see langword="null"/> 时使用默认值</param>
    /// <param name="log">日志回调集合</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>导出任务</returns>
    public Task ExportAsync(
        Ir.Chart chart,
        string outputPath,
        ChartWriteSettings? writeSettings = null,
        object? exportOptions = null,
        ChartLogSink? log = null,
        CancellationToken ct = default
    )
    {
        if (Exporter is null)
            throw new NotSupportedException($"{Type} 不支持作为导出目标。");
        ArgumentNullException.ThrowIfNull(chart);
        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(chart);
        return Exporter(
            normalized,
            outputPath,
            writeSettings ?? new ChartWriteSettings(),
            exportOptions,
            log ?? ChartLogSink.None,
            ct
        );
    }

    /// <summary>
    /// 将已弃用的 KPC 谱面导出为该格式并写入指定路径。
    /// </summary>
    /// <param name="chart">KPC 谱面</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="writeSettings">写入方式设置</param>
    /// <param name="exportOptions">导出选项，传 <see langword="null"/> 时使用默认值</param>
    /// <param name="log">日志回调集合</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>导出任务</returns>
    [Obsolete("已弃用：请迁移至 ExportAsync(Ir.Chart, ...)。")]
    public Task ExportAsync(
        KpcChart chart,
        string outputPath,
        ChartWriteSettings? writeSettings = null,
        object? exportOptions = null,
        ChartLogSink? log = null,
        CancellationToken ct = default
    ) =>
        ExportAsync(
            Compatibility.KpcCompatibilityMapper.ToIntermediate(chart),
            outputPath,
            writeSettings,
            exportOptions,
            log,
            ct
        );
}