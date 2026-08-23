using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using KaedePhi.Tool.App.Cli.Commands;
using KaedePhi.Tool.App.Cli.Commands.Test;
using KaedePhi.Tool.App.Cli.Commands.WorkSpace;
using KaedePhi.Tool.App.Cli.Infrastructure;
using KaedePhi.Tool.App.Config;

namespace KaedePhi.Tool.App;

internal static partial class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Contains("--gui"))
            return RunGui(args);
        // 若存在非 GUI 参数，无论终端状态均走 CLI，避免重定向/管道时误启动 GUI
        var effectiveArgs = args.Where(a => a is not "--cli" and not "--gui").ToArray();
        if (
            args.Contains("--cli")
            || effectiveArgs.Length > 0
            || TerminalDetector.Instance.IsInteractiveTerminal()
        )
            return RunCli(args).GetAwaiter().GetResult();

        return RunGui(args);
    }

    private static int RunGui(string[] args)
    {
#if !Debug
        // Release/PreRelease：释放控制台，避免 GUI 进程残留终端窗口
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            FreeConsole();
#endif
        // 在主线程启动 Avalonia，满足 Windows STA 与 macOS 主线程亲和性，并保留生命周期退出码
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FreeConsole();

    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();

    private static async Task<int> RunCli(string[] args)
    {
        ConsoleWriter.LogLevel = AppConfigService.Instance.Config.LogLevel;
#if !Release
        ConsoleWriter.Warn(
            string.Format(
                CliLocalizationString.warn_unstable_version,
                CliLocalizationString.project_link
            )
        );
#endif

        var root = new RootCommand(CliLocalizationString.app_description);

        var ver = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        root.SetAction(_ =>
        {
            Console.WriteLine($@"{CliLocalizationString.app_title} v{ver}");
            return 0;
        });

        root.Add(VersionCommand.Create());

        var testCmd = GetTypeTestCommand.Create();
        testCmd.Hidden = true;
        root.Add(testCmd);

        var peStreamCmd = OnlyStreamLoadCommand.Create();
        peStreamCmd.Hidden = true;
        root.Add(peStreamCmd);

        root.Add(LoadCommand.Create());
        root.Add(SaveCommand.Create());
        root.Add(ConvertCommand.Create());
        root.Add(FitEventCommand.Create());
        root.Add(CutEventCommand.Create());
        root.Add(LayerMergeCommand.Create());
        root.Add(UnbindFatherCommand.Create());
        root.Add(RenderCommand.Create());

        var configBranch = new Command("config", CliLocalizationString.branch_config_desc)
        {
            ConfigResetCommand.Create(),
        };
        root.Add(configBranch);

        var workspaceBranch = new Command("workspace", CliLocalizationString.branch_workspace_desc)
        {
            WorkspaceListCommand.Create(),
            WorkspaceClearCommand.Create(),
            LoadCommand.Create(),
            SaveCommand.Create(),
        };
        root.Add(workspaceBranch);

        try
        {
            var cliArgs = args.Where(a => a is not "--cli" and not "--gui").ToArray();
            return await root.Parse(cliArgs).InvokeAsync();
        }
        catch (OperationCanceledException)
        {
            ConsoleWriter.Warn(CliLocalizationString.err_cancelled);
            return 130;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (ex is OutOfMemoryException)
            {
                ConsoleWriter.Error(string.Format(CliLocalizationString.err_out_of_memory, ex));
                return 1;
            }

            ConsoleWriter.Error(string.Format(CliLocalizationString.err_ukerr, ex));
            return 1;
        }
    }
}
