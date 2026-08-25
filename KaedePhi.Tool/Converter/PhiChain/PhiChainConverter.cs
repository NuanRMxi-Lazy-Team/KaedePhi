using KaedePhi.Core.PhiChain.v6;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiChain.Model;
using KaedePhi.Tool.Converter.PhiChain.Utils;
using PhiChainChart = KaedePhi.Core.PhiChain.v6.Chart;

namespace KaedePhi.Tool.Converter.PhiChain;

/// <summary>
/// PhiChain 格式转换器。
/// </summary>
public class PhiChainConverter
    : LoggableBase,
        IChartConverter<PhiChainChart, PhiChainToKpcConvertOptions, KpcToPhiChainConvertOptions>,
        ICancellableChartConverter
{
    private CancellationToken _ct;

    /// <summary>
    /// 设置取消令牌。
    /// </summary>
    public void SetCancellationToken(CancellationToken ct) => _ct = ct;

    /// <summary>
    /// 将 PhiChain 格式转换为 KPC 内部格式。
    /// </summary>
    /// <param name="source">PhiChain 谱面</param>
    /// <param name="options">转换选项</param>
    /// <returns>KPC 谱面</returns>
    public Kpc.Chart ToKpc(PhiChainChart source, PhiChainToKpcConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        ConversionOptionsValidator.Validate(options);
        _ct.ThrowIfCancellationRequested();

        var kpcChart = new Kpc.Chart
        {
            BpmList = source.BpmList.ConvertAll(BpmBuilder.ConvertBpmPoint),
            Meta = new Kpc.Meta
            {
                Offset = (int)source.Offset, // PhiChain 和 KPC 的 offset 单位均为毫秒
            },
        };

        // 展开树形线结构为扁平列表
        var lineIndex = 0;
        foreach (var line in source.Lines)
        {
            _ct.ThrowIfCancellationRequested();
            JudgeLineBuilder.FlattenLine(
                line,
                -1,
                kpcChart.JudgeLineList,
                ref lineIndex,
                options,
                OnWarning,
                _ct
            );
        }

        return KpcChartNormalizer.NormalizeAndValidateNoteEndBeats(kpcChart);
    }

    /// <summary>
    /// 将 KPC 内部格式转换为 PhiChain 格式。
    /// </summary>
    /// <param name="input">KPC 谱面</param>
    /// <param name="options">输出转换选项</param>
    /// <returns>PhiChain 谱面</returns>
    public PhiChainChart FromKpc(Kpc.Chart input, KpcToPhiChainConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);
        ConversionOptionsValidator.Validate(options);
        var normalized = KpcChartNormalizer.NormalizeAndValidateNoteEndBeats(input);
        KpcChartValidator.ValidateJudgeLineHierarchy(normalized.JudgeLineList);
        _ct.ThrowIfCancellationRequested();
        WarnIfUnsupportedMeta(normalized.Meta);

        var chart = new PhiChainChart
        {
            Offset = normalized.Meta.Offset, // PhiChain 和 KPC 的 offset 单位均为毫秒
            BpmList = new BpmList(normalized.BpmList.ConvertAll(BpmBuilder.ConvertBpmItem)),
            // 构建父子关系树
            Lines = JudgeLineBuilder.BuildLineTree(
                normalized.JudgeLineList,
                options,
                OnWarning,
                _ct
            ),
        };

        return chart;
    }

    /// <summary>
    /// 检查 KPC Meta 字段是否会被 PhiChain 丢弃，发出警告。
    /// </summary>
    /// <param name="src">KPC 元数据</param>
    private void WarnIfUnsupportedMeta(Kpc.Meta src) => WarnIfUnsupportedMeta("PhiChain", src);
}
