using PhiFanmade.Tool.RePhiEdit.Layers.Internal;

namespace PhiFanmade.Tool.RePhiEdit.Layers;

/// <summary>RPE 格式层级操作工具。</summary>
public static class RpeLayerTools
{
    [Obsolete("请转换为PhiFanmadeNrc后再进行操作")]
    public static Rpe.EventLayer LayerMerge(
        List<Rpe.EventLayer> layers, double precision = 64d, double tolerance = 5d, bool compress = true)
        => LayerProcessor.LayerMerge(layers, precision, tolerance, compress);

    [Obsolete("请转换为PhiFanmadeNrc后再进行操作")]
    public static Rpe.EventLayer LayerMergePlus(
        List<Rpe.EventLayer> layers, double precision = 64d, double tolerance = 5d)
        => LayerProcessor.LayerMergePlus(layers, precision, tolerance);

    [Obsolete("请转换为PhiFanmadeNrc后再进行操作")]
    public static Rpe.EventLayer CutLayerEvents(
        Rpe.EventLayer layer, double precision = 64d, double tolerance = 5d, bool compress = true)
        => LayerProcessor.CutLayerEvents(layer, precision, tolerance, compress);

    [Obsolete("请转换为PhiFanmadeNrc后再进行操作")]
    public static List<Rpe.EventLayer> CutLayerEvents(
        List<Rpe.EventLayer> layers, double precision = 64d, double tolerance = 5d, bool compress = true)
        => LayerProcessor.CutLayerEvents(layers, precision, tolerance, compress);
}


