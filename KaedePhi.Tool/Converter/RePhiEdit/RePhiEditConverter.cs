using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using KaedePhi.Tool.Converter.RePhiEdit.Utils;

namespace KaedePhi.Tool.Converter.RePhiEdit;

/// <summary>
/// RePhiEdit 格式转换器。
/// </summary>
public class RePhiEditConverter
    : LoggableBase,
        IChartConverter<Rpe.Chart, Unit?, ConvertOption>,
        ICancellableChartConverter
{
    private CancellationToken _ct;

    /// <inheritdoc/>
    public void SetCancellationToken(CancellationToken ct) => _ct = ct;

    /// <summary>
    /// 将 RePhiEdit 格式转换为 IR 内部格式。
    /// </summary>
    /// <param name="source">RePhiEdit 谱面</param>
    /// <param name="_">未使用</param>
    /// <returns>IR 谱面</returns>
    public Ir.Chart ToIr(Rpe.Chart source, Unit? _)
    {
        ArgumentNullException.ThrowIfNull(source);
        _ct.ThrowIfCancellationRequested();
        var converted = new Ir.Chart
        {
            BpmList = source.BpmList.ConvertAll(ConvertBpmItem),
            Meta = MetaBuilder.ConvertMeta(source.Meta),
            JudgeLineList = ConvertJudgeLinesWithCancellation(source.JudgeLineList),
        };
        return IrChartNormalizer.NormalizeAndValidateNoteEndBeats(converted);
    }

    private List<Ir.JudgeLine> ConvertJudgeLinesWithCancellation(List<Rpe.JudgeLine> judgeLines)
    {
        var result = new List<Ir.JudgeLine>(judgeLines.Count);
        for (var i = 0; i < judgeLines.Count; i++)
        {
            _ct.ThrowIfCancellationRequested();
            result.Add(JudgeLineBuilder.ConvertJudgeLine(judgeLines[i]));
        }

        return result;
    }

    /// <summary>
    /// 将 IR 内部格式转换为 RePhiEdit 格式。
    /// </summary>
    /// <param name="input">IR 谱面</param>
    /// <param name="options">输出转换选项</param>
    /// <returns>RePhiEdit 谱面</returns>
    public Rpe.Chart FromIr(Ir.Chart input, ConvertOption options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);
        ConversionOptionsValidator.Validate(options);
        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(input);
        IrChartValidator.ValidateJudgeLineHierarchy(normalized.JudgeLineList);
        _ct.ThrowIfCancellationRequested();

        var lines = new List<Rpe.JudgeLine>(normalized.JudgeLineList.Count);
        foreach (var line in normalized.JudgeLineList)
        {
            _ct.ThrowIfCancellationRequested();
            lines.Add(JudgeLineBuilder.ConvertJudgeLine(line, options.Cutting));
        }

        return new Rpe.Chart
        {
            BpmList = normalized.BpmList.ConvertAll(ConvertBpmItem),
            Meta = MetaBuilder.ConvertMeta(normalized.Meta),
            JudgeLineList = lines,
        };
    }

    private static Ir.BpmItem ConvertBpmItem(Rpe.BpmItem src) =>
        new() { Bpm = src.Bpm, StartBeat = new Beat((int[])src.StartBeat) };

    private static Rpe.BpmItem ConvertBpmItem(Ir.BpmItem src) =>
        new() { Bpm = src.Bpm, StartBeat = new Beat((int[])src.StartBeat) };
}
