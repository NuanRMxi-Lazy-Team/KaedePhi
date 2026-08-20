using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using PhigrosNote = KaedePhi.Core.Phigros.v3.Note;
using PhigrosNoteType = KaedePhi.Core.Phigros.v3.NoteType;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

/// <summary>
/// PhigrosV3 音符到 KPC 音符的转换工具。
/// </summary>
public static class NoteBuilder
{
    private const float NoteSegmentation = 32f;

    public static List<Kpc.Note> ConvertNotes(
        List<PhigrosNote>? notesAbove,
        List<PhigrosNote>? notesBelow
    )
    {
        var capacity = (notesAbove?.Count ?? 0) + (notesBelow?.Count ?? 0);
        if (capacity == 0)
            return [];

        var result = new List<Kpc.Note>(capacity);
        if (notesAbove != null)
            result.AddRange(notesAbove.Select(n => ConvertNote(n, true)));
        if (notesBelow != null)
            result.AddRange(notesBelow.Select(n => ConvertNote(n, false)));
        return result;
    }

    public static Kpc.Note ConvertNote(PhigrosNote src, bool above)
    {
        if (
            src.Type == PhigrosNoteType.Hold
            && (
                !src.HasExplicitHoldTime
                || !float.IsFinite(src.HoldTime)
                || !(src.HoldTime > 0f)
            )
        )
            throw new FormatException("Phigros Hold 音符缺少有效的持续时间。");

        return new Kpc.Note
        {
            Above = above,
            StartBeat = new Beat(src.Time / NoteSegmentation),
            EndBeat = new Beat(
                src.Type == PhigrosNoteType.Hold
                    ? (src.Time + src.HoldTime) / NoteSegmentation
                    : src.Time / NoteSegmentation
            ),
            PositionX = src.PositionX * Constants.NotePositionXRatio,
            SpeedMultiplier = src.Type != PhigrosNoteType.Hold ? src.Speed : 1f,
            Type = ConvertNoteType(src.Type),
        };
    }

    private static NoteType ConvertNoteType(PhigrosNoteType type) =>
        type switch
        {
            PhigrosNoteType.Tap => NoteType.Tap,
            PhigrosNoteType.Drag => NoteType.Drag,
            PhigrosNoteType.Hold => NoteType.Hold,
            PhigrosNoteType.Flick => NoteType.Flick,
            _ => NoteType.Tap,
        };
}
