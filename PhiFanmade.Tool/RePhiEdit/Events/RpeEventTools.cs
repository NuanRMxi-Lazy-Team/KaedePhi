using PhiFanmade.Tool.RePhiEdit.Events.Internal;

namespace PhiFanmade.Tool.RePhiEdit.Events;

/// <summary>RPE 格式事件操作工具。</summary>
public static class RpeEventTools
{
    [Obsolete("请转换为PhiFanmadeNrc后再进行操作")]
    public static List<Rpe.Event<float>> EventListCompress(List<Rpe.Event<float>> events, double tolerance = 5)
        => EventCompressor.EventListCompress<float>(events, tolerance);

    [Obsolete("请转换为PhiFanmadeNrc后再进行操作")]
    public static List<Rpe.Event<T>> EventListMerge<T>(
        List<Rpe.Event<T>> toEvents, List<Rpe.Event<T>> fromEvents,
        double precision = 64d, double tolerance = 5d, bool compress = true)
        => EventMerger.EventListMerge<T>(toEvents, fromEvents, precision, tolerance, compress);

    [Obsolete("请转换为PhiFanmadeNrc后再进行操作")]
    public static List<Rpe.Event<T>> EventMergePlus<T>(
        List<Rpe.Event<T>> toEvents, List<Rpe.Event<T>> fromEvents,
        double precision = 64d, double tolerance = 5d)
        => EventMerger.EventMergePlus<T>(toEvents, fromEvents, precision, tolerance);
}


