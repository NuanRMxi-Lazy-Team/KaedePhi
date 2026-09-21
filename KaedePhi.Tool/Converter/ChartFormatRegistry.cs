using System.Collections.ObjectModel;
using KpcChart = KaedePhi.Core.KaedePhi.Chart;
using KaedePhi.Core.Formats.PhiChain.v6.Serialization;
using KaedePhi.Core.Formats.PhiEdit.Serialization;
using KaedePhi.Core.Formats.PhiFans.Serialization;
using KaedePhi.Core.Formats.Phigros.v3.Serialization;
using KaedePhi.Core.Formats.RePhiEdit.Serialization;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiChain;
using KaedePhi.Tool.Converter.PhiChain.Model;
using KaedePhi.Tool.Converter.PhiEdit;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Converter.PhiFans;
using KaedePhi.Tool.Converter.PhiFans.Model;
using KaedePhi.Tool.Converter.Phigros.v3;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using KaedePhi.Tool.Converter.RePhiEdit;
using KaedePhi.Tool.Converter.RePhiEdit.Model;

namespace KaedePhi.Tool.Converter;

/// <summary>
/// 谱面格式注册表，集中描述各格式的导入导出能力
/// </summary>
public static class ChartFormatRegistry
{
    private static readonly ReadOnlyDictionary<ChartType, ChartFormatDescriptor> Descriptors = new(
        new Dictionary<ChartType, ChartFormatDescriptor>
        {
            [ChartType.RePhiEdit] = new()
            {
                Type = ChartType.RePhiEdit,
                FileExtension = "json",
                ExportOptionsFactory = () => new ConvertOption(),
                Importer = async (text, _, log, ct) =>
                {
                    var converter = Prepare(new RePhiEditConverter(), log, ct);
                    var source = await RpeSerialization.LoadFromJsonAsync(text);
                    return converter.ToIr(source, null);
                },
                StreamImporter = async (stream, _, log, ct) =>
                {
                    var converter = Prepare(new RePhiEditConverter(), log, ct);
                    var source = await RpeSerialization.LoadFromStreamAsync(stream);
                    return converter.ToIr(source, null);
                },
                Exporter = async (chart, path, write, options, log, ct) =>
                {
                    var converter = Prepare(new RePhiEditConverter(), log, ct);
                    var target = converter.FromIr(
                        chart,
                        Coerce(options, () => new ConvertOption())
                    );
                    await WriteAsync(
                        path,
                        write,
                        () => target.ExportToJsonAsync(write.Indented),
                        stream => target.ExportToJsonStreamAsync(stream, write.Indented),
                        ct
                    );
                },
            },

            [ChartType.PhiEdit] = new()
            {
                Type = ChartType.PhiEdit,
                FileExtension = "pec",
                ImportOptionsFactory = () => new PhiEditToIrConvertOptions(),
                ExportOptionsFactory = () => new IrToPhiEditConvertOptions(),
                Importer = async (text, options, log, ct) =>
                {
                    var converter = Prepare(new PhiEditConverter(), log, ct);
                    var source = await PeSerialization.LoadAsync(text);
                    return converter.ToIr(
                        source,
                        Coerce(options, () => new PhiEditToIrConvertOptions())
                    );
                },
                StreamImporter = async (stream, options, log, ct) =>
                {
                    var converter = Prepare(new PhiEditConverter(), log, ct);
                    var source = await PeSerialization.LoadStreamAsync(stream);
                    return converter.ToIr(
                        source,
                        Coerce(options, () => new PhiEditToIrConvertOptions())
                    );
                },
                Exporter = async (chart, path, write, options, log, ct) =>
                {
                    var converter = Prepare(new PhiEditConverter(), log, ct);
                    var target = converter.FromIr(
                        chart,
                        Coerce(options, () => new IrToPhiEditConvertOptions())
                    );
                    await WriteAsync(
                        path,
                        write,
                        target.ExportAsync,
                        target.ExportToStreamAsync,
                        ct
                    );
                },
            },

            [ChartType.PhigrosV3] = new()
            {
                Type = ChartType.PhigrosV3,
                FileExtension = "json",
                ExportOptionsFactory = () => new IrToPhigrosV3ConvertOptions(),
                Importer = async (text, _, log, ct) =>
                {
                    var converter = Prepare(new PhigrosV3Converter(), log, ct);
                    var source = await PhigrosSerialization.LoadFromJsonAsync(text);
                    return converter.ToIr(source, null);
                },
                StreamImporter = async (stream, _, log, ct) =>
                {
                    var converter = Prepare(new PhigrosV3Converter(), log, ct);
                    var source = await PhigrosSerialization.LoadFromStreamAsync(stream);
                    return converter.ToIr(source, null);
                },
                Exporter = async (chart, path, write, options, log, ct) =>
                {
                    var converter = Prepare(new PhigrosV3Converter(), log, ct);
                    var target = converter.FromIr(
                        chart,
                        Coerce(options, () => new IrToPhigrosV3ConvertOptions())
                    );
                    await WriteAsync(
                        path,
                        write,
                        () => target.ExportToJsonAsync(write.Indented),
                        stream => target.ExportToJsonStreamAsync(stream, write.Indented),
                        ct
                    );
                },
            },

            [ChartType.PhiChain] = new()
            {
                Type = ChartType.PhiChain,
                FileExtension = "json",
                ImportOptionsFactory = () => new PhiChainToIrConvertOptions(),
                ExportOptionsFactory = () => new IrToPhiChainConvertOptions(),
                Importer = async (text, options, log, ct) =>
                {
                    var converter = Prepare(new PhiChainConverter(), log, ct);
                    var source = await PhichainSerialization.LoadFromJsonAsync(text);
                    return converter.ToIr(
                        source,
                        Coerce(options, () => new PhiChainToIrConvertOptions())
                    );
                },
                StreamImporter = async (stream, options, log, ct) =>
                {
                    var converter = Prepare(new PhiChainConverter(), log, ct);
                    var source = await PhichainSerialization.LoadFromJsonStreamAsync(stream);
                    return converter.ToIr(
                        source,
                        Coerce(options, () => new PhiChainToIrConvertOptions())
                    );
                },
                Exporter = async (chart, path, write, options, log, ct) =>
                {
                    var converter = Prepare(new PhiChainConverter(), log, ct);
                    var target = converter.FromIr(
                        chart,
                        Coerce(options, () => new IrToPhiChainConvertOptions())
                    );
                    await WriteAsync(
                        path,
                        write,
                        () => target.ExportToJsonAsync(write.Indented),
                        stream => target.ExportToJsonStreamAsync(stream, write.Indented),
                        ct
                    );
                },
            },

            [ChartType.PhiFans] = new()
            {
                Type = ChartType.PhiFans,
                FileExtension = "json",
                ExportOptionsFactory = () => new IrToPhiFansConvertOptions(),
                Importer = async (text, _, log, ct) =>
                {
                    var converter = Prepare(new PhiFansConverter(), log, ct);
                    var source = await PhiFansSerialization.LoadFromJsonAsync(text);
                    return converter.ToIr(source, null);
                },
                StreamImporter = async (stream, _, log, ct) =>
                {
                    var converter = Prepare(new PhiFansConverter(), log, ct);
                    var source = await PhiFansSerialization.LoadFromStreamAsync(stream);
                    return converter.ToIr(source, null);
                },
                Exporter = async (chart, path, write, options, log, ct) =>
                {
                    var converter = Prepare(new PhiFansConverter(), log, ct);
                    var target = converter.FromIr(
                        chart,
                        Coerce(options, () => new IrToPhiFansConvertOptions())
                    );
                    await WriteAsync(
                        path,
                        write,
                        () => target.ExportToJsonAsync(write.Indented),
                        stream => target.ExportToJsonStreamAsync(stream, write.Indented),
                        ct
                    );
                },
            },
            [ChartType.PhigrosV1] = new() { Type = ChartType.PhigrosV1, FileExtension = "json" },
        }
    );

