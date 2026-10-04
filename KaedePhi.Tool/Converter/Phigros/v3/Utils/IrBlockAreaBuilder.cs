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
            MoveXEvents = ConvertEvents(
                source.MoveEvents,
                evt => evt.Time,
                evt => ToIrX(evt.EndPosition.X),
                evt => evt.EaseTypeX.Value,
                centerX,
                timeMapper
            ),
            MoveYEvents = ConvertEvents(
                source.MoveEvents,
                evt => evt.Time,
                evt => ToIrY(evt.EndPosition.Y),
                evt => evt.EaseTypeY.Value,
                centerY,
                timeMapper
            ),
            RotateEvents = ConvertEvents(
                source.RotateEvents,
                evt => evt.Time,
                evt => ToIrAngle(evt.Rotation),
                evt => evt.EaseType.Value,
                0d,
                timeMapper
            ),
            RotateAnchorXEvents = ConvertEvents(
                source.RotateEvents,
                evt => evt.Time,
                evt => ToIrX(evt.Anchor.X),
                evt => evt.EaseType.Value,
                centerX,
                timeMapper
            ),
            RotateAnchorYEvents = ConvertEvents(
                source.RotateEvents,
                evt => evt.Time,
                evt => ToIrY(evt.Anchor.Y),
                evt => evt.EaseType.Value,
                centerY,
                timeMapper
            ),
            ScaleXEvents = ConvertEvents(
                source.ScaleEvents,
                evt => evt.Time,
                evt => ToFiniteDouble(evt.Scale.X),
                evt => evt.EaseTypeX.Value,
                1d,
                timeMapper
            ),
            ScaleYEvents = ConvertEvents(
                source.ScaleEvents,
                evt => evt.Time,
                evt => ToFiniteDouble(evt.Scale.Y),
                evt => evt.EaseTypeY.Value,
                1d,
                timeMapper
            ),
            ScaleAnchorXEvents = ConvertEvents(
                source.ScaleEvents,
                evt => evt.Time,
                evt => ToIrX(evt.Anchor.X),
                evt => evt.EaseTypeX.Value,
                centerX,
                timeMapper
            ),
            ScaleAnchorYEvents = ConvertEvents(
                source.ScaleEvents,
                evt => evt.Time,
                evt => ToIrY(evt.Anchor.Y),
                evt => evt.EaseTypeY.Value,
                centerY,
                timeMapper
            ),
        };
    }

    private static List<IrEvent>? ConvertEvents<TSource>(
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
        var previousBeat = new Beat(0);
        var previousValue = defaultValue;

        for (var index = 0; index < ordered.Count; index++)
        {
            var sourceEvent = ordered[index].Event;
            var endBeat = timeMapper.ToBeat(timeSelector(sourceEvent));
            var startBeat = previousBeat;
            if (index == 0 && endBeat < startBeat)
                startBeat = endBeat;

            var endValue = valueSelector(sourceEvent);
            if (!double.IsFinite(endValue))
                throw new FormatException("PhigrosV3 噪域事件值必须是有限数值。");

            AddEvent(
                result,
                startBeat,
                endBeat,
                previousValue,
                endValue,
                easingSelector(sourceEvent)
            );
            previousBeat = endBeat;
            previousValue = endValue;
        }

        return result.Count == 0 ? null : result;
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
        if (!float.IsFinite(value))
            throw new FormatException("PhigrosV3 噪域值必须是有限数值。");
        return value;
    }

    private static float ToFiniteFloat(float value) =>
        float.IsFinite(value)
            ? value
            : throw new FormatException("PhigrosV3 噪域坐标必须是有限数值。");
}
