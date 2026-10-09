using KaedePhi.Core.Formats.Phigros.v3.Model;
using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Converter.Phigros.v3;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using IrBlockArea = KaedePhi.Core.Intermediate.Model.BlockArea;
using IrChart = KaedePhi.Core.Intermediate.Model.Chart;
using IrEvent = KaedePhi.Core.Intermediate.Model.Events.Event<double>;
using PhigrosAreaEaseType = KaedePhi.Core.Formats.Phigros.v3.Model.AreaEaseType;
using PhigrosChart = KaedePhi.Core.Formats.Phigros.v3.Model.Chart;
using PhigrosJudgeLine = KaedePhi.Core.Formats.Phigros.v3.Model.JudgeLine;
using Xunit;

namespace KaedePhi.Tests.Converter;

public class PhigrosV3BlockAreaConversionTests
{
    [Fact]
    public void BlockAreaRoundTripUsesBeatEventsAndActualCurveNames()
    {
        var source = new PhigrosChart
        {
            JudgeLineList = [new PhigrosJudgeLine { Bpm = 120f }],
            BlockAreaList =
            [
                new BlockArea
                {
                    TopRightPercentage = new PositionUnit { X = 1.5f, Y = 1.2f },
                    BottomLeftPercentage = new PositionUnit { X = 0.5f, Y = -0.2f },
                    AppearTime = 1f,
                    EnableTime = 2f,
                    DisableTime = 3f,
                    DisappearTime = 4f,
                    IsSubtract = true,
                    MoveEvents =
                    [
                        new AreaMoveEvent
                        {
                            Time = 1.5f,
                            EndPosition = new PositionUnit { X = 0.25f, Y = 0.75f },
                            EaseTypeX = PhigrosAreaEaseType.EaseInQuad,
                            EaseTypeY = PhigrosAreaEaseType.EaseOutQuad,
                        },
                    ],
                    RotateEvents =
                    [
                        new AreaRotateEvent
                        {
                            Time = 1.5f,
                            Anchor = new PositionUnit { X = 0.4f, Y = 0.6f },
                            Rotation = 45f,
                            EaseType = PhigrosAreaEaseType.EaseInCubic,
                        },
                    ],
                    ScaleEvents =
                    [
                        new AreaScaleEvent
                        {
                            Time = 1.5f,
                            Anchor = new PositionUnit { X = 0.6f, Y = 0.4f },
                            Scale = new PositionUnit { X = 2f, Y = 1.5f },
                            EaseTypeX = PhigrosAreaEaseType.EaseInQuart,
                            EaseTypeY = PhigrosAreaEaseType.EaseOutQuint,
                        },
                    ],
                },
            ],
        };

        var converter = new PhigrosV3Converter();
        var intermediate = converter.ToIr(source, null);
        var area = Assert.Single(intermediate.BlockAreaList);
        var moveXEvent = area.MoveXEvents![^1];
        var moveYEvent = area.MoveYEvents![^1];

        Assert.Equal(2d, (double)area.AppearBeat, 6);
        Assert.Equal(4d, (double)area.EnableBeat, 6);
        Assert.Equal(6d, (double)area.DisableBeat, 6);
        Assert.Equal(8d, (double)area.DisappearBeat, 6);
        Assert.Equal(2d, area.TopRightX, 6);
        Assert.Equal(1.4d, area.TopRightY, 6);
        Assert.Equal(2, area.MoveXEvents.Count);
        Assert.Equal(-0.5d, moveXEvent.EndValue, 6);
        Assert.Equal(0.5d, moveYEvent.EndValue, 6);
        Assert.Equal(2, area.RotateEvents!.Count);
        Assert.Equal(2, area.ScaleXEvents!.Count);
        Assert.Equal(2, area.ScaleYEvents!.Count);
        Assert.Equal(45d, area.RotateEvents[^1].EndValue, 6);
        Assert.Equal(2d, area.ScaleXEvents[^1].EndValue, 6);
        Assert.Equal(1.5d, area.ScaleYEvents[^1].EndValue, 6);

        var converted = converter.FromIr(intermediate, new IrToPhigrosV3ConvertOptions());
        var result = Assert.Single(converted.BlockAreaList);

        Assert.Equal(source.BlockAreaList[0].AppearTime, result.AppearTime, 6);
        Assert.Equal(source.BlockAreaList[0].EnableTime, result.EnableTime, 6);
        Assert.Equal(source.BlockAreaList[0].DisableTime, result.DisableTime, 6);
        Assert.Equal(source.BlockAreaList[0].DisappearTime, result.DisappearTime, 6);
        Assert.Equal(source.BlockAreaList[0].TopRightPercentage, result.TopRightPercentage);
        Assert.Equal(source.BlockAreaList[0].BottomLeftPercentage, result.BottomLeftPercentage);
        Assert.Equal(3, result.MoveEvents.Count);
        Assert.Equal(
            source.BlockAreaList[0].MoveEvents[0].EndPosition,
            result.MoveEvents[^1].EndPosition
        );
        Assert.Equal(PhigrosAreaEaseType.One, result.MoveEvents[^1].EaseTypeX.Type);
        Assert.Equal(3, result.RotateEvents.Count);
        Assert.Equal(
            source.BlockAreaList[0].RotateEvents[0].Rotation,
            result.RotateEvents[^1].Rotation
        );
        Assert.Equal(PhigrosAreaEaseType.One, result.RotateEvents[^1].EaseType.Type);
        Assert.Equal(3, result.ScaleEvents.Count);
        Assert.Equal(
            source.BlockAreaList[0].ScaleEvents[0].Scale,
            result.ScaleEvents[^1].Scale
        );
        Assert.Equal(PhigrosAreaEaseType.One, result.ScaleEvents[^1].EaseTypeY.Type);
    }

