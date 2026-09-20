namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

/// <summary>
/// RPE 与 IR 缓动类型之间的映射与转换工具。
/// </summary>
public static class EasingConverter
{
    /// <summary>
    /// 将 IR 缓动转换为 RPE 缓动；贝塞尔事件强制降级为线性。
    /// </summary>
    /// <param name="src">IR 缓动实例。</param>
    /// <param name="isBezier">是否为贝塞尔事件。</param>
    /// <returns>RPE 缓动实例。</returns>
    public static Rpe.Easing ConvertEasing(Ir.Easing src, bool isBezier)
    {
        return isBezier ? new Rpe.Easing(1) : new Rpe.Easing(MapToPe((int)src));
    }

    public static Ir.Easing ConvertEasing(Rpe.Easing src) => new(MapToIr((int)src));

    public static Rpe.Easing ConvertEasing(Ir.Easing src) => new(MapToPe((int)src));

    public static int MapToIr(int rpeEasingNum) =>
        rpeEasingNum switch
        {
            1 => 1,
            2 => 3,
            3 => 2,
            4 => 6,
            5 => 5,
            6 => 4,
            7 => 7,
            8 => 9,
            9 => 8,
            10 => 12,
            11 => 11,
            12 => 10,
            13 => 13,
            14 => 15,
            15 => 14,
            16 => 18,
            17 => 17,
            18 => 21,
            19 => 20,
            20 => 24,
            21 => 23,
            22 => 22,
            23 => 25,
            24 => 27,
            25 => 26,
            26 => 30,
            27 => 29,
            28 => 31,
            29 => 28,
            _ => 1,
        };

    public static int MapToPe(int irEasingNum) =>
        PhiEdit.Utils.EasingConverter.MapToPe(irEasingNum);
}
