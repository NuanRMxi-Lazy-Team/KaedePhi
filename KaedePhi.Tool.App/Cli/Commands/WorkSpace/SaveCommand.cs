using KaedePhi.Tool.App.Cli.Infrastructure;

namespace KaedePhi.Tool.App.Cli.Commands.WorkSpace;

[CliCommand("save")]
public static partial class SaveCommand
{
    private static string Description => CliLocalizationString.cmd_save_desc;

    public static readonly Option<string?> OutputOpt = SharedOptions.CreateOutputPathOption();

    public static readonly Option<string> WorkspaceOpt = new("--workspace", "-w")
    {
        Description = CliLocalizationString.cli_opt_workspace_default_desc,
        Arity = ArgumentArity.ExactlyOne,
    };

    [CliHandler]
    private static async Task<int> HandleAsync(ParseResult result, CancellationToken ct)
    {
        var output = result.GetValue(OutputOpt);
        if (string.IsNullOrWhiteSpace(output))
        {
            ConsoleWriter.Error(CliLocalizationString.err_output_required);
            return 1;
        }

        var workspaceId = result.GetValue(WorkspaceOpt);
        if (string.IsNullOrWhiteSpace(workspaceId))
            workspaceId = "default";

        var ws = new WorkspaceService();
        await ws.SaveAsync(workspaceId, output);
        ConsoleWriter.Info(
            string.Format(CliLocalizationString.msg_workspace_saved, workspaceId, output)
        );
        return 0;
    }
}
