#pragma warning disable CS0618

using Kpc = KaedePhi.Core.KaedePhi;
using KpcControls = KaedePhi.Core.KaedePhi.Controls;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;
using KpcCommon = KaedePhi.Core.Common;
using Primitives = KaedePhi.Core.Primitives;

namespace KaedePhi.Tool.Compatibility;

/// <summary>
/// 旧 KPC 模型与中间表示（IR）模型之间的结构转换，仅供废弃兼容层使用。
/// </summary>
internal static class KpcCompatibilityMapper
{
    internal static Ir.Chart ToIntermediate(Kpc.Chart chart)
    {
        ArgumentNullException.ThrowIfNull(chart);
        return new Ir.Chart
        {
            BpmList = MapList(chart.BpmList, ToIntermediate)!,
            Meta = ToIntermediate(chart.Meta),
            JudgeLineList = MapList(chart.JudgeLineList, ToIntermediate)!,
        };
    }

    internal static Kpc.Chart ToKpc(Ir.Chart chart)
    {
        ArgumentNullException.ThrowIfNull(chart);
        return new Kpc.Chart
        {
            BpmList = MapList(chart.BpmList, ToKpc)!,
            Meta = ToKpc(chart.Meta),
            JudgeLineList = MapList(chart.JudgeLineList, ToKpc)!,
        };
    }

    internal static List<Ir.JudgeLine> ToIntermediate(IReadOnlyList<Kpc.JudgeLine> judgeLines)
    {
        ArgumentNullException.ThrowIfNull(judgeLines);
        var result = new List<Ir.JudgeLine>(judgeLines.Count);
        foreach (var line in judgeLines)
            result.Add(line is null ? null! : ToIntermediate(line));
        return result;
    }

    internal static List<Kpc.JudgeLine> ToKpc(List<Ir.JudgeLine> judgeLines) =>
        MapList(judgeLines, ToKpc)!;

    internal static List<IrEvents.EventLayer> ToIntermediate(List<KpcEvents.EventLayer> layers) =>
        MapList(layers, ToIntermediate)!;

    internal static List<KpcEvents.EventLayer> ToKpc(List<IrEvents.EventLayer> layers) =>
        MapList(layers, ToKpc)!;

    internal static List<IrEvents.Event<T>>? ToIntermediate<T>(
        List<KpcEvents.Event<T>>? events
    )
        where T : notnull => MapList(events, ToIntermediate)!;

    internal static List<KpcEvents.Event<T>>? ToKpc<T>(List<IrEvents.Event<T>>? events)
        where T : notnull => MapList(events, ToKpc)!;

    internal static IrEvents.EventLayer ToIntermediate(KpcEvents.EventLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        return new IrEvents.EventLayer
        {
            MoveXEvents = ToIntermediate(layer.MoveXEvents),
            MoveYEvents = ToIntermediate(layer.MoveYEvents),
            RotateEvents = ToIntermediate(layer.RotateEvents),
            AlphaEvents = ToIntermediate(layer.AlphaEvents),
            SpeedEvents = ToIntermediate(layer.SpeedEvents),
        };
    }

    internal static KpcEvents.EventLayer ToKpc(IrEvents.EventLayer layer)
    {
        return new KpcEvents.EventLayer
        {
            MoveXEvents = ToKpc(layer.MoveXEvents),
            MoveYEvents = ToKpc(layer.MoveYEvents),
            RotateEvents = ToKpc(layer.RotateEvents),
            AlphaEvents = ToKpc(layer.AlphaEvents),
            SpeedEvents = ToKpc(layer.SpeedEvents),
        };
    }

    internal static IrEvents.Event<T> ToIntermediate<T>(KpcEvents.Event<T> evt)
        where T : notnull
    {
        return new IrEvents.Event<T>
        {
            IsBezier = evt.IsBezier,
            BezierPoints = (float[])evt.BezierPoints.Clone(),
            EasingLeft = evt.EasingLeft,
            EasingRight = evt.EasingRight,
            StartValue = CloneValue(evt.StartValue),
            EndValue = CloneValue(evt.EndValue),
            StartBeat = ToShared(evt.StartBeat),
            EndBeat = ToShared(evt.EndBeat),
            Font = evt.Font,
            Easing = ToIntermediate(evt.Easing),
            StartTime = evt.StartTime,
            EndTime = evt.EndTime,
            FloorPosition = evt.FloorPosition,
        };
    }

    internal static KpcEvents.Event<T> ToKpc<T>(IrEvents.Event<T> evt)
        where T : notnull
    {
        return new KpcEvents.Event<T>
        {
            IsBezier = evt.IsBezier,
            BezierPoints = (float[])evt.BezierPoints.Clone(),
            EasingLeft = evt.EasingLeft,
            EasingRight = evt.EasingRight,
            StartValue = CloneValue(evt.StartValue),
            EndValue = CloneValue(evt.EndValue),
            StartBeat = ToCommon(evt.StartBeat),
            EndBeat = ToCommon(evt.EndBeat),
            Font = evt.Font,
            Easing = ToKpc(evt.Easing),
            StartTime = evt.StartTime,
            EndTime = evt.EndTime,
            FloorPosition = evt.FloorPosition,
        };
    }

