using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KpcEventLayer = KaedePhi.Core.KaedePhi.Events.EventLayer;
using KpcNote = KaedePhi.Core.KaedePhi.Note;
using KpcSpeedEvent = KaedePhi.Core.KaedePhi.Events.Event<float>;
using PhigrosNote = KaedePhi.Core.Phigros.v3.Note;
using PhigrosNoteType = KaedePhi.Core.Phigros.v3.NoteType;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

/// <summary>
/// KPC 音符到 PhigrosV3 音符的转换工具。
/// </summary>
public static class PhigrosV3NoteBuilder
{
    public static (List<PhigrosNote> above, List<PhigrosNote> below) ConvertNotes(
        List<KpcNote>? notes,
        List<KpcSpeedEvent>? speedEvents,
        Action<string>? warnLogger,
        bool filterFakeNotes = false
    )
    {
        if (notes is not { Count: > 0 })
            return ([], []);

        var above = new List<PhigrosNote>();
        var below = new List<PhigrosNote>();

        foreach (var note in notes)
        {
            if (filterFakeNotes && note.IsFake)
                continue;

            var converted = ConvertNote(note, speedEvents, warnLogger);
            if (note.Above)
                above.Add(converted);
            else
                below.Add(converted);
        }

        return (above, below);
    }

    public static PhigrosNote ConvertNote(
        KpcNote src,
        List<KpcSpeedEvent>? speedEvents,
        Action<string>? warnLogger
    )
    {
        WarnIfUnsupportedNoteFields(src, warnLogger);

        var startBeat = (double)src.StartBeat;
        var time = (int)Math.Round(startBeat * 32);

        var phigrosType = ConvertNoteType(src.Type);

        var holdTime = 0f;
        var speed = src.SpeedMultiplier;

        if (phigrosType == PhigrosNoteType.Hold)
        {
            var endBeat = (double)src.EndBeat;
            holdTime = (float)((endBeat - startBeat) * 32);
            speed = GetSpeedAtBeat(speedEvents, endBeat);
        }

        return new PhigrosNote
        {
            Type = phigrosType,
            Time = time,
            PositionX = (float)(src.PositionX / Constants.NotePositionXRatio),
            HoldTime = holdTime,
            Speed = speed,
        };
    }

    private static float GetSpeedAtBeat(List<KpcSpeedEvent>? speedEvents, double beat)
    {
        if (speedEvents is not { Count: > 0 })
            return 1f;

        var beatObj = new Beat(beat);
        if (beatObj < speedEvents[0].StartBeat)
            return 1f;

        var speed = KpcEventLayer.GetValueAtBeat(speedEvents, beatObj);
        return (float)(speed / Constants.SpeedValueRatio);
    }

    public static PhigrosNoteType ConvertNoteType(NoteType type) =>
        type switch
        {
            NoteType.Tap => PhigrosNoteType.Tap,
            NoteType.Hold => PhigrosNoteType.Hold,
            NoteType.Flick => PhigrosNoteType.Flick,
            NoteType.Drag => PhigrosNoteType.Drag,
            _ => PhigrosNoteType.Tap,
        };

    private static void WarnIfUnsupportedNoteFields(KpcNote src, Action<string>? warnLogger)
    {
        if (src.Alpha != 255)
            Warn($"PhigrosV3 不支持 Note.Alpha（值={src.Alpha}）");
        if (Math.Abs(src.JudgeArea - 1f) > Constants.FloatEpsilon)
            Warn($"PhigrosV3 不支持 Note.JudgeArea（值={src.JudgeArea}）");
        if (Math.Abs(src.VisibleTime - 999999f) > Constants.FloatEpsilon)
            Warn($"PhigrosV3 不支持 Note.VisibleTime（值={src.VisibleTime}）");
        if (Math.Abs(src.YOffset) > Constants.FloatEpsilon)
            Warn($"PhigrosV3 不支持 Note.YOffset（值={src.YOffset}）");
        if (!IsDefaultTint(src.Tint))
            Warn($"PhigrosV3 不支持 Note.Tint（值='[{string.Join(", ", src.Tint)}]'）");
        if (src.HitFxColor != null)
            Warn($"PhigrosV3 不支持 Note.HitFxColor（值='[{string.Join(", ", src.HitFxColor)}]'）");
        if (!string.IsNullOrWhiteSpace(src.HitSound))
            Warn($"PhigrosV3 不支持 Note.HitSound（值='{src.HitSound}'）");
        if (src.IsFake)
            Warn($"PhigrosV3 不支持 Note.IsFake（值={src.IsFake}）");
        if (Math.Abs(src.WidthRatio - 1f) > Constants.FloatEpsilon)
            Warn($"PhigrosV3 不支持 Note.WidthRatio（值={src.WidthRatio}）");
        return;
        void Warn(string message) => warnLogger?.Invoke(message);
    }

    private static bool IsDefaultTint(byte[]? tint) => tint is [255, 255, 255];
}
