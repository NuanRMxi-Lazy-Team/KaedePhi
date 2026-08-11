namespace KaedePhi.Tool.Analyzers.Analysis;

/// <summary>
/// 父线解绑容差参数的判定阈值。
/// </summary>
internal static class JudgeLineUnbinderTolerance
{
    /// <summary>
    /// 容差大于等于该值时判定为过大（百分比语义下通常不应超过 100）。
    /// </summary>
    public const double ErrorThreshold = 100.0;

    /// <summary>
    /// 容差小于该值时判定为过小。
    /// </summary>
    public const double SmallToleranceThreshold = 0.01;
}