    /// <summary>
    /// 获取指定格式的能力描述。
    /// </summary>
    /// <param name="type">谱面格式</param>
    /// <returns>格式描述，未注册时返回 <see langword="null"/></returns>
    public static ChartFormatDescriptor? Find(ChartType type) =>
        Descriptors.GetValueOrDefault(type);

    /// <summary>
    /// 获取指定格式的能力描述，未注册时抛出异常。
    /// </summary>
    /// <param name="type">谱面格式</param>
    /// <returns>格式描述</returns>
    public static ChartFormatDescriptor Get(ChartType type) =>
        Find(type) ?? throw new NotSupportedException($"未注册的谱面格式：{type}");

    /// <summary>
    /// 所有支持导入的格式。
    /// </summary>
    public static IEnumerable<ChartFormatDescriptor> ImportableFormats =>
        Descriptors.Values.Where(d => d.CanImport);

    /// <summary>
    /// 所有支持导出的格式。
    /// </summary>
    public static IEnumerable<ChartFormatDescriptor> ExportableFormats =>
        Descriptors.Values.Where(d => d.CanExport);

    /// <summary>
    /// 检测谱面文本格式并转换为 IR 中间格式。
    /// </summary>
    /// <param name="text">谱面原始文本</param>
    /// <param name="importOptions">导入选项，传 <see langword="null"/> 时使用格式默认值</param>
    /// <param name="log">日志回调集合</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>检测到的格式与转换结果</returns>
    public static async Task<(ChartType DetectedType, Ir.Chart Chart)> ImportIrAsync(
        string text,
        object? importOptions = null,
        ChartLogSink? log = null,
        CancellationToken ct = default
    )
    {
        using var textReader = new StringReader(text);
        var detectedType = ChartGetType.GetType(textReader);
        var chart = await Get(detectedType).ImportIrAsync(text, importOptions, log, ct);
        return (detectedType, chart);
    }

