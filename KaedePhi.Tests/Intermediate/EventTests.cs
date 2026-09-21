using KaedePhi.Core.Primitives;
using KaedePhi.Core.Intermediate.Model;
using IrEvents = KaedePhi.Core.Intermediate.Model.Events;

namespace KaedePhi.Tests.Intermediate;

public class EventTests
{
    #region GetValueAtBeat Tests

    [Fact]
    public void GetValueAtBeat_AtStart_ReturnsStartValue()
    {
        var evt = new IrEvents.Event<double>
        {
            StartBeat = new Beat(new[] { 0, 0, 1 }),
            EndBeat = new Beat(new[] { 1, 0, 1 }),
            StartValue = 10.0,
            EndValue = 20.0,
            Easing = Easing.Linear, // Linear
        };

        var result = evt.GetValueAtBeat(new Beat(new[] { 0, 0, 1 }));

        result.Should().Be(10.0);
    }

    [Fact]
    public void GetValueAtBeat_AtEnd_ReturnsEndValue()
    {
        var evt = new IrEvents.Event<double>
        {
            StartBeat = new Beat(new[] { 0, 0, 1 }),
            EndBeat = new Beat(new[] { 1, 0, 1 }),
            StartValue = 10.0,
            EndValue = 20.0,
            Easing = Easing.Linear, // Linear
        };

        var result = evt.GetValueAtBeat(new Beat(new[] { 1, 0, 1 }));

        result.Should().Be(20.0);
    }

    [Fact]
    public void GetValueAtBeat_AtMiddle_ReturnsInterpolatedValue()
    {
        var evt = new IrEvents.Event<double>
        {
            StartBeat = new Beat(new[] { 0, 0, 1 }),
            EndBeat = new Beat(new[] { 2, 0, 1 }),
            StartValue = 0.0,
            EndValue = 100.0,
            Easing = Easing.Linear, // Linear
        };

        var result = evt.GetValueAtBeat(new Beat(new[] { 1, 0, 1 }));

        result.Should().BeApproximately(50.0, 1e-10);
    }

    [Fact]
    public void GetValueAtBeat_BeforeStart_ReturnsStartValue()
    {
        var evt = new IrEvents.Event<double>
        {
            StartBeat = new Beat(new[] { 1, 0, 1 }),
            EndBeat = new Beat(new[] { 2, 0, 1 }),
            StartValue = 10.0,
            EndValue = 20.0,
            Easing = Easing.Linear,
        };

        var result = evt.GetValueAtBeat(new Beat(new[] { 0, 0, 1 }));

        result.Should().Be(10.0);
    }

    [Fact]
    public void GetValueAtBeat_AfterEnd_ReturnsEndValue()
    {
        var evt = new IrEvents.Event<double>
        {
            StartBeat = new Beat(new[] { 0, 0, 1 }),
            EndBeat = new Beat(new[] { 1, 0, 1 }),
            StartValue = 10.0,
            EndValue = 20.0,
            Easing = Easing.Linear,
        };

        var result = evt.GetValueAtBeat(new Beat(new[] { 2, 0, 1 }));

        result.Should().Be(20.0);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(50.0, 50.0)]
    [InlineData(100.0, 100.0)]
    public void GetValueAtBeat_WithFloat_AtStart_ReturnsStartValue(float startVal, float expected)
    {
        var evt = new IrEvents.Event<float>
        {
            StartBeat = new Beat(new[] { 0, 0, 1 }),
            EndBeat = new Beat(new[] { 1, 0, 1 }),
            StartValue = startVal,
            EndValue = 200.0f,
            Easing = Easing.Linear,
        };

        // At StartBeat (t=0), returns StartValue
        var result = evt.GetValueAtBeat(new Beat(new[] { 0, 0, 1 }));

        result.Should().Be(expected);
    }

    [Fact]
    public void GetValueAtBeat_WithInt_AtMiddle_ReturnsInterpolatedValue()
    {
        var evt = new IrEvents.Event<int>
        {
            StartBeat = new Beat(new[] { 0, 0, 1 }),
            EndBeat = new Beat(new[] { 2, 0, 1 }),
            StartValue = 0,
            EndValue = 100,
            Easing = Easing.Linear,
        };

        // At beat 1 (middle of [0,2]), should return 50
        var result = evt.GetValueAtBeat(new Beat(new[] { 1, 0, 1 }));

        result.Should().Be(50);
    }

