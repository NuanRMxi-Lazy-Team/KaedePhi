using KaedePhi.Core.Primitives;
using KaedePhi.Core.Formats.PhiFans.Model;
using IrNoteType = KaedePhi.Core.Primitives.NoteType;
using PfNoteType = KaedePhi.Core.Formats.PhiFans.Model.NoteType;

namespace KaedePhi.Tool.Converter.PhiFans.Utils;

internal static class NoteBuilder
{
    internal static Ir.Note ConvertToIr(Note src)
    {
        if (
            src.Type == PfNoteType.Hold
            && (!src.HasExplicitHoldEndBeat || src.HoldEndBeat <= src.Beat)
        )
            throw new FormatException("PhiFans Hold 音符缺少有效的结束拍。");

        return new Ir.Note
        {
            Type = MapToIr(src.Type),
            StartBeat = new Beat((int[])src.Beat),
            PositionX = src.PositionX / 100.0,
            SpeedMultiplier = src.Speed,
            Above = src.IsAbove,
            EndBeat = new Beat((int[])(src.Type == PfNoteType.Hold ? src.HoldEndBeat : src.Beat)),
        };
    }

    internal static Note ConvertFromIr(Ir.Note src) =>
        new()
        {
            Type = MapFromIr(src.Type),
            Beat = new Beat((int[])src.StartBeat),
            PositionX = (float)(src.PositionX * 100.0),
            Speed = src.SpeedMultiplier,
            IsAbove = src.Above,
            HoldEndBeat = new Beat((int[])src.EndBeat),
        };

    /// <summary>
    /// 将 PhiFans NoteType (Tap=1, Drag=2, Hold=3, Flick=4) 映射为 IR NoteType (Tap=1, Hold=2, Flick=3, Drag=4)。
    /// </summary>
    private static IrNoteType MapToIr(PfNoteType pfType) =>
        pfType switch
        {
            PfNoteType.Drag => IrNoteType.Drag,
            PfNoteType.Hold => IrNoteType.Hold,
            PfNoteType.Flick => IrNoteType.Flick,
            _ => IrNoteType.Tap,
        };

    private static PfNoteType MapFromIr(IrNoteType irType) =>
        irType switch
        {
            IrNoteType.Drag => PfNoteType.Drag,
            IrNoteType.Hold => PfNoteType.Hold,
            IrNoteType.Flick => PfNoteType.Flick,
            _ => PfNoteType.Tap,
        };
}
