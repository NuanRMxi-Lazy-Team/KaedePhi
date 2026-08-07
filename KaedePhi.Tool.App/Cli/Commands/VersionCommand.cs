using System.Reflection;
using KaedePhi.Tool.App.Cli.Infrastructure;
using KaedePhi.Tool.App.Shared;

namespace KaedePhi.Tool.App.Cli.Commands;

public static class VersionCommand
{
    public static Command Create()
    {
        var cmd = new Command("version", CliHelper.L("cmd_version_desc"));
        cmd.Aliases.Add("ver");
        cmd.SetAction(_ =>
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
            return 0;        });
        return cmd;
    }
}
