using KaedePhi.Tool.Converter.RePhiEdit;
using Chart = KaedePhi.Core.KaedePhi.Chart;

namespace KaedePhi.Tool.RePhiEdit.Converters;

[Obsolete("请改用 KaedePhi.Tool.Converter.RePhiEdit.RePhiEditConverter.ToKpc()")]
public static class RpeToKpc
{
    [Obsolete("请改用 new RePhiEditConverter().ToKpc(rpe)")]
    public static Chart Convert(Rpe.Chart rpe)
        => new RePhiEditConverter().ToKpc(rpe);
}
