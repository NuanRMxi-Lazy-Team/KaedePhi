using Chart = KaedePhi.Core.Intermediate.Chart;
using PhichainChart = KaedePhi.Core.Formats.PhiChain.v6.Chart;
using PhiFansChart = KaedePhi.Core.Formats.PhiFans.Chart;

namespace KaedePhi.Tool.Common;

/// <summary>
/// 坐标系参数描述。
/// 使用 Min/Max 明确描述每个轴的完整区间，
/// ClockwiseRotation 表示角度正方向是否为顺时针。
/// </summary>
public readonly record struct CoordinateProfile(
    double MinX,
    double MaxX,
    double MinY,
    double MaxY,
    bool ClockwiseRotation
)
{
    /// <summary>
    /// IR 的归一化坐标系配置。
    /// </summary>
    public static readonly CoordinateProfile IrProfile = new(
        Chart.CoordinateSystem.MinX,
        Chart.CoordinateSystem.MaxX,
        Chart.CoordinateSystem.MinY,
        Chart.CoordinateSystem.MaxY,
        Chart.CoordinateSystem.ClockwiseRotation
    );

    /// <summary>
    /// 默认渲染坐标系配置
    /// </summary>
    public static readonly CoordinateProfile DefaultRenderProfile = new(
        -675d,
        675d,
        -450d,
        450d,
        true
    );

    /// <summary>
    /// PhiChain 坐标系配置。
    /// </summary>
    public static readonly CoordinateProfile PhiChainProfile = new(
        PhichainChart.CoordinateSystem.MinX,
        PhichainChart.CoordinateSystem.MaxX,
        PhichainChart.CoordinateSystem.MinY,
        PhichainChart.CoordinateSystem.MaxY,
        PhichainChart.CoordinateSystem.ClockwiseRotation
    );

    /// <summary>
    /// PhiFans 坐标系配置。
    /// </summary>
    public static readonly CoordinateProfile PhiFansProfile = new(
        PhiFansChart.CoordinateSystem.MinX,
        PhiFansChart.CoordinateSystem.MaxX,
        PhiFansChart.CoordinateSystem.MinY,
        PhiFansChart.CoordinateSystem.MaxY,
        PhiFansChart.CoordinateSystem.ClockwiseRotation
    );
}
