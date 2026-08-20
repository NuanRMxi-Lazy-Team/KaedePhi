using KaedePhi.Core.Common;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

/// <summary>
/// PE 与 KPC 音符之间的双向转换工具。
/// </summary>
public static class NoteBuilder
{
    public static Kpc.Note ConvertNote(Pe.Note src)
    {
        if (
            src.Type == Pe.NoteType.Hold
            && (!float.IsFinite(src.EndBeat) || !(src.EndBeat > src.StartBeat))
        )
            throw new FormatException("PhiEdit Hold 音符缺少有效的结束拍。");

        return new Kpc.Note
        {
            Above = src.Above,
            StartBeat = new Beat(src.StartBeat),
            EndBeat = new Beat(src.Type == Pe.NoteType.Hold ? src.EndBeat : src.StartBeat),
            IsFake = src.IsFake,
            PositionX = Transform.TransformToKpcX(src.PositionX) + Kpc.Chart.CoordinateSystem.MaxX,
            WidthRatio = src.WidthRatio,
            SpeedMultiplier = src.SpeedMultiplier,
            Type = (NoteType)(int)src.Type,
        };
    }

    public static Pe.Note ConvertNote(Kpc.Note src, Action<string>? warnLogger)
    {
        WarnIfUnsupportedNoteFields(src, warnLogger);
        return new Pe.Note
        {
            Above = src.Above,
            StartBeat = (float)(double)src.StartBeat,
            EndBeat = (float)(double)src.EndBeat,
            IsFake = src.IsFake,
            PositionX = Transform.TransformToPeX(src.PositionX - Kpc.Chart.CoordinateSystem.MaxX),
            WidthRatio = src.WidthRatio,
            SpeedMultiplier = src.SpeedMultiplier,
            Type = (Pe.NoteType)(int)src.Type,
        };
    }

    private static void WarnIfUnsupportedNoteFields(Kpc.Note src, Action<string>? warnLogger)
    {
        if (src.Alpha != 255)
            Warn($"PE 不支持 Note.Alpha（值={src.Alpha}）");
        if (Math.Abs(src.JudgeArea - 1f) > 1e-6f)
            Warn($"PE 不支持 Note.JudgeArea（值={src.JudgeArea}）");
        if (Math.Abs(src.VisibleTime - 999999f) > 1e-6f)
            Warn($"PE 不支持 Note.VisibleTime（值={src.VisibleTime}）");
        if (Math.Abs(src.YOffset) > 1e-6f)
            Warn($"PE 不支持 Note.YOffset（值={src.YOffset}）");
        if (!IsDefaultTint(src.Tint))
            Warn($"PE 不支持 Note.Tint（值='[{string.Join(", ", src.Tint)}]'）");
        if (src.HitFxColor != null)
            Warn($"PE 不支持 Note.HitFxColor（值='[{string.Join(", ", src.HitFxColor)}]'）");
        if (!string.IsNullOrWhiteSpace(src.HitSound))
            Warn($"PE 不支持 Note.HitSound（值='{src.HitSound}'）");
        return;
        void Warn(string message) => warnLogger?.Invoke(message);
    }

    private static bool IsDefaultTint(byte[]? tint) => tint is [255, 255, 255];
}
