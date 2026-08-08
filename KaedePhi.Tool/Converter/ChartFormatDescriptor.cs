using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter;

/// <summary>
/// 谱面写出设置，描述导出时的文件写入方式。
/// </summary>
public sealed record ChartWriteSettings
{
    /// <summary>是否使用流式写入（适合大文件，降低内存占用）。</summary>
    public bool UseStream { get; init; }

    /// <summary>是否格式化输出（对文本格式无效）。</summary>
    public bool Indented { get; init; }

    /// <summary>是否为演习模式：完成序列化但跳过文件写入。</summary>
    public bool DryRun { get; init; }
}

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

    /// <summary>将该格式的谱面文本转换为 KPC 中间格式。</summary>
    internal Func<
        string,
        object?,
        ChartLogSink,
        CancellationToken,
        Task<Kpc.Chart>
    >? Importer { get; init; }

    /// <summary>将 KPC 中间格式导出为该格式并写入目标路径。</summary>
    internal Func<
        Kpc.Chart,
        string,
        ChartWriteSettings,
        object?,
        ChartLogSink,
        CancellationToken,
        Task
    >? Exporter { get; init; }

    /// <summary>该格式是否支持作为导入源。</summary>
    public bool CanImport => Importer is not null;

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
    /// 将该格式的谱面文本转换为 KPC 中间格式。
    /// </summary>
    /// <param name="text">谱面原始文本</param>
    /// <param name="importOptions">导入选项，传 <see langword="null"/> 时使用默认值</param>
    /// <param name="log">日志回调集合</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>KPC 谱面</returns>
    public Task<Kpc.Chart> ImportAsync(
        string text,
        object? importOptions = null,
        ChartLogSink? log = null,
        CancellationToken ct = default
    )
    {
        if (Importer is null)
            throw new NotSupportedException($"{Type} 不支持作为导入源。");
        return Importer(text, importOptions, log ?? ChartLogSink.None, ct);
    }

    /// <summary>
    /// 将 KPC 谱面导出为该格式并写入指定路径。
    /// </summary>
    /// <param name="chart">KPC 谱面</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="writeSettings">写入方式设置</param>
    /// <param name="exportOptions">导出选项，传 <see langword="null"/> 时使用默认值</param>
    /// <param name="log">日志回调集合</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>导出任务</returns>
    public Task ExportAsync(
        Kpc.Chart chart,
        string outputPath,
        ChartWriteSettings? writeSettings = null,
        object? exportOptions = null,
        ChartLogSink? log = null,
        CancellationToken ct = default
    )
    {
        if (Exporter is null)
            throw new NotSupportedException($"{Type} 不支持作为导出目标。");
        return Exporter(
            chart,
            outputPath,
            writeSettings ?? new ChartWriteSettings(),
            exportOptions,
            log ?? ChartLogSink.None,
            ct
        );
    }
}