    #endregion

    #region GetValueAtBeatAsDouble Tests

    [Fact]
    public void GetValueAtBeatAsDouble_AtMiddle_ReturnsInterpolatedValue()
    {
        var evt = new IrEvents.Event<float>
        {
            StartBeat = new Beat(new[] { 0, 0, 1 }),
            EndBeat = new Beat(new[] { 2, 0, 1 }),
            StartValue = 0.0f,
            EndValue = 100.0f,
            Easing = Easing.Linear,
        };

        // At beat 1 (middle of [0,2]), should return 50
        var result = evt.GetValueAtBeatAsDouble(new Beat(new[] { 1, 0, 1 }));

        result.Should().BeApproximately(50.0, 1e-10);
    }

    [Fact]
    public void GetStartValueAsDouble_WithDouble_ReturnsDirectly()
    {
        var evt = new IrEvents.Event<double> { StartValue = 42.5 };

        evt.GetStartValueAsDouble().Should().Be(42.5);
    }

    [Fact]
    public void GetStartValueAsDouble_WithFloat_ConvertsCorrectly()
    {
        var evt = new IrEvents.Event<float> { StartValue = 42.5f };

        evt.GetStartValueAsDouble().Should().BeApproximately(42.5, 1e-6);
    }

    [Fact]
    public void GetStartValueAsDouble_WithInt_ConvertsCorrectly()
    {
        var evt = new IrEvents.Event<int> { StartValue = 42 };

        evt.GetStartValueAsDouble().Should().Be(42.0);
    }

    [Fact]
    public void GetStartValueAsSingle_WithFloat_ReturnsDirectly()
    {
        var evt = new IrEvents.Event<float> { StartValue = 42.5f };

        evt.GetStartValueAsSingle().Should().Be(42.5f);
    }

    [Fact]
    public void GetStartValueAsSingle_WithDouble_ConvertsCorrectly()
    {
        var evt = new IrEvents.Event<double> { StartValue = 42.5 };

        evt.GetStartValueAsSingle().Should().Be(42.5f);
    }

    [Fact]
    public void GetStartValueAsInt32_WithInt_ReturnsDirectly()
    {
        var evt = new IrEvents.Event<int> { StartValue = 42 };

        evt.GetStartValueAsInt32().Should().Be(42);
    }

