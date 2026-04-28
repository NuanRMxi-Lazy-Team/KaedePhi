namespace KaedePhi.Tool.Converter;

public interface IChartConverter<T, TInOptions, TOutOptions>
{
    Kpc.Chart ToKpc(T input, TInOptions options);
    T FromKpc(Kpc.Chart input, TOutOptions options);
}