    [Fact]
    public void ZeroAndOneCurvesBecomeIntervalAndStepEventsInIr()
    {
        var source = new PhigrosChart
        {
            JudgeLineList = [new PhigrosJudgeLine { Bpm = 120f }],
            BlockAreaList =
            [
                new BlockArea
                {
                    TopRightPercentage = new PositionUnit { X = 1f, Y = 1f },
                    BottomLeftPercentage = new PositionUnit { X = 0f, Y = 0f },
                    MoveEvents =
                    [
                        new AreaMoveEvent
                        {
                            Time = 1f,
                            EndPosition = new PositionUnit { X = 0.25f, Y = 0.75f },
                            EaseTypeX = PhigrosAreaEaseType.Zero,
                            EaseTypeY = PhigrosAreaEaseType.One,
                        },
                    ],
                },
            ],
        };

        var area = Assert.Single(new PhigrosV3Converter().ToIr(source, null).BlockAreaList);
        var moveXEvents = area.MoveXEvents!;
        var moveYEvents = area.MoveYEvents!;

        Assert.Equal(2, moveXEvents.Count);
        Assert.Equal(2, moveYEvents.Count);
        Assert.Equal(0d, EvaluateIrTrack(moveXEvents, new Beat(1), 0d), 6);
        Assert.Equal(-0.5d, EvaluateIrTrack(moveXEvents, new Beat(2), 0d), 6);
        Assert.Equal(0d, EvaluateIrTrack(moveYEvents, new Beat(1), 0d), 6);
        Assert.Equal(0.5d, EvaluateIrTrack(moveYEvents, new Beat(2), 0d), 6);
        Assert.Equal(new Beat(2), moveXEvents[^1].StartBeat);
        Assert.Equal(new Beat(2), moveXEvents[^1].EndBeat);
    }

