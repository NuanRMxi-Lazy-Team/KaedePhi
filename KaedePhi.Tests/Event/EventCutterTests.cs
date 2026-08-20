using KaedePhi.Core.Common;
using KaedePhi.Tool.Event.KaedePhi;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;

namespace KaedePhi.Tests.Event;

public class EventCutterTests
{
    private readonly EventCutter<double> _cutter = new();

    [Fact(Timeout = 1_000)]
    public void CutEventToLinear_WithMaximumPrecision_ProducesStrictlyAdvancingSegments()
    {
        var evt = new KpcEvents.Event<double>
        {
            StartBeat = new Beat([0, 0, 1]),
            EndBeat = new Beat([0, 1, 512]),
            StartValue = 0d,
            EndValue = 2d,
        };

        var result = _cutter.CutEventToLinear(evt, 1d / 1024d);

        result.Should().HaveCount(2);
        result[0].StartBeat.Should().Be(new Beat([0, 0, 1]));
        result[0].EndBeat.Should().Be(new Beat([0, 1, 1024]));
        result[1].StartBeat.Should().Be(result[0].EndBeat);
        result[1].EndBeat.Should().Be(evt.EndBeat);
        result.Should().OnlyContain(segment => segment.EndBeat > segment.StartBeat);
    }

    [Fact(Timeout = 1_000)]
    public void CutEventToLinear_WithUnrepresentableStep_ThrowsArgumentOutOfRangeException()
    {
        var evt = new KpcEvents.Event<double>
        {
            StartBeat = new Beat([0, 0, 1]),
            EndBeat = new Beat([0, 1, 1024]),
            StartValue = 0d,
            EndValue = 1d,
        };

        var act = () => _cutter.CutEventToLinear(evt, 1d / 2048d);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