    internal static Ir.JudgeLine ToIntermediate(Kpc.JudgeLine line)
    {
        var target = new Ir.JudgeLine
        {
            Name = line.Name,
            Texture = line.Texture,
            Anchor = (float[])line.Anchor.Clone(),
            Father = line.Father,
            IsCover = line.IsCover,
            ZOrder = line.ZOrder,
            AttachUi = line.AttachUi is null
                ? null
                : (Primitives.AttachUi)(int)line.AttachUi.Value,
            IsGif = line.IsGif,
            BpmFactor = line.BpmFactor,
            RotateWithFather = line.RotateWithFather,
        };

        target.EventLayers = MapList(line.EventLayers, ToIntermediate)!;
        target.Notes = MapList(line.Notes, ToIntermediate)!;
        target.Extended = ToIntermediate(line.Extended);
        target.PositionControls = MapList(line.PositionControls, ToIntermediate)!;
        target.AlphaControls = MapList(line.AlphaControls, ToIntermediate)!;
        target.SizeControls = MapList(line.SizeControls, ToIntermediate)!;
        target.SkewControls = MapList(line.SkewControls, ToIntermediate)!;
        target.YControls = MapList(line.YControls, ToIntermediate)!;
        return target;
    }

    internal static Kpc.JudgeLine ToKpc(Ir.JudgeLine line)
    {
        var target = new Kpc.JudgeLine
        {
            Name = line.Name,
            Texture = line.Texture,
            Anchor = (float[])line.Anchor.Clone(),
            Father = line.Father,
            IsCover = line.IsCover,
            ZOrder = line.ZOrder,
            AttachUi = line.AttachUi is null ? null : (KpcCommon.AttachUi)(int)line.AttachUi.Value,
            IsGif = line.IsGif,
            BpmFactor = line.BpmFactor,
            RotateWithFather = line.RotateWithFather,
        };

        target.EventLayers = MapList(line.EventLayers, ToKpc)!;
        target.Notes = MapList(line.Notes, ToKpc)!;
        target.Extended = ToKpc(line.Extended);
        target.PositionControls = MapList(line.PositionControls, ToKpc)!;
        target.AlphaControls = MapList(line.AlphaControls, ToKpc)!;
        target.SizeControls = MapList(line.SizeControls, ToKpc)!;
        target.SkewControls = MapList(line.SkewControls, ToKpc)!;
        target.YControls = MapList(line.YControls, ToKpc)!;
        return target;
    }

    internal static Ir.Note ToIntermediate(Kpc.Note note)
    {
        return new Ir.Note
        {
            Above = note.Above,
            Alpha = note.Alpha,
            StartBeat = ToShared(note.StartBeat),
            StartTime = note.StartTime,
            EndBeat = ToShared(note.EndBeat),
            EndTime = note.EndTime,
            IsFake = note.IsFake,
            PositionX = note.PositionX,
            WidthRatio = note.WidthRatio,
            JudgeArea = note.JudgeArea,
            SpeedMultiplier = note.SpeedMultiplier,
            Type = (Primitives.NoteType)(int)note.Type,
            VisibleTime = note.VisibleTime,
            YOffset = note.YOffset,
            Tint = (byte[])note.Tint.Clone(),
            HitFxColor = note.HitFxColor is null ? null : (byte[])note.HitFxColor.Clone(),
            HitSound = note.HitSound,
            FloorPosition = note.FloorPosition,
            EndFloorPosition = note.EndFloorPosition,
        };
    }

    internal static Kpc.Note ToKpc(Ir.Note note)
    {
        return new Kpc.Note
        {
            Above = note.Above,
            Alpha = note.Alpha,
            StartBeat = ToCommon(note.StartBeat),
            StartTime = note.StartTime,
            EndBeat = ToCommon(note.EndBeat),
            EndTime = note.EndTime,
            IsFake = note.IsFake,
            PositionX = note.PositionX,
            WidthRatio = note.WidthRatio,
            JudgeArea = note.JudgeArea,
            SpeedMultiplier = note.SpeedMultiplier,
            Type = (KpcCommon.NoteType)(int)note.Type,
            VisibleTime = note.VisibleTime,
            YOffset = note.YOffset,
            Tint = (byte[])note.Tint.Clone(),
            HitFxColor = note.HitFxColor is null ? null : (byte[])note.HitFxColor.Clone(),
            HitSound = note.HitSound,
            FloorPosition = note.FloorPosition,
            EndFloorPosition = note.EndFloorPosition,
        };
    }

    internal static Ir.Meta ToIntermediate(Kpc.Meta meta)
    {
        return new Ir.Meta
        {
            Background = meta.Background,
            Author = meta.Author,
            Composer = meta.Composer,
            Artist = meta.Artist,
            Level = meta.Level,
            Name = meta.Name,
            Offset = meta.Offset,
            Song = meta.Song,
        };
    }

