using KaedePhi.Core.Common;
using KaedePhi.Core.KaedePhi;
using KaedePhi.Core.KaedePhi.Events;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;

namespace KaedePhi.Tests.Converter;

public class PhigrosV3JudgeLineBuilderTests
{
    [Fact]
    public void NegativeAlpha_UsesEventInterpolationForZeroCrossing()
    {
        var line = new JudgeLine
        {
            EventLayers =
            [
                new EventLayer
                {
                    AlphaEvents =
                    [
                        new KpcEvents.Event<int>
                        {
                            StartBeat = Beat(0),
                            EndBeat = Beat(1),
                            StartValue = -100,
                            EndValue = 100,
                            Easing = new Easing(5),
                        },
                    ],
                },
            ],
        };
        var options = new KpcToPhigrosV3ConvertOptions();
        options.NegativeAlpha.Enabled = true;

        var converted = new PhigrosV3JudgeLineBuilder(
            options,
            120f,
            PhigrosTime(2),
            null
        ).ConvertJudgeLine(line, [line]);

        // 输入判定线不被原地修改
        line.EventLayers[0].MoveYEvents.Should().BeNull();
        converted.Should().NotBeNull();
        // 负不透明度段仍被抬高到屏幕外
        converted!.JudgeLineMoveEvents.Should().Contain(e => e.Start2 > 1f || e.End2 > 1f);
    }

    [Fact]
    public void NegativeAlpha_MovementGapKeepsOriginalYOffset()
    {
        var line = new JudgeLine
        {
            EventLayers =
            [
                new EventLayer
                {
                    MoveYEvents =
                    [
                        new KpcEvents.Event<double>
                        {
                            StartBeat = Beat(0),
                            EndBeat = Beat(1),
                            StartValue = 0.2,
                            EndValue = 0.2,
                        },
                    ],
                    AlphaEvents =
                    [
                        new KpcEvents.Event<int>
                        {
                            StartBeat = Beat(1),
                            EndBeat = Beat(2),
                            StartValue = -1,
                            EndValue = -1,
                        },
                    ],
                },
            ],
        };
        var options = new KpcToPhigrosV3ConvertOptions();
        options.NegativeAlpha.Enabled = true;

        var converted = new PhigrosV3JudgeLineBuilder(
            options,
            120f,
            PhigrosTime(4),
            null
        ).ConvertJudgeLine(line, [line]);

        // 输入判定线不被原地修改
        line.EventLayers[0].MoveYEvents.Should().ContainSingle();
        line.EventLayers[0].MoveYEvents![0].StartValue.Should().Be(0.2d);
        line.EventLayers[0].MoveYEvents![0].EndValue.Should().Be(0.2d);
        converted.Should().NotBeNull();
        // 负不透明度段仍被抬高到屏幕外
        converted!.JudgeLineMoveEvents.Should().Contain(e => e.Start2 > 1f || e.End2 > 1f);
    }

    [Fact]
    public void NoAlphaEvents_KeepsJudgeLineInvisible()
    {
        var line = new JudgeLine();

        var converted = new PhigrosV3JudgeLineBuilder(
            new KpcToPhigrosV3ConvertOptions(),
            120f,
            97f,
            null
        ).ConvertJudgeLine(line, [line]);

        converted.Should().NotBeNull();
        converted!.JudgeLineDisappearEvents.Should().ContainSingle();
        var fallback = converted.JudgeLineDisappearEvents[0];
        fallback.StartTime.Should().Be(0f);
        fallback.EndTime.Should().Be(97f);
        fallback.Start.Should().Be(0f);
        fallback.End.Should().Be(0f);
    }

