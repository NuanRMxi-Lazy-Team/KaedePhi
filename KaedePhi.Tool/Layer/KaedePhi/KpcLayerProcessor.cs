using KaedePhi.Core.Common;
using KaedePhi.Tool.Event.KaedePhi;
using EventLayer = KaedePhi.Core.KaedePhi.EventLayer;

namespace KaedePhi.Tool.Layer.KaedePhi;

/// <summary>
/// NRC（KaedePhi）谱面事件层处理器。
/// </summary>
public class KpcLayerProcessor : ILayerProcessor<EventLayer>
{
    private readonly EventMerger<double> _doubleMerger = new();
    private readonly EventMerger<int> _intMerger = new();
    private readonly EventMerger<float> _floatMerger = new();
    private readonly EventCutter<double> _doubleCutter = new();
    private readonly EventCutter<int> _intCutter = new();
    private readonly EventCutter<float> _floatCutter = new();
    private readonly EventCompressor<double> _doubleCompressor = new();
    private readonly EventCompressor<int> _intCompressor = new();
    private readonly EventCompressor<float> _floatCompressor = new();

    /// <inheritdoc/>
    public EventLayer LayerMerge(List<EventLayer> layers, double precision)
    {
        layers.RemoveAll(layer => (object?)layer is null);
        if (layers.Count <= 1) return layers.FirstOrDefault() ?? new EventLayer();
        layers = RemoveUnlessLayer(layers) ?? layers;

        var mergedLayer = new EventLayer();
        foreach (var layer in layers)
        {
            if (layer.AlphaEvents is { Count: > 0 })
                mergedLayer.AlphaEvents =
                    _intMerger.EventListMerge(mergedLayer.AlphaEvents, layer.AlphaEvents, precision);
            if (layer.MoveXEvents is { Count: > 0 })
                mergedLayer.MoveXEvents =
                    _doubleMerger.EventListMerge(mergedLayer.MoveXEvents, layer.MoveXEvents, precision);
            if (layer.MoveYEvents is { Count: > 0 })
                mergedLayer.MoveYEvents =
                    _doubleMerger.EventListMerge(mergedLayer.MoveYEvents, layer.MoveYEvents, precision);
            if (layer.RotateEvents is { Count: > 0 })
                mergedLayer.RotateEvents =
                    _doubleMerger.EventListMerge(mergedLayer.RotateEvents, layer.RotateEvents, precision);
            if (layer.SpeedEvents is { Count: > 0 })
                mergedLayer.SpeedEvents =
                    _floatMerger.EventListMerge(mergedLayer.SpeedEvents, layer.SpeedEvents, precision);
        }

        return mergedLayer;
    }

    /// <inheritdoc/>
    public EventLayer LayerMergePlus(List<EventLayer> layers, double precision, double tolerance)
    {
        layers.RemoveAll(layer => (object?)layer is null);
        if (layers.Count <= 1) return layers.FirstOrDefault() ?? new EventLayer();
        layers = RemoveUnlessLayer(layers) ?? layers;

        var mergedLayer = new EventLayer();
        foreach (var layer in layers)
        {
            if (layer.AlphaEvents is { Count: > 0 })
                mergedLayer.AlphaEvents =
                    _intMerger.EventMergePlus(mergedLayer.AlphaEvents, layer.AlphaEvents, precision, tolerance);
            if (layer.MoveXEvents is { Count: > 0 })
                mergedLayer.MoveXEvents =
                    _doubleMerger.EventMergePlus(mergedLayer.MoveXEvents, layer.MoveXEvents, precision, tolerance);
            if (layer.MoveYEvents is { Count: > 0 })
                mergedLayer.MoveYEvents =
                    _doubleMerger.EventMergePlus(mergedLayer.MoveYEvents, layer.MoveYEvents, precision, tolerance);
            if (layer.RotateEvents is { Count: > 0 })
                mergedLayer.RotateEvents =
                    _doubleMerger.EventMergePlus(mergedLayer.RotateEvents, layer.RotateEvents, precision, tolerance);
            if (layer.SpeedEvents is { Count: > 0 })
                mergedLayer.SpeedEvents =
                    _floatMerger.EventMergePlus(mergedLayer.SpeedEvents, layer.SpeedEvents, precision, tolerance);
        }

        return mergedLayer;
    }

