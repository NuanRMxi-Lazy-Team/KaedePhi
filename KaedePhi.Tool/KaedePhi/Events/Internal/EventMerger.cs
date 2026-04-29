using KaedePhi.Core.Common;

namespace KaedePhi.Tool.KaedePhi.Events.Internal;

/// <summary>
/// NRC 事件合并器。
/// </summary>
[Obsolete("请使用 global::KaedePhi.Tool.Event.KaedePhi.EventMerger<T>")]
internal static class EventMerger
{
    [Obsolete("请使用 global::KaedePhi.Tool.Event.KaedePhi.EventMerger<T>().EventListMerge")]
    internal static List<Kpc.Event<T>> EventListMerge<T>(
        List<Kpc.Event<T>>? toEvents,
        List<Kpc.Event<T>>? fromEvents,
        double precision = 64d)
    {
        var merger = new global::KaedePhi.Tool.Event.KaedePhi.EventMerger<T>();
        return merger.EventListMerge(toEvents, fromEvents, precision);
    }

    [Obsolete("请使用 global::KaedePhi.Tool.Event.KaedePhi.EventMerger<T>().EventMergePlus")]
    internal static List<Kpc.Event<T>> EventMergePlus<T>(
        List<Kpc.Event<T>>? toEvents,
        List<Kpc.Event<T>>? fromEvents,
        double precision = 64d,
        double tolerance = 5d)
    {
        var merger = new global::KaedePhi.Tool.Event.KaedePhi.EventMerger<T>();
        return merger.EventMergePlus(toEvents, fromEvents, precision, tolerance);
    }
}
