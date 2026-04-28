using KaedePhi.Tool.Converter.RePhiEdit.Model;

namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

public class EventLayer
{
    #region RpeToKpc

    public static Kpc.EventLayer ConvertEventLayer(Rpe.EventLayer src)
    {
        var result = new Kpc.EventLayer();
        if (src.MoveXEvents != null)
            result.MoveXEvents =
                src.MoveXEvents.ConvertAll(e => Event.ConvertFloatToDoubleEvent(e, Transform.TransformToKpcX));
        if (src.MoveYEvents != null)
            result.MoveYEvents =
                src.MoveYEvents.ConvertAll(e => Event.ConvertFloatToDoubleEvent(e, Transform.TransformToKpcY));
        if (src.RotateEvents != null)
            result.RotateEvents =
                src.RotateEvents.ConvertAll(e => Event.ConvertFloatToDoubleEvent(e, Transform.TransformToKpcAngle));
        if (src.AlphaEvents != null) result.AlphaEvents = src.AlphaEvents.ConvertAll(Event.ConvertIntEvent);
        if (src.SpeedEvents != null) result.SpeedEvents = src.SpeedEvents.ConvertAll(Event.ConvertFloatEvent);
        return result;
    }

    public static Kpc.ExtendLayer? ConvertExtendLayer(Rpe.ExtendLayer? src)
    {
        if (src == null) return null;
        var result = new Kpc.ExtendLayer();
        if (src.ColorEvents != null) result.ColorEvents = src.ColorEvents.ConvertAll(Event.ConvertByteArrayEvent);
        if (src.ScaleXEvents != null) result.ScaleXEvents = src.ScaleXEvents.ConvertAll(Event.ConvertFloatEvent);
        if (src.ScaleYEvents != null) result.ScaleYEvents = src.ScaleYEvents.ConvertAll(Event.ConvertFloatEvent);
        if (src.TextEvents != null) result.TextEvents = src.TextEvents.ConvertAll(Event.ConvertStringEvent);
        if (src.PaintEvents != null) result.PaintEvents = src.PaintEvents.ConvertAll(Event.ConvertFloatEvent);
        if (src.GifEvents != null) result.GifEvents = src.GifEvents.ConvertAll(Event.ConvertFloatEvent);
        return result;
    }

    #endregion

    #region KpcToRpe

    public static Rpe.EventLayer ConvertEventLayer(Kpc.EventLayer src, ConvertOption.CuttingOptions options)
    {
        var rpe = new Rpe.EventLayer();
        if (src.MoveXEvents is not null)
        {
            rpe.MoveXEvents = [];
            foreach (var e in src.MoveXEvents)
                rpe.MoveXEvents.AddRange(Event.ConvertDoubleEventExpanding(e, options, Transform.TransformToRpeX));
        }

        if (src.MoveYEvents is not null)
        {
            rpe.MoveYEvents = [];
            foreach (var e in src.MoveYEvents)
                rpe.MoveYEvents.AddRange(Event.ConvertDoubleEventExpanding(e, options, Transform.TransformToRpeY));
        }

        if (src.RotateEvents is not null)
        {
            rpe.RotateEvents = [];
            foreach (var e in src.RotateEvents)
                rpe.RotateEvents.AddRange(Event.ConvertDoubleEventExpanding(e, options, Transform.TransformToRpeAngle));
        }

        if (src.AlphaEvents is not null)
        {
            rpe.AlphaEvents = [];
            foreach (var e in src.AlphaEvents) rpe.AlphaEvents.AddRange(Event.ConvertIntEventExpanding(e, options));
        }

        if (src.SpeedEvents is not null)
        {
            rpe.SpeedEvents = [];
            foreach (var e in src.SpeedEvents) rpe.SpeedEvents.AddRange(Event.ConvertFloatEventExpanding(e, options));
        }

        return rpe;
    }

    public static Rpe.ExtendLayer? ConvertExtendLayer(Kpc.ExtendLayer? src, ConvertOption.CuttingOptions options)
    {
        if (src is null) return null;
        var rpe = new Rpe.ExtendLayer();
        if (src.ColorEvents != null) rpe.ColorEvents = src.ColorEvents.ConvertAll(Event.ConvertByteArrayEvent);
        if (src.ScaleXEvents != null)
        {
            rpe.ScaleXEvents = [];
            foreach (var e in src.ScaleXEvents) rpe.ScaleXEvents.AddRange(Event.ConvertFloatEventExpanding(e, options));
        }

        if (src.ScaleYEvents != null)
        {
            rpe.ScaleYEvents = [];
            foreach (var e in src.ScaleYEvents) rpe.ScaleYEvents.AddRange(Event.ConvertFloatEventExpanding(e, options));
        }

        if (src.TextEvents != null) rpe.TextEvents = src.TextEvents.ConvertAll(Event.ConvertStringEvent);
        if (src.PaintEvents != null)
        {
            rpe.PaintEvents = [];
            foreach (var e in src.PaintEvents) rpe.PaintEvents.AddRange(Event.ConvertFloatEventExpanding(e, options));
        }

        if (src.GifEvents != null)
        {
            rpe.GifEvents = [];
            foreach (var e in src.GifEvents) rpe.GifEvents.AddRange(Event.ConvertFloatEventExpanding(e, options));
        }

        return rpe;
    }

    #endregion
}