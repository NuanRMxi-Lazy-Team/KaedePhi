using KaedePhi.Tool.Converter.RePhiEdit.Model;

namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

/// <summary>
/// RPE 与 KPC 事件层之间的双向转换工具。
/// </summary>
public static class EventLayerBuilder
{
    /// <summary>
    /// 将 RPE 事件层转换为 KPC 事件层。
    /// </summary>
    /// <param name="src">RPE 事件层。</param>
    /// <returns>KPC 事件层。</returns>
    public static KpcEvents.EventLayer ConvertEventLayer(RpeEvents.EventLayer src)
    {
        var result = new KpcEvents.EventLayer();
        if (src.MoveXEvents is not null)
            result.MoveXEvents = src.MoveXEvents.ConvertAll(e =>
                EventBuilder.ConvertFloatToDoubleEvent(e, Transform.TransformToKpcX)
            );
        if (src.MoveYEvents is not null)
            result.MoveYEvents = src.MoveYEvents.ConvertAll(e =>
                EventBuilder.ConvertFloatToDoubleEvent(e, Transform.TransformToKpcY)
            );
        if (src.RotateEvents is not null)
            result.RotateEvents = src.RotateEvents.ConvertAll(e =>
                EventBuilder.ConvertFloatToDoubleEvent(e, Transform.TransformToKpcAngle)
            );
        if (src.AlphaEvents is not null)
            result.AlphaEvents = src.AlphaEvents.ConvertAll(EventBuilder.ConvertIntEvent);
        if (src.SpeedEvents is not null)
            result.SpeedEvents = src.SpeedEvents.ConvertAll(EventBuilder.ConvertFloatEvent);
        return result;
    }

    /// <summary>
    /// 将 KPC 事件层转换为 RPE 事件层，不支持的缓动将被切段降级。
    /// </summary>
    /// <param name="src">KPC 事件层。</param>
    /// <param name="options">切割选项。</param>
    /// <returns>RPE 事件层。</returns>
    public static RpeEvents.EventLayer ConvertEventLayer(
        KpcEvents.EventLayer src,
        ConvertOption.CuttingOptions options
    )
    {
        var rpe = new RpeEvents.EventLayer();
        if (src.MoveXEvents is not null)
        {
            rpe.MoveXEvents = [];
            foreach (var e in src.MoveXEvents)
                rpe.MoveXEvents.AddRange(
                    EventBuilder.ConvertDoubleEventExpanding(e, options, Transform.TransformToRpeX)
                );
        }

        if (src.MoveYEvents is not null)
        {
            rpe.MoveYEvents = [];
            foreach (var e in src.MoveYEvents)
                rpe.MoveYEvents.AddRange(
                    EventBuilder.ConvertDoubleEventExpanding(e, options, Transform.TransformToRpeY)
                );
        }

        if (src.RotateEvents is not null)
        {
            rpe.RotateEvents = [];
            foreach (var e in src.RotateEvents)
                rpe.RotateEvents.AddRange(
                    EventBuilder.ConvertDoubleEventExpanding(
                        e,
                        options,
                        Transform.TransformToRpeAngle
                    )
                );
        }

        if (src.AlphaEvents is not null)
        {
            rpe.AlphaEvents = [];
            foreach (var e in src.AlphaEvents)
                rpe.AlphaEvents.AddRange(EventBuilder.ConvertIntEventExpanding(e, options));
        }

        if (src.SpeedEvents is not null)
        {
            rpe.SpeedEvents = [];
            foreach (var e in src.SpeedEvents)
                rpe.SpeedEvents.AddRange(EventBuilder.ConvertFloatEventExpanding(e, options));
        }

        return rpe;
    }

    /// <summary>
    /// 将 RPE 扩展层转换为 KPC 扩展层。
    /// </summary>
    /// <param name="src">RPE 扩展层，可为 null。</param>
    /// <returns>KPC 扩展层，输入为 null 时返回 null。</returns>
    public static KpcEvents.ExtendLayer? ConvertExtendLayer(RpeEvents.ExtendLayer? src)
    {
        if (src == null)
            return null;
        var result = new KpcEvents.ExtendLayer();
        if (src.ColorEvents is not null)
            result.ColorEvents = src.ColorEvents.ConvertAll(EventBuilder.ConvertByteArrayEvent);
        if (src.ScaleXEvents is not null)
            result.ScaleXEvents = src.ScaleXEvents.ConvertAll(EventBuilder.ConvertFloatEvent);
        if (src.ScaleYEvents is not null)
            result.ScaleYEvents = src.ScaleYEvents.ConvertAll(EventBuilder.ConvertFloatEvent);
        if (src.TextEvents is not null)
            result.TextEvents = src.TextEvents.ConvertAll(EventBuilder.ConvertStringEvent);
        if (src.PaintEvents is not null)
            result.PaintEvents = src.PaintEvents.ConvertAll(EventBuilder.ConvertFloatEvent);
        if (src.GifEvents is not null)
            result.GifEvents = src.GifEvents.ConvertAll(EventBuilder.ConvertFloatEvent);
        if (src.InclineEvents is not null)
            result.InclineEvents = src.InclineEvents.ConvertAll(EventBuilder.ConvertFloatEvent);
        return result;
    }

    /// <summary>
    /// 将 KPC 扩展层转换为 RPE 扩展层，不支持的缓动将被切段降级。
    /// </summary>
    /// <param name="src">KPC 扩展层，可为 null。</param>
    /// <param name="options">切割选项。</param>
    /// <returns>RPE 扩展层，输入为 null 时返回 null。</returns>
    public static RpeEvents.ExtendLayer? ConvertExtendLayer(
        KpcEvents.ExtendLayer? src,
        ConvertOption.CuttingOptions options
    )
    {
        if (src is null)
            return null;
        var rpe = new RpeEvents.ExtendLayer();

        if (src.ColorEvents is not null)
            rpe.ColorEvents = src.ColorEvents.ConvertAll(EventBuilder.ConvertByteArrayEvent);
        if (src.ScaleXEvents is not null)
        {
            rpe.ScaleXEvents = [];
            foreach (var e in src.ScaleXEvents)
                rpe.ScaleXEvents.AddRange(EventBuilder.ConvertFloatEventExpanding(e, options));
        }

        if (src.ScaleYEvents is not null)
        {
            rpe.ScaleYEvents = [];
            foreach (var e in src.ScaleYEvents)
                rpe.ScaleYEvents.AddRange(EventBuilder.ConvertFloatEventExpanding(e, options));
        }

        if (src.TextEvents is not null)
            rpe.TextEvents = src.TextEvents.ConvertAll(EventBuilder.ConvertStringEvent);
        if (src.PaintEvents is not null)
        {
            rpe.PaintEvents = [];
            foreach (var e in src.PaintEvents)
                rpe.PaintEvents.AddRange(EventBuilder.ConvertFloatEventExpanding(e, options));
        }

        if (src.GifEvents is not null)
        {
            rpe.GifEvents = [];
            foreach (var e in src.GifEvents)
                rpe.GifEvents.AddRange(EventBuilder.ConvertFloatEventExpanding(e, options));
        }

        if (src.InclineEvents is not null)
        {
            rpe.InclineEvents = [];
            foreach (var e in src.InclineEvents)
                rpe.InclineEvents.AddRange(EventBuilder.ConvertFloatEventExpanding(e, options));
        }

        return rpe;
    }
}
