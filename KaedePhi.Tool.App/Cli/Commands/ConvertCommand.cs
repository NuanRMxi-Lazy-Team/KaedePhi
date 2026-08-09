using KaedePhi.Tool.App.Cli.Infrastructure;
using KaedePhi.Tool.App.Config;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Converter.PhiFans.Model;
using KaedePhi.Tool.Converter.Phigros.v3.Model;

namespace KaedePhi.Tool.App.Cli.Commands;

[CliCommand("convert")]
public static partial class ConvertCommand
{
    private static string Description => CliLocalizationString.convert_command_desc;

    #region 共享选项

    private static readonly Option<string?> InputOpt = SharedOptions.CreateInputRpeOption();
    private static readonly Option<string?> OutputOpt = SharedOptions.CreateOutputAutoOption();
    private static readonly Option<string?> WorkspaceOpt = SharedOptions.CreateWorkspaceRpeOption();
    private static readonly Option<double> PrecisionOpt = SharedOptions.PrecisionOption;
    private static readonly Option<double> ToleranceOpt = SharedOptions.ToleranceOption;
    private static readonly Option<bool> ClassicOpt = SharedOptions.ClassicOption;
    private static readonly Option<bool> NoCompressOpt = SharedOptions.NoCompressOption;
    private static readonly Option<bool> DryRunOpt = SharedOptions.DryRunOption;
    private static readonly Option<bool> StreamOpt = SharedOptions.StreamOutputOption;
    private static readonly Option<bool> FormatOpt = SharedOptions.FormatOutputOption;

    #endregion

    #region 专属选项

