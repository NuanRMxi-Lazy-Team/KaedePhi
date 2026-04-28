using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter;

public static class ChartConverter
{
    public static TOut Convert<TIn, TOut, TInOptions, TOutOptions>(
        TIn input,
        IChartConverter<TIn, TInOptions, TOutOptions> from,
        IChartConverter<TOut, TInOptions, TOutOptions> to,
        TInOptions toKpcOptions,
        TOutOptions toTargetOptions)
    {
        var ir = from.ToKpc(input, toKpcOptions);
        return to.FromKpc(ir, toTargetOptions);
    }

    public static TOut Convert<TIn, TOut, TOutOptions>(
        TIn input,
        IChartConverter<TIn, Unit, TOutOptions> from,
        IChartConverter<TOut, Unit, TOutOptions> to,
        TOutOptions toTargetOptions)
    {
        var ir = from.ToKpc(input, new Unit());
        return to.FromKpc(ir, toTargetOptions);
    }
    
    public static TOut Convert<TIn, TOut, TInOptions>(
        TIn input,
        IChartConverter<TIn, TInOptions, Unit> from,
        IChartConverter<TOut, TInOptions, Unit> to,
        TInOptions toKpcOptions)
    {
        var ir = from.ToKpc(input, toKpcOptions);
        return to.FromKpc(ir, new Unit());
    }
}