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
        events.Select(e => (double)e.Beat)
            .Should()
            .Equal(0d, 1d, 2d, 3d);
        events.Select(e => e.Value)
            .Should()
            .Equal(0f, 10f, 20f, 30f);
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
        new()
        {
            JudgeLineList =
            [
                new Kpc.JudgeLine
                {
                    EventLayers =
                    [
                        new KpcEvents.EventLayer { MoveXEvents = events.ToList() },
                    ],
                },
            ],
        };

    private static Beat Beat(double value) => new(value);
}
