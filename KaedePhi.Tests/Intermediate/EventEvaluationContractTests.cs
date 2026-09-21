using KaedePhi.Core.Primitives;
using Ir = KaedePhi.Core.Intermediate.Model;
using IrEvents = KaedePhi.Core.Intermediate.Model.Events;

namespace KaedePhi.Tests.Intermediate;

public class EventEvaluationContractTests
{
    [Fact]
    public void GetValueAtBeatAsDouble_BezierEvent_UsesBezierCurve()
    {
        var evt = CreateIrEvent(0, 2, 0, 100);
        evt.IsBezier = true;
        evt.BezierPoints = [0, 0, 1, 1];
        evt.Easing = new Ir.Easing(5);

        evt.GetValueAtBeatAsDouble(Beat(1)).Should().BeApproximately(50, 1e-6);
    }

    [Fact]
    public void GetValueAtBeat_BezierByteArray_IgnoresEasingBounds()
    {
        var evt = new IrEvents.Event<byte[]>
        {
            StartBeat = Beat(0),
            EndBeat = Beat(4),
            StartValue = [0],
            EndValue = [100],
            IsBezier = true,
            BezierPoints = [0, 0, 1, 1],
            EasingLeft = 0.2f,
            EasingRight = 0.8f,
        };

        evt.GetValueAtBeat(Beat(1)).Should().Equal(25);
    }

    [Fact]
    public void GetValueAtBeat_ZeroDuration_ReturnsEndValueAtExactBeat()
    {
        var evt = CreateIrEvent(3, 3, 10, 20);

        evt.GetValueAtBeat(Beat(3)).Should().Be(20);
    }

    [Fact]
    public void GetValueAtBeatAsDouble_ZeroDuration_ReturnsEndValueAtExactBeat()
    {
        var evt = CreateIrEvent(3, 3, 10, 20);

        evt.GetValueAtBeatAsDouble(Beat(3)).Should().Be(20);
    }

    [Fact]
    public void EventLayerGetValueAtBeat_ZeroDuration_ReturnsEndValueAtExactBeat()
    {
        var evt = CreateIrEvent(3, 3, 10, 20);

        IrEvents.EventLayer.GetValueAtBeat([evt], Beat(3)).Should().Be(20);
    }

    [Fact]
    public void EasingsEvaluate_EqualBounds_ReturnsLinearProgressAndFiniteEventValue()
    {
        Ir.Easings.Evaluate(5, 0.4, 0.4, 0.25).Should().BeApproximately(0.25, 1e-12);

        var evt = CreateIrEvent(0, 4, 0, 100);
        evt.Easing = new Ir.Easing(5);
        evt.EasingLeft = 0.4f;
        evt.EasingRight = 0.4f;

        var value = evt.GetValueAtBeat(Beat(1));
        double.IsFinite(value).Should().BeTrue();
        value.Should().BeApproximately(25, 1e-6);
    }

    [Fact]
    public void GetValueAtBeatAsDouble_IntEvent_PreservesContinuousPrecision()
    {
        var evt = new IrEvents.Event<int>
        {
            StartBeat = Beat(0),
            EndBeat = Beat(2),
            StartValue = 0,
            EndValue = 3,
            Easing = Ir.Easing.Linear,
        };

        evt.GetValueAtBeatAsDouble(Beat(1)).Should().Be(1.5);
        evt.GetValueAtBeat(Beat(1)).Should().Be(1);
    }

    [Fact]
    public void GetValueAtBeat_CollidingBeatRepresentations_PreservesNonzeroDurationBehavior()
    {
        var positive = CreateIrEvent(CollidingStartBeat(), CollidingEndBeat(), 10, 20);
        var reverse = CreateIrEvent(CollidingEndBeat(), CollidingStartBeat(), 10, 20);

        new[]
        {
            positive.GetValueAtBeat(positive.StartBeat),
            reverse.GetValueAtBeat(reverse.StartBeat),
        }
            .Should()
            .Equal(10, 10);
    }

    [Fact]
    public void GetValueAtBeatAsDouble_CollidingBeatRepresentations_PreservesNonzeroDurationBehavior()
    {
        var positive = CreateIrEvent(CollidingStartBeat(), CollidingEndBeat(), 10, 20);
        var reverse = CreateIrEvent(CollidingEndBeat(), CollidingStartBeat(), 10, 20);

        new[]
        {
            positive.GetValueAtBeatAsDouble(positive.StartBeat),
            reverse.GetValueAtBeatAsDouble(reverse.StartBeat),
        }
            .Should()
            .Equal(10, 10);
    }

    private static IrEvents.Event<double> CreateIrEvent(
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
            Easing = Ir.Easing.Linear,
        };

    private static IrEvents.Event<double> CreateIrEvent(
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
            Easing = Ir.Easing.Linear,
        };

    private static Beat Beat(double value) => new(value);

    private static Beat CollidingStartBeat() => new(new[] { 2147483646, 0, 1 });

    private static Beat CollidingEndBeat() => new(new[] { 2147483646, 1, 2000000000 });
}
