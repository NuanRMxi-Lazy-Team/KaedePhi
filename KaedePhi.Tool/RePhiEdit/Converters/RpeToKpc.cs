using System.Diagnostics.Contracts;
using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Interface;
using AlphaControl = KaedePhi.Core.KaedePhi.AlphaControl;
using AttachUi = KaedePhi.Core.KaedePhi.AttachUi;
using BpmItem = KaedePhi.Core.KaedePhi.BpmItem;
using Chart = KaedePhi.Core.KaedePhi.Chart;
using Easing = KaedePhi.Core.KaedePhi.Easing;
using EventLayer = KaedePhi.Core.KaedePhi.EventLayer;
using ExtendLayer = KaedePhi.Core.KaedePhi.ExtendLayer;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;
using Meta = KaedePhi.Core.KaedePhi.Meta;
using Note = KaedePhi.Core.KaedePhi.Note;
using NoteType = KaedePhi.Core.KaedePhi.NoteType;
using SizeControl = KaedePhi.Core.KaedePhi.SizeControl;
using SkewControl = KaedePhi.Core.KaedePhi.SkewControl;
using XControl = KaedePhi.Core.KaedePhi.XControl;
using YControl = KaedePhi.Core.KaedePhi.YControl;

namespace KaedePhi.Tool.RePhiEdit.Converters;

/// <summary>
/// RPE 格式 → NRC 格式转换器。
/// </summary>
public static class RpeToKpc
{
    [Pure]
    public static Chart Convert(Rpe.Chart rpe) => new()
    {
        BpmList = rpe.BpmList.ConvertAll(ConvertBpmItem),
        Meta = ConvertMeta(rpe.Meta),
        JudgeLineList = rpe.JudgeLineList.ConvertAll(ConvertJudgeLine)
    };

    [Pure]
    public static int MapEasingNumber(int rpe) => rpe switch
    {
        1 => 1, 2 => 3, 3 => 2, 4 => 6, 5 => 5, 6 => 4, 7 => 7,
        8 => 9, 9 => 8, 10 => 12, 11 => 11, 12 => 10, 13 => 13,
        14 => 15, 15 => 14, 16 => 18, 17 => 17, 18 => 21, 19 => 20,
        20 => 24, 21 => 23, 22 => 22, 23 => 25, 24 => 27, 25 => 26,
        26 => 30, 27 => 29, 28 => 31, 29 => 28, _ => 1
    };

    private static Easing ConvertEasing(Rpe.Easing src) => new(MapEasingNumber((int)src));
    private static double TransformX(float x) => CoordinateGeometry.ToNrcX(x);
    private static double TransformY(float y) => CoordinateGeometry.ToNrcY(y);
    private static double TransformAngle(float angle) => CoordinateGeometry.ToNrcAngle(angle);

    private static BpmItem ConvertBpmItem(Rpe.BpmItem src) => new()
    {
        Bpm = src.BeatPerMinute,
        StartBeat = new Beat((int[])src.StartBeat)
    };

    private static Meta ConvertMeta(Rpe.Meta src) => new()
    {
        Background = src.Background,
        Author = src.Charter,
        Composer = src.Composer,
        Artist = src.Illustration,
        Level = src.Level,
        Name = src.Name,
        Offset = src.Offset,
        Song = src.Song
    };

    private static JudgeLine ConvertJudgeLine(Rpe.JudgeLine src) => new()
    {
        Name = src.Name,
        Texture = src.Texture,
        Anchor = (float[])src.Anchor.Clone(),
        Father = src.Father,
        IsCover = src.IsCover,
        ZOrder = src.ZOrder,
        AttachUi = src.AttachUi.HasValue ? (AttachUi?)(int)src.AttachUi.Value : null,
        IsGif = src.IsGif,
        BpmFactor = src.BpmFactor,
        RotateWithFather = src.RotateWithFather,
        Notes = src.Notes.ConvertAll(ConvertNote),
        EventLayers = src.EventLayers.ConvertAll(ConvertEventLayer),
        Extended = ConvertExtendLayer(src.Extended),
        PositionControls = src.PositionControls.ConvertAll(ConvertXControl),
        AlphaControls = src.AlphaControls.ConvertAll(ConvertAlphaControl),
        SizeControls = src.SizeControls.ConvertAll(ConvertSizeControl),
        SkewControls = src.SkewControls.ConvertAll(ConvertSkewControl),
        YControls = src.YControls.ConvertAll(ConvertYControl)
    };

    private static Note ConvertNote(Rpe.Note src) => new()
    {
        Above = src.Above,
        Alpha = src.Alpha,
        StartBeat = new Beat((int[])src.StartBeat),
        EndBeat = new Beat((int[])src.EndBeat),
        IsFake = src.IsFake,
        PositionX = TransformX(src.PositionX),
        WidthRatio = src.Size,
        JudgeArea = src.JudgeArea,
        SpeedMultiplier = src.SpeedMultiplier,
        Type = (NoteType)(int)src.Type,
        VisibleTime = src.VisibleTime,
        YOffset = TransformY(src.YOffset),
        Tint = src.Color.ToArray(),
        HitFxColor = src.HitFxColor?.ToArray(),
        HitSound = src.HitSound
    };

