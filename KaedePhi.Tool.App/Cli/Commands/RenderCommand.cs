using KaedePhi.Tool.App.Cli.Infrastructure;
using KaedePhi.Tool.App.Config;
using KaedePhi.Tool.App.Shared;
using KaedePhi.Tool.Render.KaedePhi;

namespace KaedePhi.Tool.App.Cli.Commands;

[CliCommand("render-event", Aliases = ["render"])]
public static partial class RenderCommand
{
    private static string Description => CliLocalizationString.render_command_desc;

    private static readonly Option<string?> InputOpt = SharedOptions.CreateInputRpeOption();
    private static readonly Option<string?> OutputOpt = SharedOptions.CreateOutputAutoOption();
    private static readonly Option<string?> WorkspaceOpt = SharedOptions.CreateWorkspaceRpeOption();

    private static readonly Option<float> PixelsPerBeatOpt = new("--pixels-per-beat", "-r")
    {
        Description = CliLocalizationString.render_opt_pixels_per_beat,
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<int> ChannelWidthOpt = new("--channel-width")
    {
        Description = CliLocalizationString.render_opt_channel_width,
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<int> SamplesPerEventOpt = new("--samples")
    {
        Description = CliLocalizationString.render_opt_samples,
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<int> BeatSubdivisionsOpt = new("--beat-subdivisions", "-b")
    {
        Description = CliLocalizationString.render_opt_beat_subdivisions,
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<int> LineIndexOpt = new("--line")
    {
        Description = CliLocalizationString.render_opt_line,
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<int> LayerIndexOpt = new("--layer")
    {
        Description = CliLocalizationString.render_opt_layer,
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<double> RangePaddingRatioOpt = new("--range-padding-ratio")
    {
        Description = CliLocalizationString.render_opt_range_padding_ratio,
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<int> RangeSamplesOpt = new("--range-samples")
    {
        Description = CliLocalizationString.render_opt_range_samples,
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<double> SegmentToleranceOpt = new("--segment-tolerance")
    {
        Description = CliLocalizationString.render_opt_segment_tolerance,
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<double> MinRangeHalfOpt = new("--min-range-half")
    {
        Description = CliLocalizationString.render_opt_min_range_half,
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<double> MinRangeHalfRatioOpt = new("--min-range-half-ratio")
    {
        Description = CliLocalizationString.render_opt_min_range_half_ratio,
        Arity = ArgumentArity.ZeroOrOne,
    };

    [CliHandler]
    private static async Task<int> HandleAsync(ParseResult result, CancellationToken ct)
    {
        var input = result.GetValue(InputOpt);
        var workspace = result.GetValue(WorkspaceOpt);
        if (string.IsNullOrWhiteSpace(input) && string.IsNullOrWhiteSpace(workspace))
        {
            ConsoleWriter.Error(CliLocalizationString.err_input_required);
            return 1;
        }

        var c = AppConfigService.Instance.Config.Render;

        var svc = new ChartService();
        var kpc = await svc.LoadKpcAsync(input, workspace, ct);
        if (kpc == null)
        {
            ConsoleWriter.Error(CliLocalizationString.render_err_load_failed);
            return 1;
        }

        string? outputDir;
        var outputValue = result.GetValue(OutputOpt);
        if (!string.IsNullOrWhiteSpace(outputValue))
            outputDir = outputValue;
        else
            outputDir = !string.IsNullOrWhiteSpace(input)
                ? Path.Combine(Path.GetDirectoryName(input) ?? ".", "render_output")
                : Path.Combine(Directory.GetCurrentDirectory(), "render_output");

        ConsoleWriter.Info(string.Format(CliLocalizationString.render_msg_start, outputDir));

        var opts = new KpcRenderOptions
        {
            PixelsPerBeat =
                SharedOptions.GetIfSpecified(result, PixelsPerBeatOpt) ?? c.PixelsPerBeat,
            ChannelWidth = SharedOptions.GetIfSpecified(result, ChannelWidthOpt) ?? c.ChannelWidth,
            SamplesPerEvent =
                SharedOptions.GetIfSpecified(result, SamplesPerEventOpt) ?? c.SamplesPerEvent,
            BeatSubdivisions =
                SharedOptions.GetIfSpecified(result, BeatSubdivisionsOpt) ?? c.BeatSubdivisions,
            RangePaddingRatio =
                SharedOptions.GetIfSpecified(result, RangePaddingRatioOpt) ?? c.RangePaddingRatio,
            RangeSamplesPerEvent =
                SharedOptions.GetIfSpecified(result, RangeSamplesOpt) ?? c.RangeSamplesPerEvent,
            SegmentGroupTolerance =
                SharedOptions.GetIfSpecified(result, SegmentToleranceOpt)
                ?? c.SegmentGroupTolerance,
            MinValueRangeHalf =
                SharedOptions.GetIfSpecified(result, MinRangeHalfOpt) ?? c.MinValueRangeHalf,
            MinValueRangeHalfRatio =
                SharedOptions.GetIfSpecified(result, MinRangeHalfRatioOpt)
                ?? c.MinValueRangeHalfRatio,
        };

        try
        {
            var lineIndex = SharedOptions.GetIfSpecified(result, LineIndexOpt);
            var layerIndex = SharedOptions.GetIfSpecified(result, LayerIndexOpt);
            var files = ChartProcessor.Render(
                kpc,
                outputDir,
                opts,
                lineIndex,
                layerIndex,
                ConsoleWriter.Info,
                ConsoleWriter.Warn,
                ConsoleWriter.Error
            );
            if (files.Count == 0)
                ConsoleWriter.Warn(CliLocalizationString.render_warn_nothing);
            else
                ConsoleWriter.Info(
                    string.Format(CliLocalizationString.render_msg_done, files.Count, outputDir)
                );
        }
        catch (Exception ex)
        {
            ConsoleWriter.Error(
                string.Format(CliLocalizationString.render_err_render_failed, ex.Message)
            );
            return 2;
        }

        return 0;
    }
}
