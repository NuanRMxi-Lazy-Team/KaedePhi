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

        new PhigrosV3JudgeLineBuilder(options, 120f, 2f, null).ConvertJudgeLine(line, [line]);

        var moveY = line.EventLayers[0].MoveYEvents;
        moveY.Should().NotBeNull();
        moveY![0].StartValue.Should().Be(4d);
        ((double)moveY[0].EndBeat).Should().BeApproximately(Math.Sqrt(0.5), 0.01);
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

        new PhigrosV3JudgeLineBuilder(options, 120f, 4f, null).ConvertJudgeLine(line, [line]);

        var value = KpcEvents.EventLayer.GetValueAtBeat(line.EventLayers[0].MoveYEvents!, Beat(2));
        value.Should().BeApproximately(4.2, 1e-6);
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

    private sealed class CollidingJudgeLine : JudgeLine
    {
        public override int GetHashCode() => 1;
    }
}
