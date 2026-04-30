using KaedePhi.Tool.Common;
using PhigrosV3 = KaedePhi.Core.Phigros.v3.Chart;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

public static class Transform
{
    private static readonly CoordinateProfile PhigrosV3Profile = new(
        PhigrosV3.CoordinateSystem.MinX,
        PhigrosV3.CoordinateSystem.MaxX,
        PhigrosV3.CoordinateSystem.MinY,
        PhigrosV3.CoordinateSystem.MaxY,
        PhigrosV3.CoordinateSystem.ClockwiseRotation);

    public static double ToKpcX(float x) => CoordinateGeometry.ToKpcX(x, PhigrosV3Profile);
    public static double ToKpcY(float y) => CoordinateGeometry.ToKpcY(y, PhigrosV3Profile);
    public static double ToKpcAngle(float angle) => CoordinateGeometry.ToKpcAngle(angle, PhigrosV3Profile);
}