    [Fact]
    public void DelayedFirstAlpha_FillsInvisiblePrefix()
    {
        var line = new JudgeLine
        {
            EventLayers =
            [
                new EventLayer
                {
                    AlphaEvents =
                    [
                        new KpcEvents.Event<int>
                        {
                            StartBeat = Beat(2),
                            EndBeat = Beat(3),
                            StartValue = 255,
                            EndValue = 255,
                        },
                    ],
                },
            ],
        };
        var options = new KpcToPhigrosV3ConvertOptions();
        options.Alpha.CutPrecision = 1d;

        var converted = new PhigrosV3JudgeLineBuilder(options, 120f, 97f, null).ConvertJudgeLine(
            line,
            [line]
        );

        converted.Should().NotBeNull();
        var prefix = converted!.JudgeLineDisappearEvents[0];
        prefix.StartTime.Should().Be(0f);
        prefix.EndTime.Should().Be(64f);
        prefix.Start.Should().Be(0f);
        prefix.End.Should().Be(0f);
        var sourceEvent = converted.JudgeLineDisappearEvents[1];
        sourceEvent.StartTime.Should().Be(64f);
        sourceEvent.EndTime.Should().Be(96f);
        sourceEvent.Start.Should().Be(1f);
        sourceEvent.End.Should().Be(1f);
    }

    [Fact]
    public void FromKpc_NegativeAlphaTailUsesChartEndTime()
    {
        var negativeAlphaLine = new JudgeLine
        {
            EventLayers =
            [
                new EventLayer
                {
                    AlphaEvents =
                    [
                        new KpcEvents.Event<int>
                        {
                            StartBeat = Beat(0),
                            EndBeat = Beat(1),
                            StartValue = -1,
                            EndValue = -1,
                        },
                    ],
                },
            ],
        };
        var chart = new Chart { JudgeLineList = [negativeAlphaLine, new JudgeLine()] };
        var options = new KpcToPhigrosV3ConvertOptions();
        options.NegativeAlpha.Enabled = true;
        options.Cutting.MisalignedXyEventPrecision = 1d;
        options.Alpha.CutPrecision = 1d;

        var converted = new global::KaedePhi.Tool.Converter.Phigros.v3.PhigrosV3Converter().FromKpc(
            chart,
            options
        );

        var chartEndTime = converted.JudgeLineList[1].JudgeLineDisappearEvents.Single().EndTime;
        chartEndTime.Should().Be(33f);
        var elevatedEndTime = converted
            .JudgeLineList[0]
            .JudgeLineMoveEvents.Where(e => e.Start2 > 1f || e.End2 > 1f)
            .Max(e => e.EndTime);
        elevatedEndTime.Should().Be(chartEndTime);
    }

    [Fact]
    public void FromKpc_HoldUsesMergedMultiLayerSpeedTimeline()
    {
        var line = new JudgeLine
        {
            Notes = [Hold(1, 5f)],
            EventLayers =
            [
                new EventLayer { SpeedEvents = [SpeedEvent(0, 2, 4.5f, 4.5f)] },
                new EventLayer { SpeedEvents = [SpeedEvent(0, 2, 4.5f, 4.5f)] },
            ],
        };
        var options = new KpcToPhigrosV3ConvertOptions();
        options.MultiLayerMerge.ClassicMode = true;
        options.MultiLayerMerge.Precision = 1d;
        options.Speed.CutPrecision = 1d;

        var converted = new global::KaedePhi.Tool.Converter.Phigros.v3.PhigrosV3Converter()
            .FromKpc(new Chart { JudgeLineList = [line] }, options)
            .JudgeLineList.Single();

        converted.NotesAbove.Single().Speed.Should().Be(2f);
        converted.SpeedEvents[0].Value.Should().Be(2f);
    }

