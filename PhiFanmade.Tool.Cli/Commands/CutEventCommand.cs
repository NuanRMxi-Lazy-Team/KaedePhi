using PhiFanmade.Tool.Cli.Settings.Operation;
using PhiFanmade.Tool.PhiFanmadeNrc.Events;
using PhiFanmade.Tool.PhiFanmadeNrc.Layers;

namespace PhiFanmade.Tool.Cli.Commands;

/// <summary>
/// 切割事件命令
/// </summary>
public sealed class CutEventCommand : AsyncCommand<CutEventCommand.Settings>
{
    public sealed class Settings : OperationSettingsWithPrecisionToleranceAndCompress
    {
        protected override double? GetConfigPrecisionDefault() => AppConfig.CutConfig?.Precision;
        protected override double? GetConfigToleranceDefault() => AppConfig.CutConfig?.Tolerance;
        protected override bool? GetConfigDisableCompressDefault() => AppConfig.CutConfig?.DisableCompress;
        protected override bool? GetConfigDryRunDefault() => AppConfig.CutConfig?.DryRun;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        settings.ApplyConfigDefaults();
        var writer = new ConsoleWriter();
        var nrc = await settings.LoadNrcChartAsync(cancellationToken);

        if (nrc == null)
        {
            writer.Error(string.Format(Strings.cli_err_unimplemented));
            return 1;
        }

        var nrcCopy = nrc.Clone();
        for (var i = 0; i < nrcCopy.JudgeLineList.Count; i++)
        {
            var line = nrcCopy.JudgeLineList[i];
            line.EventLayers = NrcLayerTools.CutLayerEvents(line.EventLayers, settings.Precision);
            if (settings.DisableCompress) continue;
            foreach (var eventLayer in line.EventLayers.OfType<NrcCore.EventLayer>())
            {
                eventLayer.MoveXEvents = NrcEventTools.EventListCompress(eventLayer.MoveXEvents ?? [], settings.Tolerance);
                eventLayer.MoveYEvents = NrcEventTools.EventListCompress(eventLayer.MoveYEvents ?? [], settings.Tolerance);
                eventLayer.RotateEvents = NrcEventTools.EventListCompress(eventLayer.RotateEvents ?? [], settings.Tolerance);
                eventLayer.AlphaEvents = NrcEventTools.EventListCompress(eventLayer.AlphaEvents ?? [], settings.Tolerance);
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
