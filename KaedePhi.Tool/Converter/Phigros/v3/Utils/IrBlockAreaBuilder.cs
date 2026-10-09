using KaedePhi.Core.Primitives;
using IrBlockArea = KaedePhi.Core.Intermediate.Model.BlockArea;
using IrEasing = KaedePhi.Core.Intermediate.Model.Easing;
using IrEvent = KaedePhi.Core.Intermediate.Model.Events.Event<double>;
using PhigrosAreaEaseType = KaedePhi.Core.Formats.Phigros.v3.Model.AreaEaseType;
using PhigrosBlockArea = KaedePhi.Core.Formats.Phigros.v3.Model.BlockArea;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

/// <summary>
/// PhigrosV3 噪域到 IR 噪域的转换工具。
/// </summary>
internal static class IrBlockAreaBuilder
{
    internal static List<IrBlockArea> ConvertBlockAreas(
        IReadOnlyList<PhigrosBlockArea> blockAreas,
        PhigrosV3TimeMapper timeMapper,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(blockAreas);
        ArgumentNullException.ThrowIfNull(timeMapper);

        var result = new List<IrBlockArea>(blockAreas.Count);
        foreach (var blockArea in blockAreas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (blockArea is null)
                throw new FormatException("PhigrosV3 噪域列表不能包含 null。");
            result.Add(ConvertBlockArea(blockArea, timeMapper));
        }

        return result;
    }

    private static IrBlockArea ConvertBlockArea(
        PhigrosBlockArea source,
        PhigrosV3TimeMapper timeMapper
    )
    {
        var centerX = ToIrX((source.TopRightPercentage.X + source.BottomLeftPercentage.X) / 2f);
        var centerY = ToIrY((source.TopRightPercentage.Y + source.BottomLeftPercentage.Y) / 2f);

        return new IrBlockArea
        {
            TopRightX = ToIrX(source.TopRightPercentage.X),
            TopRightY = ToIrY(source.TopRightPercentage.Y),
            BottomLeftX = ToIrX(source.BottomLeftPercentage.X),
            BottomLeftY = ToIrY(source.BottomLeftPercentage.Y),
            AppearBeat = timeMapper.ToBeat(source.AppearTime),
            EnableBeat = timeMapper.ToBeat(source.EnableTime),
            DisableBeat = timeMapper.ToBeat(source.DisableTime),
            DisappearBeat = timeMapper.ToBeat(source.DisappearTime),
            IsSubtract = source.IsSubtract,
            MoveXEvents = ConvertLeftKeyframeEvents(
                source.MoveEvents,
                evt => evt.Time,
                evt => ToIrX(evt.EndPosition.X),
                evt => evt.EaseTypeX.Value,
                centerX,
                timeMapper
            ),
            MoveYEvents = ConvertLeftKeyframeEvents(
                source.MoveEvents,
                evt => evt.Time,
                evt => ToIrY(evt.EndPosition.Y),
                evt => evt.EaseTypeY.Value,
                centerY,
                timeMapper
            ),
            RotateEvents = ConvertLeftKeyframeEvents(
                source.RotateEvents,
                evt => evt.Time,
                evt => ToIrAngle(evt.Rotation),
                evt => evt.EaseType.Value,
                0d,
                timeMapper
            ),
            RotateAnchorXEvents = ConvertAnchorEvents(
                source.RotateEvents,
                evt => evt.Time,
                evt => ToIrX(evt.Anchor.X),
                centerX,
                timeMapper
            ),
            RotateAnchorYEvents = ConvertAnchorEvents(
                source.RotateEvents,
                evt => evt.Time,
                evt => ToIrY(evt.Anchor.Y),
                centerY,
                timeMapper
            ),
            ScaleXEvents = ConvertLeftKeyframeEvents(
                source.ScaleEvents,
                evt => evt.Time,
                evt => ToFiniteDouble(evt.Scale.X),
                evt => evt.EaseTypeX.Value,
                1d,
                timeMapper
            ),
            ScaleYEvents = ConvertLeftKeyframeEvents(
                source.ScaleEvents,
                evt => evt.Time,
                evt => ToFiniteDouble(evt.Scale.Y),
                evt => evt.EaseTypeY.Value,
                1d,
                timeMapper
            ),
            ScaleAnchorXEvents = ConvertAnchorEvents(
                source.ScaleEvents,
                evt => evt.Time,
                evt => ToIrX(evt.Anchor.X),
                centerX,
                timeMapper
            ),
            ScaleAnchorYEvents = ConvertAnchorEvents(
                source.ScaleEvents,
                evt => evt.Time,
                evt => ToIrY(evt.Anchor.Y),
                centerY,
                timeMapper
            ),
        };
    }