    [Fact]
    public void FromKpc_ChangingFinalSpeedEmitsEndValueInTail()
    {
        var line = new JudgeLine
        {
            EventLayers = [new EventLayer { SpeedEvents = [SpeedEvent(0, 2, 4.5f, 13.5f)] }],
        };
        var options = new KpcToPhigrosV3ConvertOptions();
        options.Speed.CutPrecision = 1d;

        var converted = new global::KaedePhi.Tool.Converter.Phigros.v3.PhigrosV3Converter()
            .FromKpc(new Chart { JudgeLineList = [line] }, options)
            .JudgeLineList.Single();

        converted
            .SpeedEvents.Should()
            .SatisfyRespectively(
                e =>
                    e.Should()
                        .BeEquivalentTo(
                            new
                            {
                                StartTime = 0f,
                                EndTime = 32f,
                                Value = 1f,
                            }
                        ),
                e =>
                    e.Should()
                        .BeEquivalentTo(
                            new
                            {
                                StartTime = 32f,
                                EndTime = 64f,
                                Value = 2f,
                            }
                        ),
                e =>
                    e.Should()
                        .BeEquivalentTo(
                            new
                            {
                                StartTime = 64f,
                                EndTime = 1_000_000_000f,
                                Value = 3f,
                            }
                        )
            );
    }

    [Fact]
    public void FromKpc_HoldSamplesSpeedPrefixGapBoundaryAndTail()
    {
        var line = new JudgeLine
        {
            Notes = [Hold(0.5), Hold(2), Hold(3), Hold(4), Hold(6)],
            EventLayers =
            [
                new EventLayer
                {
                    SpeedEvents = [SpeedEvent(1, 2, 4.5f, 9f), SpeedEvent(4, 5, 13.5f, 13.5f)],
                },
            ],
        };
        var options = new KpcToPhigrosV3ConvertOptions();
        options.Speed.CutPrecision = 1d;

        var converted = new global::KaedePhi.Tool.Converter.Phigros.v3.PhigrosV3Converter()
            .FromKpc(new Chart { JudgeLineList = [line] }, options)
            .JudgeLineList.Single();

        converted.NotesAbove.Select(n => n.Speed).Should().Equal(1f, 2f, 2f, 3f, 3f);
        converted.SpeedEvents[0].StartTime.Should().Be(32f);
    }

    [Fact]
    public void FromKpc_WithBpmList_ReparameterizesNotesAndAllEventChannels()
    {
        var firstLine = new JudgeLine
        {
            Notes =
            [
                new Note
                {
                    Type = NoteType.Tap,
                    StartBeat = Beat(2),
                    EndBeat = Beat(2),
                },
                new Note
                {
                    Type = NoteType.Hold,
                    StartBeat = Beat(3),
                    EndBeat = Beat(5),
                },
            ],
            EventLayers =
            [
                new EventLayer
                {
                    MoveXEvents = [DoubleEvent(3, 5, 0, 1)],
                    MoveYEvents = [DoubleEvent(3, 5, 0, 1)],
                    RotateEvents = [DoubleEvent(3, 5, 0, 100)],
                    AlphaEvents = [IntEvent(3, 5, 0, 255)],
                    SpeedEvents = [SpeedEvent(3, 5, 4.5f, 9f)],
                },
            ],
        };
        var secondLine = new JudgeLine
        {
            BpmFactor = 2f,
            Notes =
            [
                new Note
                {
                    Type = NoteType.Tap,
                    StartBeat = Beat(2),
                    EndBeat = Beat(2),
                },
                new Note
                {
                    Type = NoteType.Hold,
                    StartBeat = Beat(3),
                    EndBeat = Beat(5),
                },
            ],
        };
        var chart = new Chart
        {
            BpmList =
            [
                new BpmItem { StartBeat = Beat(3.5), Bpm = 180f },
                new BpmItem { StartBeat = Beat(2), Bpm = 120f },
                new BpmItem { StartBeat = Beat(3.5), Bpm = 240f },
            ],
            JudgeLineList = [firstLine, secondLine],
        };
        var options = new KpcToPhigrosV3ConvertOptions();
        options.Cutting.EasingPrecision = 1d;
        options.Cutting.MisalignedXyEventPrecision = 1d;
        options.Alpha.CutPrecision = 1d;
        options.Speed.CutPrecision = 1d;

        var converted = new global::KaedePhi.Tool.Converter.Phigros.v3.PhigrosV3Converter().FromKpc(
            chart,
            options
        );

        converted.JudgeLineList.Select(line => line.Bpm).Should().Equal(1000f, 1000f);

        var first = converted.JudgeLineList[0];
        first.NotesAbove[0].Time.Should().Be(533);
        first.NotesAbove[1].Time.Should().Be(800);
        first.NotesAbove[1].HoldTime.Should().Be(333f);
        AssertTimeError(first.NotesAbove[0].Time, 1d);
        AssertTimeError(first.NotesAbove[1].Time, 1.5d);
        AssertTimeError(first.NotesAbove[1].Time + first.NotesAbove[1].HoldTime, 2.125d);

        var second = converted.JudgeLineList[1];
        second.NotesAbove[0].Time.Should().Be(1067);
        second.NotesAbove[1].Time.Should().Be(1600);
        second.NotesAbove[1].HoldTime.Should().Be(667f);
        AssertTimeError(second.NotesAbove[0].Time, 2d);
        AssertTimeError(second.NotesAbove[1].Time, 3d);
        AssertTimeError(second.NotesAbove[1].Time + second.NotesAbove[1].HoldTime, 4.25d);

        AssertSplitAtTempoBoundary(first.JudgeLineMoveEvents.Select(e => (e.StartTime, e.EndTime)));
        AssertSplitAtTempoBoundary(
            first.JudgeLineRotateEvents.Select(e => (e.StartTime, e.EndTime))
        );
        AssertSplitAtTempoBoundary(
            first.JudgeLineDisappearEvents.Select(e => (e.StartTime, e.EndTime))
        );
        AssertSplitAtTempoBoundary(first.SpeedEvents.Select(e => (e.StartTime, e.EndTime)));
        first
            .SpeedEvents.Should()
            .Contain(eventItem =>
                eventItem.StartTime == 933f
                && eventItem.EndTime == 1000f
                && eventItem.Value == 1.25f
            );

        chart.BpmList.Select(item => item.Bpm).Should().Equal(180f, 120f, 240f);
        firstLine.BpmFactor.Should().Be(1f);
        ((double)firstLine.EventLayers[0].RotateEvents![0].StartBeat).Should().Be(3d);
        ((double)firstLine.EventLayers[0].RotateEvents![0].EndBeat).Should().Be(5d);
    }

