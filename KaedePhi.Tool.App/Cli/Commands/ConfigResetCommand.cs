using KaedePhi.Tool.App.Cli.Infrastructure;
using KaedePhi.Tool.App.Config;

namespace KaedePhi.Tool.App.Cli.Commands;

[CliCommand("reset")]
public static partial class ConfigResetCommand
{
    private static string Description => CliLocalizationString.cmd_config_reset_desc;

    [CliHandler]
    private static int Handle(ParseResult _)
    {
        var service = AppConfigService.Instance;
        service.ResetToDefaults();
        ConsoleWriter.Info(
            string.Format(CliLocalizationString.msg_config_reset_done, service.ConfigPath)
        );
        return 0;
    }
}