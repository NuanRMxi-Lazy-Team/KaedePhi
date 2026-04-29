using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Cli.Model;
using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KaedePhi.Tool.Cli.Settings;

/// <summary>
/// 所有操作命令的通用 CLI 参数基类：输入、输出、工作区、精度、容差、模式开关。
/// </summary>
public class OperationSettings : CommandSettings
{
    [CommandOption("-i|--input <PATH>")]
    [LocalizedDescription("cli_opt_input_rpe_desc")]
    public string? Input { get; set; }

    [CommandOption("-o|--output <PATH>")]
    [LocalizedDescription("cli_opt_output_auto_desc")]
    public string? Output { get; set; }

    [CommandOption("-w|--workspace <ID>")]
    [LocalizedDescription("cli_opt_workspace_rpe_desc")]
    public string? Workspace { get; set; }

    [CommandOption("-p|--precision <N>")]
    [LocalizedDescription("cli_opt_precision_desc")]
    public double? Precision { get; set; }

    [CommandOption("-t|--tolerance <N>")]
    [LocalizedDescription("cli_opt_tolerance_desc")]
    public double? Tolerance { get; set; }

    [CommandOption("--classic")]
    [LocalizedDescription("cli_opt_classic_mode_desc")]
    public bool? Classic { get; set; }

    [CommandOption("--no-compress")]
    [LocalizedDescription("cli_opt_compress_desc")]
    public bool? DisableCompress { get; set; }

    [CommandOption("--dry-run")]
    [LocalizedDescription("cli_opt_dry_run_desc")]
    public bool? DryRun { get; set; }

    [CommandOption("--stream")]
    [LocalizedDescription("cli_opt_stream_output_desc")]
    public bool? StreamOutput { get; set; }

    [CommandOption("--format")]
    [LocalizedDescription("cli_opt_format_desc")]
    public bool? FormatOutput { get; set; }

    /// <summary>从 config.yaml 加载的配置实例。</summary>
    public AppConfig AppConfig { get; }

    protected OperationSettings()
    {
        AppConfig = LoadOrCreateConfig();
    }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Input) && string.IsNullOrWhiteSpace(Workspace))
            return ValidationResult.Error(Strings.cli_err_input_required);
        return base.Validate();
    }

    private static AppConfig LoadOrCreateConfig()
    {
        if (File.Exists("config.yaml"))
        {
            var text = File.ReadAllText("config.yaml");
            return new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build()
                .Deserialize<AppConfig>(text);
        }

        var config = new AppConfig();
        var yaml = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build()
            .Serialize(config);
        File.WriteAllText("config.yaml", yaml);
        return config;
    }
}
