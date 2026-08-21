namespace KaedePhi.Tool.Converter.PhiFans.Utils;

internal static class EasingConverter
{
    internal static int ToKpc(int pfEasing) =>
        pfEasing switch
        {
            0 => 1,
            1 => 2,
            2 => 3,
            3 => 4,
            4 => 5,
            5 => 6,
            6 => 7,
            7 => 8,
            8 => 9,
            9 => 10,
            10 => 11,
            11 => 12,
            12 => 13,
            13 => 14,
            14 => 15,
            15 => 16,
            16 => 17,
            17 => 18,
            18 => 19,
            19 => 20,
            20 => 21,
            21 => 22,
            22 => 23,
            23 => 24,
            24 => 25,
            25 => 26,
            26 => 27,
            27 => 28,
            28 => 29,
            29 => 30,
            30 => 31,
            _ => 1,
        };

    /// <summary>
    /// 将 KPC 缓动编号映射为 PhiFans 缓动编号。
    /// </summary>
    internal static int FromKpc(int kpcEasing) =>
        kpcEasing switch
        {
            1 => 0,
            2 => 1,
            3 => 2,
            4 => 3,
            5 => 4,
            6 => 5,
            7 => 6,
            8 => 7,
            9 => 8,
            10 => 9,
            11 => 10,
            12 => 11,
            13 => 12,
            14 => 13,
            15 => 14,
            16 => 15,
            17 => 16,
            18 => 17,
            19 => 18,
            20 => 19,
            21 => 20,
            22 => 21,
            23 => 22,
            24 => 23,
            25 => 24,
            26 => 25,
            27 => 26,
            28 => 27,
            29 => 28,
            30 => 29,
            31 => 30,
            _ => throw new ArgumentOutOfRangeException(
                nameof(kpcEasing),
                kpcEasing,
                "PhiFans 不支持该缓动编号。"
            ),
        };
}
