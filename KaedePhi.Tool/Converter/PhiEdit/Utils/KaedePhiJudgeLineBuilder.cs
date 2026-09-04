using KaedePhi.Tool.Converter.PhiEdit.Model;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

/// <summary>
/// PE 判定线到 KPC 判定线的构建器。
/// </summary>
public class KaedePhiJudgeLineBuilder
{
    private readonly PhiEditFrameEventBuilder _phiEditFrameEvent;
    private readonly EventLayerBuilder _eventLayerConverter;
    private readonly CancellationToken _ct;

    public KaedePhiJudgeLineBuilder(
        PhiEditToKpcConvertOptions options,
        CancellationToken ct = default
    )
    {
        _eventLayerConverter = new EventLayerBuilder(options);
        _phiEditFrameEvent = new PhiEditFrameEventBuilder(options);
        _ct = ct;
    }

    /// <summary>
    /// 转换全部判定线。
    /// </summary>
    public List<Kpc.JudgeLine> ConvertJudgeLines(List<Pe.JudgeLine>? judgeLines)
    {
        if (judgeLines == null || judgeLines.Count == 0)
            return [];

        var result = new List<Kpc.JudgeLine>(judgeLines.Count);
        for (var i = 0; i < judgeLines.Count; i++)
        {
            _ct.ThrowIfCancellationRequested();
            result.Add(ConvertJudgeLine(judgeLines[i], i));
        }

        return result;
    }

    /// <summary>
    /// 转换单条判定线，并合成为单事件层的 KPC 判定线。
    /// </summary>
    public Kpc.JudgeLine ConvertJudgeLine(Pe.JudgeLine src, int index)
    {
        var horizonBeat = _phiEditFrameEvent.GetJudgeLineHorizonBeat(src);
        var eventLayer = _eventLayerConverter.ConvertEventLayer(src, horizonBeat);
        eventLayer.Anticipation();

        return new Kpc.JudgeLine
        {
            Name = $"PeJudgeLine_{index}",
            Notes = src.NoteList.ConvertAll(NoteBuilder.ConvertNote),
            EventLayers = [eventLayer],
        };
    }
}
