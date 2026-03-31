using PhiFanmade.Core.Common;

namespace PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;

/// <summary>
/// NRC 事件拟合器：将连续线性事件拟合为更少的缓动事件，并保留原有非线性事件不变。
/// </summary>
internal static class EventFit
{
    private const int MinEasingId = 1;
    private const int MaxEasingId = 31;

    /// <summary>
    /// 对事件列表执行缓动拟合。仅会拟合连续、线性、数值型的事件段；原有非线性事件会被原样保留。
    /// </summary>
    internal static List<Nrc.Event<T>> EventListFit<T>(
        List<Nrc.Event<T>>? events,
        double precision = 64d,
        double tolerance = 5d)
    {
        if (tolerance is > 100 or < 0)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be between 0 and 100.");
        if (precision <= 0)
            throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be greater than 0.");

        EnsureSupportedNumericType<T>();

        if (events == null || events.Count == 0)
            return [];

        var sortedEvents = events
            .Select(e => e.Clone())
            .OrderBy(e => e.StartBeat)
            .ThenBy(e => e.EndBeat)
            .ToList();

        var result = new List<Nrc.Event<T>>(sortedEvents.Count);

        for (var index = 0; index < sortedEvents.Count;)
        {
            if (!CanParticipateInFit(sortedEvents[index]))
            {
                result.Add(sortedEvents[index]);
                index++;
                continue;
            }

            var runEnd = index + 1;
            while (runEnd < sortedEvents.Count && CanAppendToFitRun(sortedEvents[runEnd - 1], sortedEvents[runEnd], tolerance))
            {
                runEnd++;
            }

            if (runEnd - index < 2)
            {
                result.Add(sortedEvents[index]);
                index++;
                continue;
            }

            result.AddRange(FitLinearRun(sortedEvents, index, runEnd, precision, tolerance));
            index = runEnd;
        }

        return result;
    }

    private static List<Nrc.Event<T>> FitLinearRun<T>(
        List<Nrc.Event<T>> runEvents,
        int startIndex,
        int endExclusive,
        double precision,
        double tolerance)
    {
        var result = new List<Nrc.Event<T>>();

        for (var index = startIndex; index < endExclusive;)
        {
            var fitted = TryFitLongestRange(runEvents, index, endExclusive, precision, tolerance);
            if (fitted is null)
            {
                result.Add(runEvents[index]);
                index++;
                continue;
            }

            result.Add(fitted.Value.Event);
            index += fitted.Value.Length;
        }

        return result;
    }

    private static (Nrc.Event<T> Event, int Length)? TryFitLongestRange<T>(
        List<Nrc.Event<T>> runEvents,
        int startIndex,
        int endExclusive,
        double precision,
        double tolerance)
    {
        for (var endIndex = endExclusive - 1; endIndex > startIndex; endIndex--)
        {
            if (!TryCreateBestFittedEvent(runEvents, startIndex, endIndex, precision, tolerance, out var fittedEvent))
                continue;

            return (fittedEvent, endIndex - startIndex + 1);
        }

        return null;
    }

    private static bool TryCreateBestFittedEvent<T>(
        List<Nrc.Event<T>> runEvents,
        int startIndex,
        int endIndex,
        double precision,
        double tolerance,
        out Nrc.Event<T> fittedEvent)
    {
        var samples = BuildSamples(runEvents, startIndex, endIndex, precision);
        var errorScale = GetErrorScale(samples);

        var first = runEvents[startIndex];
        var last  = runEvents[endIndex];

        Nrc.Event<T>? bestCandidate = null;
        var bestError = double.PositiveInfinity;

        for (var easingId = MinEasingId; easingId <= MaxEasingId; easingId++)
        {
            var candidate = CreateCandidateEvent(first, last, easingId);
            if (!TryMeasureCandidateError(candidate, samples, errorScale, tolerance, out var candidateError))
                continue;

            if (candidateError >= bestError)
                continue;

            bestError = candidateError;
            bestCandidate = candidate;
        }

        if (bestCandidate is null)
        {
            fittedEvent = first.Clone();
            return false;
        }

        fittedEvent = bestCandidate;
        return true;
    }

