using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter.KaedePhi;

/// <summary>
/// 无实际用途，防止Converter路径中传入的本来就是kpc，所以需要传入一个多此一举的转换器
/// </summary>
public class KaedePhiConverter : IChartConverter<Kpc.Chart,Unit,Unit>
{
    public Kpc.Chart ToKpc(Kpc.Chart input, Unit options)
    {
        return input;
    }

    public Kpc.Chart FromKpc(Kpc.Chart input, Unit options)
    {
        return input;
    }
}