    internal static Kpc.Meta ToKpc(Ir.Meta meta)
    {
        return new Kpc.Meta
        {
            Background = meta.Background,
            Author = meta.Author,
            Composer = meta.Composer,
            Artist = meta.Artist,
            Level = meta.Level,
            Name = meta.Name,
            Offset = meta.Offset,
            Song = meta.Song,
        };
    }

    internal static Ir.BpmItem ToIntermediate(Kpc.BpmItem bpm)
    {
        return new Ir.BpmItem { Bpm = bpm.Bpm, StartBeat = ToShared(bpm.StartBeat) };
    }

    internal static Kpc.BpmItem ToKpc(Ir.BpmItem bpm)
    {
        return new Kpc.BpmItem { Bpm = bpm.Bpm, StartBeat = ToCommon(bpm.StartBeat) };
    }

    internal static IrEvents.ExtendLayer ToIntermediate(KpcEvents.ExtendLayer layer)
    {
        return new IrEvents.ExtendLayer
        {
            ColorEvents = ToIntermediate(layer.ColorEvents),
            ScaleXEvents = ToIntermediate(layer.ScaleXEvents),
            ScaleYEvents = ToIntermediate(layer.ScaleYEvents),
            TextEvents = ToIntermediate(layer.TextEvents),
            PaintEvents = ToIntermediate(layer.PaintEvents),
            GifEvents = ToIntermediate(layer.GifEvents),
            InclineEvents = ToIntermediate(layer.InclineEvents),
        };
    }

    internal static KpcEvents.ExtendLayer ToKpc(IrEvents.ExtendLayer layer)
    {
        return new KpcEvents.ExtendLayer
        {
            ColorEvents = ToKpc(layer.ColorEvents),
            ScaleXEvents = ToKpc(layer.ScaleXEvents),
            ScaleYEvents = ToKpc(layer.ScaleYEvents),
            TextEvents = ToKpc(layer.TextEvents),
            PaintEvents = ToKpc(layer.PaintEvents),
            GifEvents = ToKpc(layer.GifEvents),
            InclineEvents = ToKpc(layer.InclineEvents),
        };
    }

    internal static Ir.Easing ToIntermediate(Kpc.Easing easing) => new((int)easing);

    internal static Kpc.Easing ToKpc(Ir.Easing easing) => new((int)easing);

    internal static Primitives.Beat ToShared(KpcCommon.Beat beat) => new((int[])beat);

    internal static KpcCommon.Beat ToCommon(Primitives.Beat beat) => new((int[])beat);

    private static IrControls.XControl ToIntermediate(KpcControls.XControl control) =>
        new()
        {
            Easing = ToIntermediate(control.Easing),
            X = control.X,
            Pos = control.Pos,
        };

    private static KpcControls.XControl ToKpc(IrControls.XControl control) =>
        new()
        {
            Easing = ToKpc(control.Easing),
            X = control.X,
            Pos = control.Pos,
        };

    private static IrControls.YControl ToIntermediate(KpcControls.YControl control) =>
        new()
        {
            Easing = ToIntermediate(control.Easing),
            X = control.X,
            Y = control.Y,
        };

    private static KpcControls.YControl ToKpc(IrControls.YControl control) =>
        new()
        {
            Easing = ToKpc(control.Easing),
            X = control.X,
            Y = control.Y,
        };

    private static IrControls.AlphaControl ToIntermediate(KpcControls.AlphaControl control) =>
        new()
        {
            Easing = ToIntermediate(control.Easing),
            X = control.X,
            Alpha = control.Alpha,
        };

    private static KpcControls.AlphaControl ToKpc(IrControls.AlphaControl control) =>
        new()
        {
            Easing = ToKpc(control.Easing),
            X = control.X,
            Alpha = control.Alpha,
        };

    private static IrControls.SizeControl ToIntermediate(KpcControls.SizeControl control) =>
        new()
        {
            Easing = ToIntermediate(control.Easing),
            X = control.X,
            Size = control.Size,
        };

    private static KpcControls.SizeControl ToKpc(IrControls.SizeControl control) =>
        new()
        {
            Easing = ToKpc(control.Easing),
            X = control.X,
            Size = control.Size,
        };

    private static IrControls.SkewControl ToIntermediate(KpcControls.SkewControl control) =>
        new()
        {
            Easing = ToIntermediate(control.Easing),
            X = control.X,
            Skew = control.Skew,
        };

    private static KpcControls.SkewControl ToKpc(IrControls.SkewControl control) =>
        new()
        {
            Easing = ToKpc(control.Easing),
            X = control.X,
            Skew = control.Skew,
        };

    private static List<TTarget>? MapList<TSource, TTarget>(
        List<TSource>? source,
        Func<TSource, TTarget> map
    )
    {
        if (source is null)
            return null;
        var result = new List<TTarget>(source.Count);
        foreach (var item in source)
            result.Add(ReferenceEquals(item, null) ? default! : map(item));
        return result;
    }

    private static T CloneValue<T>(T value) => value is byte[] bytes ? (T)(object)(byte[])bytes.Clone() : value;
}
