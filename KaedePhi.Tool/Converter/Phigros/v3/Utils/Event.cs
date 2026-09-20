using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Common;
using PhigrosEvent = KaedePhi.Core.Formats.Phigros.v3.Event;
using PhigrosJudgeLine = KaedePhi.Core.Formats.Phigros.v3.JudgeLine;
using PhigrosNoteType = KaedePhi.Core.Formats.Phigros.v3.NoteType;
using PhigrosSpeedEvent = KaedePhi.Core.Formats.Phigros.v3.SpeedEvent;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

/// <summary>
/// PhigrosV3 事件到 IR 事件的转换工具。
/// </summary>
public static class EventBuilder
{
    private const double TrailingBeatPadding = 1d / 64d;

    public static List<IrEvents.Event<T>>? ConvertEvents<T>(
        List<PhigrosEvent>? events,
        double horizonBeat,
        Func<float, T> valueTransformer
    )
        where T : notnull
    {
        if (events is not { Count: > 0 })
            return null;

        var sorted = events.OrderBy(e => e.StartTime).ToList();
        var result = new List<IrEvents.Event<T>>();

        foreach (var ev in sorted)
        {
            var startBeat = Math.Max(0d, ev.StartTime / 32.0);
            var endBeat = ev.EndTime / 32.0;
            if (endBeat <= startBeat)
                continue;

            var startValue = valueTransformer(ev.Start);
            var endValue = valueTransformer(ev.End);
            if (
                EqualityComparer<T>.Default.Equals(startValue, endValue)
                && endBeat - startBeat > 1d
            )
                endBeat = startBeat + 1d;
            result.Add(CreateLinearEvent(startBeat, endBeat, startValue, endValue));
        }

        if (result.Count == 0)
            return null;
        AddTrailingEvent(result, horizonBeat);
        return result;
    }

    public static List<IrEvents.Event<double>>? ConvertMoveAxisEvents(
        List<PhigrosEvent>? events,
        double horizonBeat,
        Func<PhigrosEvent, float> startSelector,
        Func<PhigrosEvent, float> endSelector,
        Func<float, double> valueTransformer
    )
    {
        if (events is not { Count: > 0 })
            return null;

        var sorted = events.OrderBy(e => e.StartTime).ToList();
        var result = new List<IrEvents.Event<double>>();

        foreach (var ev in sorted)
        {
            var startBeat = Math.Max(0d, ev.StartTime / 32.0);
            var endBeat = ev.EndTime / 32.0;
            if (endBeat <= startBeat)
                continue;

            var startValue = valueTransformer(startSelector(ev));
            var endValue = valueTransformer(endSelector(ev));
            if (
                Math.Abs(startValue - endValue) < Constants.FloatEpsilon
                && endBeat - startBeat > 1d
            )
                endBeat = startBeat + 1d;
            result.Add(CreateLinearEvent(startBeat, endBeat, startValue, endValue));
        }

        if (result.Count == 0)
            return null;
        AddTrailingEvent(result, horizonBeat);
        return result;
    }

    public static List<IrEvents.Event<float>>? ConvertSpeedEvents(
        List<PhigrosSpeedEvent>? events,
        double horizonBeat
    )
    {
        if (events is not { Count: > 0 })
            return null;

        var sorted = events.OrderBy(e => e.StartTime).ToList();
        var result = new List<IrEvents.Event<float>>();

        foreach (var ev in sorted)
        {
            var startBeat = Math.Max(0d, ev.StartTime / 32.0);
            var endBeat = ev.EndTime / 32.0;
            var speedValue = (float)(ev.Value * Constants.SpeedValueRatio);
            if (endBeat <= startBeat)
                continue;

            if (endBeat - startBeat > 1d)
                endBeat = startBeat + 1d;
            result.Add(CreateLinearEvent(startBeat, endBeat, speedValue, speedValue));
        }

        if (result.Count == 0)
            return null;
        AddTrailingEvent(result, horizonBeat);
        return result;
    }

    public static double GetJudgeLineHorizonBeat(PhigrosJudgeLine src)
    {
        var maxBeat = 0d;
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.NotesAbove.Select(n => (double)n.Time / 32)));
        maxBeat = Math.Max(maxBeat, GetMaxBeat(src.NotesBelow.Select(n => (double)n.Time / 32)));
        maxBeat = Math.Max(
            maxBeat,
            GetMaxBeat(
                src.NotesAbove.Where(n => n.Type == PhigrosNoteType.Hold)
                    .Select(n => (double)(n.Time + n.HoldTime) / 32)
            )
        );
        maxBeat = Math.Max(
            maxBeat,
            GetMaxBeat(
                src.NotesBelow.Where(n => n.Type == PhigrosNoteType.Hold)
                    .Select(n => (double)(n.Time + n.HoldTime) / 32)
            )
        );
        maxBeat = Math.Max(
            maxBeat,
            GetMaxBeat(
                src.SpeedEvents.Select(e =>
                    Math.Min((double)e.EndTime / 32, Math.Max(0d, (double)e.StartTime / 32) + 1)
                )
            )
        );
        maxBeat = Math.Max(
            maxBeat,
            GetMaxBeat(
                src.JudgeLineMoveEvents.Select(e =>
                    Math.Abs(e.Start - e.End) < Constants.FloatEpsilon
                        ? Math.Min(
                            (double)e.EndTime / 32,
                            Math.Max(0d, (double)e.StartTime / 32) + 1
                        )
                        : (double)e.EndTime / 32
                )
            )
        );
        maxBeat = Math.Max(
            maxBeat,
            GetMaxBeat(
                src.JudgeLineRotateEvents.Select(e =>
                    Math.Abs(e.Start - e.End) < Constants.FloatEpsilon
                        ? Math.Min(
                            (double)e.EndTime / 32,
                            Math.Max(0d, (double)e.StartTime / 32) + 1
                        )
                        : (double)e.EndTime / 32
                )
            )
        );
        maxBeat = Math.Max(
            maxBeat,
            GetMaxBeat(
                src.JudgeLineDisappearEvents.Select(e =>
                    Math.Abs(e.Start - e.End) < Constants.FloatEpsilon
                        ? Math.Min(
                            (double)e.EndTime / 32,
                            Math.Max(0d, (double)e.StartTime / 32) + 1
                        )
                        : (double)e.EndTime / 32
                )
            )
        );
        return maxBeat + TrailingBeatPadding;
    }

    private static IrEvents.Event<T> CreateLinearEvent<T>(
        double startBeat,
        double endBeat,
        T startValue,
        T endValue
    )
        where T : notnull =>
        new()
        {
            StartBeat = new Beat(startBeat),
            EndBeat = new Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue,
        };

    private static void AddTrailingEvent<T>(List<IrEvents.Event<T>> events, double horizonBeat)
        where T : notnull
    {
        var lastEnd = (double)events[^1].EndBeat;
        var trailingEnd = Math.Max(horizonBeat, lastEnd + TrailingBeatPadding);
        if (trailingEnd <= lastEnd)
            return;

        events.Add(
            CreateLinearEvent(lastEnd, trailingEnd, events[^1].EndValue, events[^1].EndValue)
        );
    }

    private static double GetMaxBeat(IEnumerable<double>? beats) =>
        beats?.DefaultIfEmpty(0d).Max() ?? 0d;
}