    private static List<(Beat Beat, double Value)> BuildSamples<T>(
        List<Nrc.Event<T>> runEvents,
        int startIndex,
        int endIndex,
        double precision)
    {
        var samples = new List<(Beat Beat, double Value)>();

        for (var index = startIndex; index <= endIndex; index++)
        {
            var currentEvent = runEvents[index];
            AddSample(samples, currentEvent.StartBeat, Convert.ToDouble(currentEvent.StartValue));

            var segmentCount = Math.Max(2, (int)Math.Ceiling((double)(currentEvent.EndBeat - currentEvent.StartBeat) * precision));
            for (var step = 1; step < segmentCount; step++)
            {
                var ratio = step / (double)segmentCount;
                var beat  = LerpBeat(currentEvent.StartBeat, currentEvent.EndBeat, ratio);
                AddSample(samples, beat, Convert.ToDouble(currentEvent.GetValueAtBeat(beat)));
            }

            AddSample(samples, currentEvent.EndBeat, Convert.ToDouble(currentEvent.EndValue));
        }

        return samples;
    }

    private static void AddSample(
        List<(Beat Beat, double Value)> samples,
        Beat beat,
        double value)
    {
        if (samples.Count != 0 && samples[^1].Beat == beat)
        {
            samples[^1] = (beat, value);
            return;
        }

        samples.Add((beat, value));
    }

    private static Beat LerpBeat(Beat startBeat, Beat endBeat, double t)
        => new((double)startBeat + ((double)endBeat - (double)startBeat) * t);

    private static double GetErrorScale(List<(Beat Beat, double Value)> samples)
    {
        var maxAbsValue = samples.Count == 0 ? 0d : samples.Max(sample => Math.Abs(sample.Value));
        return Math.Max(maxAbsValue, 1d);
    }

    private static bool TryMeasureCandidateError<T>(
        Nrc.Event<T> candidate,
        List<(Beat Beat, double Value)> samples,
        double errorScale,
        double tolerance,
        out double normalizedMaxError)
    {
        var allowedError = tolerance / 100d * errorScale;
        var maxError     = 0d;

        foreach (var sample in samples)
        {
            var candidateValue = Convert.ToDouble(candidate.GetValueAtBeat(sample.Beat));
            var error          = Math.Abs(candidateValue - sample.Value);
            if (error > allowedError)
            {
                normalizedMaxError = double.PositiveInfinity;
                return false;
            }

            if (error > maxError)
                maxError = error;
        }

        normalizedMaxError = maxError / errorScale;
        return true;
    }

    private static Nrc.Event<T> CreateCandidateEvent<T>(Nrc.Event<T> first, Nrc.Event<T> last, int easingId)
        => new()
        {
            StartBeat  = new Beat((int[])first.StartBeat),
            EndBeat    = new Beat((int[])last.EndBeat),
            StartValue = first.StartValue,
            EndValue   = last.EndValue,
            Easing     = new Nrc.Easing(easingId),
            Font       = first.Font
        };

    private static bool CanParticipateInFit<T>(Nrc.Event<T> evt)
        => evt.EndBeat > evt.StartBeat &&
           !evt.IsBezier &&
           evt.Easing == 1;

    private static bool CanAppendToFitRun<T>(
        Nrc.Event<T> previousEvent,
        Nrc.Event<T> currentEvent,
        double tolerance)
    {
        if (!CanParticipateInFit(currentEvent))
            return false;
        if (previousEvent.EndBeat != currentEvent.StartBeat)
            return false;
        if (!string.Equals(previousEvent.Font, currentEvent.Font, StringComparison.Ordinal))
            return false;

        return AreClose(Convert.ToDouble(previousEvent.EndValue), Convert.ToDouble(currentEvent.StartValue), tolerance);
    }

    private static bool AreClose(double left, double right, double tolerance)
    {
        var scale = Math.Max(Math.Max(Math.Abs(left), Math.Abs(right)), 1d);
        return Math.Abs(left - right) <= tolerance / 100d * scale;
    }

    private static void EnsureSupportedNumericType<T>()
    {
        if (typeof(T) != typeof(int) && typeof(T) != typeof(float) && typeof(T) != typeof(double))
            throw new NotSupportedException("EventListFit only supports int, float, and double types.");
    }
}