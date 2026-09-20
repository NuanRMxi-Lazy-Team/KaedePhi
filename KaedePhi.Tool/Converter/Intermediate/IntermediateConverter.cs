using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter.Intermediate;

/// <summary>
/// IR 格式直通转换器（输入已是 IR 格式时使用）。
/// </summary>
public class IntermediateConverter : LoggableBase, IChartConverter<Ir.Chart, Unit?, Unit?>
{
    /// <summary>
    /// 复制并规范输入的 IR 谱面。
    /// </summary>
    /// <param name="input">IR 谱面</param>
    /// <param name="options">未使用</param>
    /// <returns>规范后的独立谱面副本</returns>
    public Ir.Chart ToIr(Ir.Chart input, Unit? options)
    {
        return IrChartNormalizer.NormalizeAndValidateNoteEndBeats(input);
    }

    /// <summary>
    /// 复制并规范输入的 IR 谱面。
    /// </summary>
    /// <param name="input">IR 谱面</param>
    /// <param name="options">未使用</param>
    /// <returns>规范后的独立谱面副本</returns>
    public Ir.Chart FromIr(Ir.Chart input, Unit? options)
    {
        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(input);
        return normalized;
    }
}
