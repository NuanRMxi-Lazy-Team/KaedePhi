using PhigrosChart = KaedePhi.Core.Phigros.v3.Chart;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

public static class Meta
{
    public static Kpc.Meta ConvertMeta(PhigrosChart src) => new()
    {
        Offset = (int)(src.Offset * 1000)
    };
}
