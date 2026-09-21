using KaedePhi.Core.Formats.PhiChain.v6.Model;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiChain.Model;
using KaedePhi.Tool.Converter.PhiChain.Utils;
using PhiChainChart = KaedePhi.Core.Formats.PhiChain.v6.Model.Chart;

namespace KaedePhi.Tool.Converter.PhiChain;

/// <summary>
/// PhiChain 格式转换器。
/// </summary>
public class PhiChainConverter
    : LoggableBase,
        IChartConverter<PhiChainChart, PhiChainToIrConvertOptions, IrToPhiChainConvertOptions>,
        ICancellableChartConverter
{
    private CancellationToken _ct;

    /// <summary>
    /// 设置取消令牌。
    /// </summary>
    public void SetCancellationToken(CancellationToken ct) => _ct = ct;

    /// <summary>
    /// 将 PhiChain 格式转换为 IR 内部格式。
    /// </summary>
    /// <param name="source">PhiChain 谱面</param>
    /// <param name="options">转换选项</param>
    /// <returns>IR 谱面</returns>
    public Ir.Chart ToIr(PhiChainChart source, PhiChainToIrConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        ConversionOptionsValidator.Validate(options);
        _ct.ThrowIfCancellationRequested();

        var irChart = new Ir.Chart
        {
            BpmList = source.BpmList.ConvertAll(BpmBuilder.ConvertBpmPoint),
            Meta = new Ir.Meta
            {
                Offset = (int)source.Offset, // PhiChain 和 IR 的 offset 单位均为毫秒
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
                irChart.JudgeLineList,
                ref lineIndex,
                options,
                OnWarning,
                _ct
            );
        }

        return IrChartNormalizer.NormalizeAndValidateNoteEndBeats(irChart);
    }

    /// <summary>
    /// 将 IR 内部格式转换为 PhiChain 格式。
    /// </summary>
    /// <param name="input">IR 谱面</param>
    /// <param name="options">输出转换选项</param>
    /// <returns>PhiChain 谱面</returns>
    public PhiChainChart FromIr(Ir.Chart input, IrToPhiChainConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);
        ConversionOptionsValidator.Validate(options);
        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(input);
        IrChartValidator.ValidateJudgeLineHierarchy(normalized.JudgeLineList);
        _ct.ThrowIfCancellationRequested();
        WarnIfUnsupportedMeta(normalized.Meta);

        var chart = new PhiChainChart
        {
            Offset = normalized.Meta.Offset, // PhiChain 和 IR 的 offset 单位均为毫秒
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
    /// 检查 IR Meta 字段是否会被 PhiChain 丢弃，发出警告。
    /// </summary>
    /// <param name="src">IR 元数据</param>
    private void WarnIfUnsupportedMeta(Ir.Meta src) => WarnIfUnsupportedMeta("PhiChain", src);
}
