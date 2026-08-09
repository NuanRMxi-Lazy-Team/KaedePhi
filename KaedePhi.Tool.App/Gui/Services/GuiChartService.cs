using KaedePhi.Tool.App.Shared;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter;
using KaedePhi.Tool.Render.KaedePhi;
using Serilog;
using static KaedePhi.Tool.Localization.GuiLocalizationString;
using Chart = KaedePhi.Core.KaedePhi.Chart;

namespace KaedePhi.Tool.App.Gui.Services;

public sealed class GuiChartService
{
    private readonly ILogger _log;
    private string? _detectedFilePath;
    private string? _detectedText;

    public GuiChartService(LogService logService)
    {
        _log = logService.ForContext<GuiChartService>();
    }

    /// <summary>
    /// 当前加载的 KPC 图表（内存中）
    /// </summary>
    public Chart? CurrentChart { get; private set; }

    /// <summary>
    /// 源文件的格式类型
    /// </summary>
    public ChartType SourceFormat { get; private set; }

    /// <summary>
    /// 源文件路径
    /// </summary>
    public string? SourceFilePath { get; private set; }

    /// <summary>
    /// 是否已加载图表
    /// </summary>
    public bool IsLoaded => CurrentChart != null;

    /// <summary>
    /// 检测文件的图表格式类型
    /// </summary>
    public ChartType DetectChartType(string filePath, bool stream)
    {
        ChartProcessingValidator.ValidateInputFile(filePath);
        var text = File.ReadAllText(filePath);

        var detectedType = ChartGetType.GetType(text);
        _detectedFilePath = filePath;
        _detectedText = text;
        _log.Information(log_step_detected, detectedType);
        return detectedType;
    }

    /// <summary>
    /// 异步检测文件格式，避免在界面线程同步读取大型文件。
    /// </summary>
    /// <param name="filePath">输入文件路径。</param>
    /// <param name="stream">是否使用流式导入兼容模式。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>检测到的谱面格式。</returns>
    public async Task<ChartType> DetectChartTypeAsync(
        string filePath,
        bool stream,
        CancellationToken ct = default
    )
    {
        ChartProcessingValidator.ValidateInputFile(filePath);
        var text = await File.ReadAllTextAsync(filePath, ct);
        var detectedType = ChartGetType.GetType(text);
        _detectedFilePath = filePath;
        _detectedText = text;
        _log.Information(log_step_detected, detectedType);
        return detectedType;
    }

    /// <summary>
    /// 从文件加载图表并转换为 KPC 格式存储在内存中
    /// </summary>
    public async Task LoadChartAsync(
        string filePath,
        bool stream,
        CancellationToken ct,
        object? importOptions = null
    )
    {
        _log.Information(log_file_selected, filePath, stream);

        ChartProcessingValidator.ValidateInputFile(filePath);
        var text =
            _detectedFilePath == filePath && _detectedText is not null
                ? _detectedText
                : await File.ReadAllTextAsync(filePath, ct);
        _detectedFilePath = null;
        _detectedText = null;

        var detectedType = ChartGetType.GetType(text);
        _log.Information(log_step_detected, detectedType);

        var descriptor = ChartFormatRegistry.Get(detectedType);
        Chart kpcChart;
        if (stream && descriptor.CanStreamImport)
        {
            await using var inputStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                65536,
                useAsync: true
            );
            kpcChart = await descriptor.ImportStreamAsync(
                inputStream,
                importOptions,
                CreateLogSink(),
                ct
            );
        }
        else
        {
            kpcChart = await descriptor.ImportAsync(text, importOptions, CreateLogSink(), ct);
        }

