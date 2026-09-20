using KaedePhi.Core.Primitives.Mathematics;

namespace KaedePhi.Tests.Utils;

public class EasingsTests
{
    private const double Tolerance = 1e-10;

    #region Boundary Tests

    [Theory]
    [MemberData(nameof(AllEasingFunctions))]
    public void AllEasings_AtZero_ReturnsZeroOrNearZero(Easings.EasingFunction func, string name)
    {
        // Some elastic/bounce functions may not return exactly 0
        var result = func(0.0);
        result.Should().BeInRange(-0.1, 0.1, because: $"{name} at t=0 should be near 0");
    }

    [Theory]
    [MemberData(nameof(AllEasingFunctions))]
    public void AllEasings_AtOne_ReturnsOneOrNearOne(Easings.EasingFunction func, string name)
    {
        // Some elastic/bounce functions may not return exactly 1
        var result = func(1.0);
        result.Should().BeInRange(0.9, 1.1, because: $"{name} at t=1 should be near 1");
    }

    #endregion

    #region Linear

    [Fact]
    public void Linear_AtHalf_ReturnsHalf()
    {
        Easings.Linear(0.5).Should().BeApproximately(0.5, Tolerance);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.25, 0.25)]
    [InlineData(0.5, 0.5)]
    [InlineData(0.75, 0.75)]
    [InlineData(1.0, 1.0)]
    public void Linear_IsIdentity(double input, double expected)
    {
        Easings.Linear(input).Should().BeApproximately(expected, Tolerance);
    }

    #endregion

    #region Quadratic

    [Fact]
    public void EaseInQuad_AtHalf_ReturnsQuarter()
    {
        Easings.EaseInQuad(0.5).Should().BeApproximately(0.25, Tolerance);
    }

    [Fact]
    public void EaseOutQuad_AtHalf_ReturnsThreeQuarters()
    {
        Easings.EaseOutQuad(0.5).Should().BeApproximately(0.75, Tolerance);
    }

    [Fact]
    public void EaseInOutQuad_AtHalf_ReturnsHalf()
    {
        Easings.EaseInOutQuad(0.5).Should().BeApproximately(0.5, Tolerance);
    }

    #endregion

    #region Cubic

    [Fact]
    public void EaseInCubic_AtHalf_ReturnsEighth()
    {
        Easings.EaseInCubic(0.5).Should().BeApproximately(0.125, Tolerance);
    }

    [Fact]
    public void EaseOutCubic_AtHalf_ReturnsSevenEighths()
    {
        Easings.EaseOutCubic(0.5).Should().BeApproximately(0.875, Tolerance);
    }

    [Fact]
    public void EaseInOutCubic_AtHalf_ReturnsHalf()
    {
        Easings.EaseInOutCubic(0.5).Should().BeApproximately(0.5, Tolerance);
    }

    #endregion

    #region Quartic

    [Fact]
    public void EaseInQuart_AtHalf_ReturnsSixteenth()
    {
        Easings.EaseInQuart(0.5).Should().BeApproximately(0.0625, Tolerance);
    }

    [Fact]
    public void EaseOutQuart_AtHalf_ReturnsFifteenSixteenths()
    {
        Easings.EaseOutQuart(0.5).Should().BeApproximately(0.9375, Tolerance);
    }

    [Fact]
    public void EaseInOutQuart_AtHalf_ReturnsHalf()
    {
        Easings.EaseInOutQuart(0.5).Should().BeApproximately(0.5, Tolerance);
    }

    #endregion

    #region Quintic

    [Fact]
    public void EaseInQuint_AtHalf_ReturnsThirtySecond()
    {
        Easings.EaseInQuint(0.5).Should().BeApproximately(0.03125, Tolerance);
    }

    [Fact]
    public void EaseOutQuint_AtHalf_ReturnsThirtyOneThirtySeconds()
    {
        Easings.EaseOutQuint(0.5).Should().BeApproximately(0.96875, Tolerance);
    }

    [Fact]
    public void EaseInOutQuint_AtHalf_ReturnsHalf()
    {
        Easings.EaseInOutQuint(0.5).Should().BeApproximately(0.5, Tolerance);
    }

    #endregion

    #region Sine

    [Fact]
    public void EaseInSine_AtHalf_ReturnsCorrectValue()
    {
        // 1 - cos(π/4) ≈ 0.2929
        Easings.EaseInSine(0.5).Should().BeApproximately(1 - Math.Cos(Math.PI / 4), Tolerance);
    }

    [Fact]
    public void EaseOutSine_AtHalf_ReturnsCorrectValue()
    {
        // sin(π/4) ≈ 0.7071
        Easings.EaseOutSine(0.5).Should().BeApproximately(Math.Sin(Math.PI / 4), Tolerance);
    }

    [Fact]
    public void EaseInOutSine_AtHalf_ReturnsHalf()
    {
        Easings.EaseInOutSine(0.5).Should().BeApproximately(0.5, Tolerance);
    }

    #endregion

    #region Exponential

    [Fact]
    public void EaseInExpo_AtZero_ReturnsZero()
    {
        Easings.EaseInExpo(0.0).Should().Be(0.0);
    }

    [Fact]
    public void EaseOutExpo_AtOne_ReturnsOne()
    {
        Easings.EaseOutExpo(1.0).Should().Be(1.0);
    }

    [Fact]
    public void EaseInOutExpo_AtZero_ReturnsZero()
    {
        Easings.EaseInOutExpo(0.0).Should().Be(0.0);
    }

    [Fact]
    public void EaseInOutExpo_AtOne_ReturnsOne()
    {
        Easings.EaseInOutExpo(1.0).Should().Be(1.0);
    }

    #endregion

    #region Circular

    [Fact]
    public void EaseInCirc_AtHalf_ReturnsCorrectValue()
    {
        // 1 - sqrt(1 - 0.25) ≈ 0.13397
        Easings.EaseInCirc(0.5).Should().BeApproximately(1 - Math.Sqrt(0.75), Tolerance);
    }

    [Fact]
    public void EaseOutCirc_AtHalf_ReturnsCorrectValue()
    {
        // sqrt(1 - 0.25) ≈ 0.86603
        Easings.EaseOutCirc(0.5).Should().BeApproximately(Math.Sqrt(0.75), Tolerance);
    }

    [Fact]
    public void EaseInOutCirc_AtHalf_ReturnsHalf()
    {
        Easings.EaseInOutCirc(0.5).Should().BeApproximately(0.5, Tolerance);
    }

    #endregion

    #region Back

    [Fact]
    public void EaseInBack_AtZero_ReturnsZero()
    {
        Easings.EaseInBack(0.0).Should().Be(0.0);
    }

    [Fact]
    public void EaseInBack_AtOne_ReturnsOne()
    {
        Easings.EaseInBack(1.0).Should().BeApproximately(1.0, Tolerance);
    }

    [Fact]
    public void EaseOutBack_AtZero_ReturnsZero()
    {
        Easings.EaseOutBack(0.0).Should().BeApproximately(0.0, Tolerance);
    }

    [Fact]
    public void EaseOutBack_AtOne_ReturnsOne()
    {
        Easings.EaseOutBack(1.0).Should().Be(1.0);
    }

    [Fact]
    public void EaseInOutBack_AtHalf_ReturnsHalf()
    {
        Easings.EaseInOutBack(0.5).Should().BeApproximately(0.5, Tolerance);
    }

    #endregion

    #region Elastic

    [Fact]
    public void EaseInElastic_AtZero_ReturnsZero()
    {
        Easings.EaseInElastic(0.0).Should().Be(0.0);
    }

    [Fact]
    public void EaseInElastic_AtOne_ReturnsOne()
    {
        Easings.EaseInElastic(1.0).Should().Be(1.0);
    }

    [Fact]
    public void EaseOutElastic_AtZero_ReturnsZero()
    {
        Easings.EaseOutElastic(0.0).Should().Be(0.0);
    }

    [Fact]
    public void EaseOutElastic_AtOne_ReturnsOne()
    {
        Easings.EaseOutElastic(1.0).Should().Be(1.0);
    }

    [Fact]
    public void EaseInOutElastic_AtZero_ReturnsZero()
    {
        Easings.EaseInOutElastic(0.0).Should().Be(0.0);
    }

    [Fact]
    public void EaseInOutElastic_AtOne_ReturnsOne()
    {
        Easings.EaseInOutElastic(1.0).Should().Be(1.0);
    }

    #endregion

    #region Bounce

    [Fact]
    public void EaseInBounce_AtZero_ReturnsZero()
    {
        Easings.EaseInBounce(0.0).Should().Be(0.0);
    }

    [Fact]
    public void EaseInBounce_AtOne_ReturnsOne()
    {
        Easings.EaseInBounce(1.0).Should().BeApproximately(1.0, Tolerance);
    }

    [Fact]
    public void EaseOutBounce_AtZero_ReturnsZero()
    {
        Easings.EaseOutBounce(0.0).Should().Be(0.0);
    }

    [Fact]
    public void EaseOutBounce_AtOne_ReturnsOne()
    {
        Easings.EaseOutBounce(1.0).Should().BeApproximately(1.0, Tolerance);
    }

    [Fact]
    public void EaseInOutBounce_AtHalf_ReturnsHalf()
    {
        Easings.EaseInOutBounce(0.5).Should().BeApproximately(0.5, Tolerance);
    }

    #endregion

    #region Monotonicity Tests

    [Theory]
    [MemberData(nameof(EaseInFunctions))]
    public void EaseIn_FunctionsAreMonotonicallyIncreasing(Easings.EasingFunction func, string name)
    {
        var previous = func(0.0);
        for (double t = 0.1; t <= 1.0; t += 0.1)
        {
            var current = func(t);
            current
                .Should()
                .BeGreaterThanOrEqualTo(
                    previous,
                    because: $"{name} should be monotonically increasing"
                );
            previous = current;
        }
    }

    #endregion

    #region Symmetry Tests

    [Theory]
    [InlineData(nameof(Easings.EaseInOutQuad))]
    [InlineData(nameof(Easings.EaseInOutCubic))]
    [InlineData(nameof(Easings.EaseInOutQuart))]
    [InlineData(nameof(Easings.EaseInOutQuint))]
    [InlineData(nameof(Easings.EaseInOutSine))]
    [InlineData(nameof(Easings.EaseInOutExpo))]
    [InlineData(nameof(Easings.EaseInOutCirc))]
    public void EaseInOut_FunctionsAreSymmetricAroundHalf(string functionName)
    {
        var func = GetEasingFunction(functionName);

        var atQuarter = func(0.25);
        var atThreeQuarter = func(0.75);

        // For symmetric functions: f(0.25) + f(0.75) should ≈ 1.0
        (atQuarter + atThreeQuarter)
            .Should()
            .BeApproximately(
                1.0,
                1e-6,
                because: $"{functionName} should be symmetric around t=0.5"
            );
    }

    #endregion

    #region Helper Methods and Data

    public static TheoryData<Easings.EasingFunction, string> AllEasingFunctions =>
        new()
        {
            { Easings.Linear, nameof(Easings.Linear) },
            { Easings.EaseInQuad, nameof(Easings.EaseInQuad) },
            { Easings.EaseOutQuad, nameof(Easings.EaseOutQuad) },
            { Easings.EaseInOutQuad, nameof(Easings.EaseInOutQuad) },
            { Easings.EaseInCubic, nameof(Easings.EaseInCubic) },
            { Easings.EaseOutCubic, nameof(Easings.EaseOutCubic) },
            { Easings.EaseInOutCubic, nameof(Easings.EaseInOutCubic) },
            { Easings.EaseInQuart, nameof(Easings.EaseInQuart) },
            { Easings.EaseOutQuart, nameof(Easings.EaseOutQuart) },
            { Easings.EaseInOutQuart, nameof(Easings.EaseInOutQuart) },
            { Easings.EaseInQuint, nameof(Easings.EaseInQuint) },
            { Easings.EaseOutQuint, nameof(Easings.EaseOutQuint) },
            { Easings.EaseInOutQuint, nameof(Easings.EaseInOutQuint) },
            { Easings.EaseInSine, nameof(Easings.EaseInSine) },
            { Easings.EaseOutSine, nameof(Easings.EaseOutSine) },
            { Easings.EaseInOutSine, nameof(Easings.EaseInOutSine) },
            { Easings.EaseInExpo, nameof(Easings.EaseInExpo) },
            { Easings.EaseOutExpo, nameof(Easings.EaseOutExpo) },
            { Easings.EaseInOutExpo, nameof(Easings.EaseInOutExpo) },
            { Easings.EaseInCirc, nameof(Easings.EaseInCirc) },
            { Easings.EaseOutCirc, nameof(Easings.EaseOutCirc) },
            { Easings.EaseInOutCirc, nameof(Easings.EaseInOutCirc) },
            { Easings.EaseInBack, nameof(Easings.EaseInBack) },
            { Easings.EaseOutBack, nameof(Easings.EaseOutBack) },
            { Easings.EaseInOutBack, nameof(Easings.EaseInOutBack) },
            { Easings.EaseInElastic, nameof(Easings.EaseInElastic) },
            { Easings.EaseOutElastic, nameof(Easings.EaseOutElastic) },
            { Easings.EaseInOutElastic, nameof(Easings.EaseInOutElastic) },
            { Easings.EaseInBounce, nameof(Easings.EaseInBounce) },
            { Easings.EaseOutBounce, nameof(Easings.EaseOutBounce) },
            { Easings.EaseInOutBounce, nameof(Easings.EaseInOutBounce) },
        };

    public static TheoryData<Easings.EasingFunction, string> EaseInFunctions =>
        new()
        {
            { Easings.EaseInQuad, nameof(Easings.EaseInQuad) },
            { Easings.EaseInCubic, nameof(Easings.EaseInCubic) },
            { Easings.EaseInQuart, nameof(Easings.EaseInQuart) },
            { Easings.EaseInQuint, nameof(Easings.EaseInQuint) },
            { Easings.EaseInSine, nameof(Easings.EaseInSine) },
            { Easings.EaseInExpo, nameof(Easings.EaseInExpo) },
            { Easings.EaseInCirc, nameof(Easings.EaseInCirc) },
        };

    private static Easings.EasingFunction GetEasingFunction(string name) =>
        name switch
        {
            nameof(Easings.EaseInOutQuad) => Easings.EaseInOutQuad,
            nameof(Easings.EaseInOutCubic) => Easings.EaseInOutCubic,
            nameof(Easings.EaseInOutQuart) => Easings.EaseInOutQuart,
            nameof(Easings.EaseInOutQuint) => Easings.EaseInOutQuint,
            nameof(Easings.EaseInOutSine) => Easings.EaseInOutSine,
            nameof(Easings.EaseInOutExpo) => Easings.EaseInOutExpo,
            nameof(Easings.EaseInOutCirc) => Easings.EaseInOutCirc,
            _ => throw new ArgumentException($"Unknown easing function: {name}"),
        };

    #endregion
}