    [Fact]
    public void GetStartValueAsInt32_WithDouble_TruncatesCorrectly()
    {
        var evt = new IrEvents.Event<double> { StartValue = 42.7 };

        evt.GetStartValueAsInt32().Should().Be(42);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_WithDouble_CreatesIndependentCopy()
    {
        var original = new IrEvents.Event<double>
        {
            StartBeat = new Beat(new[] { 0, 0, 1 }),
            EndBeat = new Beat(new[] { 1, 0, 1 }),
            StartValue = 10.0,
            EndValue = 20.0,
            Easing = Easing.Linear,
            EasingLeft = 0.1f,
            EasingRight = 0.9f,
            IsBezier = false,
        };

        var clone = original.Clone();

        clone.StartValue.Should().Be(original.StartValue);
        clone.EndValue.Should().Be(original.EndValue);
        clone.EasingLeft.Should().Be(original.EasingLeft);
        clone.EasingRight.Should().Be(original.EasingRight);

        // Verify independence
        clone.StartValue = 999.0;
        original.StartValue.Should().Be(10.0);
    }

    [Fact]
    public void Clone_WithFloat_CreatesIndependentCopy()
    {
        var original = new IrEvents.Event<float>
        {
            StartValue = 1.0f,
            EndValue = 2.0f,
            StartBeat = new Beat(new[] { 0, 0, 1 }),
            EndBeat = new Beat(new[] { 1, 0, 1 }),
        };

        var clone = original.Clone();

        clone.StartValue.Should().Be(1.0f);
        clone.EndValue.Should().Be(2.0f);

        clone.StartValue = 999.0f;
        original.StartValue.Should().Be(1.0f);
    }

    [Fact]
    public void Clone_WithInt_CreatesIndependentCopy()
    {
        var original = new IrEvents.Event<int>
        {
            StartValue = 10,
            EndValue = 20,
            StartBeat = new Beat(new[] { 0, 0, 1 }),
            EndBeat = new Beat(new[] { 1, 0, 1 }),
        };

        var clone = original.Clone();

        clone.StartValue.Should().Be(10);
        clone.EndValue.Should().Be(20);

        clone.StartValue = 999;
        original.StartValue.Should().Be(10);
    }

    [Fact]
    public void Clone_WithByteArray_CreatesDeepCopy()
    {
        var original = new IrEvents.Event<byte[]>
        {
            StartValue = new byte[] { 1, 2, 3 },
            EndValue = new byte[] { 4, 5, 6 },
            StartBeat = new Beat(new[] { 0, 0, 1 }),
            EndBeat = new Beat(new[] { 1, 0, 1 }),
        };

        var clone = original.Clone();

        clone.StartValue.Should().Equal(1, 2, 3);
        clone.EndValue.Should().Equal(4, 5, 6);

        // Verify deep copy
        clone.StartValue[0] = 99;
        original.StartValue[0].Should().Be(1);
    }

    [Fact]
    public void Clone_WithBezierPoints_CopiesArray()
    {
        var original = new IrEvents.Event<double>
        {
            IsBezier = true,
            BezierPoints = new float[] { 0.1f, 0.2f, 0.3f, 0.4f },
            StartValue = 0.0,
            EndValue = 1.0,
            StartBeat = new Beat(new[] { 0, 0, 1 }),
            EndBeat = new Beat(new[] { 1, 0, 1 }),
        };

        var clone = original.Clone();

        clone.BezierPoints.Should().Equal(0.1f, 0.2f, 0.3f, 0.4f);

        // Verify independence
        clone.BezierPoints[0] = 0.99f;
        original.BezierPoints[0].Should().Be(0.1f);
    }

    [Fact]
    public void Clone_CopiesBeatCorrectly()
    {
        var original = new IrEvents.Event<double>
        {
            StartBeat = new Beat(new[] { 1, 1, 2 }),
            EndBeat = new Beat(new[] { 3, 1, 4 }),
            StartValue = 0.0,
            EndValue = 1.0,
        };

        var clone = original.Clone();

        ((double)clone.StartBeat).Should().Be(1.5);
        ((double)clone.EndBeat).Should().Be(3.25);

        // Verify independence: modifying clone doesn't affect original
        clone.StartBeat = new Beat(new[] { 99, 0, 1 });
        ((double)original.StartBeat).Should().Be(1.5);
    }

    #endregion

    #region EasingLeft/EasingRight Tests

    [Fact]
    public void GetValueAtBeat_WithEasingLeft_ClampsCorrectly()
    {
        var evt = new IrEvents.Event<double>
        {
            StartBeat = new Beat(new[] { 0, 0, 1 }),
            EndBeat = new Beat(new[] { 2, 0, 1 }),
            StartValue = 0.0,
            EndValue = 100.0,
            Easing = Easing.Linear, // Linear
            EasingLeft = 0.2f,
            EasingRight = 0.8f,
        };

        // At beat 1 (middle of [0,2])
        var result = evt.GetValueAtBeat(new Beat(new[] { 1, 0, 1 }));

        // With EasingLeft=0.2, EasingRight=0.8, the effective range is [20, 80]
        // at t=0.5 the interpolated value should be around 50
        result.Should().BeGreaterThan(0.0);
        result.Should().BeLessThan(100.0);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void NewEvent_HasDefaultValues()
    {
        var evt = new IrEvents.Event<double>();

        evt.IsBezier.Should().BeFalse();
        evt.EasingLeft.Should().Be(0.0f);
        evt.EasingRight.Should().Be(1.0f);
        evt.BezierPoints.Should().HaveCount(4);
        evt.BezierPoints.Should().AllBeEquivalentTo(0.0f);
    }

    [Fact]
    public void NewEvent_HasDefaultBeats()
    {
        var evt = new IrEvents.Event<double>();

        ((double)evt.StartBeat).Should().Be(0.0);
        ((double)evt.EndBeat).Should().Be(1.0);
    }

    #endregion
}
