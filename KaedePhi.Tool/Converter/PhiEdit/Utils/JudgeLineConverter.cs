using KaedePhi.Tool.Converter.PhiEdit.Model;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

public class JudgeLineConverter
{
    private readonly FrameEventInterpolator _frameEventInterpolator;
    private readonly EventLayerConverter _eventLayerConverter;
    public JudgeLineConverter(PhiEditToKpcConvertOptions options)
    {
        _eventLayerConverter = new EventLayerConverter(options);
        _frameEventInterpolator = new FrameEventInterpolator(options);
    }
    /// <summary>
    /// 转换全部判定线。
    /// </summary>
    public List<Kpc.JudgeLine> ConvertJudgeLines(List<Pe.JudgeLine>? judgeLines)
    {
        if (judgeLines == null || judgeLines.Count == 0) return [];

        var result = new List<Kpc.JudgeLine>(judgeLines.Count);
        for (var i = 0; i < judgeLines.Count; i++)
            result.Add(ConvertJudgeLine(judgeLines[i], i));
        return result;
    }

    /// <summary>
    /// 转换单条判定线，并合成为单事件层的 KPC 判定线。
    /// </summary>
    public Kpc.JudgeLine ConvertJudgeLine(Pe.JudgeLine src, int index)
    {
        var horizonBeat = _frameEventInterpolator.GetJudgeLineHorizonBeat(src);
        var eventLayer = _eventLayerConverter.ConvertEventLayer(src, horizonBeat);
        eventLayer.Anticipation();

        return new Kpc.JudgeLine
        {
            Name = $"PeJudgeLine_{index}",
            Notes = src.NoteList?.ConvertAll(Note.ConvertNote) ?? [],
            EventLayers = [eventLayer]
        };
    }
}