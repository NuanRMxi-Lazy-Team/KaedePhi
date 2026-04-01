using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;
using PhiFanmade.Tool.PhiFanmadeNrc;
using PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands;

/// <summary>
/// 解绑父级命令
/// </summary>
public sealed class FitEventCommand : AsyncCommand<FitEventCommand.Settings>
{
    public sealed class Settings : OperationSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = settings.CreateWriter();
        var nrc = await settings.LoadNrcChartAsync(cancellationToken);

        if (nrc == null)
        {
            writer.Error(string.Format(Strings.cli_err_unimplemented));
            return 1;
        }

        var nrcCopy = nrc.Clone();
        using var logSubscription = NrcToolLog.Subscribe(
            info: writer.Info,
            warning: writer.Warn,
            error: writer.Error,
            debug: writer.Info);

        for (var i = 0; i < nrc.JudgeLineList.Count; i++)
        {
            var jdl = nrc.JudgeLineList[i];
            for (var index = 0; index < jdl.EventLayers.Count; index++)
            {
                var eventLayer = jdl.EventLayers[index];
                if (eventLayer == null) continue;
                nrcCopy.JudgeLineList[i].EventLayers[index].MoveXEvents = NrcTool.Events.NrcEventTools.EventListFit(eventLayer.MoveXEvents,
                    settings.Precision, settings.Tolerance);
                nrcCopy.JudgeLineList[i].EventLayers[index].MoveYEvents = NrcTool.Events.NrcEventTools.EventListFit(eventLayer.MoveYEvents,
                    settings.Precision, settings.Tolerance);
                nrcCopy.JudgeLineList[i].EventLayers[index].AlphaEvents = NrcTool.Events.NrcEventTools.EventListFit(eventLayer.AlphaEvents,
                    settings.Precision, settings.Tolerance);
                nrcCopy.JudgeLineList[i].EventLayers[index].RotateEvents = NrcTool.Events.NrcEventTools.EventListFit(eventLayer.RotateEvents,
                    settings.Precision, settings.Tolerance);
            }
        }

        var output = await settings.SaveFromNrcAsync(nrcCopy, cancellationToken);
        if (output == null)
        {
            writer.Warn(Strings.cli_warn_rpe_convert);
            return 2;
        }

        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}