using PhiFanmade.Core.Common;

namespace PhiFanmade.Tool.RePhiEdit.Internal;

/// <summary>
/// RePhiEdit层级处理器
/// </summary>
internal static class LayerProcessor
{
    /// <summary>
    /// 移除无用层级（所有事件都为默认值的层级）
    /// </summary>
    internal static List<Rpe.EventLayer>? RemoveUnlessLayer(List<Rpe.EventLayer>? layers)
    {
        if (layers is not { Count: > 1 }) return layers;
        var layersCopy = layers.Select(l => l.Clone()).ToList();
        // index非1 layer头部的0值事件去除
        foreach (var layer in layersCopy)
        {
            layer.AlphaEvents = EventProcessor.RemoveUnlessEvent(layer.AlphaEvents);
            layer.MoveXEvents = EventProcessor.RemoveUnlessEvent(layer.MoveXEvents);
            layer.MoveYEvents = EventProcessor.RemoveUnlessEvent(layer.MoveYEvents);
            layer.RotateEvents = EventProcessor.RemoveUnlessEvent(layer.RotateEvents);
        }

        return layersCopy;
    }

    /// <summary>
    /// 合并多个事件层级
    /// </summary>
    internal static List<Rpe.EventLayer> CutLayerEvents(List<Rpe.EventLayer> layers, double precision = 64d,
        double tolerance = 5d, bool compress = true)
    {
        // 清理null层级
        layers.RemoveAll(layer => layer == null);

        // index非1 layer头部的0值事件去除
        layers = RemoveUnlessLayer(layers) ?? layers;

        return layers.Select(layer => CutLayerEvents(layer, precision, tolerance, compress)).ToList();
    }

    internal static Rpe.EventLayer CutLayerEvents(Rpe.EventLayer layer, double precision = 64d,
        double tolerance = 5d, bool compress = true)
    {
        // 清理null层级
        if (layer == null)
            return new Rpe.EventLayer();

        var cutLength = new Beat(1d / precision);
        var cutEventLayer = new Rpe.EventLayer();

        if (layer.AlphaEvents is { Count: > 0 })
        {
            // 寻找StartBeat最小值和EndBeat最大值，然后CutInRange
            var rangeMin = layer.AlphaEvents.Min(e => e.StartBeat);
            var rangeMax = layer.AlphaEvents.Max(e => e.EndBeat);
            cutEventLayer.AlphaEvents =
                EventProcessor.CutEventsInRange(layer.AlphaEvents, rangeMin, rangeMax, cutLength);
        }

        if (layer.MoveXEvents is { Count: > 0 })
        {
            var rangeMin = layer.MoveXEvents.Min(e => e.StartBeat);
            var rangeMax = layer.MoveXEvents.Max(e => e.EndBeat);
            cutEventLayer.MoveXEvents =
                EventProcessor.CutEventsInRange(layer.MoveXEvents, rangeMin, rangeMax, cutLength);
        }

        if (layer.MoveYEvents is { Count: > 0 })
        {
            var rangeMin = layer.MoveYEvents.Min(e => e.StartBeat);
            var rangeMax = layer.MoveYEvents.Max(e => e.EndBeat);
            cutEventLayer.MoveYEvents =
                EventProcessor.CutEventsInRange(layer.MoveYEvents, rangeMin, rangeMax, cutLength);
        }

        if (layer.RotateEvents is { Count: > 0 })
        {
            var rangeMin = layer.RotateEvents.Min(e => e.StartBeat);
            var rangeMax = layer.RotateEvents.Max(e => e.EndBeat);
            cutEventLayer.RotateEvents =
                EventProcessor.CutEventsInRange(layer.RotateEvents, rangeMin, rangeMax, cutLength);
        }

        if (layer.SpeedEvents is { Count: > 0 })
        {
            var rangeMin = layer.SpeedEvents.Min(e => e.StartBeat);
            var rangeMax = layer.SpeedEvents.Max(e => e.EndBeat);
            cutEventLayer.SpeedEvents =
                EventProcessor.CutEventsInRange(layer.SpeedEvents, rangeMin, rangeMax, cutLength);
        }

        if (compress)
        {
            cutEventLayer.AlphaEvents = EventProcessor.EventListCompress(cutEventLayer.AlphaEvents, tolerance);
            cutEventLayer.MoveXEvents = EventProcessor.EventListCompress(cutEventLayer.MoveXEvents, tolerance);
            cutEventLayer.MoveYEvents = EventProcessor.EventListCompress(cutEventLayer.MoveYEvents, tolerance);
            cutEventLayer.RotateEvents = EventProcessor.EventListCompress(cutEventLayer.RotateEvents, tolerance);
            cutEventLayer.SpeedEvents = EventProcessor.EventListCompress(cutEventLayer.SpeedEvents, tolerance);
        }

        return cutEventLayer;
    }