    private static List<IrEvent>? ConvertLeftKeyframeEvents<TSource>(
        IReadOnlyList<TSource>? sourceEvents,
        Func<TSource, float> timeSelector,
        Func<TSource, double> valueSelector,
        Func<TSource, int> easingSelector,
        double defaultValue,
        PhigrosV3TimeMapper timeMapper
    )
        where TSource : class
    {
        if (sourceEvents is not { Count: > 0 })
            return null;
        if (sourceEvents.Any(evt => evt is null))
            throw new FormatException("PhigrosV3 噪域事件列表不能包含 null。");

        var ordered = sourceEvents
            .Select((evt, index) => (Event: evt, Index: index))
            .OrderBy(item => timeSelector(item.Event))
            .ThenBy(item => item.Index)
            .ToList();
        var result = new List<IrEvent>();
        var firstBeat = timeMapper.ToBeat(timeSelector(ordered[0].Event));
        if (firstBeat > new Beat(0))
            result.Add(CreateEvent(new Beat(0), firstBeat, defaultValue, defaultValue, 1));

        for (var index = 0; index < ordered.Count - 1; index++)
        {
            var leftEvent = ordered[index].Event;
            var rightEvent = ordered[index + 1].Event;
            var startBeat = timeMapper.ToBeat(timeSelector(leftEvent));
            var endBeat = timeMapper.ToBeat(timeSelector(rightEvent));
            var startValue = valueSelector(leftEvent);
            var endValue = valueSelector(rightEvent);
            if (!double.IsFinite(startValue) || !double.IsFinite(endValue))
                throw new FormatException("PhigrosV3 噪域事件值必须是有限数值。");

            AddEvent(
                result,
                startBeat,
                endBeat,
                startValue,
                endValue,
                easingSelector(leftEvent)
            );
        }

        var lastEvent = ordered[^1].Event;
        var lastBeat = timeMapper.ToBeat(timeSelector(lastEvent));
        var lastValue = valueSelector(lastEvent);
        if (!double.IsFinite(lastValue))
            throw new FormatException("PhigrosV3 噪域事件值必须是有限数值。");
        result.Add(CreateEvent(lastBeat, lastBeat, lastValue, lastValue, 1));

        return result.Count == 0 ? null : result;
    }

    private static List<IrEvent>? ConvertAnchorEvents<TSource>(
        IReadOnlyList<TSource>? sourceEvents,
        Func<TSource, float> timeSelector,
        Func<TSource, double> valueSelector,
        double defaultValue,
        PhigrosV3TimeMapper timeMapper
    )
        where TSource : class
    {
        if (sourceEvents is not { Count: > 0 })
            return null;
        if (sourceEvents.Any(evt => evt is null))
            throw new FormatException("PhigrosV3 噪域事件列表不能包含 null。");

        var ordered = sourceEvents
            .Select((evt, index) => (Event: evt, Index: index))
            .OrderBy(item => timeSelector(item.Event))
            .ThenBy(item => item.Index)
            .ToList();
        var result = new List<IrEvent>();
        var previousBeat = new Beat(0);
        var previousValue = defaultValue;

        for (var index = 0; index < ordered.Count; index++)
        {
            var sourceEvent = ordered[index].Event;
            var keyframeBeat = timeMapper.ToBeat(timeSelector(sourceEvent));
            var startBeat = previousBeat;
            if (index == 0 && keyframeBeat < startBeat)
                startBeat = keyframeBeat;

            var anchorValue = valueSelector(sourceEvent);
            if (!double.IsFinite(anchorValue))
                throw new FormatException("PhigrosV3 噪域锚点必须是有限数值。");

            if (keyframeBeat > startBeat)
                result.Add(
                    CreateEvent(startBeat, keyframeBeat, previousValue, previousValue, 1)
                );
            // 锚点在两帧之间保持左帧的绝对坐标，到新帧时才切换。
            result.Add(CreateEvent(keyframeBeat, keyframeBeat, previousValue, anchorValue, 1));
            previousBeat = keyframeBeat;
            previousValue = anchorValue;
        }

        return result;
    }

