using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.KaedePhi.Converters.Utils;
using AlphaControl = KaedePhi.Core.KaedePhi.AlphaControl;
using BpmItem = KaedePhi.Core.KaedePhi.BpmItem;
using Chart = KaedePhi.Core.KaedePhi.Chart;
using Easing = KaedePhi.Core.KaedePhi.Easing;
using EventLayer = KaedePhi.Core.KaedePhi.EventLayer;
using ExtendLayer = KaedePhi.Core.KaedePhi.ExtendLayer;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;
using Meta = KaedePhi.Core.KaedePhi.Meta;
using Note = KaedePhi.Core.KaedePhi.Note;
using SizeControl = KaedePhi.Core.KaedePhi.SizeControl;
using SkewControl = KaedePhi.Core.KaedePhi.SkewControl;
using XControl = KaedePhi.Core.KaedePhi.XControl;
using YControl = KaedePhi.Core.KaedePhi.YControl;

namespace KaedePhi.Tool.KaedePhi.Converters;

/// <summary>
/// NRC 格式 → RPE 格式转换器。
/// </summary>
public static class KpcToRpe
{
    private const int UnsupportedEasingCutPrecision = 64;

    public static Rpe.Chart Convert(Chart nrc) => new()
    {
        BpmList = nrc.BpmList.ConvertAll(ConvertBpmItem),
        Meta = ConvertMeta(nrc.Meta),
        JudgeLineList = nrc.JudgeLineList.ConvertAll(ConvertJudgeLine)
    };

    private static Rpe.Easing ConvertEasing(Easing src, bool isBezier = false)
        => isBezier ? new Rpe.Easing(1) : new Rpe.Easing(KpcToCmdysjEasings.MapEasingNumber((int)src));

    private static double TransformX(double x) => CoordinateGeometry.ToRenderX(x);
    private static double TransformY(double y) => CoordinateGeometry.ToRenderY(y);

    private static float FloatTransformX(double x) => CoordinateGeometry.ToRenderXf(x);
    private static float FloatTransformY(double y) => CoordinateGeometry.ToRenderYf(y);

    private static double TransformAngle(double angle) => CoordinateGeometry.ToRenderAngle(angle);

    private static Rpe.BpmItem ConvertBpmItem(BpmItem src) => new()
    {
        BeatPerMinute = src.Bpm,
        StartBeat = new Beat((int[])src.StartBeat)
    };

    private static Rpe.Meta ConvertMeta(Meta src) => new()
    {
        Background = src.Background,
        Charter = src.Author,
        Composer = src.Composer,
        Illustration = src.Artist,
        Level = src.Level,
        Name = src.Name,
        Offset = src.Offset,
        Song = src.Song
    };

