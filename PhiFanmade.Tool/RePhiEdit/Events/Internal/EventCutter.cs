using PhiFanmade.Core.Common;

namespace PhiFanmade.Tool.RePhiEdit.Events.Internal;

/// <summary>RPE 事件切割器。</summary>
internal static class EventCutter
{
    internal static List<Rpe.Event<T>> CutEventsInRange<T>(
        List<Rpe.Event<T>> events, Beat startBeat, Beat endBeat, Beat cutLength)
    {
        var cutEvents  = new List<Rpe.Event<T>>();
        var eventsToCut = events.Where(e => e.StartBeat < endBeat && e.EndBeat > startBeat).ToList();

        foreach (var evt in eventsToCut)
        {
            var cutStart    = evt.StartBeat < startBeat ? startBeat : evt.StartBeat;
            var cutEnd      = evt.EndBeat   > endBeat   ? endBeat   : evt.EndBeat;
            var totalBeats  = cutEnd - cutStart;
            var segmentCount = (int)Math.Ceiling((totalBeats / cutLength));

            for (var i = 0; i < segmentCount; i++)
            {
                var currentBeat = new Beat(cutStart + (cutLength * i));
                var segmentEnd  = new Beat(cutStart + (cutLength * (i + 1)));
                if (segmentEnd > cutEnd) segmentEnd = cutEnd;

                cutEvents.Add(new Rpe.Event<T>
                {
                    StartBeat  = currentBeat,
                    EndBeat    = segmentEnd,
                    StartValue = evt.GetValueAtBeat(currentBeat),
                    EndValue   = evt.GetValueAtBeat(segmentEnd),
                });
            }
        }

        return cutEvents;
    }
}


