namespace PhiFanmade.Tool.RePhiEdit.Events.Internal;

/// <summary>RPE 事件压缩器。</summary>
internal static class EventCompressor
{
    internal static List<Rpe.Event<T>> EventListCompress<T>(
        List<Rpe.Event<T>>? events, double tolerance = 5)
    {
        if (typeof(T) != typeof(int) && typeof(T) != typeof(float) && typeof(T) != typeof(double))
            throw new NotSupportedException("EventListCompress only supports int, float, and double types.");

        if (events == null || events.Count == 0) return [];

        var compressed = new List<Rpe.Event<T>> { events[0] };
        for (var i = 1; i < events.Count; i++)
        {
            var lastEvent    = compressed[^1];
            var currentEvent = events[i];

            if (lastEvent.Easing == 1 && currentEvent.Easing == 1)
            {
                var lastRate = ((dynamic?)lastEvent.EndValue - (dynamic?)lastEvent.StartValue) /
                               (lastEvent.EndBeat - lastEvent.StartBeat);
                var currentRate = ((dynamic?)currentEvent.EndValue - (dynamic?)currentEvent.StartValue) /
                                  (currentEvent.EndBeat - currentEvent.StartBeat);

                if (Math.Abs((double)(lastRate - currentRate)) <=
                        tolerance * (Math.Abs((double)lastRate) + Math.Abs((double)currentRate)) / 2.0 / 100.0 &&
                    lastEvent.EndBeat == currentEvent.StartBeat &&
                    Math.Abs((double)((dynamic?)lastEvent.EndValue - (dynamic?)currentEvent.StartValue)) <=
                        tolerance * (Math.Abs((dynamic?)lastEvent.EndValue) + 1e-9) / 100.0)
                {
                    lastEvent.EndBeat  = currentEvent.EndBeat;
                    lastEvent.EndValue = currentEvent.EndValue;
                    continue;
                }
            }

            compressed.Add(currentEvent);
        }

        return compressed;
    }

    internal static List<Rpe.Event<T>>? RemoveUselessEvent<T>(List<Rpe.Event<T>>? events)
    {
        var eventsCopy = events?.Select(e => e.Clone()).ToList();
        if (eventsCopy is { Count: 1 } &&
            EqualityComparer<T>.Default.Equals(eventsCopy[0].StartValue, default) &&
            EqualityComparer<T>.Default.Equals(eventsCopy[0].EndValue,   default))
        {
            eventsCopy.RemoveAt(0);
        }
        return eventsCopy;
    }
}


