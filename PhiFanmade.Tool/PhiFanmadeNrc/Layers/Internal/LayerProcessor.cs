using PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;

namespace PhiFanmade.Tool.PhiFanmadeNrc.Layers.Internal;

/// <summary>
/// NRC 层级处理器：合并、切割与清理事件层级。
/// </summary>
internal static class LayerProcessor
{
    /// <summary>移除无用层级（所有事件都为默认值的层级）。</summary>
    internal static List<Nrc.EventLayer>? RemoveUnlessLayer(List<Nrc.EventLayer>? layers)
    {
        if (layers is not { Count: > 1 }) return layers;
        var layersCopy = layers.Select(l => l.Clone()).ToList();
        foreach (var layer in layersCopy)
        {
            layer.AlphaEvents  = EventCompressor.RemoveUselessEvent(layer.AlphaEvents);
            layer.MoveXEvents  = EventCompressor.RemoveUselessEvent(layer.MoveXEvents);
            layer.MoveYEvents  = EventCompressor.RemoveUselessEvent(layer.MoveYEvents);
            layer.RotateEvents = EventCompressor.RemoveUselessEvent(layer.RotateEvents);
        }

        return layersCopy;
    }

    /// <summary>将多个事件层级各通道的事件切割到指定精度。</summary>
    internal static List<Nrc.EventLayer> CutLayerEvents(
        List<Nrc.EventLayer> layers, double precision = 64d, double tolerance = 5d, bool compress = true)
    {
        layers.RemoveAll(layer => layer == null);
        layers = RemoveUnlessLayer(layers) ?? layers;
        return layers.Select(layer => CutLayerEvents(layer, precision, tolerance, compress)).ToList();
    }

    /// <summary>将单个事件层级各通道的事件切割到指定精度。</summary>
    internal static Nrc.EventLayer CutLayerEvents(
        Nrc.EventLayer? layer, double precision = 64d, double tolerance = 5d, bool compress = true)
    {
        if (layer == null) return new Nrc.EventLayer();

        var cutLength     = new PhiFanmade.Core.Common.Beat(1d / precision);
        var cutEventLayer = new Nrc.EventLayer();

        if (layer.AlphaEvents is { Count: > 0 })
            cutEventLayer.AlphaEvents = EventCutter.CutEventsInRange(
                layer.AlphaEvents,
                layer.AlphaEvents.Min(e => e.StartBeat),
                layer.AlphaEvents.Max(e => e.EndBeat), cutLength);

        if (layer.MoveXEvents is { Count: > 0 })
            cutEventLayer.MoveXEvents = EventCutter.CutEventsInRange(
                layer.MoveXEvents,
                layer.MoveXEvents.Min(e => e.StartBeat),
                layer.MoveXEvents.Max(e => e.EndBeat), cutLength);

        if (layer.MoveYEvents is { Count: > 0 })
            cutEventLayer.MoveYEvents = EventCutter.CutEventsInRange(
                layer.MoveYEvents,
                layer.MoveYEvents.Min(e => e.StartBeat),
                layer.MoveYEvents.Max(e => e.EndBeat), cutLength);

        if (layer.RotateEvents is { Count: > 0 })
            cutEventLayer.RotateEvents = EventCutter.CutEventsInRange(
                layer.RotateEvents,
                layer.RotateEvents.Min(e => e.StartBeat),
                layer.RotateEvents.Max(e => e.EndBeat), cutLength);

        if (layer.SpeedEvents is { Count: > 0 })
            cutEventLayer.SpeedEvents = EventCutter.CutEventsInRange(
                layer.SpeedEvents,
                layer.SpeedEvents.Min(e => e.StartBeat),
                layer.SpeedEvents.Max(e => e.EndBeat), cutLength);

        if (compress)
        {
            cutEventLayer.AlphaEvents  = EventCompressor.EventListCompress(cutEventLayer.AlphaEvents,  tolerance);
            cutEventLayer.MoveXEvents  = EventCompressor.EventListCompress(cutEventLayer.MoveXEvents,  tolerance);
            cutEventLayer.MoveYEvents  = EventCompressor.EventListCompress(cutEventLayer.MoveYEvents,  tolerance);
            cutEventLayer.RotateEvents = EventCompressor.EventListCompress(cutEventLayer.RotateEvents, tolerance);
            cutEventLayer.SpeedEvents  = EventCompressor.EventListCompress(cutEventLayer.SpeedEvents,  tolerance);
        }

        return cutEventLayer;
    }

