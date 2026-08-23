using KaedePhi.Tool.Converter.PhiChain.Model;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Converter.PhiFans.Model;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using KaedePhi.Tool.Converter.RePhiEdit.Model;

namespace KaedePhi.Tool.Common;

internal static class ConversionOptionsValidator
{
    public static void Validate(PhiEditToKpcConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidatePositive(options.FrameDurationBeat, nameof(options.FrameDurationBeat));
        ValidateNonNegative(options.TrailingBeatPadding, nameof(options.TrailingBeatPadding));
    }

    public static void Validate(KpcToPhiEditConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidatePositive(options.Cutting.UnsupportedEasingPrecision, "UnsupportedEasingPrecision");
        ValidatePositive(options.Cutting.MisalignedXyEventPrecision, "MisalignedXyEventPrecision");
        ValidatePositive(options.Alpha.CutPrecision, "Alpha.CutPrecision");
        ValidatePositive(options.Speed.CutPrecision, "Speed.CutPrecision");
        ValidatePositive(options.FatherLineUnbind.Precision, "FatherLineUnbind.Precision");
        ValidatePositive(options.MultiLayerMerge.Precision, "MultiLayerMerge.Precision");
        ValidateTolerance(options.Alpha.CutTolerance, "Alpha.CutTolerance");
        ValidateTolerance(options.FatherLineUnbind.Tolerance, "FatherLineUnbind.Tolerance");
        ValidateTolerance(
            options.FatherLineUnbind.MergeTolerance,
            "FatherLineUnbind.MergeTolerance"
        );
        ValidateTolerance(options.MultiLayerMerge.Tolerance, "MultiLayerMerge.Tolerance");
        ValidateNonNegative(options.TrailingBeatPadding, nameof(options.TrailingBeatPadding));
    }

    public static void Validate(ConvertOption options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Cutting is null || options.Cutting.UnsupportedEasingPrecision <= 0)
            throw new ArgumentOutOfRangeException(nameof(options));
    }

    public static void Validate(KpcToPhiFansConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.MultiLayerMerge);
        if (options.Cutting is null || options.Cutting.UnsupportedEasingPrecision <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(options.Cutting.UnsupportedEasingPrecision)
            );
        ValidatePositive(options.MultiLayerMerge.Precision, "MultiLayerMerge.Precision");
        ValidateTolerance(options.MultiLayerMerge.Tolerance, "MultiLayerMerge.Tolerance");
        if (options.DiscontinuityBeatPrecision <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(options.DiscontinuityBeatPrecision),
                "必须是正整数。"
            );
    }

    public static void Validate(KpcToPhigrosV3ConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidatePositive(options.DefaultBpm, nameof(options.DefaultBpm));
        ValidatePositive(options.Cutting.EasingPrecision, "Cutting.EasingPrecision");
        ValidatePositive(
            options.Cutting.MisalignedXyEventPrecision,
            "Cutting.MisalignedXyEventPrecision"
        );
        ValidatePositive(options.Alpha.CutPrecision, "Alpha.CutPrecision");
        ValidatePositive(options.Speed.CutPrecision, "Speed.CutPrecision");
        ValidatePositive(options.FatherLineUnbind.Precision, "FatherLineUnbind.Precision");
        ValidatePositive(options.MultiLayerMerge.Precision, "MultiLayerMerge.Precision");
        ValidateTolerance(options.FatherLineUnbind.Tolerance, "FatherLineUnbind.Tolerance");
        ValidateTolerance(
            options.FatherLineUnbind.MergeTolerance,
            "FatherLineUnbind.MergeTolerance"
        );
        ValidateTolerance(options.MultiLayerMerge.Tolerance, "MultiLayerMerge.Tolerance");
        ValidatePositive(options.NegativeAlpha.ElevationStep, "NegativeAlpha.ElevationStep");
    }

    public static void Validate(KpcToPhiChainConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidatePositive(options.UnbindPrecision, nameof(options.UnbindPrecision));
        ValidatePositive(
            options.MultiLayerMergePrecision,
            nameof(options.MultiLayerMergePrecision)
        );
        ValidatePositive(options.EasingCutPrecision, nameof(options.EasingCutPrecision));
        ValidateTolerance(options.UnbindTolerance, nameof(options.UnbindTolerance));
        ValidateTolerance(options.UnbindMergeTolerance, nameof(options.UnbindMergeTolerance));
        ValidateTolerance(
            options.MultiLayerMergeTolerance,
            nameof(options.MultiLayerMergeTolerance)
        );
    }

    public static void Validate(PhiChainToKpcConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (
            options.UnsupportedEasingPrecision <= 0
            || options.UnsupportedEasingPrecision > NumericParameterValidator.MaximumPrecision
        )
            throw new ArgumentOutOfRangeException(
                nameof(options.UnsupportedEasingPrecision),
                $"不支持的缓动切段精度必须是 1 到 {(int)NumericParameterValidator.MaximumPrecision} 之间的正整数。"
            );
    }

    private static void ValidatePositive(double value, string name)
    {
        NumericParameterValidator.ValidatePrecision(value, name);
    }

    private static void ValidatePositive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(name, value, "数值必须是有限正数。");
    }

    private static void ValidateNonNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(name, value, "数值必须是有限非负数。");
    }

    private static void ValidateTolerance(double value, string name)
    {
        NumericParameterValidator.ValidateTolerance(value, name);
    }
}
