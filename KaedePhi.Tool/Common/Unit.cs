namespace KaedePhi.Tool.Common;

/// <summary>
/// 完全无实际用途的占位类，使用了此类型的地方放心大胆使用<see langword="null"/>
/// </summary>
public struct Unit
{
    public static readonly Unit Value = new();
}