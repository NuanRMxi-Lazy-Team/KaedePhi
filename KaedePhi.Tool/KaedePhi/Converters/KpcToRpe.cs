using KaedePhi.Tool.Converter.RePhiEdit;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using RpeChart = KaedePhi.Core.RePhiEdit.Chart;
using KpcChart = KaedePhi.Core.KaedePhi.Chart;

namespace KaedePhi.Tool.KaedePhi.Converters;

[Obsolete("请改用 KaedePhi.Tool.Converter.RePhiEdit.RePhiEditConverter.FromKpc()")]
public static class KpcToRpe
{
    [Obsolete("请改用 new RePhiEditConverter().FromKpc(kpc, new ConvertOption())")]
    public static RpeChart Convert(KpcChart kpc)
        => new RePhiEditConverter().FromKpc(kpc, new ConvertOption());
}
