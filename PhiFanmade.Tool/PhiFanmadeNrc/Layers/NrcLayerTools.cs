using PhiFanmade.Tool.PhiFanmadeNrc.Layers.Internal;

namespace PhiFanmade.Tool.PhiFanmadeNrc.Layers;

/// <summary>
/// NRC 格式层级操作工具。提供多层级的合并与切割功能。
/// </summary>
public static class NrcLayerTools
{
    /// <summary>合并多个事件层级（固定采样），将所有层的数值叠加到第一层。</summary>
    public static Nrc.EventLayer LayerMerge(
        List<Nrc.EventLayer> layers, double precision = 64d, double tolerance = 5d, bool compress = true)
        => LayerProcessor.LayerMerge(layers, precision, tolerance, compress);

    /// <summary>合并多个事件层级（自适应采样），性能更优。</summary>
    public static Nrc.EventLayer LayerMergePlus(
        List<Nrc.EventLayer> layers, double precision = 64d, double tolerance = 5d)
        => LayerProcessor.LayerMergePlus(layers, precision, tolerance);

    /// <summary>将单个事件层级的所有事件切割到指定精度的时间点上。</summary>
    public static Nrc.EventLayer CutLayerEvents(
        Nrc.EventLayer layer, double precision = 64d, double tolerance = 5d, bool compress = true)
        => LayerProcessor.CutLayerEvents(layer, precision, tolerance, compress);

    /// <summary>将多个事件层级的所有事件切割到指定精度的时间点上。</summary>
    public static List<Nrc.EventLayer> CutLayerEvents(
        List<Nrc.EventLayer> layers, double precision = 64d, double tolerance = 5d, bool compress = true)
        => LayerProcessor.CutLayerEvents(layers, precision, tolerance, compress);
}

