using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter;

/// <summary>
/// 管线式图表转换入口
/// </summary>
public static class ChartPipeline
{
    /// <summary>
    /// 以指定输入和来源转换器开始一条转换管线，inOptions 为该转换器 ToIr 所需的 option
    /// </summary>
    public static ChartPipelineSource From<TIn, TInOptions, TOutOptions>(
        TIn input,
        IChartConverter<TIn, TInOptions, TOutOptions> converter,
        TInOptions inOptions,
        CancellationToken ct = default
    )
    {
        ct.ThrowIfCancellationRequested();
        var ir = converter.ToIr(input, inOptions);
        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(ir);
        return new ChartPipelineSource(normalized, ct);
    }
}

/// <summary>
/// 管线中间态：已持有 IR 中间格式，等待指定目标转换器
/// </summary>
public sealed class ChartPipelineSource
{
    private readonly Ir.Chart _ir;
    private readonly CancellationToken _ct;

    internal ChartPipelineSource(Ir.Chart ir, CancellationToken ct)
    {
        _ir = ir;
        _ct = ct;
    }

    /// <summary>
    /// 指定目标转换器及其 option，完成管线并立即执行
    /// </summary>
    public TOut To<TOut, TInOptions, TOutOptions>(
        IChartConverter<TOut, TInOptions, TOutOptions> toConverter,
        TOutOptions outOptions
    )
    {
        _ct.ThrowIfCancellationRequested();
        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(_ir);
        return toConverter.FromIr(normalized, outOptions);
    }
}
