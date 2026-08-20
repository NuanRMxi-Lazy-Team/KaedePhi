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
}
