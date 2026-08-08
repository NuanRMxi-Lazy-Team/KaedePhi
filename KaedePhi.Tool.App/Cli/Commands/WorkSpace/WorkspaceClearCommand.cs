using KaedePhi.Tool.App.Cli.Infrastructure;

namespace KaedePhi.Tool.App.Cli.Commands.WorkSpace;

[CliCommand("clear")]
public static partial class WorkspaceClearCommand
{
    private static string Description => CliLocalizationString.cmd_workspace_clear_desc;

    public static readonly Option<string?> IdOpt = new("--id")
    {
        Description = CliLocalizationString.cli_opt_workspace_clear_id_desc,
        Arity = ArgumentArity.ZeroOrOne,
    };
    public static readonly Option<bool> AllOpt = new("--all")
    {
        Description = CliLocalizationString.cli_opt_workspace_clear_all_desc,
    };

    [CliHandler]
    private static int Handle(ParseResult result)
    {
        var ws = new WorkspaceService();
        var id = result.GetValue(IdOpt);
        var all = result.GetValue(AllOpt);
        if (all == !string.IsNullOrWhiteSpace(id))
        {
            ConsoleWriter.Error(CliLocalizationString.err_workspace_clear_selection);
            return 1;
        }

        ws.Clear(id);
        ConsoleWriter.Info(
            string.IsNullOrWhiteSpace(id)
                ? CliLocalizationString.msg_cleared
                : string.Format(CliLocalizationString.msg_workspace_cleared, id)
        );
        return 0;
    }
}