    /// <summary>
    /// 合并多个事件层级
    /// </summary>
    internal static Rpe.EventLayer LayerMerge(List<Rpe.EventLayer> layers, double precision = 64d,
        double tolerance = 5d, bool compress = true)
    {
        // 清理null层级
        layers.RemoveAll(layer => layer == null);
        if (layers.Count <= 1)
            return layers.FirstOrDefault() ?? new Rpe.EventLayer();

        // index非1 layer头部的0值事件去除
        layers = RemoveUnlessLayer(layers) ?? layers;

        var mergedLayer = new Rpe.EventLayer();
        foreach (var layer in layers)
        {
            if (layer.AlphaEvents is { Count: > 0 })
                mergedLayer.AlphaEvents =
                    EventProcessor.EventListMerge(mergedLayer.AlphaEvents, layer.AlphaEvents, precision, tolerance,
                        compress);
            if (layer.MoveXEvents is { Count: > 0 })
                mergedLayer.MoveXEvents =
                    EventProcessor.EventListMerge(mergedLayer.MoveXEvents, layer.MoveXEvents, precision, tolerance,
                        compress);
            if (layer.MoveYEvents is { Count: > 0 })
                mergedLayer.MoveYEvents =
                    EventProcessor.EventListMerge(mergedLayer.MoveYEvents, layer.MoveYEvents, precision, tolerance,
                        compress);
            if (layer.RotateEvents is { Count: > 0 })
                mergedLayer.RotateEvents =
                    EventProcessor.EventListMerge(mergedLayer.RotateEvents, layer.RotateEvents, precision, tolerance,
                        compress);
            if (layer.SpeedEvents is { Count: > 0 })
                mergedLayer.SpeedEvents =
                    EventProcessor.EventListMerge(mergedLayer.SpeedEvents, layer.SpeedEvents, precision, tolerance,
                        compress);
        }

        return mergedLayer;
    }

    /// <summary>
    /// 更节省性能的合并多个事件层级
    /// </summary>
    public static Rpe.EventLayer LayerMergePlus(List<Rpe.EventLayer> layers, double precision = 64d,
        double tolerance = 5d)
    {
        // 清理null层级
        layers.RemoveAll(layer => layer == null);
        if (layers.Count <= 1)
            return layers.FirstOrDefault() ?? new Rpe.EventLayer();

        // index非1 layer头部的0值事件去除
        layers = RemoveUnlessLayer(layers) ?? layers;

        var mergedLayer = new Rpe.EventLayer();
        foreach (var layer in layers)
        {
            if (layer.AlphaEvents is { Count: > 0 })
                mergedLayer.AlphaEvents =
                    EventProcessor.EventMergePlus(mergedLayer.AlphaEvents, layer.AlphaEvents, precision, tolerance);
            if (layer.MoveXEvents is { Count: > 0 })
                mergedLayer.MoveXEvents =
                    EventProcessor.EventMergePlus(mergedLayer.MoveXEvents, layer.MoveXEvents, precision, tolerance);
            if (layer.MoveYEvents is { Count: > 0 })
                mergedLayer.MoveYEvents =
                    EventProcessor.EventMergePlus(mergedLayer.MoveYEvents, layer.MoveYEvents, precision, tolerance);
            if (layer.RotateEvents is { Count: > 0 })
                mergedLayer.RotateEvents =
                    EventProcessor.EventMergePlus(mergedLayer.RotateEvents, layer.RotateEvents, precision, tolerance);
            if (layer.SpeedEvents is { Count: > 0 })
                mergedLayer.SpeedEvents =
                    EventProcessor.EventMergePlus(mergedLayer.SpeedEvents, layer.SpeedEvents, precision, tolerance);
        }

        return mergedLayer;
    }
}