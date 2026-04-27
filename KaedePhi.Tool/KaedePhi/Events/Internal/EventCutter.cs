using KaedePhi.Core.Common;

namespace KaedePhi.Tool.KaedePhi.Events.Internal;

/// <summary>
/// NRC 事件切割器：将事件列表按指定拍长切割为等长段。
/// </summary>
internal static class EventCutter
{
    /// <summary>
    /// 在指定的拍范围内切割事件列表。
    /// </summary>
    internal static List<Kpc.Event<T>> CutEventsInRange<T>(
        List<Kpc.Event<T>> events,
        Beat startBeat,
        Beat endBeat,
        Beat cutLength)
    {
        var cutEvents = new List<Kpc.Event<T>>();
        var eventsToCut = events.Where(e => e.StartBeat < endBeat && e.EndBeat > startBeat).ToList();

        foreach (var evt in eventsToCut)
        {
            var cutStart = evt.StartBeat < startBeat ? startBeat : evt.StartBeat;
            var cutEnd = evt.EndBeat > endBeat ? endBeat : evt.EndBeat;

            var totalBeats = cutEnd - cutStart;
            var segmentCount = (int)Math.Ceiling((totalBeats / cutLength));

            for (var i = 0; i < segmentCount; i++)
            {
                var currentBeat = new Beat(cutStart + (cutLength * i));
                var segmentEnd = new Beat(cutStart + (cutLength * (i + 1)));
                if (segmentEnd > cutEnd) segmentEnd = cutEnd;

                cutEvents.Add(new Kpc.Event<T>
                {
                    StartBeat = currentBeat,
                    EndBeat = segmentEnd,
                    StartValue = evt.GetValueAtBeat(currentBeat),
                    EndValue = evt.GetValueAtBeat(segmentEnd),
                });
            }
        }

        return cutEvents;
    }

    /// <see cref="CutEventsInRange{T}(List{Event{T}}, Beat, Beat, Beat)"/>
    internal static List<Kpc.Event<T>> CutEventsInRange<T>(
        List<Kpc.Event<T>> events,
        Beat startBeat,
        Beat endBeat,
        double cutLength)
    {
        var cutLengthBeat = new Beat(cutLength);
        return CutEventsInRange(events, startBeat, endBeat, cutLengthBeat);
    }

    internal static List<Kpc.Event<T>> CutEventToLiner<T>(
        Kpc.Event<T> evt, double cutLength)
        => CutEventToLiner(evt, new Beat(cutLength));

    internal static List<Kpc.Event<T>> CutEventToLiner<T>(
        Kpc.Event<T> evt,
        Beat cutLength)
    {
        var cutEvents = new List<Kpc.Event<T>>();
        // 在evt中均匀采样，并返回
        var nowBeat = evt.StartBeat;
        while (nowBeat < evt.EndBeat)
        {
            var segmentEnd = nowBeat + cutLength;
            if (segmentEnd > evt.EndBeat)
            {
                segmentEnd = evt.EndBeat;
            }

            cutEvents.Add(new Kpc.Event<T>()
            {
                StartBeat = nowBeat,
                EndBeat = segmentEnd,
                StartValue = evt.GetValueAtBeat(nowBeat),
                EndValue = evt.GetValueAtBeat(segmentEnd),
            });

            nowBeat = segmentEnd;
        }

        return cutEvents;
    }
}