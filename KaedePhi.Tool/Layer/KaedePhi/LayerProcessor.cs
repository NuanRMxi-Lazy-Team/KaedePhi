using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Event.KaedePhi;
using KaedePhi.Tool.JudgeLines.KaedePhi.Utils;
using EventLayer = KaedePhi.Core.KaedePhi.Events.EventLayer;

namespace KaedePhi.Tool.Layer.KaedePhi;

/// <summary>
/// KPC 谱面事件层处理器。
/// </summary>
public class LayerProcessor : LoggableBase, ILayerProcessor<EventLayer>
{
    private readonly EventListMerger<double> _doubleMerger = new();
    private readonly EventListMerger<int> _intMerger = new();
    private readonly EventListMerger<float> _floatMerger = new();
    private readonly EventListMergerSqrt<double> _doubleMergerSqrt = new();
    private readonly EventListMergerPlus<double> _doubleMergerPlus = new();
    private readonly EventListMergerPlus<int> _intMergerPlus = new();
    private readonly EventListMergerPlus<float> _floatMergerPlus = new();
    private readonly EventListMergerSqrt<double> _doubleMergerSqrtPlus = new();
    private readonly EventCutter<double> _doubleCutter = new();
    private readonly EventCutter<int> _intCutter = new();
    private readonly EventCutter<float> _floatCutter = new();
    private readonly EventCompressor<double> _doubleCompressor = new();
    private readonly EventCompressor<int> _intCompressor = new();
    private readonly EventCompressor<float> _floatCompressor = new();

    /// <inheritdoc/>
    public EventLayer LayerMerge(
        List<EventLayer> layers,
        double precision,
        IProgress<ToolProgress>? progress = null
    )
    {
        layers = [.. layers.Where(layer => (object?)layer is not null)];
        if (layers.Count <= 1)
            return layers.FirstOrDefault()?.Clone() ?? new EventLayer();
        layers = RemoveUnlessLayer(layers) ?? layers;

        var mergedLayer = new EventLayer();
        var totalLayers = layers.Count;
        for (var li = 0; li < totalLayers; li++)
        {
            var layer = layers[li];
            if (layer.AlphaEvents is { Count: > 0 })
                mergedLayer.AlphaEvents = _intMerger.EventListMerge(
                    mergedLayer.AlphaEvents,
                    layer.AlphaEvents,
                    precision
                );
            if (layer.MoveXEvents is { Count: > 0 })
                mergedLayer.MoveXEvents = _doubleMergerSqrt.EventListMerge(
                    mergedLayer.MoveXEvents,
                    layer.MoveXEvents,
                    precision
                );
            if (layer.MoveYEvents is { Count: > 0 })
                mergedLayer.MoveYEvents = _doubleMergerSqrt.EventListMerge(
                    mergedLayer.MoveYEvents,
                    layer.MoveYEvents,
                    precision
                );
            if (layer.RotateEvents is { Count: > 0 })
                mergedLayer.RotateEvents = _doubleMerger.EventListMerge(
                    mergedLayer.RotateEvents,
                    layer.RotateEvents,
                    precision
                );
            if (layer.SpeedEvents is { Count: > 0 })
                mergedLayer.SpeedEvents = _floatMerger.EventListMerge(
                    mergedLayer.SpeedEvents,
                    layer.SpeedEvents,
                    precision
                );

            progress?.Report(new ToolProgress((double)(li + 1) / totalLayers));
        }

        progress?.Report(new ToolProgress(1.0));
        return mergedLayer;
    }

