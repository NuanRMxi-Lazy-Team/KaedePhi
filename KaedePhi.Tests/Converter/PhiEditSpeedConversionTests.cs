using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Converter.PhiEdit;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Converter.PhiEdit.Utils;
using IrSpeedEvent = KaedePhi.Core.Intermediate.Events.Event<float>;
using PeChart = KaedePhi.Core.Formats.PhiEdit.Chart;
using PeEvent = KaedePhi.Core.Formats.PhiEdit.Event;
using PeFrame = KaedePhi.Core.Formats.PhiEdit.Frame;
using PeJudgeLine = KaedePhi.Core.Formats.PhiEdit.JudgeLine;
using PeMoveEvent = KaedePhi.Core.Formats.PhiEdit.MoveEvent;

namespace KaedePhi.Tests.Converter;

public class PhiEditSpeedConversionTests
{
    [Fact]
    public void ToIr_ConvertsEventsWhenMoveAndRotateFramesAreMissing()
    {
        var source = new PeChart
        {
            JudgeLineList =
            [
                new PeJudgeLine
                {
                    MoveEvents =
                    [
                        new PeMoveEvent
                        {
                            StartBeat = 1f,
                            EndBeat = 2f,
                            EndXValue = 1024f,
                            EndYValue = 700f,
                        },
                    ],
                    RotateEvents =
                    [
                        new PeEvent
                        {
                            StartBeat = 1f,
                            EndBeat = 2f,
                            EndValue = 45f,
                        },
                    ],
                    SpeedFrames = [new PeFrame(0f, 14f)],
                },
            ],
        };

        var converted = new PhiEditConverter().ToIr(source, new PhiEditToIrConvertOptions());
        var layer = converted
            .JudgeLineList.Should()
            .ContainSingle()
            .Subject.EventLayers.Should()
            .ContainSingle()
            .Subject;

        layer.MoveXEvents.Should().NotBeNullOrEmpty();
        layer.MoveYEvents.Should().NotBeNullOrEmpty();
        layer.RotateEvents.Should().NotBeNullOrEmpty();
        layer.SpeedEvents.Should().ContainSingle();
        layer.SpeedEvents![0].StartValue.Should().BeApproximately(9f, 1e-6f);
        ((double)layer.SpeedEvents[0].EndBeat).Should().BeApproximately(1d / 64d, 1e-9d);
    }

    [Fact]
    public void ConvertSpeedFrames_UsesFixedIrToPeSpeedRatio()
    {
        var target = new PeJudgeLine();
        var options = new IrToPhiEditConvertOptions
        {
            Speed = new IrToPhiEditConvertOptions.SpeedOptions { CutPrecision = 1d },
        };
        var source = new List<IrSpeedEvent>
        {
            new()
            {
                StartBeat = new Beat(0d),
                EndBeat = new Beat(1d),
                StartValue = 9f,
                EndValue = 9f,
            },
        };

        new LineEventBuilder(options).ConvertSpeedFrames(target, source);

        target.SpeedFrames.Should().NotBeEmpty();
        target.SpeedFrames[0].Value.Should().BeApproximately(14f, 1e-6f);
    }

    [Fact]
    public void ConvertSpeedFrames_EmitsFirstStartAndEverySliceEnd()
    {
        var target = new PeJudgeLine();
        var options = new IrToPhiEditConvertOptions
        {
            Speed = new IrToPhiEditConvertOptions.SpeedOptions { CutPrecision = 4d },
        };
        var source = new List<IrSpeedEvent>
        {
            new()
            {
                StartBeat = new Beat(0d),
                EndBeat = new Beat(1d),
                StartValue = 9f,
                EndValue = 18f,
            },
        };

        new LineEventBuilder(options).ConvertSpeedFrames(target, source);

        target.SpeedFrames.Select(f => f.Beat).Should().Equal(0f, 0.25f, 0.5f, 0.75f, 1f);
        target.SpeedFrames.Select(f => f.Value).Should().Equal(14f, 17.5f, 21f, 24.5f, 28f);
    }

    [Fact]
    public void ConvertSpeedFrames_AdjacentEventsUseLaterStartAtSharedBeat()
    {
        var target = new PeJudgeLine();
        var options = new IrToPhiEditConvertOptions
        {
            Speed = new IrToPhiEditConvertOptions.SpeedOptions { CutPrecision = 1d },
        };
        var source = new List<IrSpeedEvent>
        {
            new()
            {
                StartBeat = new Beat(0d),
                EndBeat = new Beat(1d),
                StartValue = 9f,
                EndValue = 18f,
            },
            new()
            {
                StartBeat = new Beat(1d),
                EndBeat = new Beat(2d),
                StartValue = 27f,
                EndValue = 27f,
            },
        };

        new LineEventBuilder(options).ConvertSpeedFrames(target, source);

        target.SpeedFrames.Select(f => f.Beat).Should().Equal(0f, 1f, 2f);
        target.SpeedFrames.Select(f => f.Value).Should().Equal(14f, 42f, 42f);
    }
}
