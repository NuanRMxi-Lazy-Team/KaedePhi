using Chart = KaedePhi.Core.KaedePhi.Chart;
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
    bool ClockwiseRotation)
{
    /// <summary>
    /// KPC 的归一化坐标系配置。
    /// </summary>
    public static readonly CoordinateProfile KpcProfile = new(
        Chart.CoordinateSystem.MinX,
        Chart.CoordinateSystem.MaxX,
        Chart.CoordinateSystem.MinY,
        Chart.CoordinateSystem.MaxY,
        Chart.CoordinateSystem.ClockwiseRotation);

    /// <summary>
    /// 默认渲染坐标系配置（当前与常见 675x450 编辑器坐标兼容）。
    /// </summary>
    public static readonly CoordinateProfile DefaultRenderProfile = new(-675d, 675d, -450d, 450d, true);
}


