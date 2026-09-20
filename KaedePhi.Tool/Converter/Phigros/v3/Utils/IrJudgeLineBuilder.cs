using PhigrosJudgeLine = KaedePhi.Core.Formats.Phigros.v3.JudgeLine;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

/// <summary>
/// PhigrosV3 判定线到 IR 判定线的构建器。
/// </summary>
public static class IrJudgeLineBuilder
{
    public static Ir.JudgeLine ConvertJudgeLine(PhigrosJudgeLine src, int index, float defaultBpm)
    {
        var horizonBeat = EventBuilder.GetJudgeLineHorizonBeat(src);

        return new Ir.JudgeLine
        {
            Name = $"PhigrosLine_{index}",
            Notes = NoteBuilder.ConvertNotes(src.NotesAbove, src.NotesBelow),
            EventLayers = [EventLayerBuilder.ConvertEventLayer(src, horizonBeat)],
            BpmFactor = defaultBpm / src.Bpm,
        };
    }
}