    /// <inheritdoc/>
    public EventLayer LayerMergePlus(
        List<EventLayer> layers,
        double precision,
        double tolerance,
        IProgress<ToolProgress>? progress = null
    )
    {
        layers = [.. layers.Where(layer => (object?)layer is not null)];
        if (layers.Count <= 1)
            return layers.FirstOrDefault()?.Clone() ?? new EventLayer();
        layers = RemoveUnlessLayer(layers) ?? layers;

        var mergedLayer = new EventLayer();
        var totalLayers = layers.Count;
        for (var li = 0; li < totalLayers; li++)
        {
            var layer = layers[li];
            if (layer.AlphaEvents is { Count: > 0 })
                mergedLayer.AlphaEvents = _intMergerPlus.EventListMerge(
                    mergedLayer.AlphaEvents,
                    layer.AlphaEvents,
                    precision,
                    tolerance
                );
            if (layer.MoveXEvents is { Count: > 0 })
                mergedLayer.MoveXEvents = _doubleMergerSqrtPlus.EventListMerge(
                    mergedLayer.MoveXEvents,
                    layer.MoveXEvents,
                    precision,
                    tolerance
                );
            if (layer.MoveYEvents is { Count: > 0 })
                mergedLayer.MoveYEvents = _doubleMergerSqrtPlus.EventListMerge(
                    mergedLayer.MoveYEvents,
                    layer.MoveYEvents,
                    precision,
                    tolerance
                );
            if (layer.RotateEvents is { Count: > 0 })
                mergedLayer.RotateEvents = _doubleMergerPlus.EventListMerge(
                    mergedLayer.RotateEvents,
                    layer.RotateEvents,
                    precision,
                    tolerance
                );
            if (layer.SpeedEvents is { Count: > 0 })
                mergedLayer.SpeedEvents = _floatMergerPlus.EventListMerge(
                    mergedLayer.SpeedEvents,
                    layer.SpeedEvents,
                    precision,
                    tolerance
                );

            progress?.Report(new ToolProgress((double)(li + 1) / totalLayers));
        }

        progress?.Report(new ToolProgress(1.0));
        return mergedLayer;
    }

    /// <inheritdoc/>
    public EventLayer CutLayerEvents(
        EventLayer? layer,
        double precision,
        IProgress<ToolProgress>? progress = null
    )
    {
        if (layer == null)
            return new EventLayer();

        var cutLength = new Beat(1d / precision);
        var cutEventLayer = new EventLayer();
        const int totalChannels = 5;
        var completedChannels = 0;

        if (layer.AlphaEvents is { Count: > 0 })
            cutEventLayer.AlphaEvents = _intCutter.CutEventsInRange(
                layer.AlphaEvents,
                layer.AlphaEvents.Min(e => e.StartBeat),
                layer.AlphaEvents.Max(e => e.EndBeat),
                cutLength
            );
        progress?.Report(new ToolProgress((double)++completedChannels / totalChannels));

        if (layer.MoveXEvents is { Count: > 0 })
            cutEventLayer.MoveXEvents = _doubleCutter.CutEventsInRange(
                layer.MoveXEvents,
                layer.MoveXEvents.Min(e => e.StartBeat),
                layer.MoveXEvents.Max(e => e.EndBeat),
                cutLength
            );
        progress?.Report(new ToolProgress((double)++completedChannels / totalChannels));

        if (layer.MoveYEvents is { Count: > 0 })
            cutEventLayer.MoveYEvents = _doubleCutter.CutEventsInRange(
                layer.MoveYEvents,
                layer.MoveYEvents.Min(e => e.StartBeat),
                layer.MoveYEvents.Max(e => e.EndBeat),
                cutLength
            );
        progress?.Report(new ToolProgress((double)++completedChannels / totalChannels));

        if (layer.RotateEvents is { Count: > 0 })
            cutEventLayer.RotateEvents = _doubleCutter.CutEventsInRange(
                layer.RotateEvents,
                layer.RotateEvents.Min(e => e.StartBeat),
                layer.RotateEvents.Max(e => e.EndBeat),
                cutLength
            );
        progress?.Report(new ToolProgress((double)++completedChannels / totalChannels));

        if (layer.SpeedEvents is { Count: > 0 })
            cutEventLayer.SpeedEvents = _floatCutter.CutEventsInRange(
                layer.SpeedEvents,
                layer.SpeedEvents.Min(e => e.StartBeat),
                layer.SpeedEvents.Max(e => e.EndBeat),
                cutLength
            );
        progress?.Report(new ToolProgress(1.0));

        return cutEventLayer;
    }

