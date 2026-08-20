using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

/// <summary>
/// PE 与 KPC 之间的坐标及速度值变换工具。
/// </summary>
public static class Transform
{
    // 该比例由 PE 与 KPC 的速度单位定义，不能作为用户选项。
    private const float SpeedValueRatio = 14f / 9f;

    private static readonly CoordinateProfile PeCoordinateProfile = new(
        Pe.Chart.CoordinateSystem.MinX,
        Pe.Chart.CoordinateSystem.MaxX,
        Pe.Chart.CoordinateSystem.MinY,
        Pe.Chart.CoordinateSystem.MaxY,
        Pe.Chart.CoordinateSystem.ClockwiseRotation
    );

    public static double TransformToKpcX(float x) =>
        CoordinateGeometry.ToKpcX(x, PeCoordinateProfile);

    public static double TransformToKpcY(float y) =>
        CoordinateGeometry.ToKpcY(y, PeCoordinateProfile);

    public static double TransformToKpcAngle(float angle) =>
        CoordinateGeometry.ToKpcAngle(angle, PeCoordinateProfile);

    public static float TransformToPeX(double x) =>
        CoordinateGeometry.ToTargetXf(x, PeCoordinateProfile);

    public static float TransformToPeY(double y) =>
        CoordinateGeometry.ToTargetYf(y, PeCoordinateProfile);

    public static float TransformToPeAngle(double angle) =>
        (float)CoordinateGeometry.ToTargetAngle(angle, PeCoordinateProfile);

    internal static float TransformToKpcSpeed(float speed) => speed / SpeedValueRatio;

    internal static float TransformToPeSpeed(float speed) => speed * SpeedValueRatio;
}
