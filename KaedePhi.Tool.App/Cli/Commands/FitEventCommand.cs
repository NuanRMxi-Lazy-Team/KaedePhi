using KaedePhi.Tool.App.Cli.Infrastructure;
using KaedePhi.Tool.App.Config;
using KaedePhi.Tool.App.Shared;

namespace KaedePhi.Tool.App.Cli.Commands;

[CliCommand("fit", Aliases = ["fit-event"])]
public static partial class FitEventCommand
{
    private static string Description => CliLocalizationString.fit_command_desc;

    public static readonly Option<string?> InputOpt = SharedOptions.CreateInputRpeOption();
    public static readonly Option<string?> OutputOpt = SharedOptions.CreateOutputAutoOption();
    public static readonly Option<string?> WorkspaceOpt = SharedOptions.CreateWorkspaceRpeOption();
    public static readonly Option<double> ToleranceOpt = SharedOptions.ToleranceOption;
    public static readonly Option<bool> DryRunOpt = SharedOptions.DryRunOption;

    [CliHandler]
    private static async Task<int> HandleAsync(ParseResult result, CancellationToken ct)
    {
        var input = result.GetValue(InputOpt);
        var workspace = result.GetValue(WorkspaceOpt);
        if (string.IsNullOrWhiteSpace(input) && string.IsNullOrWhiteSpace(workspace))
        {
            ConsoleWriter.Error(CliLocalizationString.err_input_required);
            return 1;
        }

        var c = AppConfigService.Instance.Config.Fit;
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
        ChartProcessor.FitEvent(
            kpcClone,
            tolerance,
            ConsoleWriter.Info,
            ConsoleWriter.Warn,
            ConsoleWriter.Error,
            ConsoleWriter.Debug,
            ConsoleWriter.CreateProgress(),
            ct
        );

        var output = await ChartService.SaveAsRpeAsync(
            kpcClone,
            svc.ResolveOutputPath(input, result.GetValue(OutputOpt), workspace),
            dryRun,
            ct
        );
        ConsoleWriter.Info(string.Format(CliLocalizationString.msg_written, output));
        return 0;
    }
}