    private static Rpe.JudgeLine ConvertJudgeLine(JudgeLine src) => new()
    {
        Name = src.Name,
        Texture = src.Texture,
        Anchor = (float[])src.Anchor.Clone(),
        Father = src.Father,
        IsCover = src.IsCover,
        ZOrder = src.ZOrder,
        AttachUi = src.AttachUi.HasValue ? (Rpe.AttachUi?)(int)src.AttachUi.Value : null,
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

    private static Rpe.Note ConvertNote(Note src) => new()
    {
        Above = src.Above,
        Alpha = src.Alpha,
        StartBeat = new Beat((int[])src.StartBeat),
        EndBeat = new Beat((int[])src.EndBeat),
        IsFake = src.IsFake,
        PositionX = FloatTransformX(src.PositionX),
        Size = src.WidthRatio,
        JudgeArea = src.JudgeArea,
        SpeedMultiplier = src.SpeedMultiplier,
        Type = (Rpe.NoteType)(int)src.Type,
        VisibleTime = src.VisibleTime,
        YOffset = FloatTransformY(src.YOffset),
        Color = src.Tint.ToArray(),
        HitFxColor = src.HitFxColor?.ToArray(),
        HitSound = src.HitSound
    };

    private static Rpe.EventLayer ConvertEventLayer(EventLayer src)
    {
        var rpe = new Rpe.EventLayer();
        if (src.MoveXEvents != null)
        {
            rpe.MoveXEvents = [];
            foreach (var e in src.MoveXEvents) rpe.MoveXEvents.AddRange(ConvertDoubleEventExpanding(e, TransformX));
        }

        if (src.MoveYEvents != null)
        {
            rpe.MoveYEvents = [];
            foreach (var e in src.MoveYEvents) rpe.MoveYEvents.AddRange(ConvertDoubleEventExpanding(e, TransformY));
        }

        if (src.RotateEvents != null)
        {
            rpe.RotateEvents = [];
            foreach (var e in src.RotateEvents)
                rpe.RotateEvents.AddRange(ConvertDoubleEventExpanding(e, TransformAngle));
        }

        if (src.AlphaEvents != null)
        {
            rpe.AlphaEvents = [];
            foreach (var e in src.AlphaEvents) rpe.AlphaEvents.AddRange(ConvertIntEventExpanding(e));
        }

        if (src.SpeedEvents != null)
        {
            rpe.SpeedEvents = [];
            foreach (var e in src.SpeedEvents) rpe.SpeedEvents.AddRange(ConvertFloatEventExpanding(e));
        }

        return rpe;
    }

    private static Rpe.ExtendLayer? ConvertExtendLayer(ExtendLayer? src)
    {
        if (src == null) return null;
        var rpe = new Rpe.ExtendLayer();
        if (src.ColorEvents != null) rpe.ColorEvents = src.ColorEvents.ConvertAll(ConvertByteArrayEvent);
        if (src.ScaleXEvents != null)
        {
            rpe.ScaleXEvents = [];
            foreach (var e in src.ScaleXEvents) rpe.ScaleXEvents.AddRange(ConvertFloatEventExpanding(e));
        }

        if (src.ScaleYEvents != null)
        {
            rpe.ScaleYEvents = [];
            foreach (var e in src.ScaleYEvents) rpe.ScaleYEvents.AddRange(ConvertFloatEventExpanding(e));
        }

        if (src.TextEvents != null) rpe.TextEvents = src.TextEvents.ConvertAll(ConvertStringEvent);
        if (src.PaintEvents != null)
        {
            rpe.PaintEvents = [];
            foreach (var e in src.PaintEvents) rpe.PaintEvents.AddRange(ConvertFloatEventExpanding(e));
        }

        if (src.GifEvents != null)
        {
            rpe.GifEvents = [];
            foreach (var e in src.GifEvents) rpe.GifEvents.AddRange(ConvertFloatEventExpanding(e));
        }

        return rpe;
    }

    private static Rpe.Event<T> ConvertEvent<T>(Kpc.Event<T> src,
        Func<T, T>? valueCopier = null, Func<T, T>? valueTransformer = null)
    {
        valueCopier ??= v => v;
        valueTransformer ??= v => v;
        return new Rpe.Event<T>
        {
            IsBezier = src.IsBezier,
            BezierPoints = src.BezierPoints.ToArray(),
            EasingLeft = src.EasingLeft,
            EasingRight = src.EasingRight,
            Easing = ConvertEasing(src.Easing, src.IsBezier),
            StartValue = valueTransformer(valueCopier(src.StartValue)),
            EndValue = valueTransformer(valueCopier(src.EndValue)),
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
            Font = src.Font
        };
    }

    private static List<Rpe.Event<float>> ConvertFloatEventExpanding(Kpc.Event<float> src)
    {
        try
        {
            return [ConvertEvent(src)];
        }
        catch (KpcToCmdysjEasings.EasingNotSupportedException)
        {
            return CutEventToLinearFloat(src);
        }
    }

    private static List<Rpe.Event<float>> CutEventToLinearFloat(Kpc.Event<float> src)
    {
        var result = new List<Rpe.Event<float>>(UnsupportedEasingCutPrecision);
        var totalBeats = (double)(src.EndBeat - src.StartBeat);
        var current = src.StartBeat;
        var currentVal = src.GetValueAtBeat(src.StartBeat);
        for (var i = 0; i < UnsupportedEasingCutPrecision; i++)
        {
            var isLast = i == UnsupportedEasingCutPrecision - 1;
            var next = isLast
                ? src.EndBeat
                : new Beat((double)src.StartBeat + (i + 1.0) / UnsupportedEasingCutPrecision * totalBeats);
            var nextVal = isLast ? src.EndValue : src.GetValueAtBeat(next);
            result.Add(new Rpe.Event<float>
            {
                StartBeat = new Beat((int[])current),
                EndBeat = new Beat((int[])next),
                StartValue = currentVal,
                EndValue = nextVal,
                Easing = new Rpe.Easing(1),
                BezierPoints = new float[4],
                EasingLeft = 0f,
                EasingRight = 1f,
                Font = src.Font
            });
            current = next;
            currentVal = nextVal;
        }

        return result;
    }

    private static List<Rpe.Event<float>> ConvertDoubleEventExpanding(
        Kpc.Event<double> src, Func<double, double>? valueTransformer = null)
    {
        valueTransformer ??= v => v;
        try
        {
            return
            [
                new Rpe.Event<float>
                {
                    IsBezier = src.IsBezier,
                    BezierPoints = src.BezierPoints.ToArray(),
                    EasingLeft = src.EasingLeft,
                    EasingRight = src.EasingRight,
                    Easing = ConvertEasing(src.Easing, src.IsBezier),
                    StartValue = (float)valueTransformer(src.StartValue),
                    EndValue = (float)valueTransformer(src.EndValue),
                    StartBeat = new Beat((int[])src.StartBeat),
                    EndBeat = new Beat((int[])src.EndBeat),
                    Font = src.Font
                }
            ];
        }
        catch (KpcToCmdysjEasings.EasingNotSupportedException)
        {
            return CutEventToLinear(src, valueTransformer);
        }
    }

    private static List<Rpe.Event<float>> CutEventToLinear(
        Kpc.Event<double> src, Func<double, double> valueTransformer)
    {
        var result = new List<Rpe.Event<float>>(UnsupportedEasingCutPrecision);
        var totalBeats = (double)(src.EndBeat - src.StartBeat);
        var current = src.StartBeat;
        var currentVal = valueTransformer(src.GetValueAtBeat(src.StartBeat));
        for (var i = 0; i < UnsupportedEasingCutPrecision; i++)
        {
            var isLast = i == UnsupportedEasingCutPrecision - 1;
            var next = isLast
                ? src.EndBeat
                : new Beat((double)src.StartBeat + (i + 1.0) / UnsupportedEasingCutPrecision * totalBeats);
            var nextVal = isLast ? valueTransformer(src.EndValue) : valueTransformer(src.GetValueAtBeat(next));
            result.Add(new Rpe.Event<float>
            {
                StartBeat = new Beat((int[])current),
                EndBeat = new Beat((int[])next),
                StartValue = (float)currentVal,
                EndValue = (float)nextVal,
                Easing = new Rpe.Easing(1),
                BezierPoints = new float[4],
                EasingLeft = 0f,
                EasingRight = 1f,
                Font = src.Font
            });
            current = next;
            currentVal = nextVal;
        }

        return result;
    }

    private static List<Rpe.Event<int>> ConvertIntEventExpanding(Kpc.Event<int> src)
    {
        try
        {
            return [ConvertEvent(src)];
        }
        catch (KpcToCmdysjEasings.EasingNotSupportedException)
        {
            return CutEventToLinearInt(src);
        }
    }

    private static List<Rpe.Event<int>> CutEventToLinearInt(Kpc.Event<int> src)
    {
        var result = new List<Rpe.Event<int>>(UnsupportedEasingCutPrecision);
        var totalBeats = (double)(src.EndBeat - src.StartBeat);
        var current = src.StartBeat;
        var currentVal = src.GetValueAtBeat(src.StartBeat);
        for (var i = 0; i < UnsupportedEasingCutPrecision; i++)
        {
            var isLast = i == UnsupportedEasingCutPrecision - 1;
            var next = isLast
                ? src.EndBeat
                : new Beat((double)src.StartBeat + (i + 1.0) / UnsupportedEasingCutPrecision * totalBeats);
            var nextVal = isLast ? src.EndValue : src.GetValueAtBeat(next);
            result.Add(new Rpe.Event<int>
            {
                StartBeat = new Beat((int[])current),
                EndBeat = new Beat((int[])next),
                StartValue = currentVal,
                EndValue = nextVal,
                Easing = new Rpe.Easing(1),
                BezierPoints = new float[4],
                EasingLeft = 0f,
                EasingRight = 1f,
                Font = src.Font
            });
            current = next;
            currentVal = nextVal;
        }

        return result;
    }

    private static Rpe.Event<string> ConvertStringEvent(Kpc.Event<string> src) => ConvertEvent(src);

    private static Rpe.Event<byte[]> ConvertByteArrayEvent(Kpc.Event<byte[]> src) =>
        ConvertEvent(src, v => v.ToArray());

    private static Rpe.XControl ConvertXControl(XControl src) =>
        new() { Easing = ConvertEasing(src.Easing), X = src.X, Pos = src.Pos };

    private static Rpe.AlphaControl ConvertAlphaControl(AlphaControl src) => new()
        { Easing = ConvertEasing(src.Easing), X = src.X, Alpha = src.Alpha };

    private static Rpe.SizeControl ConvertSizeControl(SizeControl src) =>
        new() { Easing = ConvertEasing(src.Easing), X = src.X, Size = src.Size };

    private static Rpe.SkewControl ConvertSkewControl(SkewControl src) =>
        new() { Easing = ConvertEasing(src.Easing), X = src.X, Skew = src.Skew };

    private static Rpe.YControl ConvertYControl(YControl src) =>
        new() { Easing = ConvertEasing(src.Easing), X = src.X, Y = src.Y };
}