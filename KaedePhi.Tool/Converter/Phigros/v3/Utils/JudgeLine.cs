using PhigrosJudgeLine = KaedePhi.Core.Phigros.v3.JudgeLine;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

public static class JudgeLine
{
    public static Kpc.JudgeLine ConvertJudgeLine(PhigrosJudgeLine src, int index)
    {
        var horizonBeat = Event.GetJudgeLineHorizonBeat(src);

        return new Kpc.JudgeLine
        {
            Name = $"PhigrosLine_{index}",
            Notes = Note.ConvertNotes(src.NotesAbove, src.NotesBelow),
            EventLayers = [EventLayer.ConvertEventLayer(src, horizonBeat)]
        };
    }
}
