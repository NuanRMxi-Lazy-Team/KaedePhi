using KaedePhi.Tool.App.Cli.Infrastructure;

namespace KaedePhi.Tool.App.Cli.Commands.WorkSpace;

[CliCommand("load")]
public static partial class LoadCommand
{
    private static string Description => CliLocalizationString.cmd_load_desc;

    public static readonly Option<string?> InputOpt = SharedOptions.CreateInputPhieditOption();

    public static readonly Option<string> WorkspaceOpt = new("--workspace", "-w")
    {
        Description = CliLocalizationString.cli_opt_workspace_default_desc,
        Arity = ArgumentArity.ExactlyOne,
    };

    [CliHandler]
    private static async Task<int> HandleAsync(ParseResult result, CancellationToken ct)
    {
        var input = result.GetValue(InputOpt);
        if (string.IsNullOrWhiteSpace(input))
        {
            ConsoleWriter.Error(CliLocalizationString.err_input_required);
            return 1;
        }

        var workspaceId = result.GetValue(WorkspaceOpt);
        if (string.IsNullOrWhiteSpace(workspaceId))
            workspaceId = "default";

        var ws = new WorkspaceService();
        await ws.LoadAsync(workspaceId, input);
        ConsoleWriter.Info(string.Format(CliLocalizationString.msg_workspace_loaded, workspaceId));
        return 0;
    }
}
