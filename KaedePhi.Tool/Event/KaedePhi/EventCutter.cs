using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Event.KaedePhi;

/// <summary>
/// KPC 事件切割器：将事件列表按指定拍长切割为等长段。
/// </summary>
public class EventCutter<TPayload> : LoggableBase, IEventCutter<KpcEvents.Event<TPayload>, Beat>
    where TPayload : notnull
{
    /// <inheritdoc/>
    public List<KpcEvents.Event<TPayload>> CutEventsInRange(
        List<KpcEvents.Event<TPayload>> events,
        Beat startBeat,
        Beat endBeat,
        double cutLength
    )
    {
        var cutLengthBeat = new Beat(cutLength);
        return CutEventsInRange(events, startBeat, endBeat, cutLengthBeat);
    }

    /// <inheritdoc/>
    public List<KpcEvents.Event<TPayload>> CutEventsInRange(
        List<KpcEvents.Event<TPayload>> events,
        Beat startBeat,
        Beat endBeat,
        Beat cutLength
    )
    {
        if (cutLength <= new Beat(0))
            throw new ArgumentOutOfRangeException(nameof(cutLength), "切割长度必须大于0.");

        var cutEvents = new List<KpcEvents.Event<TPayload>>();

        // 直接遍历并过滤，避免 Where().ToList() 中间分配
        foreach (var evt in events)
        {
            // 跳过不在范围内的事件
            if (evt.StartBeat >= endBeat || evt.EndBeat <= startBeat)
                continue;

            var cutStart = evt.StartBeat < startBeat ? startBeat : evt.StartBeat;
            var cutEnd = evt.EndBeat > endBeat ? endBeat : evt.EndBeat;

            var totalBeats = cutEnd - cutStart;
            var segmentCount = (int)Math.Ceiling((totalBeats / cutLength));

            for (var i = 0; i < segmentCount; i++)
            {
                var currentBeat = new Beat(cutStart + (cutLength * i));
                var segmentEnd = new Beat(cutStart + (cutLength * (i + 1)));
                if (segmentEnd > cutEnd)
                    segmentEnd = cutEnd;

                cutEvents.Add(
                    new KpcEvents.Event<TPayload>
                    {
                        StartBeat = currentBeat,
                        EndBeat = segmentEnd,
                        StartValue = evt.GetValueAtBeat(currentBeat),
                        EndValue = evt.GetValueAtBeat(segmentEnd),
                    }
                );
            }
        }

        return cutEvents;
    }

    /// <inheritdoc/>
    public List<KpcEvents.Event<TPayload>> CutEventToLinear(
        KpcEvents.Event<TPayload> evt,
        double cutLength
    ) => CutEventToLinear(evt, new Beat(cutLength));

    /// <inheritdoc/>
    public List<KpcEvents.Event<TPayload>> CutEventToLinear(
        KpcEvents.Event<TPayload> evt,
        Beat cutLength
    )
    {
        if (cutLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(cutLength), "切割长度必须大于0.");
        var cutEvents = new List<KpcEvents.Event<TPayload>>();
        // 在evt中均匀采样，并返回
        var nowBeat = evt.StartBeat;
        while (nowBeat < evt.EndBeat)
        {
            var segmentEnd = nowBeat + cutLength;
            if (segmentEnd > evt.EndBeat)
            {
                segmentEnd = evt.EndBeat;
            }

            if (segmentEnd <= nowBeat)
                throw new InvalidOperationException("切割步长无法推进事件位置。");

            cutEvents.Add(
                new KpcEvents.Event<TPayload>
                {
                    StartBeat = nowBeat,
                    EndBeat = segmentEnd,
                    StartValue = evt.GetValueAtBeat(nowBeat),
                    EndValue = evt.GetValueAtBeat(segmentEnd),
                }
            );

            nowBeat = segmentEnd;
        }

        return cutEvents;
    }
}