    /// <summary>合并多个事件层级（固定采样）。</summary>
    internal static Nrc.EventLayer LayerMerge(
        List<Nrc.EventLayer> layers, double precision = 64d, double tolerance = 5d, bool compress = true)
    {
        layers.RemoveAll(layer => layer == null);
        if (layers.Count <= 1) return layers.FirstOrDefault() ?? new Nrc.EventLayer();
        layers = RemoveUnlessLayer(layers) ?? layers;

        var mergedLayer = new Nrc.EventLayer();
        foreach (var layer in layers)
        {
            if (layer.AlphaEvents  is { Count: > 0 })
                mergedLayer.AlphaEvents  = EventMerger.EventListMerge(mergedLayer.AlphaEvents,  layer.AlphaEvents,  precision, tolerance, compress);
            if (layer.MoveXEvents  is { Count: > 0 })
                mergedLayer.MoveXEvents  = EventMerger.EventListMerge(mergedLayer.MoveXEvents,  layer.MoveXEvents,  precision, tolerance, compress);
            if (layer.MoveYEvents  is { Count: > 0 })
                mergedLayer.MoveYEvents  = EventMerger.EventListMerge(mergedLayer.MoveYEvents,  layer.MoveYEvents,  precision, tolerance, compress);
            if (layer.RotateEvents is { Count: > 0 })
                mergedLayer.RotateEvents = EventMerger.EventListMerge(mergedLayer.RotateEvents, layer.RotateEvents, precision, tolerance, compress);
            if (layer.SpeedEvents  is { Count: > 0 })
                mergedLayer.SpeedEvents  = EventMerger.EventListMerge(mergedLayer.SpeedEvents,  layer.SpeedEvents,  precision, tolerance, compress);
        }

        return mergedLayer;
    }

    /// <summary>合并多个事件层级（自适应采样，性能更优）。</summary>
    internal static Nrc.EventLayer LayerMergePlus(
        List<Nrc.EventLayer> layers, double precision = 64d, double tolerance = 5d)
    {
        layers.RemoveAll(layer => layer == null);
        if (layers.Count <= 1) return layers.FirstOrDefault() ?? new Nrc.EventLayer();
        layers = RemoveUnlessLayer(layers) ?? layers;

        var mergedLayer = new Nrc.EventLayer();
        foreach (var layer in layers)
        {
            if (layer.AlphaEvents  is { Count: > 0 })
                mergedLayer.AlphaEvents  = EventMerger.EventMergePlus(mergedLayer.AlphaEvents,  layer.AlphaEvents,  precision, tolerance);
            if (layer.MoveXEvents  is { Count: > 0 })
                mergedLayer.MoveXEvents  = EventMerger.EventMergePlus(mergedLayer.MoveXEvents,  layer.MoveXEvents,  precision, tolerance);
            if (layer.MoveYEvents  is { Count: > 0 })
                mergedLayer.MoveYEvents  = EventMerger.EventMergePlus(mergedLayer.MoveYEvents,  layer.MoveYEvents,  precision, tolerance);
            if (layer.RotateEvents is { Count: > 0 })
                mergedLayer.RotateEvents = EventMerger.EventMergePlus(mergedLayer.RotateEvents, layer.RotateEvents, precision, tolerance);
            if (layer.SpeedEvents  is { Count: > 0 })
                mergedLayer.SpeedEvents  = EventMerger.EventMergePlus(mergedLayer.SpeedEvents,  layer.SpeedEvents,  precision, tolerance);
        }

        return mergedLayer;
    }
}

