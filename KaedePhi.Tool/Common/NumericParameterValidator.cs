namespace KaedePhi.Tool.Common;

/// <summary>
/// 校验转换和处理算法使用的数值参数。
/// </summary>
public static class NumericParameterValidator
{
    /// <summary>
    /// 允许的最大采样精度，需与 Beat 的可表示分母上限保持一致。
    /// </summary>
    public const double MaximumPrecision = 1024d;

    /// <summary>
    /// 允许的最大拟合容差百分比。
    /// </summary>
    public const double MaximumTolerance = 100d;

    /// <summary>
    /// 校验采样精度是否为允许范围内的有限正数。
    /// </summary>
    /// <param name="precision">每拍采样次数。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <returns>无返回值。</returns>
    public static void ValidatePrecision(double precision, string parameterName = "precision")
    {
        if (!double.IsFinite(precision) || precision <= 0 || precision > MaximumPrecision)
            throw new ArgumentOutOfRangeException(
                parameterName,
                precision,
                $"采样精度必须是大于 0 且不超过 {MaximumPrecision} 的有限数值。"
            );
    }

    /// <summary>
    /// 校验拟合容差是否为允许范围内的有限非负数。
    /// </summary>
    /// <param name="tolerance">拟合容差。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <returns>无返回值。</returns>
    public static void ValidateTolerance(double tolerance, string parameterName = "tolerance")
    {
        if (!double.IsFinite(tolerance) || tolerance < 0 || tolerance > MaximumTolerance)
            throw new ArgumentOutOfRangeException(
                parameterName,
                tolerance,
                $"拟合容差必须是 0 到 {MaximumTolerance} 之间的有限数值。"
            );
    }
}
