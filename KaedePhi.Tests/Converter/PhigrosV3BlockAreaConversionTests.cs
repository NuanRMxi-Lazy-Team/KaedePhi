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
        var moveXEvent = Assert.Single(area.MoveXEvents!);
        var moveYEvent = Assert.Single(area.MoveYEvents!);

        Assert.Equal(2d, (double)area.AppearBeat, 6);
        Assert.Equal(4d, (double)area.EnableBeat, 6);
        Assert.Equal(6d, (double)area.DisableBeat, 6);
        Assert.Equal(8d, (double)area.DisappearBeat, 6);
        Assert.Equal(2d, area.TopRightX, 6);
        Assert.Equal(1.4d, area.TopRightY, 6);
        Assert.Equal(-0.5d, moveXEvent.EndValue, 6);
        Assert.Equal(0.5d, moveYEvent.EndValue, 6);
        Assert.Equal(5, (int)moveXEvent.Easing);
        Assert.Equal(8, (int)Assert.Single(area.RotateEvents!).Easing);
        Assert.Equal(11, (int)Assert.Single(area.ScaleXEvents!).Easing);
        Assert.Equal(15, (int)Assert.Single(area.ScaleYEvents!).Easing);

        var converted = converter.FromIr(intermediate, new IrToPhigrosV3ConvertOptions());
        var result = Assert.Single(converted.BlockAreaList);

        Assert.Equal(source.BlockAreaList[0].AppearTime, result.AppearTime, 6);
        Assert.Equal(source.BlockAreaList[0].EnableTime, result.EnableTime, 6);
        Assert.Equal(source.BlockAreaList[0].DisableTime, result.DisableTime, 6);
        Assert.Equal(source.BlockAreaList[0].DisappearTime, result.DisappearTime, 6);
        Assert.Equal(source.BlockAreaList[0].TopRightPercentage, result.TopRightPercentage);
        Assert.Equal(source.BlockAreaList[0].BottomLeftPercentage, result.BottomLeftPercentage);
        Assert.Equal(
            source.BlockAreaList[0].MoveEvents[0].EndPosition,
            Assert.Single(result.MoveEvents).EndPosition
        );
        Assert.Equal(
            source.BlockAreaList[0].MoveEvents[0].EaseTypeX.Type,
            Assert.Single(result.MoveEvents).EaseTypeX.Type
        );
        Assert.Equal(
            source.BlockAreaList[0].RotateEvents[0].EaseType.Type,
            Assert.Single(result.RotateEvents).EaseType.Type
        );
        Assert.Equal(
            source.BlockAreaList[0].ScaleEvents[0].EaseTypeY.Type,
            Assert.Single(result.ScaleEvents).EaseTypeY.Type
        );
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

        Assert.Equal(0d, moveXEvents[0].GetValueAtBeatAsDouble(new Beat(1)), 6);
        Assert.Equal(-0.5d, moveXEvents[1].GetValueAtBeatAsDouble(new Beat(2)), 6);
        Assert.Equal(0.5d, Assert.Single(moveYEvents).GetValueAtBeatAsDouble(new Beat(0)), 6);
        Assert.Equal(0.5d, moveYEvents[0].GetValueAtBeatAsDouble(new Beat(1)), 6);
        Assert.Equal(new Beat(2), moveXEvents[1].StartBeat);
        Assert.Equal(new Beat(2), moveXEvents[1].EndBeat);
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
                        },
                    ],
                },
            ],
        };
        var options = new IrToPhigrosV3ConvertOptions();
        options.Cutting.EasingPrecision = 64d;

        var converted = new PhigrosV3Converter().FromIr(source, options);
        var moves = Assert.Single(converted.BlockAreaList).MoveEvents;

        Assert.Equal(4, moves.Count);
        Assert.Equal(0.5f, moves[0].Time, 6);
        Assert.Equal(0.6f, moves[0].EndPosition.X, 6);
        Assert.Equal(PhigrosAreaEaseType.EaseInQuad, moves[0].EaseTypeX.Type);
        Assert.Equal(0.5078125f, moves[1].Time, 6);
        Assert.Equal(0.6f, moves[1].EndPosition.X, 6);
        Assert.Equal(moves[1].Time, moves[2].Time);
        Assert.Equal(0.7f, moves[2].EndPosition.X, 6);
        Assert.Equal(PhigrosAreaEaseType.One, moves[2].EaseTypeX.Type);
        Assert.Equal(0.8f, moves[3].EndPosition.X, 6);
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

        Assert.Equal(3, moves.Count);
        Assert.Equal(0.5f, moves[0].Time, 6);
        Assert.Equal(0.6f, moves[0].EndPosition.X, 6);
        Assert.Equal(moves[0].Time, moves[1].Time);
        Assert.Equal(0.7f, moves[1].EndPosition.X, 6);
        Assert.Equal(PhigrosAreaEaseType.One, moves[1].EaseTypeX.Type);
        Assert.Equal(0.8f, moves[2].EndPosition.X, 6);
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
        Assert.Equal(2f, Assert.Single(area.MoveEvents).Time);
        Assert.Equal(1f, Assert.Single(area.MoveEvents).EndPosition.X);
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

        Assert.Equal(2, area.MoveEvents.Count);
        Assert.All(
            area.MoveEvents,
            evt => Assert.Equal(PhigrosAreaEaseType.Linear, evt.EaseTypeX.Type)
        );
        Assert.Contains(warnings, message => message.Contains("线性事件"));
    }
}