    private static readonly Option<ChartType?> TargetTypeOpt = new("--target")
    {
        Description = CliLocalizationString.convert_command_opt_target,
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<double> PeTrailingPaddingOpt = new("--pe-trailing-padding")
    {
        Description = CliLocalizationString.convert_opt_pe_trailing_padding,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PeEasingPrecisionOpt = new("--pe-easing-precision")
    {
        Description = CliLocalizationString.convert_opt_pe_easing_precision,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PeXyPrecisionOpt = new("--pe-xy-precision")
    {
        Description = CliLocalizationString.convert_opt_pe_xy_precision,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PeAlphaPrecisionOpt = new("--pe-alpha-precision")
    {
        Description = CliLocalizationString.convert_opt_pe_alpha_precision,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PeAlphaToleranceOpt = new("--pe-alpha-tolerance")
    {
        Description = CliLocalizationString.convert_opt_pe_alpha_tolerance,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PeSpeedPrecisionOpt = new("--pe-speed-precision")
    {
        Description = CliLocalizationString.convert_opt_pe_speed_precision,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PeSpeedToleranceOpt = new("--pe-speed-tolerance")
    {
        Description = CliLocalizationString.convert_opt_pe_speed_tolerance,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<float> PhigrosBpmOpt = new("--phigros-bpm")
    {
        Description = CliLocalizationString.convert_opt_phigros_bpm,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PhigrosEasingPrecisionOpt = new(
        "--phigros-easing-precision"
    )
    {
        Description = CliLocalizationString.convert_opt_phigros_easing_precision,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PhigrosXyPrecisionOpt = new("--phigros-xy-precision")
    {
        Description = CliLocalizationString.convert_opt_phigros_xy_precision,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PhigrosAlphaPrecisionOpt = new(
        "--phigros-alpha-precision"
    )
    {
        Description = CliLocalizationString.convert_opt_phigros_alpha_precision,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PhigrosAlphaToleranceOpt = new(
        "--phigros-alpha-tolerance"
    )
    {
        Description = CliLocalizationString.convert_opt_phigros_alpha_tolerance,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PhigrosSpeedPrecisionOpt = new(
        "--phigros-speed-precision"
    )
    {
        Description = CliLocalizationString.convert_opt_phigros_speed_precision,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<int> PhiFansEasingPrecisionOpt = new(
        "--phifans-easing-precision"
    )
    {
        Description = CliLocalizationString.convert_opt_phifans_easing_precision,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<int> PhiFansDiscontinuityPrecisionOpt = new(
        "--phifans-discontinuity-precision"
    )
    {
        Description = CliLocalizationString.convert_opt_phifans_discontinuity_precision,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> UnbindPrecisionOpt = new("--unbind-precision")
    {
        Description = CliLocalizationString.convert_opt_unbind_precision,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> UnbindToleranceOpt = new("--unbind-tolerance")
    {
        Description = CliLocalizationString.convert_opt_unbind_tolerance,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> UnbindMergeToleranceOpt = new(
        "--unbind-merge-tolerance"
    )
    {
        Description = CliLocalizationString.convert_opt_unbind_merge_tolerance,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<bool> UnbindClassicOpt = new("--unbind-classic")
    {
        Description = CliLocalizationString.convert_opt_unbind_classic,
    };

    private static readonly Option<double> MergePrecisionOpt = new("--merge-precision")
    {
        Description = CliLocalizationString.convert_opt_merge_precision,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> MergeToleranceOpt = new("--merge-tolerance")
    {
        Description = CliLocalizationString.convert_opt_merge_tolerance,
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<bool> MergeClassicOpt = new("--merge-classic")
    {
        Description = CliLocalizationString.convert_opt_merge_classic,
    };

    private static readonly Option<bool> NoUnbindCompressOpt = new("--no-unbind-compress")
    {
        Description = CliLocalizationString.convert_opt_no_unbind_compress,
    };

    private static readonly Option<bool> NoMergeCompressOpt = new("--no-merge-compress")
    {
        Description = CliLocalizationString.convert_opt_no_merge_compress,
    };

    private static readonly Option<bool> RemoveAttachUiOpt = new("--remove-attach-ui")
    {
        Description = CliLocalizationString.convert_opt_remove_attach_ui,
    };

    private static readonly Option<bool> RemoveTextureOpt = new("--remove-texture")
    {
        Description = CliLocalizationString.convert_opt_remove_texture,
    };

    private static readonly Option<bool> FilterFakeNotesOpt = new("--filter-fake-notes")
    {
        Description = CliLocalizationString.convert_opt_filter_fake_notes,
    };

    private static readonly Option<bool> NegativeAlphaElevationOpt = new(
        "--negative-alpha-elevation"
    )
    {
        Description = CliLocalizationString.convert_opt_negative_alpha_elevation,
    };

    private static readonly Option<double> NegativeAlphaStepOpt = new("--negative-alpha-step")
    {
        Description = CliLocalizationString.convert_opt_negative_alpha_step,
        Arity = ArgumentArity.ExactlyOne,
    };

    #endregion

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

        var c = AppConfigService.Instance.Config.Convert;

        var svc = new ChartService();
        var kpc = await svc.LoadKpcAsync(input, workspace, ct);
        if (kpc == null)
        {
            ConsoleWriter.Error(CliLocalizationString.err_unimplemented);
            return 1;
        }

        var targetType = result.GetValue(TargetTypeOpt) ?? c.TargetType;
        var targetExtension = "." + (ChartFormatRegistry.Find(targetType)?.FileExtension ?? "json");
        var output = svc.ResolveOutputPath(
            input,
            result.GetValue(OutputOpt),
            workspace,
            targetExtension
        );
        var streamOutput = SharedOptions.GetIfSpecified(result, StreamOpt) ?? c.StreamOutput;
        var formatOutput = SharedOptions.GetIfSpecified(result, FormatOpt) ?? c.FormatOutput;
        var dryRun = SharedOptions.GetIfSpecified(result, DryRunOpt) ?? c.DryRun;
        var disableUnbindCompress = result.GetValue(NoUnbindCompressOpt);
        var disableMergeCompress = result.GetValue(NoMergeCompressOpt);

        var peOptions = new KpcToPhiEditConvertOptions
        {
            TrailingBeatPadding =
                SharedOptions.GetIfSpecified(result, PeTrailingPaddingOpt)
                ?? c.PeTrailingBeatPadding,
            Cutting = new KpcToPhiEditConvertOptions.CuttingOptions
            {
                UnsupportedEasingPrecision =
                    SharedOptions.GetIfSpecified(result, PeEasingPrecisionOpt)
                    ?? c.PeUnsupportedEasingPrecision,
                MisalignedXyEventPrecision =
                    SharedOptions.GetIfSpecified(result, PeXyPrecisionOpt)
                    ?? c.PeMisalignedXyEventPrecision,
            },
            Alpha = new KpcToPhiEditConvertOptions.AlphaOptions
            {
                CutPrecision =
                    SharedOptions.GetIfSpecified(result, PeAlphaPrecisionOpt)
                    ?? c.PeAlphaCutPrecision,
                CutCompress = c.PeAlphaCutCompress,
                CutTolerance =
                    SharedOptions.GetIfSpecified(result, PeAlphaToleranceOpt)
                    ?? c.PeAlphaCutTolerance,
            },
            Speed = new KpcToPhiEditConvertOptions.SpeedOptions
            {
                CutPrecision =
                    SharedOptions.GetIfSpecified(result, PeSpeedPrecisionOpt)
                    ?? c.PeSpeedCutPrecision,
                CutCompress = c.PeSpeedCutCompress,
                CutTolerance =
                    SharedOptions.GetIfSpecified(result, PeSpeedToleranceOpt)
                    ?? c.PeSpeedCutTolerance,
            },
            FatherLineUnbind = new KpcToPhiEditConvertOptions.FatherLineUnbindOptions
            {
                Precision =
                    SharedOptions.GetIfSpecified(result, UnbindPrecisionOpt)
                    ?? SharedOptions.GetIfSpecified(result, PrecisionOpt)
                    ?? c.UnbindPrecision,
                Tolerance =
                    SharedOptions.GetIfSpecified(result, UnbindToleranceOpt)
                    ?? SharedOptions.GetIfSpecified(result, ToleranceOpt)
                    ?? c.UnbindTolerance,
                MergeTolerance =
                    SharedOptions.GetIfSpecified(result, UnbindMergeToleranceOpt)
                    ?? c.UnbindMergeTolerance,
                ClassicMode =
                    SharedOptions.GetIfSpecified(result, UnbindClassicOpt)
                    ?? SharedOptions.GetIfSpecified(result, ClassicOpt)
                    ?? c.UnbindClassicMode,
                Compress = !disableUnbindCompress,
            },
            MultiLayerMerge = new KpcToPhiEditConvertOptions.MultiLayerMergeOptions
            {
                Precision =
                    SharedOptions.GetIfSpecified(result, MergePrecisionOpt)
                    ?? c.MultiLayerMergePrecision,
                Tolerance =
                    SharedOptions.GetIfSpecified(result, MergeToleranceOpt)
                    ?? c.MultiLayerMergeTolerance,
                ClassicMode =
                    SharedOptions.GetIfSpecified(result, MergeClassicOpt)
                    ?? c.MultiLayerMergeClassicMode,
                Compress = !disableMergeCompress,
            },
            LineFilter = new KpcToPhiEditConvertOptions.LineFilterOptions
            {
                RemoveAttachUiLine = result.GetValue(RemoveAttachUiOpt),
                RemoveTextureLine = result.GetValue(RemoveTextureOpt),
            },
        };

        var phigrosOptions = new KpcToPhigrosV3ConvertOptions
        {
            DefaultBpm = SharedOptions.GetIfSpecified(result, PhigrosBpmOpt) ?? c.PhigrosDefaultBpm,
            Cutting = new KpcToPhigrosV3ConvertOptions.CuttingOptions
            {
                EasingPrecision =
                    SharedOptions.GetIfSpecified(result, PhigrosEasingPrecisionOpt)
                    ?? c.PhigrosEasingPrecision,
                MisalignedXyEventPrecision =
                    SharedOptions.GetIfSpecified(result, PhigrosXyPrecisionOpt)
                    ?? c.PhigrosMisalignedXyEventPrecision,
            },
            Alpha = new KpcToPhigrosV3ConvertOptions.AlphaOptions
            {
                CutPrecision =
                    SharedOptions.GetIfSpecified(result, PhigrosAlphaPrecisionOpt)
                    ?? c.PhigrosAlphaCutPrecision,
                CutCompress = c.PhigrosAlphaCutCompress,
                CutTolerance =
                    SharedOptions.GetIfSpecified(result, PhigrosAlphaToleranceOpt)
                    ?? c.PhigrosAlphaCutTolerance,
            },
            Speed = new KpcToPhigrosV3ConvertOptions.SpeedOptions
            {
                CutPrecision =
                    SharedOptions.GetIfSpecified(result, PhigrosSpeedPrecisionOpt)
                    ?? c.PhigrosSpeedCutPrecision,
            },
            FatherLineUnbind = new KpcToPhigrosV3ConvertOptions.FatherLineUnbindOptions
            {
                Precision =
                    SharedOptions.GetIfSpecified(result, UnbindPrecisionOpt)
                    ?? SharedOptions.GetIfSpecified(result, PrecisionOpt)
                    ?? c.UnbindPrecision,
                Tolerance =
                    SharedOptions.GetIfSpecified(result, UnbindToleranceOpt)
                    ?? SharedOptions.GetIfSpecified(result, ToleranceOpt)
                    ?? c.UnbindTolerance,
                MergeTolerance =
                    SharedOptions.GetIfSpecified(result, UnbindMergeToleranceOpt)
                    ?? c.UnbindMergeTolerance,
                ClassicMode =
                    SharedOptions.GetIfSpecified(result, UnbindClassicOpt)
                    ?? SharedOptions.GetIfSpecified(result, ClassicOpt)
                    ?? c.UnbindClassicMode,
                Compress = !disableUnbindCompress,
            },
            MultiLayerMerge = new KpcToPhigrosV3ConvertOptions.MultiLayerMergeOptions
            {
                Precision =
                    SharedOptions.GetIfSpecified(result, MergePrecisionOpt)
                    ?? c.MultiLayerMergePrecision,
                Tolerance =
                    SharedOptions.GetIfSpecified(result, MergeToleranceOpt)
                    ?? c.MultiLayerMergeTolerance,
                ClassicMode =
                    SharedOptions.GetIfSpecified(result, MergeClassicOpt)
                    ?? c.MultiLayerMergeClassicMode,
                Compress = !disableMergeCompress,
            },
            LineFilter = new KpcToPhigrosV3ConvertOptions.LineFilterOptions
            {
                RemoveAttachUiLine = result.GetValue(RemoveAttachUiOpt),
                RemoveTextureLine = result.GetValue(RemoveTextureOpt),
            },
            NoteFilter = new KpcToPhigrosV3ConvertOptions.NoteFilterOptions
            {
                FilterFakeNotes =
                    SharedOptions.GetIfSpecified(result, FilterFakeNotesOpt)
                    ?? c.PhigrosFilterFakeNotes,
            },
            NegativeAlpha = new KpcToPhigrosV3ConvertOptions.NegativeAlphaOptions
            {
                Enabled =
                    SharedOptions.GetIfSpecified(result, NegativeAlphaElevationOpt)
                    ?? c.PhigrosNegativeAlphaElevation,
                ElevationStep =
                    SharedOptions.GetIfSpecified(result, NegativeAlphaStepOpt)
                    ?? c.PhigrosNegativeAlphaStep,
            },
        };

        var phiFansOptions = new KpcToPhiFansConvertOptions
        {
            Cutting = new KpcToPhiFansConvertOptions.CuttingOptions
            {
                UnsupportedEasingPrecision =
                    SharedOptions.GetIfSpecified(result, PhiFansEasingPrecisionOpt)
                    ?? c.PhiFansUnsupportedEasingPrecision,
            },
            DiscontinuityBeatPrecision =
                SharedOptions.GetIfSpecified(result, PhiFansDiscontinuityPrecisionOpt)
                ?? c.PhiFansDiscontinuityBeatPrecision,
        };

        var saveResult = await ChartService.SaveAsAsync(
            kpc,
            output,
            targetType,
            new SaveAsOptions
            {
                Stream = streamOutput,
                Format = formatOutput,
                DryRun = dryRun,
                PhiEditOptions = peOptions,
                PhigrosOptions = phigrosOptions,
                PhiFansOptions = phiFansOptions,
            },
            ct
        );

        if (saveResult is null)
        {
            ConsoleWriter.Error(CliLocalizationString.err_unimplemented);
            return 2;
        }
        ConsoleWriter.Info(string.Format(CliLocalizationString.msg_written, saveResult));
        return 0;
    }
}