    /// <inheritdoc/>
    public List<EventLayer> CutLayerEvents(
        List<EventLayer> layers,
        double precision,
        IProgress<ToolProgress>? progress = null
    )
    {
        layers = [.. layers.Where(layer => (object?)layer is not null)];
        layers = RemoveUnlessLayer(layers) ?? layers;
        var result = new List<EventLayer>(layers.Count);
        for (var i = 0; i < layers.Count; i++)
        {
            result.Add(CutLayerEvents(layers[i], precision));
            progress?.Report(new ToolProgress((double)(i + 1) / layers.Count));
        }

        progress?.Report(new ToolProgress(1.0));
        return result;
    }

    /// <inheritdoc/>
    public void LayerEventsCompress(
        EventLayer layer,
        double tolerance,
        IProgress<ToolProgress>? progress = null
    )
    {
        const int totalChannels = 5;
        var completedChannels = 0;

        if (layer.AlphaEvents is { Count: > 0 })
            layer.AlphaEvents = _intCompressor.EventListCompressSlope(layer.AlphaEvents, tolerance);
        progress?.Report(new ToolProgress((double)++completedChannels / totalChannels));

        var canCompressPositionTogether = CanCompressPositionTogether(layer);
        if (canCompressPositionTogether)
        {
            var (compressedX, compressedY) = FatherUnbindHelpers.CompressPositionEvents(
                layer.MoveXEvents!,
                layer.MoveYEvents!,
                tolerance
            );
            layer.MoveXEvents = compressedX;
            layer.MoveYEvents = compressedY;
        }
        else if (layer.MoveXEvents is { Count: > 0 })
            layer.MoveXEvents = _doubleCompressor.EventListCompressSqrt(
                layer.MoveXEvents,
                tolerance
            );
        progress?.Report(new ToolProgress((double)++completedChannels / totalChannels));

        if (!canCompressPositionTogether && layer.MoveYEvents is { Count: > 0 })
            layer.MoveYEvents = _doubleCompressor.EventListCompressSqrt(
                layer.MoveYEvents,
                tolerance
            );
        progress?.Report(new ToolProgress((double)++completedChannels / totalChannels));

        if (layer.RotateEvents is { Count: > 0 })
            layer.RotateEvents = _doubleCompressor.EventListCompressSlope(
                layer.RotateEvents,
                tolerance
            );
        progress?.Report(new ToolProgress((double)++completedChannels / totalChannels));

        if (layer.SpeedEvents is { Count: > 0 })
            layer.SpeedEvents = _floatCompressor.EventListCompressSlope(
                layer.SpeedEvents,
                tolerance
            );
        progress?.Report(new ToolProgress(1.0));
    }

    private static bool CanCompressPositionTogether(EventLayer layer)
    {
        if (
            layer.MoveXEvents is not { Count: > 0 } xEvents
            || layer.MoveYEvents is not { Count: > 0 } yEvents
            || xEvents.Count != yEvents.Count
        )
            return false;

        for (var i = 0; i < xEvents.Count; i++)
        {
            if (
                xEvents[i].StartBeat != yEvents[i].StartBeat
                || xEvents[i].EndBeat != yEvents[i].EndBeat
            )
                return false;
        }

        return true;
    }

    private List<EventLayer>? RemoveUnlessLayer(List<EventLayer>? layers)
    {
        if (layers is not { Count: > 1 })
            return layers;
        var layersCopy = layers.Select(l => l.Clone()).ToList();
        foreach (var layer in layersCopy)
        {
            layer.AlphaEvents = _intCompressor.RemoveUselessEvent(layer.AlphaEvents);
            layer.MoveXEvents = _doubleCompressor.RemoveUselessEvent(layer.MoveXEvents);
            layer.MoveYEvents = _doubleCompressor.RemoveUselessEvent(layer.MoveYEvents);
            layer.RotateEvents = _doubleCompressor.RemoveUselessEvent(layer.RotateEvents);
        }

        return layersCopy;
    }
}
