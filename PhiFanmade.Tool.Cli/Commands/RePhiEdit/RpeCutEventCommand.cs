using PhiFanmade.Tool.Localization;
using PhiFanmade.Tool.RePhiEdit.Layers;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands.RePhiEdit;

/// <summary>
/// 切割事件命令
/// </summary>
public sealed class RpeCutEventCommand : AsyncCommand<RpeCutEventCommand.Settings>
{
    public sealed class Settings : RpeOperationSettings
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = settings.CreateWriter();
        var chart = await settings.LoadChartAsync();
        var chartCopy = chart.Clone();

        for (var i = 0; i < chartCopy.JudgeLineList.Count; i++)
        {
            var judgeLine = chartCopy.JudgeLineList[i];
            judgeLine.EventLayers = RpeLayerTools.CutLayerEvents((List<global::PhiFanmade.Core.RePhiEdit.EventLayer>)judgeLine.EventLayers, settings.Precision,
                settings.Tolerance, !settings.DisableCompress);
        }

        var output = settings.ResolveOutputPath();
        if (!settings.DryRun)
        {
            if (settings.StreamOutput)
            {
                await using var stream = new FileStream(output, FileMode.Create);
                await chartCopy.ExportToJsonStreamAsync(stream, settings.FormatOutput);
            }
            else
            {
                var json = await chartCopy.ExportToJsonAsync(settings.FormatOutput);
                await File.WriteAllTextAsync(output, json, cancellationToken);
            }
        }

        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}