using KaedePhi.Tool.App.Cli.Infrastructure;

namespace KaedePhi.Tool.App.Cli.Commands.WorkSpace;

[CliCommand("list")]
public static partial class WorkspaceListCommand
{
    private static string Description => CliLocalizationString.cmd_workspace_list_desc;

    [CliHandler]
    private static int Handle(ParseResult _)
    {
        var ws = new WorkspaceService();
        foreach (var id in ws.List())
            Console.WriteLine(id);
        return 0;
    }
}
