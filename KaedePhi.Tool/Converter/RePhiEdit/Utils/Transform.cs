using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

/// <summary>
/// RPE 坐标系与 IR 坐标系之间的坐标变换工具。
/// </summary>
public static class Transform
{
    public static double TransformToIrX(float x) => CoordinateGeometry.ToIrX(x);

    public static double TransformToIrY(float y) => CoordinateGeometry.ToIrY(y);

    public static double TransformToIrAngle(float angle) => CoordinateGeometry.ToIrAngle(angle);

    public static double TransformToRpeX(double x) => CoordinateGeometry.ToRenderX(x);

    public static double TransformToRpeY(double y) => CoordinateGeometry.ToRenderY(y);

    public static float FloatTransformToRpeX(double x) => CoordinateGeometry.ToRenderXf(x);

    public static float FloatTransformToRpeY(double y) => CoordinateGeometry.ToRenderYf(y);

    public static double TransformToRpeAngle(double angle) =>
        CoordinateGeometry.ToRenderAngle(angle);
}
