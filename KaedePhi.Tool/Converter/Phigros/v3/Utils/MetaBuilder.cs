using PhigrosChart = KaedePhi.Core.Formats.Phigros.v3.Chart;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

/// <summary>
/// PhigrosV3 元数据到 IR 元数据的转换工具。
/// </summary>
public static class MetaBuilder
{
    public static Ir.Meta ConvertMeta(PhigrosChart src) =>
        new() { Offset = (int)(src.Offset * 1000) };
}
