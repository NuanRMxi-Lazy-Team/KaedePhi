using System.Globalization;

namespace KaedePhi.Tool.Cli.Infrastructure;

public static class SharedOptions
{
    private static string L(string key) =>
        CliLocalizationString.ResourceManager.GetString(key, CultureInfo.CurrentUICulture)
        ?? CliLocalizationString.ResourceManager.GetString(key, CultureInfo.CurrentCulture)
        ?? key;

    public static Option<string?> CreateInputRpeOption() =>
        new("--input", "-i")
        {
            Description = L("cli_opt_input_rpe_desc"),
            Arity = ArgumentArity.ZeroOrOne,
        };

    public static Option<string?> CreateInputPhieditOption() =>
        new("--input", "-i")
        {
            Description = L("cli_opt_input_phiedit_desc"),
            Arity = ArgumentArity.ZeroOrOne,
        };

    public static Option<string?> CreateOutputAutoOption() =>
        new("--output", "-o")
        {
            Description = L("cli_opt_output_auto_desc"),
            Arity = ArgumentArity.ZeroOrOne,
        };

    public static Option<string?> CreateOutputPathOption() =>
        new("--output", "-o")
        {
            Description = L("cli_opt_output_path_desc"),
            Arity = ArgumentArity.ZeroOrOne,
        };

    public static Option<string?> CreateWorkspaceRpeOption() =>
        new("--workspace", "-w")
        {
            Description = L("cli_opt_workspace_rpe_desc"),
            Arity = ArgumentArity.ZeroOrOne,
        };

    public static Option<string?> CreateWorkspaceDefaultOption() =>
        new("--workspace", "-w")
        {
            Description = L("cli_opt_workspace_default_desc"),
            Arity = ArgumentArity.ZeroOrOne,
        };

    public static Option<double> PrecisionOption { get; } =
        new("--precision", "-p")
        {
            Description = L("cli_opt_precision_desc"),
            Arity = ArgumentArity.ExactlyOne,
        };

    public static Option<double> ToleranceOption { get; } =
        new("--tolerance", "-t")
        {
            Description = L("cli_opt_tolerance_desc"),
            Arity = ArgumentArity.ExactlyOne,
        };

    public static Option<bool> ClassicOption { get; } =
        new("--classic") { Description = L("cli_opt_classic_mode_desc") };

    public static Option<bool> NoCompressOption { get; } =
        new("--no-compress") { Description = L("cli_opt_compress_desc") };

    public static Option<bool> DryRunOption { get; } =
        new("--dry-run") { Description = L("cli_opt_dry_run_desc") };

    public static Option<bool> StreamOutputOption { get; } =
        new("--stream") { Description = L("cli_opt_stream_output_desc") };

    public static Option<bool> FormatOutputOption { get; } =
        new("--format") { Description = L("cli_opt_format_desc") };

    public static T? GetIfSpecified<T>(ParseResult result, Option<T> option)
        where T : struct
    {
        return result.GetResult(option) is not null ? result.GetValue(option) : null;
    }
}
