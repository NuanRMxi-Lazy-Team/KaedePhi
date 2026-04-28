using KaedePhi.Core.Common;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using KaedePhi.Tool.KaedePhi.Events;

namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

public static class Event
{
    #region RpeToKpc

    public static Kpc.Event<T> ConvertEvent<T>(Rpe.Event<T> src, Func<T, T>? valueCopier = null,
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
            Easing = Easing.ConvertEasing(src.Easing),
            StartValue = valueTransformer(valueCopier(src.StartValue)),
            EndValue = valueTransformer(valueCopier(src.EndValue)),
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
            Font = src.Font
        };
    }

    public static Kpc.Event<double> ConvertFloatToDoubleEvent(Rpe.Event<float> src,
        Func<float, double> valueTransformer)
    {
        return new Kpc.Event<double>
        {
            IsBezier = src.IsBezier,
            BezierPoints = src.BezierPoints.ToArray(),
            EasingLeft = src.EasingLeft,
            EasingRight = src.EasingRight,
            Easing = Easing.ConvertEasing(src.Easing),
            StartValue = valueTransformer(src.StartValue),
            EndValue = valueTransformer(src.EndValue),
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
            Font = src.Font
        };
    }

    public static Kpc.Event<float> ConvertFloatEvent(Rpe.Event<float> src)
    {
        return ConvertEvent(src);
    }

    public static Kpc.Event<int> ConvertIntEvent(Rpe.Event<int> src)
    {
        return ConvertEvent(src);
    }

    public static Kpc.Event<string> ConvertStringEvent(Rpe.Event<string> src)
    {
        return ConvertEvent(src);
    }

    public static Kpc.Event<byte[]> ConvertByteArrayEvent(Rpe.Event<byte[]> src)
    {
        return ConvertEvent(src, v => v.ToArray());
    }

    #endregion

    #region KpcToRpe

    public static Rpe.Event<T> ConvertEvent<T>(Kpc.Event<T> src,
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
            Easing = Easing.ConvertEasing(src.Easing, src.IsBezier),
            StartValue = valueTransformer(valueCopier(src.StartValue)),
            EndValue = valueTransformer(valueCopier(src.EndValue)),
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
            Font = src.Font
        };
    }

    public static List<Rpe.Event<float>> ConvertFloatEventExpanding(Kpc.Event<float> src,
        ConvertOption.CuttingOptions options)
    {
        try
        {
            return [ConvertEvent(src)];
        }
        catch (Easing.EasingNotSupportedException)
        {
            return KpcEventTools
                .CutEventToLiner(src, 1d / options.UnsupportedEasingPrecision)
                .ConvertAll(e => new Rpe.Event<float>
                {
                    StartBeat = new Beat((int[])e.StartBeat),
                    EndBeat = new Beat((int[])e.EndBeat),
                    StartValue = e.StartValue,
                    EndValue = e.EndValue,
                    Easing = new Rpe.Easing(1)
                });
        }
    }

    public static List<Rpe.Event<float>> ConvertDoubleEventExpanding(
        Kpc.Event<double> src, ConvertOption.CuttingOptions options, Func<double, double>? valueTransformer = null)
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
                    Easing = Easing.ConvertEasing(src.Easing, src.IsBezier),
                    StartValue = (float)valueTransformer(src.StartValue),
                    EndValue = (float)valueTransformer(src.EndValue),
                    StartBeat = new Beat((int[])src.StartBeat),
                    EndBeat = new Beat((int[])src.EndBeat),
                    Font = src.Font
                }
            ];
        }
        catch (Easing.EasingNotSupportedException)
        {
            return KpcEventTools
                .CutEventToLiner(src, 1d / options.UnsupportedEasingPrecision)
                .ConvertAll(e => new Rpe.Event<float>
                {
                    StartBeat = new Beat((int[])e.StartBeat),
                    EndBeat = new Beat((int[])e.EndBeat),
                    StartValue = (float)valueTransformer(e.StartValue),
                    EndValue = (float)valueTransformer(e.EndValue),
                    Easing = new Rpe.Easing(1)
                });
        }
    }

    public static List<Rpe.Event<int>> ConvertIntEventExpanding(Kpc.Event<int> src,
        ConvertOption.CuttingOptions options)
    {
        try
        {
            return [ConvertEvent(src)];
        }
        catch (Easing.EasingNotSupportedException)
        {
            return KpcEventTools
                .CutEventToLiner(src, 1d / options.UnsupportedEasingPrecision)
                .ConvertAll(e => new Rpe.Event<int>
                {
                    StartBeat = new Beat((int[])e.StartBeat),
                    EndBeat = new Beat((int[])e.EndBeat),
                    StartValue = e.StartValue,
                    EndValue = e.EndValue,
                    Easing = new Rpe.Easing(1)
                });
        }
    }
    
    public static Rpe.Event<string> ConvertStringEvent(Kpc.Event<string> src) => ConvertEvent(src);

    public static Rpe.Event<byte[]> ConvertByteArrayEvent(Kpc.Event<byte[]> src) =>
        ConvertEvent(src, v => v.ToArray());

    #endregion
}