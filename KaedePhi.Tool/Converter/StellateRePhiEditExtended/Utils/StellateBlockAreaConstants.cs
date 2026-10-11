using KaedePhi.Core.Primitives;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;

/// <summary>
/// 提供噪域转换使用的边界和基准值。
/// </summary>
public static class StellateBlockAreaConstants
{
    /// <summary>
    /// 噪域事件比较使用的数值容差。
    /// </summary>
    public const double ValueEpsilon = 1e-10d;

    /// <summary>
    /// 锚点采样允许生成的最大采样点数。
    /// </summary>
    public const int MaximumAnchorSamples = 1_000_000;

    /// <summary>
    /// 表示第零拍的拍数值。
    /// </summary>
    public static readonly Beat ZeroBeat = new(0);
}
