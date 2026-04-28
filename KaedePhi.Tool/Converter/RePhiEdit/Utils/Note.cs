using KaedePhi.Core.Common;

namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

public class Note
{
    public static Kpc.Note ConvertNote(Rpe.Note src) => new()
    {
        Above = src.Above,
        Alpha = src.Alpha,
        StartBeat = new Beat((int[])src.StartBeat),
        EndBeat = new Beat((int[])src.EndBeat),
        IsFake = src.IsFake,
        PositionX = Transform.TransformToKpcX(src.PositionX),
        WidthRatio = src.Size,
        JudgeArea = src.JudgeArea,
        SpeedMultiplier = src.SpeedMultiplier,
        Type = (Kpc.NoteType)(int)src.Type,
        VisibleTime = src.VisibleTime,
        YOffset = Transform.TransformToKpcY(src.YOffset),
        Tint = src.Color.ToArray(),
        HitFxColor = src.HitFxColor?.ToArray(),
        HitSound = src.HitSound
    };
    
    public static Rpe.Note ConvertNote(Kpc.Note src) => new()
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
        Type = (Rpe.NoteType)(int)src.Type,
        VisibleTime = src.VisibleTime,
        YOffset = Transform.FloatTransformToRpeY(src.YOffset),
        Color = src.Tint.ToArray(),
        HitFxColor = src.HitFxColor?.ToArray(),
        HitSound = src.HitSound
    };
}