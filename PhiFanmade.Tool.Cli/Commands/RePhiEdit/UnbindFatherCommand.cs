using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;
using PhiFanmade.Tool.RePhiEdit;
using PhiFanmade.Tool.RePhiEdit.JudgeLines;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands.RePhiEdit;

/// <summary>
/// 解绑父级命令
/// </summary>
public sealed class RpeUnbindFatherCommand : AsyncCommand<RpeUnbindFatherCommand.Settings>
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

        // 订阅日志
        Action<string> onDebug = s => writer.Info(s);
        Action<string> onError = s => writer.Error(s);
        Action<string> onInfo = s => writer.Info(s);
        Action<string> onWarning = s => writer.Warn(s);
        RpeToolLog.OnDebug += onDebug;
        RpeToolLog.OnError += onError;
        RpeToolLog.OnInfo += onInfo;
        RpeToolLog.OnWarning += onWarning;

        for (var i = 0; i < chart.JudgeLineList.Count; i++)
        {
            if (chart.JudgeLineList[i].Father != -1)
                if (settings.Classic)
                    chartCopy.JudgeLineList[i] = await RpeJudgeLineTools.FatherUnbindAsync(
                        i, chart.JudgeLineList, settings.Precision, settings.Tolerance, !settings.DisableCompress);
                else
                    chartCopy.JudgeLineList[i] = await RpeJudgeLineTools.FatherUnbindPlusAsync(
                        i, chart.JudgeLineList, settings.Precision, settings.Tolerance);
        }

        // 取消订阅
        RpeToolLog.OnDebug -= onDebug;
        RpeToolLog.OnError -= onError;
        RpeToolLog.OnInfo -= onInfo;
        RpeToolLog.OnWarning -= onWarning;

        var output = settings.ResolveOutputPath();
        if (!settings.DryRun)
        {
            if (settings.StreamOutput)
            {
                await using var stream = new FileStream(output, FileMode.Create);
                await chartCopy.ExportToJsonStreamAsync(stream, settings.FormatOutput);
            }
            else
                await File.WriteAllTextAsync(output, await chartCopy.ExportToJsonAsync(settings.FormatOutput),
                    cancellationToken);
        }

        writer.Info(string.Format(Strings.cli_msg_written, output));
        return 0;
    }
}