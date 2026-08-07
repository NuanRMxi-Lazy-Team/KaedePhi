using System.Globalization;
using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Converter.Phigros.v3.Model;

namespace KaedePhi.Tool.Cli.Commands;

public static class ConvertCommand
{
    private static string L(string key) =>
        CliLocalizationString.ResourceManager.GetString(key, CultureInfo.CurrentUICulture)
        ?? CliLocalizationString.ResourceManager.GetString(key, CultureInfo.CurrentCulture)
        ?? key;

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
        Description = L("convert_command_opt_target"),
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Option<double> PeSpeedRatioOpt = new("--pe-speed-ratio")
    {
        Description = L("convert_opt_pe_speed_ratio"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PeTrailingPaddingOpt = new("--pe-trailing-padding")
    {
        Description = L("convert_opt_pe_trailing_padding"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PeEasingPrecisionOpt = new("--pe-easing-precision")
    {
        Description = L("convert_opt_pe_easing_precision"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PeXyPrecisionOpt = new("--pe-xy-precision")
    {
        Description = L("convert_opt_pe_xy_precision"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PeAlphaPrecisionOpt = new("--pe-alpha-precision")
    {
        Description = L("convert_opt_pe_alpha_precision"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PeAlphaToleranceOpt = new("--pe-alpha-tolerance")
    {
        Description = L("convert_opt_pe_alpha_tolerance"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PeSpeedPrecisionOpt = new("--pe-speed-precision")
    {
        Description = L("convert_opt_pe_speed_precision"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PeSpeedToleranceOpt = new("--pe-speed-tolerance")
    {
        Description = L("convert_opt_pe_speed_tolerance"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<float> PhigrosBpmOpt = new("--phigros-bpm")
    {
        Description = L("convert_opt_phigros_bpm"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PhigrosEasingPrecisionOpt = new(
        "--phigros-easing-precision"
    )
    {
        Description = L("convert_opt_phigros_easing_precision"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PhigrosXyPrecisionOpt = new("--phigros-xy-precision")
    {
        Description = L("convert_opt_phigros_xy_precision"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PhigrosAlphaPrecisionOpt = new(
        "--phigros-alpha-precision"
    )
    {
        Description = L("convert_opt_phigros_alpha_precision"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PhigrosAlphaToleranceOpt = new(
        "--phigros-alpha-tolerance"
    )
    {
        Description = L("convert_opt_phigros_alpha_tolerance"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> PhigrosSpeedPrecisionOpt = new(
        "--phigros-speed-precision"
    )
    {
        Description = L("convert_opt_phigros_speed_precision"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> UnbindPrecisionOpt = new("--unbind-precision")
    {
        Description = L("convert_opt_unbind_precision"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> UnbindToleranceOpt = new("--unbind-tolerance")
    {
        Description = L("convert_opt_unbind_tolerance"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<bool> UnbindClassicOpt = new("--unbind-classic")
    {
        Description = L("convert_opt_unbind_classic"),
    };

    private static readonly Option<double> MergePrecisionOpt = new("--merge-precision")
    {
        Description = L("convert_opt_merge_precision"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<double> MergeToleranceOpt = new("--merge-tolerance")
    {
        Description = L("convert_opt_merge_tolerance"),
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<bool> MergeClassicOpt = new("--merge-classic")
    {
        Description = L("convert_opt_merge_classic"),
    };

    private static readonly Option<bool> NoUnbindCompressOpt = new("--no-unbind-compress")
    {
        Description = L("convert_opt_no_unbind_compress"),
    };

    private static readonly Option<bool> NoMergeCompressOpt = new("--no-merge-compress")
    {
        Description = L("convert_opt_no_merge_compress"),
    };

    private static readonly Option<bool> RemoveAttachUiOpt = new("--remove-attach-ui")
    {
        Description = L("convert_opt_remove_attach_ui"),
    };

    private static readonly Option<bool> RemoveTextureOpt = new("--remove-texture")
    {
        Description = L("convert_opt_remove_texture"),
    };

    private static readonly Option<bool> FilterFakeNotesOpt = new("--filter-fake-notes")
    {
        Description = L("convert_opt_filter_fake_notes"),
    };

    private static readonly Option<bool> NegativeAlphaElevationOpt = new(
        "--negative-alpha-elevation"
    )
    {
        Description = L("convert_opt_negative_alpha_elevation"),
    };

    private static readonly Option<double> NegativeAlphaStepOpt = new("--negative-alpha-step")
    {
        Description = L("convert_opt_negative_alpha_step"),
        Arity = ArgumentArity.ExactlyOne,
    };

    #endregion

    public static Command Create()
    {
        var cmd = new Command("convert", L("convert_command_desc"))
        {
            InputOpt,
            OutputOpt,
            WorkspaceOpt,
            PrecisionOpt,
            ToleranceOpt,
            ClassicOpt,
            NoCompressOpt,
            DryRunOpt,
            StreamOpt,
            FormatOpt,
            TargetTypeOpt,
            PeSpeedRatioOpt,
            PeTrailingPaddingOpt,
            PeEasingPrecisionOpt,
            PeXyPrecisionOpt,
            PeAlphaPrecisionOpt,
            PeAlphaToleranceOpt,
            PeSpeedPrecisionOpt,
            PeSpeedToleranceOpt,
            PhigrosBpmOpt,
            PhigrosEasingPrecisionOpt,
            PhigrosXyPrecisionOpt,
            PhigrosAlphaPrecisionOpt,
            PhigrosAlphaToleranceOpt,
            PhigrosSpeedPrecisionOpt,
            UnbindPrecisionOpt,
            UnbindToleranceOpt,
            UnbindClassicOpt,
            MergePrecisionOpt,
            MergeToleranceOpt,
            MergeClassicOpt,
            NoUnbindCompressOpt,
            NoMergeCompressOpt,
            RemoveAttachUiOpt,
            RemoveTextureOpt,
            FilterFakeNotesOpt,
            NegativeAlphaElevationOpt,
            NegativeAlphaStepOpt,
        };

        cmd.SetAction(
            async (result, ct) =>
            {
                var input = result.GetValue(InputOpt);
                var workspace = result.GetValue(WorkspaceOpt);
                if (string.IsNullOrWhiteSpace(input) && string.IsNullOrWhiteSpace(workspace))
                {
                    ConsoleWriter.Error(CliLocalizationString.err_input_required);
                    return 1;
                }

                var config = AppConfigHelper.Load();
                var c = config.ConvertConfig;

                var svc = new ChartService();
                var kpc = await svc.LoadKpcAsync(input, workspace, ct);
                if (kpc == null)
                {
                    ConsoleWriter.Error(CliLocalizationString.err_unimplemented);
                    return 1;
                }

                var targetType = result.GetValue(TargetTypeOpt) ?? c.TargetType;
                var targetExtension = "." + (ChartFormatRegistry.Find(targetType)?.FileExtension ?? "json");
                var output = svc.ResolveOutputPath(input, result.GetValue(OutputOpt), workspace, targetExtension);
                var streamOutput =
                    SharedOptions.GetIfSpecified(result, StreamOpt) ?? c.StreamOutput;
                var formatOutput =
                    SharedOptions.GetIfSpecified(result, FormatOpt) ?? c.FormatOutput;
                var dryRun = SharedOptions.GetIfSpecified(result, DryRunOpt) ?? c.DryRun;
                var disableUnbindCompress = result.GetValue(NoUnbindCompressOpt);
                var disableMergeCompress = result.GetValue(NoMergeCompressOpt);

                var peOptions = new KpcToPhiEditConvertOptions
                {
                    SpeedConversionRatio =
                        SharedOptions.GetIfSpecified(result, PeSpeedRatioOpt)
                        ?? c.PeSpeedConversionRatio,
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
                    DefaultBpm =
                        SharedOptions.GetIfSpecified(result, PhigrosBpmOpt) ?? c.PhigrosDefaultBpm,
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
                    },
                    ct
                );

                if (saveResult == null)
                {
                    ConsoleWriter.Warn(CliLocalizationString.warn_rpe_convert);
                    return 2;
                }
                ConsoleWriter.Info(string.Format(CliLocalizationString.msg_written, saveResult));
                return 0;
            }
        );

        return cmd;
    }
}
