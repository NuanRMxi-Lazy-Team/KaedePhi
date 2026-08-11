using KaedePhi.Core.Common;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using KaedePhi.Tool.Event.KaedePhi;

namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

public static class EventBuilder
{
    private static readonly EventCutter<float> FloatCutter = new();
    private static readonly EventCutter<double> DoubleCutter = new();
    private static readonly EventCutter<int> IntCutter = new();

    public static KpcEvents.Event<T> ConvertEvent<T>(
        RpeEvents.Event<T> src,
        Func<T, T>? valueCopier = null,
        Func<T, T>? valueTransformer = null
    )
        where T : notnull
    {
        valueCopier ??= v => v;
        valueTransformer ??= v => v;
        return new KpcEvents.Event<T>
        {
            IsBezier = src.IsBezier,
            BezierPoints = [.. src.BezierPoints],
            EasingLeft = src.EasingLeft,
            EasingRight = src.EasingRight,
            Easing = EasingConverter.ConvertEasing(src.Easing),
            StartValue = valueTransformer(valueCopier(src.StartValue)),
            EndValue = valueTransformer(valueCopier(src.EndValue)),
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
            Font = src.Font,
        };
    }

    public static RpeEvents.Event<T> ConvertEvent<T>(
        KpcEvents.Event<T> src,
        Func<T, T>? valueCopier = null,
        Func<T, T>? valueTransformer = null
    )
        where T : notnull
    {
        valueCopier ??= v => v;
        valueTransformer ??= v => v;
        return new RpeEvents.Event<T>
        {
            IsBezier = src.IsBezier,
            BezierPoints = [.. src.BezierPoints],
            EasingLeft = src.EasingLeft,
            EasingRight = src.EasingRight,
            Easing = EasingConverter.ConvertEasing(src.Easing, src.IsBezier),
            StartValue = valueTransformer(valueCopier(src.StartValue)),
            EndValue = valueTransformer(valueCopier(src.EndValue)),
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
            Font = src.Font,
        };
    }

    public static KpcEvents.Event<double> ConvertFloatToDoubleEvent(
        RpeEvents.Event<float> src,
        Func<float, double> valueTransformer
    )
    {
        return new KpcEvents.Event<double>
        {
            IsBezier = src.IsBezier,
            BezierPoints = [.. src.BezierPoints],
            EasingLeft = src.EasingLeft,
            EasingRight = src.EasingRight,
            Easing = EasingConverter.ConvertEasing(src.Easing),
            StartValue = valueTransformer(src.StartValue),
            EndValue = valueTransformer(src.EndValue),
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
            Font = src.Font,
        };
    }

    public static KpcEvents.Event<float> ConvertFloatEvent(RpeEvents.Event<float> src)
    {
        return ConvertEvent(src);
    }

    public static KpcEvents.Event<int> ConvertIntEvent(RpeEvents.Event<int> src)
    {
        return ConvertEvent(src);
    }

    public static KpcEvents.Event<string> ConvertStringEvent(RpeEvents.Event<string> src)
    {
        return ConvertEvent(src);
    }

    public static RpeEvents.Event<string> ConvertStringEvent(KpcEvents.Event<string> src)
    {
        return ConvertEvent(src);
    }

    public static KpcEvents.Event<byte[]> ConvertByteArrayEvent(RpeEvents.Event<byte[]> src)
    {
        return ConvertEvent(src, v => [.. v]);
    }

    public static RpeEvents.Event<byte[]> ConvertByteArrayEvent(KpcEvents.Event<byte[]> src)
    {
        return ConvertEvent(src, v => [.. v]);
    }

    public static List<RpeEvents.Event<float>> ConvertFloatEventExpanding(
        KpcEvents.Event<float> src,
        ConvertOption.CuttingOptions options
    )
    {
        try
        {
            return [ConvertEvent(src)];
        }
        catch (PhiEdit.Utils.EasingConverter.EasingNotSupportedException)
        {
            return FloatCutter
                .CutEventToLinear(src, 1d / options.UnsupportedEasingPrecision)
                .ConvertAll(e => new RpeEvents.Event<float>
                {
                    StartBeat = new Beat((int[])e.StartBeat),
                    EndBeat = new Beat((int[])e.EndBeat),
                    StartValue = e.StartValue,
                    EndValue = e.EndValue,
                    Easing = new Rpe.Easing(1),
                });
        }
    }

    public static List<RpeEvents.Event<float>> ConvertDoubleEventExpanding(
        KpcEvents.Event<double> src,
        ConvertOption.CuttingOptions options,
        Func<double, double>? valueTransformer = null
    )
    {
        valueTransformer ??= v => v;
        try
        {
            return
            [
                new RpeEvents.Event<float>
                {
                    IsBezier = src.IsBezier,
                    BezierPoints = [.. src.BezierPoints],
                    EasingLeft = src.EasingLeft,
                    EasingRight = src.EasingRight,
                    Easing = EasingConverter.ConvertEasing(src.Easing, src.IsBezier),
                    StartValue = (float)valueTransformer(src.StartValue),
                    EndValue = (float)valueTransformer(src.EndValue),
                    StartBeat = new Beat((int[])src.StartBeat),
                    EndBeat = new Beat((int[])src.EndBeat),
                    Font = src.Font,
                },
            ];
        }
        catch (PhiEdit.Utils.EasingConverter.EasingNotSupportedException)
        {
            return DoubleCutter
                .CutEventToLinear(src, new Beat(1d))
                .ConvertAll(e => new RpeEvents.Event<float>
                {
                    StartBeat = new Beat((int[])e.StartBeat),
                    EndBeat = new Beat((int[])e.EndBeat),
                    StartValue = (float)valueTransformer(e.StartValue),
                    EndValue = (float)valueTransformer(e.EndValue),
                    Easing = new Rpe.Easing(1),
                });
        }
    }

    public static List<RpeEvents.Event<int>> ConvertIntEventExpanding(
        KpcEvents.Event<int> src,
        ConvertOption.CuttingOptions options
    )
    {
        try
        {
            return [ConvertEvent(src)];
        }
        catch (PhiEdit.Utils.EasingConverter.EasingNotSupportedException)
        {
            return IntCutter
                .CutEventToLinear(src, 1d / options.UnsupportedEasingPrecision)
                .ConvertAll(e => new RpeEvents.Event<int>
                {
                    StartBeat = new Beat((int[])e.StartBeat),
                    EndBeat = new Beat((int[])e.EndBeat),
                    StartValue = e.StartValue,
                    EndValue = e.EndValue,
                    Easing = new Rpe.Easing(1),
                });
        }
    }
}