    [Fact]
    public void FromKpc_WithFloorPosition_LogsLossAndKeepsPhigrosDefault()
    {
        var source = new JudgeLine
        {
            Notes =
            [
                new Note
                {
                    Type = NoteType.Tap,
                    StartBeat = Beat(1),
                    EndBeat = Beat(1),
                    FloorPosition = 1f,
                    EndFloorPosition = 2f,
                },
            ],
            EventLayers =
            [
                new EventLayer
                {
                    RotateEvents =
                    [
                        new KpcEvents.Event<double>
                        {
                            StartBeat = Beat(0),
                            EndBeat = Beat(1),
                            StartValue = 0d,
                            EndValue = 1d,
                            FloorPosition = 3f,
                        },
                    ],
                },
            ],
        };
        var warnings = new List<string>();
        var converter = new global::KaedePhi.Tool.Converter.Phigros.v3.PhigrosV3Converter();
        converter.OnWarning = warnings.Add;

        var converted = converter.FromKpc(
            new Chart
            {
                BpmList = [new BpmItem { StartBeat = Beat(0), Bpm = 120f }],
                JudgeLineList = [source],
            },
            new KpcToPhigrosV3ConvertOptions()
        );

        warnings
            .Should()
            .Contain(message => message.Contains("Note.FloorPosition") && message.Contains("0"));
        warnings
            .Should()
            .Contain(message => message.Contains("Event.FloorPosition") && message.Contains("0"));
        converted.JudgeLineList.Single().NotesAbove.Single().FloorPosition.Should().Be(0f);
    }

