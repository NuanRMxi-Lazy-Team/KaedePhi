using KaedePhi.Tool.Common;
using PhigrosV3 = KaedePhi.Core.Formats.Phigros.v3.Chart;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

/// <summary>
/// PhigrosV3 坐标系与 IR 坐标系之间的坐标变换工具。
/// </summary>
public static class Transform
{
    private static readonly CoordinateProfile PhigrosV3Profile = new(
        PhigrosV3.CoordinateSystem.MinX,
        PhigrosV3.CoordinateSystem.MaxX,
        PhigrosV3.CoordinateSystem.MinY,
        PhigrosV3.CoordinateSystem.MaxY,
        PhigrosV3.CoordinateSystem.ClockwiseRotation
    );

    public static double ToIrX(float x) => CoordinateGeometry.ToIrX(x, PhigrosV3Profile);

    public static double ToIrY(float y) => CoordinateGeometry.ToIrY(y, PhigrosV3Profile);

    public static double ToIrAngle(float angle) =>
        CoordinateGeometry.ToIrAngle(angle, PhigrosV3Profile);

    public static float ToPhigrosV3X(double x) =>
        CoordinateGeometry.ToTargetXf(x, PhigrosV3Profile);

    public static float ToPhigrosV3Y(double y) =>
        CoordinateGeometry.ToTargetYf(y, PhigrosV3Profile);

    public static float ToPhigrosV3Angle(double angle) =>
        (float)CoordinateGeometry.ToTargetAngle(angle, PhigrosV3Profile);
}
