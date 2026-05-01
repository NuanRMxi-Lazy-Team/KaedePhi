using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using Meta = KaedePhi.Core.KaedePhi.Meta;

namespace KaedePhi.Tool.Converter.PhiEdit;

/// <summary>
/// PhiEdit Converter.
/// ToKpc: TInOptions = PhiEditToKpcConvertOptions.
/// FromKpc: TOutOptions = KpcToPhiEditConvertOptions.
/// </summary>
public class PhiEditConverter : LoggableBase,
    IChartConverter<Pe.Chart, PhiEditToKpcConvertOptions, KpcToPhiEditConvertOptions>
{
    public Kpc.Chart ToKpc(Pe.Chart source, PhiEditToKpcConvertOptions option)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(option);

        return new Kpc.Chart
        {
            BpmList = source.BpmList?.ConvertAll(Utils.BpmItem.ConvertBpmItem) ?? [],
            Meta = Utils.Meta.ConvertMeta(source),
            JudgeLineList = new Utils.JudgeLineConverter(option).ConvertJudgeLines(source.JudgeLineList)
        };
    }

    public Pe.Chart FromKpc(Kpc.Chart input, KpcToPhiEditConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);

        WarnIfUnsupportedMeta(input.Meta);

        var judgeLineConverter = new Utils.JudgeLineKpcToPe(options, OnWarning);

        return new Pe.Chart
        {
            Offset = Utils.Meta.GetPeOffset(input.Meta),
            BpmList = input.BpmList?.ConvertAll(Utils.BpmItem.ConvertBpmItem) ?? [],
            JudgeLineList =
                input.JudgeLineList?.ConvertAll(j => judgeLineConverter.ConvertJudgeLine(j, input.JudgeLineList)) ?? []
        };
    }

    private void WarnIfUnsupportedMeta(Meta src)
    {
        var defaults = new Meta();
        if (src.Background != defaults.Background)
            Warn($"PE 不支持 Meta.Background（值='{src.Background}'）");
        if (src.Author != defaults.Author) Warn($"PE 不支持 Meta.Author（值='{src.Author}'）");
        if (src.Composer != defaults.Composer) Warn($"PE 不支持 Meta.Composer（值='{src.Composer}'）");
        if (src.Artist != defaults.Artist) Warn($"PE 不支持 Meta.Artist（值='{src.Artist}'）");
        if (src.Level != defaults.Level) Warn($"PE 不支持 Meta.Level（值='{src.Level}'）");
        if (src.Name != defaults.Name) Warn($"PE 不支持 Meta.Name（值='{src.Name}'）");
        if (src.Song != defaults.Song) Warn($"PE 不支持 Meta.Song（值='{src.Song}'）");
    }


    private void Warn(string message) => LogWarning($"[ToPe] {message}");
}