    private static EventLayer ConvertEventLayer(Rpe.EventLayer src)
    {
        var nrc = new EventLayer();
        if (src.MoveXEvents != null)
            nrc.MoveXEvents = src.MoveXEvents.ConvertAll(e => ConvertFloatToDoubleEvent(e, TransformX));
        if (src.MoveYEvents != null)
            nrc.MoveYEvents = src.MoveYEvents.ConvertAll(e => ConvertFloatToDoubleEvent(e, TransformY));
        if (src.RotateEvents != null)
            nrc.RotateEvents = src.RotateEvents.ConvertAll(e => ConvertFloatToDoubleEvent(e, TransformAngle));
        if (src.AlphaEvents != null) nrc.AlphaEvents = src.AlphaEvents.ConvertAll(ConvertIntEvent);
        if (src.SpeedEvents != null) nrc.SpeedEvents = src.SpeedEvents.ConvertAll(ConvertFloatEvent);
        return nrc;
    }

    private static ExtendLayer? ConvertExtendLayer(Rpe.ExtendLayer? src)
    {
        if (src == null) return null;
        var nrc = new ExtendLayer();
        if (src.ColorEvents != null) nrc.ColorEvents = src.ColorEvents.ConvertAll(ConvertByteArrayEvent);
        if (src.ScaleXEvents != null) nrc.ScaleXEvents = src.ScaleXEvents.ConvertAll(ConvertFloatEvent);
        if (src.ScaleYEvents != null) nrc.ScaleYEvents = src.ScaleYEvents.ConvertAll(ConvertFloatEvent);
        if (src.TextEvents != null) nrc.TextEvents = src.TextEvents.ConvertAll(ConvertStringEvent);
        if (src.PaintEvents != null) nrc.PaintEvents = src.PaintEvents.ConvertAll(ConvertFloatEvent);
        if (src.GifEvents != null) nrc.GifEvents = src.GifEvents.ConvertAll(ConvertFloatEvent);
        return nrc;
    }

    private static Kpc.Event<T> ConvertEvent<T>(Rpe.Event<T> src, Func<T, T>? valueCopier = null,
        Func<T, T>? valueTransformer = null)
    {
        valueCopier ??= v => v;
        valueTransformer ??= v => v;
        return new Kpc.Event<T>
        {
            IsBezier = src.IsBezier,
            BezierPoints = src.BezierPoints.ToArray(),
            EasingLeft = src.EasingLeft,
            EasingRight = src.EasingRight,
            Easing = ConvertEasing(src.Easing),
            StartValue = valueTransformer(valueCopier(src.StartValue)),
            EndValue = valueTransformer(valueCopier(src.EndValue)),
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
            Font = src.Font
        };
    }

    private static Kpc.Event<double> ConvertFloatToDoubleEvent(Rpe.Event<float> src,
        Func<float, double> valueTransformer) => new()
    {
        IsBezier = src.IsBezier,
        BezierPoints = src.BezierPoints.ToArray(),
        EasingLeft = src.EasingLeft,
        EasingRight = src.EasingRight,
        Easing = ConvertEasing(src.Easing),
        StartValue = valueTransformer(src.StartValue),
        EndValue = valueTransformer(src.EndValue),
        StartBeat = new Beat((int[])src.StartBeat),
        EndBeat = new Beat((int[])src.EndBeat),
        Font = src.Font
    };

    private static Kpc.Event<float> ConvertFloatEvent(Rpe.Event<float> src) => ConvertEvent(src);
    private static Kpc.Event<int> ConvertIntEvent(Rpe.Event<int> src) => ConvertEvent(src);
    private static Kpc.Event<string> ConvertStringEvent(Rpe.Event<string> src) => ConvertEvent(src);

    private static Kpc.Event<byte[]> ConvertByteArrayEvent(Rpe.Event<byte[]> src) =>
        ConvertEvent(src, v => v.ToArray());

    private static XControl ConvertXControl(Rpe.XControl src) =>
        new() { Easing = ConvertEasing(src.Easing), X = src.X, Pos = src.Pos };

    private static AlphaControl ConvertAlphaControl(Rpe.AlphaControl src) => new()
        { Easing = ConvertEasing(src.Easing), X = src.X, Alpha = src.Alpha };

    private static SizeControl ConvertSizeControl(Rpe.SizeControl src) =>
        new() { Easing = ConvertEasing(src.Easing), X = src.X, Size = src.Size };

    private static SkewControl ConvertSkewControl(Rpe.SkewControl src) =>
        new() { Easing = ConvertEasing(src.Easing), X = src.X, Skew = src.Skew };

    private static YControl ConvertYControl(Rpe.YControl src) =>
        new() { Easing = ConvertEasing(src.Easing), X = src.X, Y = src.Y };
}