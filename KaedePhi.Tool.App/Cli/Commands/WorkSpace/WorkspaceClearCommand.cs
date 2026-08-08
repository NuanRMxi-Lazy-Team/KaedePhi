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

    [CliHandler]
    private static int Handle(ParseResult result)
    {
        var ws = new WorkspaceService();
        ws.Clear(result.GetValue(IdOpt));
        ConsoleWriter.Info(CliLocalizationString.msg_cleared);
        return 0;
    }
}