        CurrentChart = kpcChart;
        SourceFormat = detectedType;
        SourceFilePath = filePath;
    }

    /// <summary>
    /// 将当前 KPC 图表导出到指定格式和路径
    /// </summary>
    public async Task ExportChartAsync(
        ChartType targetType,
        string outputPath,
        bool stream,
        bool indented,
        object? exportOptions = null,
        CancellationToken ct = default
    )
    {
        if (CurrentChart == null)
            throw new InvalidOperationException("No chart loaded");

        _log.Information(log_exporting_to, outputPath, targetType);
        await ChartFormatRegistry
            .Get(targetType)
            .ExportAsync(
                CurrentChart,
                outputPath,
                new ChartWriteSettings { UseStream = stream, Indented = indented },
                exportOptions,
                CreateLogSink(),
                ct
            );
        _log.Information(log_export_done);
    }

    /// <summary>
    /// 清除当前加载的图表
    /// </summary>
    public void Clear()
    {
        CurrentChart = null;
        SourceFormat = default;
        SourceFilePath = null;
        _detectedFilePath = null;
        _detectedText = null;
    }

    /// <summary>
    /// 将 Serilog 日志接入工具层日志回调。
    /// </summary>
    private ChartLogSink CreateLogSink() =>
        new()
        {
            Info = msg => _log.Information(msg),
            Warning = msg => _log.Warning(msg),
            Error = msg => _log.Error(msg),
            Debug = msg => _log.Debug(msg),
        };

    public void RunFatherUnbind(
        Chart chart,
        double precision,
        double tolerance,
        bool classic,
        bool disableCompress,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        _log.Information(log_running_tool, tool_unbind_name);
        ChartProcessor.UnbindFather(
            chart,
            precision,
            tolerance,
            classic,
            disableCompress,
            info: msg => _log.Information(msg),
            warning: msg => _log.Warning(msg),
            error: msg => _log.Error(msg),
            debug: msg => _log.Debug(msg),
            progress: progress,
            ct: ct
        );
    }

    public void RunLayerMerge(
        Chart chart,
        double precision,
        double tolerance,
        bool classic,
        bool disableCompress,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        _log.Information(log_running_tool, tool_layermerge_name);
        ChartProcessor.LayerMerge(
            chart,
            precision,
            tolerance,
            classic,
            disableCompress,
            info: msg => _log.Information(msg),
            warning: msg => _log.Warning(msg),
            error: msg => _log.Error(msg),
            debug: msg => _log.Debug(msg),
            progress: progress,
            ct: ct
        );
    }

    public void RunCutEvent(
        Chart chart,
        double precision,
        double tolerance,
        bool disableCompress,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        _log.Information(log_running_tool, tool_cut_name);
        ChartProcessor.CutEvent(
            chart,
            precision,
            tolerance,
            disableCompress,
            info: msg => _log.Information(msg),
            warning: msg => _log.Warning(msg),
            error: msg => _log.Error(msg),
            debug: msg => _log.Debug(msg),
            progress: progress,
            ct: ct
        );
    }

    public void RunFitEvent(
        Chart chart,
        double tolerance,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        _log.Information(log_running_tool, tool_fit_name);
        ChartProcessor.FitEvent(
            chart,
            tolerance,
            info: msg => _log.Information(msg),
            warning: msg => _log.Warning(msg),
            error: msg => _log.Error(msg),
            debug: msg => _log.Debug(msg),
            progress: progress,
            ct: ct
        );
    }

    public IReadOnlyList<string> RunRender(
        Chart chart,
        string outputDir,
        KpcRenderOptions options,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        _log.Information(log_running_tool, tool_render_name);

        // 输出目录为空时回退到导入谱面同级目录的 RenderLayer 文件夹
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            outputDir = Path.Combine(Path.GetDirectoryName(SourceFilePath) ?? ".", "RenderLayer");
        }

        Directory.CreateDirectory(outputDir);
        return ChartProcessor.Render(
            chart,
            outputDir,
            options,
            info: msg => _log.Information(msg),
            warning: msg => _log.Warning(msg),
            error: msg => _log.Error(msg),
            progress: progress,
            ct: ct
        );
    }
}
