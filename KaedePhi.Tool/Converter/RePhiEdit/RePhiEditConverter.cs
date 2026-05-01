using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using KaedePhi.Tool.Converter.RePhiEdit.Utils;

namespace KaedePhi.Tool.Converter.RePhiEdit;

/// <summary>
/// RePhiEdit Converter, 其中，TInOptions无效。
/// </summary>
public class RePhiEditConverter : LoggableBase, IChartConverter<Rpe.Chart, Unit?, ConvertOption>
{
    public Kpc.Chart ToKpc(Rpe.Chart source, Unit? _ = null) => new()
    {
        BpmList = source.BpmList.ConvertAll(ConvertBpmItem),
        Meta = Meta.ConvertMeta(source.Meta),
        JudgeLineList = source.JudgeLineList.ConvertAll(JudgeLine.ConvertJudgeLine)
    };

    public Rpe.Chart FromKpc(Kpc.Chart input, ConvertOption options) => new()
    {
        BpmList = input.BpmList.ConvertAll(ConvertBpmItem),
        Meta = Meta.ConvertMeta(input.Meta),
        JudgeLineList = input.JudgeLineList.ConvertAll(r => JudgeLine.ConvertJudgeLine(r, options.Cutting))
    };

    private static Kpc.BpmItem ConvertBpmItem(Rpe.BpmItem src) => new()
    {
        Bpm = src.Bpm,
        StartBeat = new Beat((int[])src.StartBeat)
    };

    private static Rpe.BpmItem ConvertBpmItem(Kpc.BpmItem src) => new()
    {
        Bpm = src.Bpm,
        StartBeat = new Beat((int[])src.StartBeat)
    };
}