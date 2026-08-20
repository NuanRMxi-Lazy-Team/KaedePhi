using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using Meta = KaedePhi.Core.KaedePhi.Meta;

namespace KaedePhi.Tool.Converter.PhiEdit;

/// <summary>
/// PhiEdit 格式转换器。
/// </summary>
public class PhiEditConverter
    : LoggableBase,
        IChartConverter<Pe.Chart, PhiEditToKpcConvertOptions, KpcToPhiEditConvertOptions>,
        ICancellableChartConverter
{
    private CancellationToken _ct;

    /// <inheritdoc/>
    public void SetCancellationToken(CancellationToken ct) => _ct = ct;

    /// <summary>
    /// 将 PhiEdit 格式转换为 KPC 内部格式。
    /// </summary>
    /// <param name="source">PhiEdit 谱面</param>
    /// <param name="option">输入转换选项</param>
    /// <returns>KPC 谱面</returns>
    public Kpc.Chart ToKpc(Pe.Chart source, PhiEditToKpcConvertOptions option)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(option);
        ConversionOptionsValidator.Validate(option);

        _ct.ThrowIfCancellationRequested();

        return new Kpc.Chart
        {
            BpmList = source.BpmList.ConvertAll(Utils.BpmItemBuilder.ConvertBpmItem),
            Meta = Utils.MetaBuilder.ConvertMeta(source),
            JudgeLineList = new Utils.KaedePhiJudgeLineBuilder(option, _ct).ConvertJudgeLines(
                source.JudgeLineList
            ),
        };
    }

    /// <summary>
    /// 将 KPC 内部格式转换为 PhiEdit 格式。
    /// </summary>
    /// <param name="input">KPC 谱面</param>
    /// <param name="options">输出转换选项</param>
    /// <returns>PhiEdit 谱面</returns>
    public Pe.Chart FromKpc(Kpc.Chart input, KpcToPhiEditConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);
        ConversionOptionsValidator.Validate(options);
        ChartProcessingValidator.ValidateJudgeLineHierarchy(input.JudgeLineList);
        _ct.ThrowIfCancellationRequested();

        WarnIfUnsupportedMeta(input.Meta);

        var judgeLineConverter = new Utils.PhiEditJudgeLineBuilder(options, OnWarning);
        var judgeLines = new List<Pe.JudgeLine>(input.JudgeLineList.Count);
        foreach (var line in input.JudgeLineList)
        {
            _ct.ThrowIfCancellationRequested();
            var converted = judgeLineConverter.ConvertJudgeLine(line, input.JudgeLineList);
            if (converted is not null)
                judgeLines.Add(converted);
        }

        return new Pe.Chart
        {
            Offset = Utils.MetaBuilder.GetPeOffset(input.Meta),
            BpmList = input.BpmList.ConvertAll(Utils.BpmItemBuilder.ConvertBpmItem),
            JudgeLineList = judgeLines,
        };
    }

    private void WarnIfUnsupportedMeta(Meta src) => WarnIfUnsupportedMeta("PE", src);

    private void Warn(string message) => LogWarning(message);
}
