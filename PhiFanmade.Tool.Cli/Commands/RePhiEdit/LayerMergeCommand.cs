using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;
using PhiFanmade.Tool.RePhiEdit.Layers;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands.RePhiEdit;

/// <summary>
/// 合并层级命令
/// </summary>
public sealed class RpeLayerMergeCommand : AsyncCommand<RpeLayerMergeCommand.Settings>
{
    public sealed class Settings : RpeOperationSettings
    {
        [CommandOption("--classic")]
        [LocalizedDescription("cli_opt_classic_mode_desc")]
        public bool Classic { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = settings.CreateWriter();
        var chart = await settings.LoadChartAsync();
        var chartCopy = chart.Clone();
        if (settings is { DisableCompress: true, Classic: false })
        {
            writer.Error(string.Format(Strings.cli_err_classic_disablsed));
            return 1;
        }

        foreach (var jl in chartCopy.JudgeLineList)
        {
            if (jl.EventLayers is not { Count: > 1 }) continue;
            if (settings.Classic)
                jl.EventLayers = new List<CoreRpe.EventLayer>
                {
                    RpeLayerTools.LayerMerge(jl.EventLayers, settings.Precision, settings.Tolerance,
                        !settings.DisableCompress)
                };
            else
                jl.EventLayers =
                jl.EventLayers = new List<CoreRpe.EventLayer>
                {
                    RpeLayerTools.LayerMergePlus(jl.EventLayers, settings.Precision, settings.Tolerance)
                };
        }

        var output = settings.ResolveOutputPath();
        if (!settings.DryRun)
            if (settings.StreamOutput)
            {
                await using var stream = new FileStream(output, FileMode.Create);
                await chartCopy.ExportToJsonStreamAsync(stream, settings.FormatOutput);
            }
            else
                await File.WriteAllTextAsync(output, await chartCopy.ExportToJsonAsync(settings.FormatOutput),
                    cancellationToken);

        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}