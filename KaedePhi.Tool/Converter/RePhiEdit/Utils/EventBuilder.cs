using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using KaedePhi.Tool.Event.Intermediate;

namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

public static class EventBuilder
{
    private static readonly EventCutter<float> FloatCutter = new();
    private static readonly EventCutter<double> DoubleCutter = new();
    private static readonly EventCutter<int> IntCutter = new();

    public static IrEvents.Event<T> ConvertEvent<T>(
        RpeEvents.Event<T> src,
        Func<T, T>? valueCopier = null,
        Func<T, T>? valueTransformer = null
    )
        where T : notnull
    {
        valueCopier ??= v => v;
        valueTransformer ??= v => v;
        return new IrEvents.Event<T>
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
        IrEvents.Event<T> src,
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

    public static IrEvents.Event<double> ConvertFloatToDoubleEvent(
        RpeEvents.Event<float> src,
        Func<float, double> valueTransformer
    )
    {
        return new IrEvents.Event<double>
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

    public static IrEvents.Event<float> ConvertFloatEvent(RpeEvents.Event<float> src)
    {
        return ConvertEvent(src);
    }

    public static IrEvents.Event<int> ConvertIntEvent(RpeEvents.Event<int> src)
    {
        return ConvertEvent(src);
    }

    public static IrEvents.Event<string> ConvertStringEvent(RpeEvents.Event<string> src)
    {
        return ConvertEvent(src);
    }

    public static RpeEvents.Event<string> ConvertStringEvent(IrEvents.Event<string> src)
    {
        return ConvertEvent(src);
    }

    public static IrEvents.Event<byte[]> ConvertByteArrayEvent(RpeEvents.Event<byte[]> src)
    {
        return ConvertEvent(src, v => [.. v]);
    }

    public static RpeEvents.Event<byte[]> ConvertByteArrayEvent(IrEvents.Event<byte[]> src)
    {
        return ConvertEvent(src, v => [.. v]);
    }

    public static List<RpeEvents.Event<float>> ConvertFloatEventExpanding(
        IrEvents.Event<float> src,
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
        IrEvents.Event<double> src,
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
                .CutEventToLinear(src, 1d / options.UnsupportedEasingPrecision)
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
        IrEvents.Event<int> src,
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
