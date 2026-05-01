using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter;

public interface IChartConverter<T, in TInOptions, in TOutOptions> : ILoggable
{
    Kpc.Chart ToKpc(T input, TInOptions options);
    T FromKpc(Kpc.Chart input, TOutOptions options);
}