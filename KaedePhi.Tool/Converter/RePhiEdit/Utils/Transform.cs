using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

public static class Transform
{
    public static double TransformToKpcX(float x) => CoordinateGeometry.ToKpcX(x);
    public static double TransformToKpcY(float y) => CoordinateGeometry.ToKpcY(y);
    public static double TransformToKpcAngle(float angle) => CoordinateGeometry.ToKpcAngle(angle);

    public static double TransformToRpeX(double x) => CoordinateGeometry.ToRenderX(x);
    public static double TransformToRpeY(double y) => CoordinateGeometry.ToRenderY(y);

    public static float FloatTransformToRpeX(double x) => CoordinateGeometry.ToRenderXf(x);
    public static float FloatTransformToRpeY(double y) => CoordinateGeometry.ToRenderYf(y);

    public static double TransformToRpeAngle(double angle) => CoordinateGeometry.ToRenderAngle(angle);
}