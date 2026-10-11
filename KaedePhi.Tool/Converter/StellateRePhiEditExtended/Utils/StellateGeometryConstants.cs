namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;

/// <summary>
/// 提供噪域几何转换使用的坐标、采样和简化参数。
/// </summary>
public static class StellateGeometryConstants
{
    /// <summary>
    /// 谱面画布的宽高比宽度。
    /// </summary>
    public const double AspectWidth = 3d;

    /// <summary>
    /// 谱面画布的宽高比高度。
    /// </summary>
    public const double AspectHeight = 2d;

    /// <summary>
    /// 标记纹理的基准宽度。
    /// </summary>
    public const double TextureWidth = 900d;

    /// <summary>
    /// 标记纹理的基准高度。
    /// </summary>
    public const double TextureHeight = 900d;

    /// <summary>
    /// 每拍的几何采样点数。
    /// </summary>
    public const int SamplesPerBeat = 32;

    /// <summary>
    /// 启用并行几何采样所需的最少采样点数。
    /// </summary>
    public const int ParallelSampleThreshold = 32;

    /// <summary>
    /// 几何路径简化使用的相对偏差容差。
    /// </summary>
    public const double SimplifyTolerance = 0.02d;

    /// <summary>
    /// 几何数值比较使用的容差。
    /// </summary>
    public const double ValueEpsilon = 1e-10d;

    /// <summary>
    /// 计算基准尺寸时使用的最小尺寸阈值。
    /// </summary>
    public const double MinimumBaseSize = 1e-12d;

    /// <summary>
    /// 必须保留的路径点使用的相对偏差阈值。
    /// </summary>
    public const double HardPointEpsilon = 2.384185791015625e-7;

    /// <summary>
    /// 曲线起始时刻比较使用的时间容差。
    /// </summary>
    public const double CurveStartTimeEpsilon = 1e-10d;

    /// <summary>
    /// 将百分比坐标转换为 IR 坐标。
    /// </summary>
    /// <param name="percentage">百分比坐标</param>
    public static double ToIrCoordinate(double percentage) => percentage * 2d - 1d;

    /// <summary>
    /// 在数值有限时保留原值，否则使用指定的替代值。
    /// </summary>
    /// <param name="value">待检查的数值</param>
    /// <param name="fallback">数值不是有限值时使用的替代值</param>
    public static double FiniteOr(double value, double fallback) =>
        double.IsFinite(value) ? value : fallback;
}
