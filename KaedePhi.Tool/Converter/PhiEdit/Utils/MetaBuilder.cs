namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

/// <summary>
/// PE 与 IR 元数据之间的转换工具。
/// </summary>
public static class MetaBuilder
{
    private const int OffsetOffset = 175;

    public static Ir.Meta ConvertMeta(Pe.Chart src) =>
        new() { Offset = src.Offset - OffsetOffset };

    public static int GetPeOffset(Ir.Meta src) => src.Offset + OffsetOffset;
}
