using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using Chart = KaedePhi.Core.KaedePhi.Chart;

namespace KaedePhi.Tool.App.Cli.Infrastructure;

/// <summary>
/// <see cref="ChartService.SaveAsAsync"/> 的导出选项。
/// </summary>
public sealed record SaveAsOptions
{
    /// <summary>是否使用流式写入（适合大文件）。</summary>
    public bool Stream { get; init; }

    /// <summary>是否格式化 JSON 输出。</summary>
    public bool Format { get; init; }

    /// <summary>演习模式：不实际写文件，仅返回目标路径。</summary>
    public bool DryRun { get; init; }

    /// <summary>PhiEdit 转换选项（仅 <see cref="ChartType.PhiEdit"/> 时生效）。</summary>
    public KpcToPhiEditConvertOptions? PhiEditOptions { get; init; }

    /// <summary>Phigros v3 转换选项（仅 <see cref="ChartType.PhigrosV3"/> 时生效）。</summary>
    public KpcToPhigrosV3ConvertOptions? PhigrosOptions { get; init; }

    /// <summary>其他格式的转换选项，按目标格式自行匹配类型。</summary>
    public object? ExportOptions { get; init; }

    /// <summary>
    /// 挑选与目标格式匹配的转换选项。
    /// </summary>
    /// <param name="target">目标格式</param>
    /// <returns>选项实例，无匹配时返回 <see langword="null"/> 以使用格式默认值</returns>
    internal object? ResolveFor(ChartType target) =>
        target switch
        {
            ChartType.PhiEdit => PhiEditOptions ?? ExportOptions,
            ChartType.PhigrosV3 => PhigrosOptions ?? ExportOptions,
            _ => ExportOptions,
        };
}

/// <summary>
/// 谱面加载、格式检测与导出服务。格式相关的转换细节委托给 <see cref="ChartFormatRegistry"/>。
/// </summary>
public sealed class ChartService
{
    private readonly WorkspaceService _workspace = new();

    /// <summary>从文件路径或工作区加载原始文本。</summary>
    public async Task<string> LoadChartTextAsync(
        string? input,
        string? workspace,
        CancellationToken ct = default
    )
    {
        string path;
        if (!string.IsNullOrWhiteSpace(workspace))
        {
            path =
                _workspace.GetChartPath(workspace)
                ?? throw new InvalidOperationException(
                    string.Format(CliLocalizationString.err_workspace_missing, workspace)
                );
        }
        else
        {
            path =
                input
                ?? throw new InvalidOperationException(CliLocalizationString.err_input_required);
        }

        return await File.ReadAllTextAsync(path, ct);
    }

    /// <summary>将输入谱面统一转换为中间类型，格式不支持导入时返回 <see langword="null"/>。</summary>
    public async Task<Chart?> LoadKpcAsync(
        string? input,
        string? workspace,
        CancellationToken ct = default
    )
    {
        var text = await LoadChartTextAsync(input, workspace, ct);
        var descriptor = ChartFormatRegistry.Find(ChartGetType.GetType(text));
        if (descriptor is not { CanImport: true })
            return null;

        return await descriptor.ImportAsync(text, ct: ct);
    }

    /// <summary>根据输入路径或工作区自动计算输出路径。</summary>
    public string ResolveOutputPath(
        string? input,
        string? output,
        string? workspace,
        string extension = ".json"
    )
    {
        if (!string.IsNullOrWhiteSpace(output))
            return output;
        if (!string.IsNullOrWhiteSpace(workspace))
            return Path.Combine(_workspace.Root, workspace, "chart" + extension);
        if (string.IsNullOrEmpty(input))
            throw new InvalidOperationException(CliLocalizationString.err_input_required);
        return Path.Combine(
            Path.GetDirectoryName(input) ?? ".",
            Path.GetFileNameWithoutExtension(input) + "_KaedePhi" + extension
        );
    }

    /// <summary>将 KPC 谱面导出为 RPE 格式并写入。</summary>
    public static async Task<string> SaveAsRpeAsync(
        Chart chart,
        string outputPath,
        bool dryRun,
        CancellationToken ct = default
    )
    {
        await ChartFormatRegistry
            .Get(ChartType.RePhiEdit)
            .ExportAsync(
                chart,
                outputPath,
                new ChartWriteSettings { DryRun = dryRun },
                exportOptions: new ConvertOption(),
                ct: ct
            );
        return outputPath;
    }

    /// <summary>将 KPC 谱面导出为目标格式并写入，格式不支持导出时返回 <see langword="null"/>。</summary>
    public static async Task<string?> SaveAsAsync(
        Chart chart,
        string outputPath,
        ChartType target,
        SaveAsOptions options,
        CancellationToken ct = default
    )
    {
        var descriptor = ChartFormatRegistry.Find(target);
        if (descriptor is not { CanExport: true })
            return null;

        await descriptor.ExportAsync(
            chart,
            outputPath,
            new ChartWriteSettings
            {
                UseStream = options.Stream,
                Indented = options.Format,
                DryRun = options.DryRun,
            },
            options.ResolveFor(target),
            ct: ct
        );
        return outputPath;
    }
}
