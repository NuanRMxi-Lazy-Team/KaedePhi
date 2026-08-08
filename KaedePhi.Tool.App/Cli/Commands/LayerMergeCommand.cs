using KaedePhi.Tool.App.Cli.Infrastructure;
using KaedePhi.Tool.App.Config;
using KaedePhi.Tool.App.Shared;

namespace KaedePhi.Tool.App.Cli.Commands;

[CliCommand("layer-merge")]
public static partial class LayerMergeCommand
{
    private static string Description => CliLocalizationString.cmd_layer_merge_desc;

    private static readonly Option<string?> InputOpt = SharedOptions.CreateInputRpeOption();
    private static readonly Option<string?> OutputOpt = SharedOptions.CreateOutputAutoOption();
    private static readonly Option<string?> WorkspaceOpt = SharedOptions.CreateWorkspaceRpeOption();
    private static readonly Option<double> PrecisionOpt = SharedOptions.PrecisionOption;
    private static readonly Option<double> ToleranceOpt = SharedOptions.ToleranceOption;
    private static readonly Option<bool> ClassicOpt = SharedOptions.ClassicOption;
    private static readonly Option<bool> NoCompressOpt = SharedOptions.NoCompressOption;
    private static readonly Option<bool> DryRunOpt = SharedOptions.DryRunOption;

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

        var c = AppConfigService.Instance.Config.LayerMerge;
        var precision = SharedOptions.GetIfSpecified(result, PrecisionOpt) ?? c.Precision;
        var tolerance = SharedOptions.GetIfSpecified(result, ToleranceOpt) ?? c.Tolerance;
        var classic = SharedOptions.GetIfSpecified(result, ClassicOpt) ?? c.ClassicMode;
        var disableCompress =
            SharedOptions.GetIfSpecified(result, NoCompressOpt) ?? c.DisableCompress;
        var dryRun = SharedOptions.GetIfSpecified(result, DryRunOpt) ?? c.DryRun;

        if (disableCompress && !classic)
        {
            ConsoleWriter.Error(CliLocalizationString.err_classic_disablsed);
            return 1;
        }

        var svc = new ChartService();
        var kpc = await svc.LoadKpcAsync(input, workspace, ct);
        if (kpc == null)
        {
            ConsoleWriter.Error(CliLocalizationString.err_unimplemented);
            return 1;
        }

        var kpcClone = kpc.Clone();
        ChartProcessor.LayerMerge(
            kpcClone,
            precision,
            tolerance,
            classic,
            disableCompress,
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
