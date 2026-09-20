using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter.PhiChain.Utils;

/// <summary>
/// PhiChain 坐标系与 IR 坐标系之间的坐标变换工具。
/// </summary>
public static class Transform
{
    private static readonly CoordinateProfile PhiChainProfile = CoordinateProfile.PhiChainProfile;

    /// <summary>
    /// 将 PhiChain X 坐标转换为 IR X 坐标。
    /// </summary>
    public static double TransformToIrX(float x) => CoordinateGeometry.ToIrX(x, PhiChainProfile);

    /// <summary>
    /// 将 PhiChain Y 坐标转换为 IR Y 坐标。
    /// </summary>
    public static double TransformToIrY(float y) => CoordinateGeometry.ToIrY(y, PhiChainProfile);

    /// <summary>
    /// 将 PhiChain 角度转换为 IR 角度。
    /// </summary>
    public static double TransformToIrAngle(float angle) =>
        CoordinateGeometry.ToIrAngle(angle, PhiChainProfile);

    /// <summary>
    /// 将 IR X 坐标转换为 PhiChain X 坐标。
    /// </summary>
    public static float TransformToPhiChainX(double x) =>
        CoordinateGeometry.ToTargetXf(x, PhiChainProfile);

    /// <summary>
    /// 将 IR Y 坐标转换为 PhiChain Y 坐标。
    /// </summary>
    public static float TransformToPhiChainY(double y) =>
        CoordinateGeometry.ToTargetYf(y, PhiChainProfile);

    /// <summary>
    /// 将 IR 角度转换为 PhiChain 角度。
    /// </summary>
    public static double TransformToPhiChainAngle(double angle) =>
        CoordinateGeometry.ToTargetAngle(angle, PhiChainProfile);
}
