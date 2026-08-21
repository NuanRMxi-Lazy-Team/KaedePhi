using KaedePhi.Core.Common;
using KaedePhi.Core.PhiFans;
using KpcNoteType = KaedePhi.Core.Common.NoteType;
using PfNoteType = KaedePhi.Core.PhiFans.NoteType;

namespace KaedePhi.Tool.Converter.PhiFans.Utils;

internal static class NoteBuilder
{
    internal static Kpc.Note ConvertToKpc(Note src)
    {
        if (
            src.Type == PfNoteType.Hold
            && (!src.HasExplicitHoldEndBeat || src.HoldEndBeat <= src.Beat)
        )
            throw new FormatException("PhiFans Hold 音符缺少有效的结束拍。");

        return new Kpc.Note
        {
            Type = MapToKpc(src.Type),
            StartBeat = new Beat((int[])src.Beat),
            PositionX = src.PositionX / 100.0,
            SpeedMultiplier = src.Speed,
            Above = src.IsAbove,
            EndBeat = new Beat(
                (int[])(src.Type == PfNoteType.Hold ? src.HoldEndBeat : src.Beat)
            ),
        };
    }

    internal static Note ConvertFromKpc(Kpc.Note src) =>
        new()
        {
            Type = MapFromKpc(src.Type),
            Beat = new Beat((int[])src.StartBeat),
            PositionX = (float)(src.PositionX * 100.0),
            Speed = src.SpeedMultiplier,
            IsAbove = src.Above,
            HoldEndBeat = new Beat((int[])src.EndBeat),
        };

    /// <summary>
    /// 将 PhiFans NoteType (Tap=1, Drag=2, Hold=3, Flick=4) 映射为 KPC NoteType (Tap=1, Hold=2, Flick=3, Drag=4)。
    /// </summary>
    private static KpcNoteType MapToKpc(PfNoteType pfType) =>
        pfType switch
        {
            PfNoteType.Drag => KpcNoteType.Drag,
            PfNoteType.Hold => KpcNoteType.Hold,
            PfNoteType.Flick => KpcNoteType.Flick,
            _ => KpcNoteType.Tap,
        };

    private static PfNoteType MapFromKpc(KpcNoteType kpcType) =>
        kpcType switch
        {
            KpcNoteType.Drag => PfNoteType.Drag,
            KpcNoteType.Hold => PfNoteType.Hold,
            KpcNoteType.Flick => PfNoteType.Flick,
            _ => PfNoteType.Tap,
        };
}