    [Fact]
    public void BlockAreaTransformFramesInterpolateFromTheLeftAndHoldItsAbsoluteAnchor()
    {
        var source = new PhigrosChart
        {
            JudgeLineList = [new PhigrosJudgeLine { Bpm = 120f }],
            BlockAreaList =
            [
                new BlockArea
                {
                    TopRightPercentage = new PositionUnit { X = 0.75f, Y = 0.75f },
                    BottomLeftPercentage = new PositionUnit { X = 0.25f, Y = 0.25f },
                    RotateEvents =
                    [
                        new()
                        {
                            Time = 0f,
                            Anchor = new PositionUnit { X = 0.25f, Y = 0.5f },
                            Rotation = 0f,
                            EaseType = PhigrosAreaEaseType.EaseInQuad,
                        },
                        new()
                        {
                            Time = 2f,
                            Anchor = new PositionUnit { X = 0.75f, Y = 0.5f },
                            Rotation = 90f,
                            EaseType = PhigrosAreaEaseType.EaseOutQuad,
                        },
                    ],
                    ScaleEvents =
                    [
                        new AreaScaleEvent
                        {
                            Time = 0f,
                            Anchor = new PositionUnit { X = 0.25f, Y = 0.5f },
                            Scale = new PositionUnit { X = 1f, Y = 1f },
                            EaseTypeX = PhigrosAreaEaseType.EaseInQuad,
                            EaseTypeY = PhigrosAreaEaseType.Linear,
                        },
                        new AreaScaleEvent
                        {
                            Time = 2f,
                            Anchor = new PositionUnit { X = 0.75f, Y = 0.5f },
                            Scale = new PositionUnit { X = 2f, Y = 2f },
                            EaseTypeX = PhigrosAreaEaseType.EaseOutQuad,
                            EaseTypeY = PhigrosAreaEaseType.Linear,
                        },
                    ],
                },
            ],
        };

        var area = Assert.Single(new PhigrosV3Converter().ToIr(source, null).BlockAreaList);
        var midpoint = new Beat(2);
        var secondFrame = new Beat(4);

        Assert.Equal(
            Transform.ToIrX(0.25f),
            EvaluateIrTrack(area.ScaleAnchorXEvents, midpoint, 0d),
            6
        );
        Assert.Equal(
            Transform.ToIrX(0.75f),
            EvaluateIrTrack(area.ScaleAnchorXEvents, secondFrame, 0d),
            6
        );
        Assert.Equal(
            Transform.ToIrX(0.25f),
            EvaluateIrTrack(area.RotateAnchorXEvents, midpoint, 0d),
            6
        );
        Assert.Equal(
            Transform.ToIrX(0.75f),
            EvaluateIrTrack(area.RotateAnchorXEvents, secondFrame, 0d),
            6
        );
        Assert.Equal(1.25d, EvaluateIrTrack(area.ScaleXEvents, midpoint, 1d), 6);
        Assert.Equal(22.5d, EvaluateIrTrack(area.RotateEvents, midpoint, 0d), 6);
    }

    [Fact]
    public void FromIrPreservesThePreviousEndpointBeforeADiscontinuousCurveStart()
    {
        var source = new IrChart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            BlockAreaList =
            [
                new IrBlockArea
                {
                    TopRightX = 1d,
                    BottomLeftX = -1d,
                    MoveXEvents =
                    [
                        new IrEvent
                        {
                            StartBeat = new Beat(0),
                            EndBeat = new Beat(1),
                            StartValue = 0d,
                            EndValue = 0.2d,
                            Easing = new(5),
                        },
                        new IrEvent
                        {
                            StartBeat = new Beat(1),
                            EndBeat = new Beat(2),
                            StartValue = 0.4d,
                            EndValue = 0.6d,
                            Easing = new(6),
                        },
                    ],
                },
            ],
        };
        var options = new IrToPhigrosV3ConvertOptions();
        options.Cutting.EasingPrecision = 64d;

        var converted = new PhigrosV3Converter().FromIr(source, options);
        var moves = Assert.Single(converted.BlockAreaList).MoveEvents;

