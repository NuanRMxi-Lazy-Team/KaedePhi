using KaedePhi.Core.Primitives;
using KaedePhi.Core.Formats.PhiFans;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiFans.Model;
using KaedePhi.Tool.Converter.PhiFans.Utils;

namespace KaedePhi.Tool.Converter.PhiFans;

/// <summary>
/// PhiFans 格式转换器。
/// </summary>
public class PhiFansConverter
    : LoggableBase,
        IChartConverter<Chart, Unit?, IrToPhiFansConvertOptions>,
        ICancellableChartConverter
{
    private CancellationToken _ct;

    /// <inheritdoc/>
    public void SetCancellationToken(CancellationToken ct) => _ct = ct;

    /// <summary>
    /// 将 PhiFans 格式转换为 IR 内部格式。
    /// </summary>
    /// <param name="source">PhiFans 谱面</param>
    /// <param name="_">未使用</param>
    /// <returns>IR 谱面</returns>
    public Ir.Chart ToIr(Chart source, Unit? _)
    {
        ArgumentNullException.ThrowIfNull(source);
        _ct.ThrowIfCancellationRequested();
        var converted = new Ir.Chart
        {
            BpmList = source.BpmList.ConvertAll(BpmBuilder.ConvertToIr),
            Meta = MetaBuilder.ConvertToIr(source.Info, source.Offset),
            JudgeLineList = ConvertLinesWithCancellation(source.JudgeLineList),
        };
        return IrChartNormalizer.NormalizeAndValidateNoteEndBeats(converted);
    }

    private List<Ir.JudgeLine> ConvertLinesWithCancellation(List<Line> lines)
    {
        var result = new List<Ir.JudgeLine>(lines.Count);
        foreach (var line in lines)
        {
            _ct.ThrowIfCancellationRequested();
            result.Add(JudgeLineBuilder.ConvertToIr(line));
        }

        return result;
    }

    /// <summary>
    /// 将 IR 内部格式转换为 PhiFans 格式。
    /// </summary>
    /// <param name="input">IR 谱面</param>
    /// <param name="options">输出转换选项</param>
    /// <returns>PhiFans 谱面</returns>
    public Chart FromIr(Ir.Chart input, IrToPhiFansConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);
        ConversionOptionsValidator.Validate(options);
        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(input);
        IrChartValidator.ValidateJudgeLineHierarchy(normalized.JudgeLineList);
        _ct.ThrowIfCancellationRequested();

        var lines = new List<Line>(normalized.JudgeLineList.Count);
        foreach (var line in normalized.JudgeLineList)
        {
            _ct.ThrowIfCancellationRequested();
            lines.Add(JudgeLineBuilder.ConvertFromIr(line, options));
        }

        return new Chart
        {
            Offset = normalized.Meta.Offset,
            Info = MetaBuilder.ConvertFromIr(normalized.Meta),
            BpmList = normalized.BpmList.ConvertAll(BpmBuilder.ConvertFromIr),
            JudgeLineList = lines,
        };
    }
}
