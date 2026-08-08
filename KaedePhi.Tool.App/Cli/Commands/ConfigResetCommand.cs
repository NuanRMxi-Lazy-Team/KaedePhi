using KaedePhi.Tool.App.Cli.Infrastructure;
using KaedePhi.Tool.App.Cli.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KaedePhi.Tool.App.Cli.Commands;

[CliCommand("reset")]
public static partial class ConfigResetCommand
{
    private static string Description => CliLocalizationString.cmd_config_reset_desc;

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    [CliHandler]
    private static int Handle(ParseResult _)
    {
        var configPath = "config.yaml";
        var defaults = new AppConfig();
        var yaml = YamlSerializer.Serialize(defaults);
        File.WriteAllText(configPath, yaml);
        ConsoleWriter.Info(string.Format(CliLocalizationString.msg_config_reset_done, configPath));
        return 0;
    }
}
