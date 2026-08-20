using KaedePhi.Core.Common;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Converter.PhiEdit.Utils;
using KpcSpeedEvent = KaedePhi.Core.KaedePhi.Events.Event<float>;
using PeJudgeLine = KaedePhi.Core.PhiEdit.JudgeLine;

namespace KaedePhi.Tests.Converter;

public class PhiEditSpeedConversionTests
{
    [Fact]
    public void ConvertSpeedFrames_UsesFixedKpcToPeSpeedRatio()
    {
        var target = new PeJudgeLine();
        var options = new KpcToPhiEditConvertOptions
        {
            Speed = new KpcToPhiEditConvertOptions.SpeedOptions { CutPrecision = 1d },
        };
        var source = new List<KpcSpeedEvent>
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
        var options = new KpcToPhiEditConvertOptions
        {
            Speed = new KpcToPhiEditConvertOptions.SpeedOptions { CutPrecision = 4d },
        };
        var source = new List<KpcSpeedEvent>
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
        var options = new KpcToPhiEditConvertOptions
        {
            Speed = new KpcToPhiEditConvertOptions.SpeedOptions { CutPrecision = 1d },
        };
        var source = new List<KpcSpeedEvent>
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
