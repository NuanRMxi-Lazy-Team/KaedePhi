using KaedePhi.Core.Common;
using Rpe = KaedePhi.Core.RePhiEdit;
using RpeEvents = KaedePhi.Core.RePhiEdit.Events;

namespace KaedePhi.Tests.RePhiEdit;

public class RpeEventTests
{
    [Fact]
    public void GetValueAtBeat_ZeroDuration_ReturnsEndValueAtExactBeat()
    {
        var evt = CreateRpeEvent(3, 3, 10, 20);

        evt.GetValueAtBeat(Beat(3)).Should().Be(20);
    }

    [Fact]
    public void EventLayerGetValueAtBeat_ZeroDuration_ReturnsEndValueAtExactBeat()
    {
        var evt = CreateRpeEvent(3, 3, 10, 20);

        RpeEvents.EventLayer.GetValueAtBeat([evt], Beat(3)).Should().Be(20);
    }

    [Fact]
    public void GetValueAtBeat_BezierEvent_IgnoresEasingBounds()
    {
        var evt = CreateRpeEvent(0, 4, 0, 100);
        evt.IsBezier = true;
        evt.BezierPoints = [0, 0, 1, 1];
        evt.EasingLeft = 0.2f;
        evt.EasingRight = 0.8f;

        evt.GetValueAtBeat(Beat(1)).Should().BeApproximately(25, 1e-6);
    }

    [Fact]
    public void EasingsEvaluate_EqualBounds_ReturnsLinearProgressAndFiniteEventValue()
    {
        Rpe.Easings.Evaluate(1, 0.4, 0.4, 0.25).Should().BeApproximately(0.25, 1e-12);

        var evt = CreateRpeEvent(0, 4, 0, 100);
        evt.EasingLeft = 0.4f;
        evt.EasingRight = 0.4f;

        var value = evt.GetValueAtBeat(Beat(1));
        double.IsFinite(value).Should().BeTrue();
        value.Should().BeApproximately(25, 1e-6);
    }

    [Fact]
    public void GetValueAtBeat_CollidingBeatRepresentations_PreservesNonzeroDurationBehavior()
    {
        var positive = CreateRpeEvent(CollidingStartBeat(), CollidingEndBeat(), 10, 20);
        var reverse = CreateRpeEvent(CollidingEndBeat(), CollidingStartBeat(), 10, 20);

        new[]
        {
            positive.GetValueAtBeat(positive.StartBeat),
            reverse.GetValueAtBeat(reverse.StartBeat),
        }.Should().Equal(10, 10);
    }

    private static RpeEvents.Event<double> CreateRpeEvent(
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
            Easing = Rpe.Easing.Linear,
        };

    private static RpeEvents.Event<double> CreateRpeEvent(
        Beat startBeat,
        Beat endBeat,
        double startValue,
        double endValue
    ) =>
        new()
        {
            StartBeat = startBeat,
            EndBeat = endBeat,
            StartValue = startValue,
            EndValue = endValue,
            Easing = Rpe.Easing.Linear,
        };

    private static Beat Beat(double value) => new(value);

    private static Beat CollidingStartBeat() => new(new[] { 2147483646, 0, 1 });

    private static Beat CollidingEndBeat() => new(new[] { 2147483646, 1, 2000000000 });
}
