using System.Reflection;

namespace KaedePhi.Tool.App.Cli.Commands;

[CliCommand("version", Aliases = ["ver"])]
public static partial class VersionCommand
{
    private static string Description => CliLocalizationString.cmd_version_desc;

    [CliHandler]
    private static int Handle(ParseResult _)
    {
#if PreRelease || Release
        var ver = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
#else
        var ver = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
#endif
        Console.WriteLine($"{CliLocalizationString.app_title} v{ver}");
        return 0;
    }
}
