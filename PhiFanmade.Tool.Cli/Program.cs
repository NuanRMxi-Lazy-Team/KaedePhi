using System.Reflection;
using PhiFanmade.Tool.Cli.Commands;
using PhiFanmade.Tool.Cli.Infrastructure;
using PhiFanmade.Tool.Localization;
using Spectre.Console.Cli;

var app = new CommandApp();
app.SetDefaultCommand<VersionCommand>();
app.Configure(config =>
{
    config.SetApplicationName("phi-tool");
    config.SetApplicationVersion(
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown");

    // 未知命令/参数时立即报错，而非静默跳过
    config.UseStrictParsing();

    // 统一异常处理，保持与原先相同的错误输出风格
    config.SetExceptionHandler((ex, _) =>
    {
        new ConsoleWriter().Error(string.Format(Strings.cli_err_ukerr, ex));
        return 1;
    });

    config.AddCommand<VersionCommand>("version")
        .WithDescription(Strings.cli_cmd_version_desc)
        .WithAlias("ver");

    config.AddCommand<LoadCommand>("load")
        .WithDescription(Strings.cli_cmd_load_desc);

    config.AddCommand<SaveCommand>("save")
        .WithDescription(Strings.cli_cmd_save_desc);

    config.AddBranch("workspace", ws =>
    {
        ws.SetDescription(Strings.cli_branch_workspace_desc);
        ws.AddCommand<WorkspaceListCommand>("list")
            .WithDescription(Strings.cli_cmd_workspace_list_desc);
        ws.AddCommand<WorkspaceClearCommand>("clear")
            .WithDescription(Strings.cli_cmd_workspace_clear_desc);
    });

    config.AddBranch("rpe", rpe =>
    {
        rpe.SetDescription(Strings.cli_branch_rpe_desc);
        rpe.AddCommand<RpeUnbindFatherCommand>("unbind-father")
            .WithDescription(Strings.cli_cmd_rpe_unbind_father_desc)
            .WithAlias("unbind");
        rpe.AddCommand<RpeLayerMergeCommand>("layer-merge")
            .WithDescription(Strings.cli_cmd_rpe_layer_merge_desc);
        rpe.AddCommand<RpeConvertCommand>("convert")
            .WithDescription(Strings.cli_cmd_rpe_convert_desc);
    });
});

return await app.RunAsync(args);