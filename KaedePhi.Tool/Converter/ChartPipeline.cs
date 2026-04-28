namespace KaedePhi.Tool.Converter;

/// <summary>
/// 管线式图表转换入口
/// </summary>
public static class ChartPipeline
{
    /// <summary>
    /// 以指定输入和来源转换器开始一条转换管线，inOptions 为该转换器 ToKpc 所需的 option
    /// </summary>
    public static ChartPipelineSource From<TIn, TInOptions, TOutOptions>(
        TIn input,
        IChartConverter<TIn, TInOptions, TOutOptions> converter,
        TInOptions inOptions)
    {
        var kpc = converter.ToKpc(input, inOptions);
        return new ChartPipelineSource(kpc);
    }
}

/// <summary>
/// 管线中间态：已持有 KPC 中间格式，等待指定目标转换器
/// </summary>
public sealed class ChartPipelineSource
{
    private readonly Kpc.Chart _kpc;

    internal ChartPipelineSource(Kpc.Chart kpc) => _kpc = kpc;

    /// <summary>
    /// 指定目标转换器及其 option，完成管线并立即执行
    /// </summary>
    public TOut To<TOut, TInOptions, TOutOptions>(
        IChartConverter<TOut, TInOptions, TOutOptions> toConverter,
        TOutOptions outOptions)
        => toConverter.FromKpc(_kpc, outOptions);
}
