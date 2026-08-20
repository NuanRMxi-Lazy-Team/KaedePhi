using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter.KaedePhi;

/// <summary>
/// KPC 格式直通转换器（输入已是 KPC 格式时使用）。
/// </summary>
public class KaedePhiConverter : LoggableBase, IChartConverter<Kpc.Chart, Unit?, Unit?>
{
    /// <summary>
    /// 复制并规范输入的 KPC 谱面。
    /// </summary>
    /// <param name="input">KPC 谱面</param>
    /// <param name="options">未使用</param>
    /// <returns>规范后的独立谱面副本</returns>
    public Kpc.Chart ToKpc(Kpc.Chart input, Unit? options)
    {
        return ChartProcessingValidator.NormalizeAndValidateNoteEndBeats(input);
    }

    /// <summary>
    /// 复制并规范输入的 KPC 谱面。
    /// </summary>
    /// <param name="input">KPC 谱面</param>
    /// <param name="options">未使用</param>
    /// <returns>规范后的独立谱面副本</returns>
    public Kpc.Chart FromKpc(Kpc.Chart input, Unit? options)
    {
        var normalized = ChartProcessingValidator.NormalizeAndValidateNoteEndBeats(input);
        ChartProcessingValidator.ValidateJudgeLineHierarchy(normalized.JudgeLineList);
        return normalized;
    }
}
