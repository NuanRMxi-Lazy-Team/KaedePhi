using KaedePhi.Core.Common;
using KaedePhi.Core.PhiFans;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiFans.Model;
using KaedePhi.Tool.Layer.KaedePhi;

namespace KaedePhi.Tool.Converter.PhiFans.Utils;

internal static class JudgeLineBuilder
{
    // 此值为粗略估算，并非严谨计算后得出的内容，请知悉。
    private const float SpeedRatio = 7.15f;

    internal static Kpc.JudgeLine ConvertToKpc(Line src)
    {
        var line = new Kpc.JudgeLine { Notes = src.NoteList.ConvertAll(NoteBuilder.ConvertToKpc) };

        var layer = new KpcEvents.EventLayer();
        var props = src.Props;

        if (props.Speed.Count > 0)
            layer.SpeedEvents = EventBuilder.ConvertPhiFansEventsToFloat(props.Speed, v => v * SpeedRatio);

        if (props.PositionX.Count > 0)
            layer.MoveXEvents = EventBuilder.ConvertPhiFansEventsToDouble(
                props.PositionX,
                v => v / Chart.CoordinateSystem.MaxX
            );

        if (props.PositionY.Count > 0)
            layer.MoveYEvents = EventBuilder.ConvertPhiFansEventsToDouble(
                props.PositionY,
                v => v / Chart.CoordinateSystem.MaxY
            );

        if (props.Rotate.Count > 0)
            layer.RotateEvents = EventBuilder.ConvertPhiFansEventsToDouble(
                props.Rotate,
                v => CoordinateGeometry.ToKpcAngle(v, CoordinateProfile.PhiFansProfile)
            );

        if (props.Alpha.Count > 0)
            layer.AlphaEvents = EventBuilder.ConvertPhiFansEventsToInt(props.Alpha, v => (int)v);

        line.EventLayers = [layer];
        return line;
    }

    internal static Line ConvertFromKpc(Kpc.JudgeLine src, KpcToPhiFansConvertOptions options)
    {
        var line = new Line { NoteList = src.Notes.ConvertAll(NoteBuilder.ConvertFromKpc) };
        var sourceLayers = src.EventLayers.ConvertAll(layer => layer.Clone());
        foreach (var sourceLayer in sourceLayers)
            sourceLayer.Sort();
        var layers = sourceLayers.ConvertAll(layer => layer.Clone());
        foreach (var mergeLayer in layers)
            EventBuilder.RemoveInstantEvents(mergeLayer);
        var layerProcessor = new LayerProcessor();
        KpcEvents.EventLayer layer;
        if (options.MultiLayerMerge.ClassicMode)
        {
            layer = layerProcessor.LayerMerge(layers, options.MultiLayerMerge.Precision);
            if (options.MultiLayerMerge.Compress)
                layerProcessor.LayerEventsCompress(layer, options.MultiLayerMerge.Tolerance);
        }
        else
        {
            layer = layerProcessor.LayerMergePlus(
                layers,
                options.MultiLayerMerge.Precision,
                options.MultiLayerMerge.Tolerance
            );
        }

        var cutLength = 1d / options.Cutting.UnsupportedEasingPrecision;
        var (alphaEvents, alphaStepBeats) = EventBuilder.ResolveChannelComposition(
            layer.AlphaEvents,
            sourceLayers,
            sourceLayer => sourceLayer.AlphaEvents,
            false,
            cutLength
        );
        var (moveXEvents, moveXStepBeats) = EventBuilder.ResolveChannelComposition(
            layer.MoveXEvents,
            sourceLayers,
            sourceLayer => sourceLayer.MoveXEvents,
            false,
            cutLength
        );
        var (moveYEvents, moveYStepBeats) = EventBuilder.ResolveChannelComposition(
            layer.MoveYEvents,
            sourceLayers,
            sourceLayer => sourceLayer.MoveYEvents,
            false,
            cutLength
        );
        var (rotateEvents, rotateStepBeats) = EventBuilder.ResolveChannelComposition(
            layer.RotateEvents,
            sourceLayers,
            sourceLayer => sourceLayer.RotateEvents,
            false,
            cutLength
        );
        var (speedEvents, speedStepBeats) = EventBuilder.ResolveChannelComposition(
            layer.SpeedEvents,
            sourceLayers,
            sourceLayer => sourceLayer.SpeedEvents,
            true,
            cutLength
        );
        layer.AlphaEvents = alphaEvents;
        layer.MoveXEvents = moveXEvents;
        layer.MoveYEvents = moveYEvents;
        layer.RotateEvents = rotateEvents;
        layer.SpeedEvents = speedEvents;

        if (layer.AlphaEvents is not null)
            foreach (var e in EventBuilder.ExpandUnsupportedEvents(layer.AlphaEvents, cutLength, false))
                EventBuilder.ConvertKpcEventToPhiFans(e, line.Props.Alpha, v => (float)v, EasingConverter.FromKpc);

        if (layer.MoveXEvents is not null)
            foreach (var e in EventBuilder.ExpandUnsupportedEvents(layer.MoveXEvents, cutLength, false))
                EventBuilder.ConvertKpcEventToPhiFans(
                    e,
                    line.Props.PositionX,
                    v => (float)(v * 100.0),
                    EasingConverter.FromKpc
                );

        if (layer.MoveYEvents is not null)
            foreach (var e in EventBuilder.ExpandUnsupportedEvents(layer.MoveYEvents, cutLength, false))
                EventBuilder.ConvertKpcEventToPhiFans(
                    e,
                    line.Props.PositionY,
                    v => (float)(v * 100.0),
                    EasingConverter.FromKpc
                );

        if (layer.RotateEvents is not null)
            foreach (var e in EventBuilder.ExpandUnsupportedEvents(layer.RotateEvents, cutLength, false))
                EventBuilder.ConvertKpcEventToPhiFans(
                    e,
                    line.Props.Rotate,
                    v =>
                        (float)
                            CoordinateGeometry.ToTargetAngle(
                                v,
                                CoordinateProfile.PhiFansProfile
                            ),
                    EasingConverter.FromKpc
                );

        if (layer.SpeedEvents is not null)
            foreach (var e in EventBuilder.ExpandUnsupportedEvents(layer.SpeedEvents, cutLength, true))
                EventBuilder.ConvertKpcEventToPhiFans(e, line.Props.Speed, v => v / SpeedRatio, _ => 0);

        var paddingPrecision = options.DiscontinuityBeatPrecision;
        EventBuilder.FixDiscontinuityGaps(line.Props.Alpha, paddingPrecision, alphaStepBeats);
        EventBuilder.FixDiscontinuityGaps(line.Props.PositionX, paddingPrecision, moveXStepBeats);
        EventBuilder.FixDiscontinuityGaps(line.Props.PositionY, paddingPrecision, moveYStepBeats);
        EventBuilder.FixDiscontinuityGaps(line.Props.Rotate, paddingPrecision, rotateStepBeats);
        EventBuilder.FixDiscontinuityGaps(line.Props.Speed, paddingPrecision, speedStepBeats);

        return line;
    }
}
