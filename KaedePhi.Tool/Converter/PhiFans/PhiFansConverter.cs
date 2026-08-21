using KaedePhi.Core.Common;
using KaedePhi.Core.PhiFans;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiFans.Model;
using KaedePhi.Tool.Converter.PhiFans.Utils;

namespace KaedePhi.Tool.Converter.PhiFans;

/// <summary>
/// PhiFans 格式转换器。
/// </summary>
public class PhiFansConverter
    : LoggableBase,
        IChartConverter<Chart, Unit?, KpcToPhiFansConvertOptions>,
        ICancellableChartConverter
{
    private CancellationToken _ct;

    /// <inheritdoc/>
    public void SetCancellationToken(CancellationToken ct) => _ct = ct;

    /// <summary>
    /// 将 PhiFans 格式转换为 KPC 内部格式。
    /// </summary>
    /// <param name="source">PhiFans 谱面</param>
    /// <param name="_">未使用</param>
    /// <returns>KPC 谱面</returns>
    public Kpc.Chart ToKpc(Chart source, Unit? _)
    {
        ArgumentNullException.ThrowIfNull(source);
        _ct.ThrowIfCancellationRequested();
        var converted = new Kpc.Chart
        {
            BpmList = source.BpmList.ConvertAll(BpmBuilder.ConvertToKpc),
            Meta = MetaBuilder.ConvertToKpc(source.Info, source.Offset),
            JudgeLineList = ConvertLinesWithCancellation(source.JudgeLineList),
        };
        return KpcChartNormalizer.NormalizeAndValidateNoteEndBeats(converted);
    }

    private List<Kpc.JudgeLine> ConvertLinesWithCancellation(List<Line> lines)
    {
        var result = new List<Kpc.JudgeLine>(lines.Count);
        foreach (var line in lines)
        {
            _ct.ThrowIfCancellationRequested();
            result.Add(JudgeLineBuilder.ConvertToKpc(line));
        }
        return result;
    }

    /// <summary>
    /// 将 KPC 内部格式转换为 PhiFans 格式。
    /// </summary>
    /// <param name="input">KPC 谱面</param>
    /// <param name="options">输出转换选项</param>
    /// <returns>PhiFans 谱面</returns>
    public Chart FromKpc(Kpc.Chart input, KpcToPhiFansConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);
        ConversionOptionsValidator.Validate(options);
        var normalized = KpcChartNormalizer.NormalizeAndValidateNoteEndBeats(input);
        KpcChartValidator.ValidateJudgeLineHierarchy(normalized.JudgeLineList);
        _ct.ThrowIfCancellationRequested();

        var lines = new List<Line>(normalized.JudgeLineList.Count);
        foreach (var line in normalized.JudgeLineList)
        {
            _ct.ThrowIfCancellationRequested();
            lines.Add(JudgeLineBuilder.ConvertFromKpc(line, options));
        }

        return new Chart
        {
            Offset = normalized.Meta.Offset,
            Info = MetaBuilder.ConvertFromKpc(normalized.Meta),
            BpmList = normalized.BpmList.ConvertAll(BpmBuilder.ConvertFromKpc),
            JudgeLineList = lines,
        };
    }
}