    /// <summary>
    /// 检测谱面文本格式并转换为已弃用的 KPC 中间格式。
    /// </summary>
    /// <param name="text">谱面原始文本</param>
    /// <param name="importOptions">导入选项，传 <see langword="null"/> 时使用格式默认值</param>
    /// <param name="log">日志回调集合</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>检测到的格式与转换结果</returns>
    [Obsolete("已弃用：请迁移至 ImportIrAsync。")]
    public static async Task<(
        ChartType DetectedType,
        KpcChart Chart
        )> ImportAsync(string text, object? importOptions = null, ChartLogSink? log = null,
        CancellationToken ct = default)
    {
        var (detectedType, chart) = await ImportIrAsync(text, importOptions, log, ct);
        return (detectedType, Compatibility.KpcCompatibilityMapper.ToKpc(chart));
    }

    /// <summary>
    /// 为转换器注入日志回调与取消令牌。
    /// </summary>
    private static TConverter Prepare<TConverter>(
        TConverter converter,
        ChartLogSink log,
        CancellationToken ct
    )
        where TConverter : ILoggable
    {
        log.AttachTo(converter);
        if (converter is ICancellableChartConverter cancellable)
            cancellable.SetCancellationToken(ct);
        return converter;
    }

    /// <summary>
    /// 将弱类型选项转换为目标类型，为空时回退到默认值，类型不匹配时显式失败。
    /// </summary>
    private static TOptions Coerce<TOptions>(object? options, Func<TOptions> fallback)
        where TOptions : class
    {
        if (options is null)
            return fallback();
        if (options is not TOptions typed)
            throw new ArgumentException(
                $"选项类型不匹配：期望 {typeof(TOptions).Name}，实际为 {options.GetType().Name}。",
                nameof(options)
            );
        return typed;
    }

    /// <summary>
    /// 按写入设置选择整体写入或流式写入。
    /// </summary>
    private static async Task WriteAsync(
        string path,
        ChartWriteSettings write,
        Func<Task<string>> serializeText,
        Func<Stream, Task> serializeStream,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(path);
        if (write.UseStream)
        {
            if (write.DryRun)
            {
                await using var stream = Stream.Null;
                await serializeStream(stream);
            }
            else
            {
                var temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                Directory.CreateDirectory(
                    Path.GetDirectoryName(fullPath)
                    ?? throw new InvalidOperationException("Invalid output path")
                );
                try
                {
                    await using (
                        var stream = new FileStream(
                            temporaryPath,
                            FileMode.CreateNew,
                            FileAccess.Write,
                            FileShare.None,
                            4096,
                            useAsync: true
                        )
                    )
                    {
                        await serializeStream(stream);
                        await stream.FlushAsync(ct);
                    }

                    ct.ThrowIfCancellationRequested();
                    File.Move(temporaryPath, fullPath, true);
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
            }
        }
        else
        {
            var text = await serializeText();
            if (!write.DryRun)
            {
                var temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                Directory.CreateDirectory(
                    Path.GetDirectoryName(fullPath)
                    ?? throw new InvalidOperationException("Invalid output path")
                );
                try
                {
                    await File.WriteAllTextAsync(temporaryPath, text, ct);
                    ct.ThrowIfCancellationRequested();
                    File.Move(temporaryPath, fullPath, true);
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
            }
        }
    }
}