    /// <inheritdoc/>
    public EventLayer CutLayerEvents(EventLayer? layer, double precision)
    {
        if (layer == null) return new EventLayer();

        var cutLength = new Beat(1d / precision);
        var cutEventLayer = new EventLayer();

        if (layer.AlphaEvents is { Count: > 0 })
            cutEventLayer.AlphaEvents = _intCutter.CutEventsInRange(
                layer.AlphaEvents,
                layer.AlphaEvents.Min(e => e.StartBeat),
                layer.AlphaEvents.Max(e => e.EndBeat), cutLength);

        if (layer.MoveXEvents is { Count: > 0 })
            cutEventLayer.MoveXEvents = _doubleCutter.CutEventsInRange(
                layer.MoveXEvents,
                layer.MoveXEvents.Min(e => e.StartBeat),
                layer.MoveXEvents.Max(e => e.EndBeat), cutLength);

        if (layer.MoveYEvents is { Count: > 0 })
            cutEventLayer.MoveYEvents = _doubleCutter.CutEventsInRange(
                layer.MoveYEvents,
                layer.MoveYEvents.Min(e => e.StartBeat),
                layer.MoveYEvents.Max(e => e.EndBeat), cutLength);

        if (layer.RotateEvents is { Count: > 0 })
            cutEventLayer.RotateEvents = _doubleCutter.CutEventsInRange(
                layer.RotateEvents,
                layer.RotateEvents.Min(e => e.StartBeat),
                layer.RotateEvents.Max(e => e.EndBeat), cutLength);

        if (layer.SpeedEvents is { Count: > 0 })
            cutEventLayer.SpeedEvents = _floatCutter.CutEventsInRange(
                layer.SpeedEvents,
                layer.SpeedEvents.Min(e => e.StartBeat),
                layer.SpeedEvents.Max(e => e.EndBeat), cutLength);

        return cutEventLayer;
    }

    /// <inheritdoc/>
    public List<EventLayer> CutLayerEvents(List<EventLayer> layers, double precision)
    {
        layers.RemoveAll(layer => (object?)layer is null);
        layers = RemoveUnlessLayer(layers) ?? layers;
        return layers.Select(layer => CutLayerEvents(layer, precision)).ToList();
    }

    /// <inheritdoc/>
    public void LayerEventsCompress(EventLayer layer, double tolerance)
    {
        if (layer.AlphaEvents is { Count: > 0 })
            layer.AlphaEvents = _intCompressor.EventListCompressSlope(layer.AlphaEvents, tolerance);
        if (layer.MoveXEvents is { Count: > 0 })
            layer.MoveXEvents = _doubleCompressor.EventListCompressSqrt(layer.MoveXEvents, tolerance);
        if (layer.MoveYEvents is { Count: > 0 })
            layer.MoveYEvents = _doubleCompressor.EventListCompressSqrt(layer.MoveYEvents, tolerance);
        if (layer.RotateEvents is { Count: > 0 })
            layer.RotateEvents = _doubleCompressor.EventListCompressSlope(layer.RotateEvents, tolerance);
        if (layer.SpeedEvents is { Count: > 0 })
            layer.SpeedEvents = _floatCompressor.EventListCompressSlope(layer.SpeedEvents, tolerance);
    }

    private static List<EventLayer>? RemoveUnlessLayer(List<EventLayer>? layers)
    {
        if (layers is not { Count: > 1 }) return layers;
        var layersCopy = layers.Select(l => l.Clone()).ToList();
        var intCompressor = new EventCompressor<int>();
        var doubleCompressor = new EventCompressor<double>();
        foreach (var layer in layersCopy)
        {
            layer.AlphaEvents = intCompressor.RemoveUselessEvent(layer.AlphaEvents);
            layer.MoveXEvents = doubleCompressor.RemoveUselessEvent(layer.MoveXEvents);
            layer.MoveYEvents = doubleCompressor.RemoveUselessEvent(layer.MoveYEvents);
            layer.RotateEvents = doubleCompressor.RemoveUselessEvent(layer.RotateEvents);
        }

        return layersCopy;
    }
}
