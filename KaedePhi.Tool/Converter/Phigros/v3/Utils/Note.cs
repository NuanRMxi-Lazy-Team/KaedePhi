using KaedePhi.Core.Common;
using PhigrosNote = KaedePhi.Core.Phigros.v3.Note;
using PhigrosNoteType = KaedePhi.Core.Phigros.v3.NoteType;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

public static class Note
{
    private const float NotePositionXRatio = 0.1125f;
    public static List<Kpc.Note> ConvertNotes(
        List<PhigrosNote>? notesAbove,
        List<PhigrosNote>? notesBelow)
    {
        var capacity = (notesAbove?.Count ?? 0) + (notesBelow?.Count ?? 0);
        if (capacity == 0) return [];

        var result = new List<Kpc.Note>(capacity);
        if (notesAbove != null)
            result.AddRange(notesAbove.Select(n => ConvertNote(n, true)));
        if (notesBelow != null)
            result.AddRange(notesBelow.Select(n => ConvertNote(n, false)));
        return result;
    }

    public static Kpc.Note ConvertNote(PhigrosNote src, bool above) => new()
    {
        Above = above,
        StartBeat = new Beat(src.Time / 32.0),
        EndBeat = new Beat(src.Type == PhigrosNoteType.Hold
            ? (src.Time + src.HoldTime) / 32.0
            : src.Time / 32.0),
        PositionX = src.PositionX * NotePositionXRatio,
        SpeedMultiplier = src.Type != PhigrosNoteType.Hold ? src.Speed : 1f,
        Type = ConvertNoteType(src.Type)
    };

    public static Kpc.NoteType ConvertNoteType(PhigrosNoteType type) => type switch
    {
        PhigrosNoteType.Tap => Kpc.NoteType.Tap,
        PhigrosNoteType.Drag => Kpc.NoteType.Drag,
        PhigrosNoteType.Hold => Kpc.NoteType.Hold,
        PhigrosNoteType.Flick => Kpc.NoteType.Flick,
        _ => Kpc.NoteType.Tap
    };
}