        Assert.Equal(5, moves.Count);
        Assert.Equal(0f, moves[0].Time, 6);
        Assert.Equal(PhigrosAreaEaseType.EaseInQuad, moves[0].EaseTypeX.Type);
        Assert.Equal(0.5f, moves[1].Time, 6);
        Assert.Equal(0.6f, moves[1].EndPosition.X, 6);
        Assert.Equal(PhigrosAreaEaseType.One, moves[1].EaseTypeX.Type);
        Assert.Equal(0.5078125f, moves[2].Time, 6);
        Assert.Equal(0.6f, moves[2].EndPosition.X, 6);
        Assert.Equal(PhigrosAreaEaseType.One, moves[2].EaseTypeX.Type);
        Assert.Equal(moves[2].Time, moves[3].Time);
        Assert.Equal(0.7f, moves[3].EndPosition.X, 6);
        Assert.Equal(PhigrosAreaEaseType.EaseOutQuad, moves[3].EaseTypeX.Type);
        Assert.Equal(1f, moves[4].Time, 6);
        Assert.Equal(0.8f, moves[4].EndPosition.X, 6);
        Assert.Equal(PhigrosAreaEaseType.One, moves[4].EaseTypeX.Type);
    }

    [Fact]
    public void FromIrPreservesAnExplicitInstantStepAtThePreviousEndpoint()
    {
        var source = new IrChart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            BlockAreaList =
            [
                new IrBlockArea
                {
                    TopRightX = 1d,
                    BottomLeftX = -1d,
                    MoveXEvents =
                    [
                        new IrEvent
                        {
                            StartBeat = new Beat(0),
                            EndBeat = new Beat(1),
                            StartValue = 0d,
                            EndValue = 0.2d,
                        },
                        new IrEvent
                        {
                            StartBeat = new Beat(1),
                            EndBeat = new Beat(1),
                            StartValue = 0.2d,
                            EndValue = 0.4d,
                        },
                        new IrEvent
                        {
                            StartBeat = new Beat(1),
                            EndBeat = new Beat(2),
                            StartValue = 0.4d,
                            EndValue = 0.6d,
                            Easing = new(6),
                        },
                    ],
                },
            ],
        };

        var converted = new PhigrosV3Converter().FromIr(
            source,
            new IrToPhigrosV3ConvertOptions()
        );
        var moves = Assert.Single(converted.BlockAreaList).MoveEvents;

        Assert.Equal(4, moves.Count);
        Assert.Equal(0f, moves[0].Time, 6);
        Assert.Equal(0.5f, moves[1].Time, 6);
        Assert.Equal(0.6f, moves[1].EndPosition.X, 6);
        Assert.Equal(PhigrosAreaEaseType.One, moves[1].EaseTypeX.Type);
        Assert.Equal(moves[1].Time, moves[2].Time);
        Assert.Equal(0.7f, moves[2].EndPosition.X, 6);
        Assert.Equal(PhigrosAreaEaseType.EaseOutQuad, moves[2].EaseTypeX.Type);
        Assert.Equal(1f, moves[3].Time, 6);
        Assert.Equal(0.8f, moves[3].EndPosition.X, 6);
        Assert.Equal(PhigrosAreaEaseType.One, moves[3].EaseTypeX.Type);
    }

    [Fact]
    public void ChartCloneDeepCopiesBlockAreaIntervals()
    {
        var source = new IrChart
        {
            BlockAreaList =
            [
                new IrBlockArea
                {
                    MoveXEvents =
                    [
                        new IrEvent
                        {
                            StartBeat = new Beat(0),
                            EndBeat = new Beat(1),
                            StartValue = 0d,
                            EndValue = 1d,
                        },
                    ],
                },
            ],
        };

        var clone = source.Clone();
        var sourceEvents = source.BlockAreaList[0].MoveXEvents!;
        var clonedEvents = clone.BlockAreaList[0].MoveXEvents!;
        clonedEvents[0].EndValue = 2d;

        Assert.Equal(1d, sourceEvents[0].EndValue);
        Assert.Equal(2d, clonedEvents[0].EndValue);
    }

    [Fact]
    public void FromIrUsesDefaultBpmForAreasWhenTheChartHasNoBpmList()
    {
        var source = new IrChart
        {
            BlockAreaList =
            [
                new IrBlockArea
                {
                    TopRightX = 1d,
                    BottomLeftX = -1d,
                    AppearBeat = new Beat(2),
                    MoveXEvents =
                    [
                        new IrEvent
                        {
                            StartBeat = new Beat(0),
                            EndBeat = new Beat(2),
                            StartValue = 0d,
                            EndValue = 1d,
                        },
                    ],
                },
            ],
        };
        var options = new IrToPhigrosV3ConvertOptions { DefaultBpm = 60f };

        var converted = new PhigrosV3Converter().FromIr(source, options);
        var area = Assert.Single(converted.BlockAreaList);

        Assert.Empty(converted.JudgeLineList);
        Assert.Equal(2f, area.AppearTime);
        Assert.Equal(2, area.MoveEvents.Count);
        Assert.Equal(0f, area.MoveEvents[0].Time);
        Assert.Equal(2f, area.MoveEvents[^1].Time);
        Assert.Equal(1f, area.MoveEvents[^1].EndPosition.X);
    }

    [Fact]
    public void FromIrMapsAreaTimesAcrossTempoChanges()
    {
        var source = new IrChart
        {
            BpmList =
            [
                new() { Bpm = 120f, StartBeat = new Beat(0) },
                new() { Bpm = 60f, StartBeat = new Beat(4) },
            ],
            BlockAreaList = [new IrBlockArea { AppearBeat = new Beat(6) }],
        };

        var converted = new PhigrosV3Converter().FromIr(
            source,
            new IrToPhigrosV3ConvertOptions()
        );

        Assert.Equal(4f, Assert.Single(converted.BlockAreaList).AppearTime);
    }

    [Fact]
    public void FromIrSamplesCurvesUnsupportedByTheBlockAreaFormat()
    {
        var source = new IrChart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            BlockAreaList =
            [
                new IrBlockArea
                {
                    TopRightX = 1d,
                    BottomLeftX = -1d,
                    MoveXEvents =
                    [
                        new IrEvent
                        {
                            StartBeat = new Beat(0),
                            EndBeat = new Beat(1),
                            StartValue = 0d,
                            EndValue = 1d,
                            Easing = new(2),
                        },
                    ],
                },
            ],
        };
        var warnings = new List<string>();
        var converter = new PhigrosV3Converter { OnWarning = warnings.Add };
        var options = new IrToPhigrosV3ConvertOptions();
        options.Cutting.EasingPrecision = 2d;

        var converted = converter.FromIr(source, options);
        var area = Assert.Single(converted.BlockAreaList);

        Assert.Equal(3, area.MoveEvents.Count);
        Assert.Equal(PhigrosAreaEaseType.Linear, area.MoveEvents[0].EaseTypeX.Type);
        Assert.Equal(PhigrosAreaEaseType.Linear, area.MoveEvents[1].EaseTypeX.Type);
        Assert.Equal(PhigrosAreaEaseType.One, area.MoveEvents[^1].EaseTypeX.Type);
        Assert.Contains(warnings, message => message.Contains("线性事件"));
    }

    private static double EvaluateIrTrack(
        List<IrEvent>? events,
        Beat beat,
        double defaultValue
    )
    {
        var dominant = events?.LastOrDefault(evt => evt.StartBeat <= beat);
        if (dominant is null)
            return defaultValue;
        return beat <= dominant.EndBeat
            ? dominant.GetValueAtBeatAsDouble(beat)
            : dominant.EndValue;
    }
}
