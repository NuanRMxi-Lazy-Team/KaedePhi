using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using PhigrosChart = KaedePhi.Core.Phigros.v3.Chart;

namespace KaedePhi.Tool.Converter.Phigros.v3;

public class PhigrosV3Converter : LoggableBase, IChartConverter<PhigrosChart, Unit?, Unit?>
{
    public Kpc.Chart ToKpc(PhigrosChart input, Unit? options)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new Kpc.Chart
        {
            BpmList = BpmItem.ConvertBpmList(input.JudgeLineList),
            Meta = Meta.ConvertMeta(input),
            JudgeLineList = input.JudgeLineList?
                .Select((j, i) =>
                    JudgeLine.ConvertJudgeLine(j, i, input.JudgeLineList.Count > 0 ? input.JudgeLineList[0].Bpm : 120))
                .ToList() ?? []
        };
    }

    public PhigrosChart FromKpc(Kpc.Chart input, Unit? options)
    {
        throw new NotImplementedException("别急");
    }
}