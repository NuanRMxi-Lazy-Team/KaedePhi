namespace KaedePhi.Tool.Analyzers.Analysis;

/// <summary>
/// 采样精度参数的判定阈值。
/// </summary>
internal static class PrecisionThresholds
{
    /// <summary>
    /// 运行时允许的最大采样精度，需与 NumericParameterValidator.MaximumPrecision 保持一致。
    /// </summary>
    public const double MaximumPrecision = 1024d;

    /// <summary>
    /// 超过该值时判定为精度偏高（仍在合法范围内，但事件量与内存开销显著上升）。
    /// </summary>
    public const double HighPrecisionThreshold = 256d;
}