    [Fact]
    public void FromKpc_WithoutBpmList_PreservesDefaultBpmAndBeatTime()
    {
        var source = new JudgeLine
        {
            BpmFactor = 2f,
            Notes =
            [
                new Note
                {
                    Type = NoteType.Tap,
                    StartBeat = Beat(1),
                    EndBeat = Beat(1),
                },
            ],
        };

        var converted = new global::KaedePhi.Tool.Converter.Phigros.v3.PhigrosV3Converter()
            .FromKpc(new Chart { JudgeLineList = [source] }, new KpcToPhigrosV3ConvertOptions())
            .JudgeLineList.Single();

        converted.Bpm.Should().Be(60f);
        converted.NotesAbove.Single().Time.Should().Be(32);
    }

    [Fact]
    public void FromKpc_WithBpmList_DerivesHoldTimeFromQuantizedEndpoints()
    {
        var source = new JudgeLine
        {
            Notes =
            [
                new Note
                {
                    Type = NoteType.Hold,
                    StartBeat = Beat(1),
                    EndBeat = Beat(2),
                },
            ],
        };

        var converted = new global::KaedePhi.Tool.Converter.Phigros.v3.PhigrosV3Converter()
            .FromKpc(
                new Chart
                {
                    BpmList = [new BpmItem { StartBeat = Beat(0), Bpm = 120f }],
                    JudgeLineList = [source],
                },
                new KpcToPhigrosV3ConvertOptions()
            )
            .JudgeLineList.Single()
            .NotesAbove.Single();

        converted.Time.Should().Be(267);
        converted.HoldTime.Should().Be(266f);
        AssertTimeError(converted.Time, 0.5d);
        AssertTimeError(converted.Time + converted.HoldTime, 1d);
    }

    [Fact]
    public void FromKpc_WithBpmListAndAncestorFactorMismatch_AllowsParentUnbind()
    {
        var chart = new Chart
        {
            BpmList = [new BpmItem { StartBeat = Beat(0), Bpm = 120f }],
            JudgeLineList =
            [
                new JudgeLine(),
                new JudgeLine { Father = 0 },
                new JudgeLine
                {
                    Father = 1,
                    BpmFactor = 2f,
                    Notes =
                    [
                        new Note
                        {
                            Type = NoteType.Tap,
                            StartBeat = Beat(1),
                            EndBeat = Beat(1),
                        },
                    ],
                },
            ],
        };

        Action act = () =>
            new global::KaedePhi.Tool.Converter.Phigros.v3.PhigrosV3Converter().FromKpc(
                chart,
                new KpcToPhigrosV3ConvertOptions()
            );

        act.Should().NotThrow();
    }

    [Fact]
    public void FromKpc_WithBpmList_RejectsTimeAtTailEventSentinel()
    {
        var chart = new Chart
        {
            BpmList = [new BpmItem { StartBeat = Beat(0), Bpm = 120f }],
            JudgeLineList =
            [
                new JudgeLine
                {
                    Notes =
                    [
                        new Note
                        {
                            Type = NoteType.Tap,
                            StartBeat = Beat(3_750_000),
                            EndBeat = Beat(3_750_000),
                        },
                    ],
                },
            ],
        };

        Action act = () =>
            new global::KaedePhi.Tool.Converter.Phigros.v3.PhigrosV3Converter().FromKpc(
                chart,
                new KpcToPhigrosV3ConvertOptions()
            );

        act.Should().Throw<FormatException>().WithMessage("*尾事件*哨兵*");
    }

    [Fact]
    public void FromKpc_WithFloorPositionOnFilteredLine_LogsDiscardBeforeFiltering()
    {
        var source = new JudgeLine
        {
            Texture = "filtered.png",
            Notes =
            [
                new Note
                {
                    Type = NoteType.Tap,
                    StartBeat = Beat(1),
                    EndBeat = Beat(1),
                    FloorPosition = 1f,
                },
            ],
        };
        var warnings = new List<string>();
        var converter = new global::KaedePhi.Tool.Converter.Phigros.v3.PhigrosV3Converter
        {
            OnWarning = warnings.Add,
        };
        var options = new KpcToPhigrosV3ConvertOptions();
        options.LineFilter.RemoveTextureLine = true;

        var converted = converter.FromKpc(new Chart { JudgeLineList = [source] }, options);

        converted.JudgeLineList.Should().BeEmpty();
        warnings
            .Should()
            .Contain(message => message.Contains("Note.FloorPosition") && message.Contains("丢弃"));
    }

