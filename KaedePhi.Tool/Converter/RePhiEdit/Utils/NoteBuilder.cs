using KaedePhi.Core.Primitives;

namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

/// <summary>
/// RPE 与 IR 音符之间的双向转换工具。
/// </summary>
public static class NoteBuilder
{
    public static Ir.Note ConvertNote(Rpe.Note src)
    {
        if (src.Type == NoteType.Hold && (!src.HasExplicitEndBeat || src.EndBeat <= src.StartBeat))
            throw new FormatException("RePhiEdit Hold 音符缺少有效的结束拍。");

        return new Ir.Note
        {
            Above = src.Above,
            Alpha = src.Alpha,
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])(src.Type == NoteType.Hold ? src.EndBeat : src.StartBeat)),
            IsFake = src.IsFake,
            PositionX = Transform.TransformToIrX(src.PositionX),
            WidthRatio = src.Size,
            JudgeArea = src.JudgeArea,
            SpeedMultiplier = src.SpeedMultiplier,
            Type = (NoteType)(int)src.Type,
            VisibleTime = src.VisibleTime,
            YOffset = Transform.TransformToIrY(src.YOffset),
            Tint = src.Color.ToArray(),
            HitFxColor = src.HitFxColor?.ToArray(),
            HitSound = src.HitSound,
        };
    }

    public static Rpe.Note ConvertNote(Ir.Note src) =>
        new()
        {
            Above = src.Above,
            Alpha = src.Alpha,
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
            IsFake = src.IsFake,
            PositionX = Transform.FloatTransformToRpeX(src.PositionX),
            Size = src.WidthRatio,
            JudgeArea = src.JudgeArea,
            SpeedMultiplier = src.SpeedMultiplier,
            Type = (NoteType)(int)src.Type,
            VisibleTime = src.VisibleTime,
            YOffset = Transform.FloatTransformToRpeY(src.YOffset),
            Color = src.Tint.ToArray(),
            HitFxColor = src.HitFxColor?.ToArray(),
            HitSound = src.HitSound,
        };
}