    private static void AddEvent(
        List<IrEvent> events,
        Beat startBeat,
        Beat endBeat,
        double startValue,
        double endValue,
        int areaEaseType
    )
    {
        if (endBeat < startBeat)
            throw new FormatException("PhigrosV3 噪域事件时间必须按顺序递增。");

        switch (areaEaseType)
        {
            case (int)PhigrosAreaEaseType.Zero:
                if (endBeat > startBeat)
                    events.Add(CreateEvent(startBeat, endBeat, startValue, startValue, 1));
                if (Math.Abs(startValue - endValue) > Common.Constants.FloatEpsilon)
                    events.Add(CreateEvent(endBeat, endBeat, startValue, endValue, 1));
                break;
            case (int)PhigrosAreaEaseType.One:
                if (endBeat > startBeat)
                    events.Add(CreateEvent(startBeat, endBeat, endValue, endValue, 1));
                else if (Math.Abs(startValue - endValue) > Common.Constants.FloatEpsilon)
                    events.Add(CreateEvent(startBeat, startBeat, startValue, endValue, 1));
                break;
            default:
                events.Add(
                    CreateEvent(
                        startBeat,
                        endBeat,
                        startValue,
                        endValue,
                        ToIntermediateEasing(areaEaseType)
                    )
                );
                break;
        }
    }

    private static IrEvent CreateEvent(
        Beat startBeat,
        Beat endBeat,
        double startValue,
        double endValue,
        int easing
    ) =>
        new()
        {
            StartBeat = startBeat,
            EndBeat = endBeat,
            StartValue = startValue,
            EndValue = endValue,
            Easing = new IrEasing(easing),
        };

    private static int ToIntermediateEasing(int areaEaseType) =>
        areaEaseType switch
        {
            (int)PhigrosAreaEaseType.Linear => 1,
            (int)PhigrosAreaEaseType.EaseInQuad => 5,
            (int)PhigrosAreaEaseType.EaseOutQuad => 6,
            (int)PhigrosAreaEaseType.EaseInOutQuad => 7,
            (int)PhigrosAreaEaseType.EaseInCubic => 8,
            (int)PhigrosAreaEaseType.EaseOutCubic => 9,
            (int)PhigrosAreaEaseType.EaseInOutCubic => 10,
            (int)PhigrosAreaEaseType.EaseInQuart => 11,
            (int)PhigrosAreaEaseType.EaseOutQuart => 12,
            (int)PhigrosAreaEaseType.EaseInOutQuart => 13,
            (int)PhigrosAreaEaseType.EaseInQuint => 14,
            (int)PhigrosAreaEaseType.EaseOutQuint => 15,
            (int)PhigrosAreaEaseType.EaseInOutQuint => 16,
            _ => throw new FormatException($"PhigrosV3 噪域缓动编号 {areaEaseType} 无效。"),
        };

    private static double ToIrX(float value) =>
        Transform.ToIrX(ToFiniteFloat(value));

    private static double ToIrY(float value) =>
        Transform.ToIrY(ToFiniteFloat(value));

    private static double ToIrAngle(float value) =>
        Transform.ToIrAngle(ToFiniteFloat(value));

    private static double ToFiniteDouble(float value)
    {
        return !float.IsFinite(value) ? throw new FormatException("PhigrosV3 噪域值必须是有限数值。") : value;
    }

    private static float ToFiniteFloat(float value) =>
        float.IsFinite(value)
            ? value
            : throw new FormatException("PhigrosV3 噪域坐标必须是有限数值。");
}
