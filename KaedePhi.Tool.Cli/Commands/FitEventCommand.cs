using System.Globalization;
using KaedePhi.Core.KaedePhi.Events;
using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Event.KaedePhi;

namespace KaedePhi.Tool.Cli.Commands;

public static class FitEventCommand
{
    private static string L(string key) =>
        CliLocalizationString.ResourceManager.GetString(key, CultureInfo.CurrentUICulture)
        ?? CliLocalizationString.ResourceManager.GetString(key, CultureInfo.CurrentCulture)
        ?? key;

    private static readonly Option<string?> InputOpt = SharedOptions.CreateInputRpeOption();
    private static readonly Option<string?> OutputOpt = SharedOptions.CreateOutputAutoOption();
    private static readonly Option<string?> WorkspaceOpt = SharedOptions.CreateWorkspaceRpeOption();
    private static readonly Option<double> ToleranceOpt = SharedOptions.ToleranceOption;
    private static readonly Option<bool> DryRunOpt = SharedOptions.DryRunOption;

    public static Command Create()
    {
        var cmd = new Command("fit", L("fit_command_desc"));
        cmd.Aliases.Add("fit-event");
        cmd.Add(InputOpt);
        cmd.Add(OutputOpt);
        cmd.Add(WorkspaceOpt);
        cmd.Add(ToleranceOpt);
        cmd.Add(DryRunOpt);

        cmd.SetAction(
            async (result, ct) =>
            {
                var input = result.GetValue(InputOpt);
                var workspace = result.GetValue(WorkspaceOpt);
                if (string.IsNullOrWhiteSpace(input) && string.IsNullOrWhiteSpace(workspace))
                {
                    ConsoleWriter.Error(CliLocalizationString.err_input_required);
                    return 1;
                }

                var config = AppConfigHelper.Load();
                var c = config.FitConfig;
                var tolerance = SharedOptions.GetIfSpecified(result, ToleranceOpt) ?? c.Tolerance;
                var dryRun = SharedOptions.GetIfSpecified(result, DryRunOpt) ?? c.DryRun;

                var svc = new ChartService();
                var kpc = await svc.LoadKpcAsync(input, workspace, ct);
                if (kpc == null)
                {
                    ConsoleWriter.Error(CliLocalizationString.err_unimplemented);
                    return 1;
                }

                var kpcClone = kpc.Clone();

                var mxFitter = new EventFit<double>();
                var myFitter = new EventFit<double>();
                var alFitter = new EventFit<int>();
                var roFitter = new EventFit<double>();
                var spFitter = new EventFit<float>();
                mxFitter.SubscribeLog(
                    ConsoleWriter.Info,
                    ConsoleWriter.Warn,
                    ConsoleWriter.Error,
                    ConsoleWriter.Debug
                );
                myFitter.SubscribeLog(
                    ConsoleWriter.Info,
                    ConsoleWriter.Warn,
                    ConsoleWriter.Error,
                    ConsoleWriter.Debug
                );
                alFitter.SubscribeLog(
                    ConsoleWriter.Info,
                    ConsoleWriter.Warn,
                    ConsoleWriter.Error,
                    ConsoleWriter.Debug
                );
                roFitter.SubscribeLog(
                    ConsoleWriter.Info,
                    ConsoleWriter.Warn,
                    ConsoleWriter.Error,
                    ConsoleWriter.Debug
                );
                spFitter.SubscribeLog(
                    ConsoleWriter.Info,
                    ConsoleWriter.Warn,
                    ConsoleWriter.Error,
                    ConsoleWriter.Debug
                );

                foreach (var line in kpcClone.JudgeLineList)
                {
                    foreach (var el in line.EventLayers.OfType<EventLayer>())
                    {
                        ct.ThrowIfCancellationRequested();

                        el.MoveXEvents = mxFitter.FitEvents(el.MoveXEvents, tolerance);
                        el.MoveYEvents = myFitter.FitEvents(el.MoveYEvents, tolerance);
                        el.AlphaEvents = alFitter.FitEvents(el.AlphaEvents, tolerance);
                        el.RotateEvents = roFitter.FitEvents(el.RotateEvents, tolerance);
                        el.SpeedEvents = spFitter.FitEvents(el.SpeedEvents, tolerance);
                    }
                }

                var output = await ChartService.SaveAsRpeAsync(
                    kpcClone,
                    svc.ResolveOutputPath(input, result.GetValue(OutputOpt), workspace),
                    dryRun,
                    ct
                );
                ConsoleWriter.Info(string.Format(CliLocalizationString.msg_written, output));
                return 0;
            }
        );

        return cmd;
    }
}
