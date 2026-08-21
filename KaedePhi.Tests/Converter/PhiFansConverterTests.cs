using KaedePhi.Core.Common;
using KaedePhi.Tool.Converter.PhiFans;
using KaedePhi.Tool.Converter.PhiFans.Model;
using Kpc = KaedePhi.Core.KaedePhi;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;
using Pf = KaedePhi.Core.PhiFans;

namespace KaedePhi.Tests.Converter;

public class PhiFansConverterTests
{
    [Fact]
    public void FromKpc_MultipleLayers_ComposesMoveValuesBeforeEncoding()
    {
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer
            {
                MoveXEvents = [CreateDoubleEvent(0, 1, 0, 1)],
            },
            new KpcEvents.EventLayer
            {
                MoveXEvents = [CreateDoubleEvent(0, 1, 1, 2)],
            }
        );

        var exported = new PhiFansConverter().FromKpc(chart, CreateOptions());
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var events = roundTrip.JudgeLineList[0].EventLayers[0].MoveXEvents!;

        KpcEvents.EventLayer.GetValueAtBeat(events, Beat(0.5))
            .Should()
            .BeApproximately(2d, 1e-6);
        KpcEvents.EventLayer.GetValueAtBeat(events, Beat(1))
            .Should()
            .BeApproximately(3d, 1e-6);
    }

    [Fact]
    public void FromKpc_ClassicAndAdaptiveMerge_UseConfiguredStrategies()
    {
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer
            {
                MoveXEvents = [CreateDoubleEvent(0, 1, 0, 1, 5)],
            },
            new KpcEvents.EventLayer
            {
                MoveXEvents = [CreateDoubleEvent(0, 1, 0, 1)],
            }
        );
        var classicOptions = CreateOptions();
        classicOptions.MultiLayerMerge.ClassicMode = true;
        classicOptions.MultiLayerMerge.Compress = false;
        var adaptiveOptions = CreateOptions();
        adaptiveOptions.MultiLayerMerge.ClassicMode = false;
        adaptiveOptions.MultiLayerMerge.Tolerance = 100;

        var classic = new PhiFansConverter().FromKpc(chart, classicOptions);
        var adaptive = new PhiFansConverter().FromKpc(chart, adaptiveOptions);

        classic.JudgeLineList[0].Props.PositionX.Should().HaveCount(5);
        adaptive.JudgeLineList[0].Props.PositionX.Should().HaveCount(2);
    }

    [Fact]
    public void FromKpc_MultiLayerMergeOptions_ChangeEncodedNodeDensity()
    {
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer
            {
                MoveXEvents = [CreateDoubleEvent(0, 1, 0, 1)],
            },
            new KpcEvents.EventLayer
            {
                MoveXEvents = [CreateDoubleEvent(0, 1, 0, 1)],
            }
        );
        var classicOptions = CreateOptions();
        classicOptions.MultiLayerMerge.Precision = 2;
        classicOptions.MultiLayerMerge.ClassicMode = true;
        classicOptions.MultiLayerMerge.Compress = false;
        var adaptiveOptions = CreateOptions();
        adaptiveOptions.MultiLayerMerge.ClassicMode = false;
        adaptiveOptions.MultiLayerMerge.Tolerance = 100;

        var classic = new PhiFansConverter().FromKpc(chart, classicOptions);
        var adaptive = new PhiFansConverter().FromKpc(chart, adaptiveOptions);

        classic.JudgeLineList[0].Props.PositionX.Should().HaveCount(3);
        adaptive.JudgeLineList[0].Props.PositionX.Should().HaveCount(2);
    }

    [Fact]
    public void FromKpc_BezierMove_CutsIntoLinearNodesAndPreservesBoundaryValues()
    {
        var sourceEvent = CreateDoubleEvent(0, 1, 0, 10);
        sourceEvent.IsBezier = true;
        sourceEvent.BezierPoints = [1f / 3f, 0, 2f / 3f, 1];
        var chart = CreateChart(sourceEvent);

        var exported = new PhiFansConverter().FromKpc(chart, CreateOptions());
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var phiFansEvents = exported.JudgeLineList[0].Props.PositionX;
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].MoveXEvents!;

        AssertLinearPhiFansNodes(phiFansEvents);
        AssertMoveValues(roundTripEvents, 0, 1.5625, 5, 8.4375, 10);
    }

    [Fact]
    public void FromKpc_OverlappingUnsupportedCurve_UsesCutterPrecisionAfterComposition()
    {
        var curve = CreateDoubleEvent(0, 1, 0, 4);
        curve.IsBezier = true;
        curve.BezierPoints = [1f / 3f, 0, 2f / 3f, 1];
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer { MoveXEvents = [curve] },
            new KpcEvents.EventLayer
            {
                MoveXEvents = [CreateDoubleEvent(0, 1, 0, 4)],
            }
        );
        var options = CreateOptions();
        options.MultiLayerMerge.Precision = 2;
        options.MultiLayerMerge.ClassicMode = true;
        options.MultiLayerMerge.Compress = false;

        var exported = new PhiFansConverter().FromKpc(chart, options);
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var phiFansEvents = exported.JudgeLineList[0].Props.PositionX;
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].MoveXEvents!;

        AssertLinearPhiFansNodes(phiFansEvents);
        AssertMoveValues(roundTripEvents, 0, 1.625, 4, 6.375, 8);
    }

    [Fact]
    public void FromKpc_UnsupportedOverlap_PreservesConfiguredMergeOutsideAffectedInterval()
    {
        var unsupported = CreateDoubleEvent(0, 1, 0, 4);
        unsupported.IsBezier = true;
        unsupported.BezierPoints = [1f / 3f, 0, 2f / 3f, 1];
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer
            {
                MoveXEvents =
                [
                    unsupported,
                    CreateDoubleEvent(4, 5, 0, 4, 5),
                ],
            },
            new KpcEvents.EventLayer
            {
                MoveXEvents =
                [
                    CreateDoubleEvent(0, 1, 0, 4),
                    CreateDoubleEvent(4, 5, 0, 4),
                ],
            }
        );
        var options = CreateOptions();
        options.MultiLayerMerge.Precision = 2;
        options.MultiLayerMerge.ClassicMode = true;
        options.MultiLayerMerge.Compress = false;

        var exported = new PhiFansConverter().FromKpc(chart, options);
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var phiFansEvents = exported.JudgeLineList[0].Props.PositionX;
        var distantNodes = phiFansEvents.Where(e => (double)e.Beat >= 4).ToList();
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].MoveXEvents!;

        distantNodes.Should().HaveCount(3);
        distantNodes.Select(e => (double)e.Beat).Should().Equal(4, 4.5, 5);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(4.5))
            .Should()
            .BeApproximately(3, 1e-5);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(5))
            .Should()
            .BeApproximately(8, 1e-5);
    }

    [Fact]
    public void FromKpc_UnsupportedOverlap_UsesTransitiveSpanClosure()
    {
        var unsupported = CreateDoubleEvent(0, 1, 0, 1);
        unsupported.IsBezier = true;
        unsupported.BezierPoints = [1f / 3f, 0, 2f / 3f, 1];
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer { MoveXEvents = [unsupported] },
            new KpcEvents.EventLayer
            {
                MoveXEvents = [CreateDoubleEvent(0.5, 1.5, 0, 1)],
            },
            new KpcEvents.EventLayer
            {
                MoveXEvents = [CreateDoubleEvent(1.25, 2, 0, 1)],
            }
        );
        var options = CreateOptions();
        options.MultiLayerMerge.Precision = 1;
        options.MultiLayerMerge.ClassicMode = true;
        options.MultiLayerMerge.Compress = false;

        var exported = new PhiFansConverter().FromKpc(chart, options);
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var phiFansEvents = exported.JudgeLineList[0].Props.PositionX;
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].MoveXEvents!;

        phiFansEvents.Should().Contain(e => Math.Abs((double)e.Beat - 1.75) < 1e-9);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(1.75))
            .Should()
            .BeApproximately(8d / 3d, 1e-5);
    }

    [Fact]
    public void FromKpc_IsolatedUnsupportedIntervals_PreserveFarCurveAndSourceOwnership()
    {
        var firstCurve = CreateDoubleEvent(0, 1, 0, 1);
        firstCurve.IsBezier = true;
        firstCurve.BezierPoints = [1f / 3f, 0, 2f / 3f, 1];
        var secondCurve = CreateDoubleEvent(10, 11, 1, 2);
        secondCurve.IsBezier = true;
        secondCurve.BezierPoints = [1f / 3f, 0, 2f / 3f, 1];
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer
            {
                MoveXEvents =
                [
                    firstCurve,
                    secondCurve,
                    CreateDoubleEvent(20, 21, 2, 3, 5),
                ],
            },
            new KpcEvents.EventLayer
            {
                MoveXEvents =
                [
                    CreateDoubleEvent(0, 1, 0, 1),
                    CreateDoubleEvent(10, 11, 0, 1),
                ],
            }
        );
        var options = CreateOptions();
        options.MultiLayerMerge.Precision = 2;
        options.MultiLayerMerge.ClassicMode = true;
        options.MultiLayerMerge.Compress = false;

        var exported = new PhiFansConverter().FromKpc(chart, options);
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var phiFansEvents = exported.JudgeLineList[0].Props.PositionX;
        var firstNodes = phiFansEvents.Where(e => (double)e.Beat is >= 0 and <= 1).ToList();
        var secondNodes = phiFansEvents.Where(e => (double)e.Beat is >= 10 and <= 11).ToList();
        var farNodes = phiFansEvents.Where(e => (double)e.Beat is >= 20 and <= 21).ToList();
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].MoveXEvents!;

        firstNodes.Should().HaveCount(5);
        secondNodes.Should().HaveCount(5);
        farNodes.Should().HaveCount(2);
        farNodes.Select(e => (int)e.Easing).Should().Equal(4, 4);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(10.5))
            .Should()
            .BeApproximately(2, 1e-5);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(20.5))
            .Should()
            .BeApproximately(3.25, 1e-5);
        chart.JudgeLineList[0].EventLayers.Should().HaveCount(2);
        firstCurve.BezierPoints.Should().Equal(1f / 3f, 0, 2f / 3f, 1);
        secondCurve.BezierPoints.Should().Equal(1f / 3f, 0, 2f / 3f, 1);
    }

    [Fact]
    public void FromKpc_CroppedEasingMove_CutsIntoLinearNodesAndPreservesBoundaryValues()
    {
        var sourceEvent = CreateDoubleEvent(0, 1, 0, 10, 5);
        sourceEvent.EasingLeft = 0.25f;
        sourceEvent.EasingRight = 0.75f;
        var chart = CreateChart(sourceEvent);

        var exported = new PhiFansConverter().FromKpc(chart, CreateOptions());
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var phiFansEvents = exported.JudgeLineList[0].Props.PositionX;
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].MoveXEvents!;

        AssertLinearPhiFansNodes(phiFansEvents);
        AssertMoveValues(roundTripEvents, 0, 1.5625, 3.75, 6.5625, 10);
    }

    [Fact]
    public void FromKpc_UnknownEasingMove_CutsIntoLinearNodes()
    {
        var chart = CreateChart(CreateDoubleEvent(0, 1, 0, 10, 99));

        var exported = new PhiFansConverter().FromKpc(chart, CreateOptions());
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var phiFansEvents = exported.JudgeLineList[0].Props.PositionX;
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].MoveXEvents!;

        AssertLinearPhiFansNodes(phiFansEvents);
        AssertMoveValues(roundTripEvents, 0, 2.5, 5, 7.5, 10);
    }

    [Fact]
    public void FromKpc_InstantUnknownEasingMove_EmitsLinearNodeWithEndValue()
    {
        var chart = CreateChart(CreateDoubleEvent(1, 1, 1, 2, 99));

        var exported = new PhiFansConverter().FromKpc(chart, CreateOptions());
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var phiFansEvents = exported.JudgeLineList[0].Props.PositionX;
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].MoveXEvents!;

        phiFansEvents.Should().ContainSingle();
        ((double)phiFansEvents[0].Beat).Should().Be(1);
        phiFansEvents[0].Value.Should().BeApproximately(200, 1e-5f);
        ((int)phiFansEvents[0].Easing).Should().Be(0);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(1))
            .Should()
            .BeApproximately(2, 1e-5);
    }

    [Fact]
    public void FromKpc_RepresentableMove_PreservesCompactMappedEasing()
    {
        var chart = CreateChart(CreateDoubleEvent(0, 1, 0, 10, 5));

        var exported = new PhiFansConverter().FromKpc(chart, CreateOptions());
        var phiFansEvents = exported.JudgeLineList[0].Props.PositionX;

        phiFansEvents.Should().HaveCount(2);
        phiFansEvents.Select(e => (int)e.Easing).Should().Equal(4, 4);
    }

    [Fact]
    public void FromKpc_NonlinearSpeed_CutsIntoLinearNodesAndPreservesBoundaryValues()
    {
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer
            {
                SpeedEvents = [CreateFloatEvent(0, 1, 0, 4, 5)],
            }
        );

        var exported = new PhiFansConverter().FromKpc(chart, CreateOptions());
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var phiFansEvents = exported.JudgeLineList[0].Props.Speed;
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].SpeedEvents!;

        AssertLinearPhiFansNodes(phiFansEvents);
        AssertSpeedValues(roundTripEvents, 0, 0.25, 1, 2.25, 4);
    }

    [Fact]
    public void FromKpc_InstantNonlinearSpeed_EmitsLinearNodeWithEndValue()
    {
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer
            {
                SpeedEvents = [CreateFloatEvent(1, 1, 1, 2, 5)],
            }
        );

        var exported = new PhiFansConverter().FromKpc(chart, CreateOptions());
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var phiFansEvents = exported.JudgeLineList[0].Props.Speed;
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].SpeedEvents!;

        phiFansEvents.Should().ContainSingle();
        ((double)phiFansEvents[0].Beat).Should().Be(1);
        phiFansEvents[0].Value.Should().BeApproximately(2f / 7.15f, 1e-5f);
        ((int)phiFansEvents[0].Easing).Should().Be(0);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(1))
            .Should()
            .BeApproximately(2, 1e-5f);
    }

    [Fact]
    public void FromKpc_InstantUnknownSpeedOverContinuousLayer_ComposesExactStepTimeline()
    {
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer
            {
                SpeedEvents = [CreateFloatEvent(0, 2, 1, 3)],
            },
            new KpcEvents.EventLayer
            {
                SpeedEvents = [CreateFloatEvent(1, 1, 0, 4, 99)],
            }
        );

        var exported = new PhiFansConverter().FromKpc(chart, CreateOptions());
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var phiFansEvents = exported.JudgeLineList[0].Props.Speed;
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].SpeedEvents!;

        phiFansEvents.Should().HaveCount(4);
        phiFansEvents.Select(e => (double)e.Beat).Should().Equal(0, 1, 1, 2);
        phiFansEvents.Select(e => e.Continuous).Should().Equal(false, true, false, true);
        phiFansEvents.Select(e => (int)e.Easing).Should().OnlyContain(easing => easing == 0);
        phiFansEvents[0].Value.Should().BeApproximately(1f / 7.15f, 1e-5f);
        phiFansEvents[1].Value.Should().BeApproximately(2f / 7.15f, 1e-5f);
        phiFansEvents[2].Value.Should().BeApproximately(6f / 7.15f, 1e-5f);
        phiFansEvents[3].Value.Should().BeApproximately(7f / 7.15f, 1e-5f);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(0.5))
            .Should()
            .BeApproximately(1.5f, 1e-5f);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(1))
            .Should()
            .BeApproximately(6, 1e-5f);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(1.5))
            .Should()
            .BeApproximately(6.5f, 1e-5f);
    }

    [Fact]
    public void FromKpc_SubEpsilonExactStep_PreservesRepresentableValueChange()
    {
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer
            {
                MoveXEvents = [CreateDoubleEvent(0, 2, 0, 0.000002)],
            },
            new KpcEvents.EventLayer
            {
                MoveXEvents = [CreateDoubleEvent(1, 1, 0, 0.00000005)],
            }
        );

        var exported = new PhiFansConverter().FromKpc(chart, CreateOptions());
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var nodesAtStep = exported
            .JudgeLineList[0]
            .Props.PositionX.Where(e => (double)e.Beat == 1)
            .ToList();
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].MoveXEvents!;

        nodesAtStep.Should().HaveCount(2);
        nodesAtStep.Select(e => e.Continuous).Should().Equal(true, false);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(1))
            .Should()
            .BeApproximately(0.00000105, 1e-10);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(1.5))
            .Should()
            .BeApproximately(0.00000155, 1e-10);
    }

    [Fact]
    public void FromKpc_SameBeatInstantsAcrossLayers_ComposeSingleSummedNode()
    {
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer
            {
                MoveXEvents = [CreateDoubleEvent(1, 1, 0, 2)],
            },
            new KpcEvents.EventLayer
            {
                MoveXEvents = [CreateDoubleEvent(1, 1, 0, 3)],
            }
        );

        var exported = new PhiFansConverter().FromKpc(chart, CreateOptions());
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var phiFansEvents = exported.JudgeLineList[0].Props.PositionX;
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].MoveXEvents!;

        phiFansEvents.Should().ContainSingle();
        ((double)phiFansEvents[0].Beat).Should().Be(1);
        phiFansEvents[0].Value.Should().BeApproximately(500, 1e-5f);
        ((int)phiFansEvents[0].Easing).Should().Be(0);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(1))
            .Should()
            .BeApproximately(5, 1e-5);
    }

    [Fact]
    public void FromKpc_InstantStep_IsReplacedByNextEventInSameLayer()
    {
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer
            {
                SpeedEvents = [CreateFloatEvent(0, 3, 1, 4)],
            },
            new KpcEvents.EventLayer
            {
                SpeedEvents =
                [
                    CreateFloatEvent(1, 1, 0, 4, 99),
                    CreateFloatEvent(2, 3, 10, 12),
                ],
            }
        );

        var exported = new PhiFansConverter().FromKpc(chart, CreateOptions());
        var roundTrip = new PhiFansConverter().ToKpc(exported, null);
        var roundTripEvents = roundTrip.JudgeLineList[0].EventLayers[0].SpeedEvents!;

        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(1))
            .Should()
            .BeApproximately(6, 1e-5f);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(1.5))
            .Should()
            .BeApproximately(6.5f, 1e-5f);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(2))
            .Should()
            .BeApproximately(13, 1e-5f);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(2.5))
            .Should()
            .BeApproximately(14.5f, 1e-5f);
        KpcEvents.EventLayer.GetValueAtBeat(roundTripEvents, Beat(3))
            .Should()
            .BeApproximately(16, 1e-5f);
    }

    [Fact]
    public void FromKpc_LinearSpeed_PreservesCompactNodes()
    {
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer
            {
                SpeedEvents = [CreateFloatEvent(0, 1, 1, 2)],
            }
        );

        var exported = new PhiFansConverter().FromKpc(chart, CreateOptions());

        exported.JudgeLineList[0].Props.Speed.Should().HaveCount(2);
        exported
            .JudgeLineList[0]
            .Props.Speed.Select(e => (int)e.Easing)
            .Should()
            .Equal(0, 0);
    }

    [Theory]
    [InlineData(0d, 0.1d)]
    [InlineData(double.NaN, 0.1d)]
    [InlineData(double.PositiveInfinity, 0.1d)]
    [InlineData(4d, -0.1d)]
    [InlineData(4d, 100.1d)]
    [InlineData(4d, double.NaN)]
    [InlineData(4d, double.PositiveInfinity)]
    public void FromKpc_InvalidMultiLayerMergeOptions_Throws(
        double precision,
        double tolerance
    )
    {
        var options = CreateOptions();
        options.MultiLayerMerge.Precision = precision;
        options.MultiLayerMerge.Tolerance = tolerance;

        var act = () => new PhiFansConverter().FromKpc(new Kpc.Chart(), options);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromKpc_NullMultiLayerMergeOptions_Throws()
    {
        var options = CreateOptions();
        options.MultiLayerMerge = null!;

        var act = () => new PhiFansConverter().FromKpc(new Kpc.Chart(), options);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromKpc_MergeAndCut_DoesNotMutateSourceLayersOrBezierPoints()
    {
        var sourceEvent = CreateDoubleEvent(0, 1, 0, 10, 5);
        sourceEvent.IsBezier = true;
        sourceEvent.BezierPoints = [0.1f, 0.2f, 0.8f, 0.9f];
        var secondEvent = CreateDoubleEvent(0, 1, 1, 2);
        var chart = CreateChartWithLayers(
            new KpcEvents.EventLayer { MoveXEvents = [sourceEvent] },
            new KpcEvents.EventLayer { MoveXEvents = [secondEvent] }
        );
        var options = CreateOptions();
        options.MultiLayerMerge.ClassicMode = true;
        options.MultiLayerMerge.Compress = true;

        _ = new PhiFansConverter().FromKpc(chart, options);

        chart.JudgeLineList[0].EventLayers.Should().HaveCount(2);
        chart.JudgeLineList[0].EventLayers[0].MoveXEvents.Should().ContainSingle();
        sourceEvent.StartBeat.Should().Be(Beat(0));
        sourceEvent.EndBeat.Should().Be(Beat(1));
        sourceEvent.StartValue.Should().Be(0);
        sourceEvent.EndValue.Should().Be(10);
        sourceEvent.EasingLeft.Should().Be(0);
        sourceEvent.EasingRight.Should().Be(1);
        sourceEvent.IsBezier.Should().BeTrue();
        sourceEvent.BezierPoints.Should().Equal(0.1f, 0.2f, 0.8f, 0.9f);
        secondEvent.StartValue.Should().Be(1);
        secondEvent.EndValue.Should().Be(2);
    }

    [Fact]
    public void FromKpc_ConsecutiveEventsKeepThreeNodeChainContinuous()
    {
        var chart = CreateChart(
            new KpcEvents.Event<double>
            {
                StartBeat = Beat(0),
                EndBeat = Beat(1),
                StartValue = 0,
                EndValue = 0.1,
            },
            new KpcEvents.Event<double>
            {
                StartBeat = Beat(1),
                EndBeat = Beat(2),
                StartValue = 0.1,
                EndValue = 0.2,
            },
            new KpcEvents.Event<double>
            {
                StartBeat = Beat(2),
                EndBeat = Beat(3),
                StartValue = 0.2,
                EndValue = 0.3,
            }
        );

        var result = new PhiFansConverter().FromKpc(
            chart,
            new KpcToPhiFansConvertOptions { DiscontinuityBeatPrecision = 64 }
        );
        var events = result.JudgeLineList[0].Props.PositionX;

        events.Should().HaveCount(4);
        events.Select(e => e.Continuous).Should().Equal(false, true, true, true);
        events.Select(e => (double)e.Beat).Should().Equal(0d, 1d, 2d, 3d);
        events.Select(e => e.Value).Should().Equal(0f, 10f, 20f, 30f);
    }

    [Fact]
    public void FromKpc_DiscontinuousAdjacentEventsUseConfiguredPrecision()
    {
        var chart = CreateChart(
            new KpcEvents.Event<double>
            {
                StartBeat = Beat(0),
                EndBeat = Beat(1),
                StartValue = 0,
                EndValue = 0.1,
            },
            new KpcEvents.Event<double>
            {
                StartBeat = Beat(1),
                EndBeat = Beat(2),
                StartValue = 0.2,
                EndValue = 0.3,
            }
        );

        var result = new PhiFansConverter().FromKpc(
            chart,
            new KpcToPhiFansConvertOptions { DiscontinuityBeatPrecision = 4096 }
        );
        var events = result.JudgeLineList[0].Props.PositionX;

        events.Should().HaveCount(4);
        events.Select(e => e.Continuous).Should().Equal(false, true, false, true);
        ((int[])events[2].Beat).Should().Equal(1, 1, 4096);
    }

    [Fact]
    public void RotationDirection_UsesPhiFansClockwiseConvention()
    {
        Pf.Chart.CoordinateSystem.ClockwiseRotation.Should().BeTrue();
        Kpc.Chart.CoordinateSystem.ClockwiseRotation.Should().BeFalse();

        var converter = new PhiFansConverter();
        var phiFansChart = new Pf.Chart
        {
            JudgeLineList =
            [
                new Pf.Line
                {
                    Props = new Pf.Props
                    {
                        Rotate =
                        [
                            new Pf.Event
                            {
                                Beat = Beat(0),
                                Value = 90,
                                Continuous = false,
                            },
                            new Pf.Event
                            {
                                Beat = Beat(1),
                                Value = 45,
                                Continuous = true,
                            },
                        ],
                    },
                },
            ],
        };

        var kpc = converter.ToKpc(phiFansChart, null);
        var kpcEvents = kpc.JudgeLineList[0].EventLayers[0].RotateEvents!;
        kpcEvents.Should().ContainSingle();
        kpcEvents[0].StartValue.Should().BeApproximately(-90, 1e-6);
        kpcEvents[0].EndValue.Should().BeApproximately(-45, 1e-6);

        var roundTrip = converter.FromKpc(
            kpc,
            new KpcToPhiFansConvertOptions { DiscontinuityBeatPrecision = 64 }
        );
        var roundTripEvents = roundTrip.JudgeLineList[0].Props.Rotate;
        roundTripEvents.Should().HaveCount(2);
        roundTripEvents[0].Value.Should().BeApproximately(90, 1e-6f);
        roundTripEvents[1].Value.Should().BeApproximately(45, 1e-6f);
    }

    private static Kpc.Chart CreateChart(params KpcEvents.Event<double>[] events) =>
        CreateChartWithLayers(new KpcEvents.EventLayer { MoveXEvents = events.ToList() });

    private static Kpc.Chart CreateChartWithLayers(params KpcEvents.EventLayer[] layers) =>
        new()
        {
            JudgeLineList =
            [
                new Kpc.JudgeLine
                {
                    EventLayers = layers.ToList(),
                },
            ],
        };

    private static KpcEvents.Event<double> CreateDoubleEvent(
        double startBeat,
        double endBeat,
        double startValue,
        double endValue,
        int easing = 1
    ) =>
        new()
        {
            StartBeat = Beat(startBeat),
            EndBeat = Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue,
            Easing = new Kpc.Easing(easing),
        };

    private static KpcEvents.Event<float> CreateFloatEvent(
        double startBeat,
        double endBeat,
        float startValue,
        float endValue,
        int easing = 1
    ) =>
        new()
        {
            StartBeat = Beat(startBeat),
            EndBeat = Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue,
            Easing = new Kpc.Easing(easing),
        };

    private static KpcToPhiFansConvertOptions CreateOptions() =>
        new()
        {
            Cutting = new KpcToPhiFansConvertOptions.CuttingOptions
            {
                UnsupportedEasingPrecision = 4,
            },
            MultiLayerMerge = new KpcToPhiFansConvertOptions.MultiLayerMergeOptions
            {
                Precision = 4,
                Tolerance = 0.1,
            },
        };

    private static void AssertLinearPhiFansNodes(List<Pf.Event> events)
    {
        events.Should().HaveCount(5);
        events.Select(e => (double)e.Beat).Should().Equal(0, 0.25, 0.5, 0.75, 1);
        events.Select(e => (int)e.Easing).Should().OnlyContain(easing => easing == 0);
    }

    private static void AssertMoveValues(
        List<KpcEvents.Event<double>> events,
        params double[] expected
    )
    {
        for (var i = 0; i < expected.Length; i++)
        {
            KpcEvents.EventLayer.GetValueAtBeat(events, Beat(i / 4d))
                .Should()
                .BeApproximately(expected[i], 1e-5);
        }
    }

    private static void AssertSpeedValues(
        List<KpcEvents.Event<float>> events,
        params double[] expected
    )
    {
        for (var i = 0; i < expected.Length; i++)
        {
            KpcEvents.EventLayer.GetValueAtBeat(events, Beat(i / 4d))
                .Should()
                .BeApproximately((float)expected[i], 1e-5f);
        }
    }

    private static Beat Beat(double value) => new(value);
}