    [Fact]
    public void FromKpc_WithFloorPositionOnFilteredFather_LogsDiscardBeforeUnbinding()
    {
        var parent = new JudgeLine
        {
            Texture = "filtered.png",
            EventLayers =
            [
                new EventLayer
                {
                    RotateEvents =
                    [
                        new KpcEvents.Event<double>
                        {
                            StartBeat = Beat(0),
                            EndBeat = Beat(1),
                            StartValue = 0d,
                            EndValue = 1d,
                            FloorPosition = 1f,
                        },
                    ],
                },
            ],
        };
        var child = new JudgeLine
        {
            Father = 0,
            Notes =
            [
                new Note
                {
                    Type = NoteType.Tap,
                    StartBeat = Beat(1),
                    EndBeat = Beat(1),
                },
            ],
        };
        var warnings = new List<string>();
        var converter = new global::KaedePhi.Tool.Converter.Phigros.v3.PhigrosV3Converter
        {
            OnWarning = warnings.Add,
        };
        var options = new KpcToPhigrosV3ConvertOptions();
        options.LineFilter.RemoveTextureLine = true;

        var converted = converter.FromKpc(new Chart { JudgeLineList = [parent, child] }, options);

        converted.JudgeLineList.Should().ContainSingle();
        warnings
            .Should()
            .Contain(message =>
                message.Contains("Event.FloorPosition") && message.Contains("丢弃")
            );
    }

    [Fact]
    public void FatherLineLookup_UsesSourceObjectIdentity()
    {
        var father = new CollidingJudgeLine();
        var source = new CollidingJudgeLine
        {
            Father = 0,
            Notes = [new Note { StartBeat = Beat(1), EndBeat = Beat(1) }],
        };
        var options = new KpcToPhigrosV3ConvertOptions();

        var converted = new PhigrosV3JudgeLineBuilder(options, 120f, 2f, null).ConvertJudgeLine(
            source,
            [father, source]
        );

        converted.Should().NotBeNull();
        converted!.NotesAbove.Should().ContainSingle();
    }

    private static Beat Beat(double value) => new(value);

    private static Note Hold(double endBeat, float speedMultiplier = 1f) =>
        new()
        {
            Type = NoteType.Hold,
            StartBeat = Beat(0),
            EndBeat = Beat(endBeat),
            SpeedMultiplier = speedMultiplier,
        };

    private static KpcEvents.Event<float> SpeedEvent(
        double startBeat,
        double endBeat,
        float startValue,
        float endValue
    ) =>
        new()
        {
            StartBeat = Beat(startBeat),
            EndBeat = Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue,
        };

    private static KpcEvents.Event<double> DoubleEvent(
        double startBeat,
        double endBeat,
        double startValue,
        double endValue
    ) =>
        new()
        {
            StartBeat = Beat(startBeat),
            EndBeat = Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue,
        };

    private static KpcEvents.Event<int> IntEvent(
        double startBeat,
        double endBeat,
        int startValue,
        int endValue
    ) =>
        new()
        {
            StartBeat = Beat(startBeat),
            EndBeat = Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue,
        };

    private static void AssertSplitAtTempoBoundary(IEnumerable<(float Start, float End)> events)
    {
        events.Should().Contain((800f, 933f));
        events.Should().Contain((933f, 1000f));
        events.Should().Contain((1000f, 1133f));
    }

    private static void AssertTimeError(double time, double expectedSeconds) =>
        Math.Abs(time * 0.001875d - expectedSeconds).Should().BeLessThanOrEqualTo(0.001d);

    private static float PhigrosTime(double beat) => (float)(beat * 32d);

    private sealed class CollidingJudgeLine : JudgeLine
    {
        public override int GetHashCode() => 1;
    }
}
