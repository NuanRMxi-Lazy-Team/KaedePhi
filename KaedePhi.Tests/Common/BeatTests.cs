using KaedePhi.Core.Common;

namespace KaedePhi.Tests.Common;

public class BeatTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithIntArray_CorrectlyCalculatesBeat()
    {
        // Arrange & Act
        var beat = new Beat(new[] { 1, 1, 2 });

        // Assert
        beat[0].Should().Be(1);
        beat[1].Should().Be(1);
        beat[2].Should().Be(2);
        ((double)beat).Should().Be(1.5);
    }

    [Fact]
    public void Constructor_WithZeroBeat_CorrectlyCalculates()
    {
        var beat = new Beat(new[] { 0, 0, 1 });

        ((double)beat).Should().Be(0.0);
    }

    [Fact]
    public void Constructor_WithNullArray_ThrowsArgumentException()
    {
        var act = () => new Beat(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DefaultBeat_UsesSafeZeroRepresentation()
    {
        var beat = default(Beat);

        beat[0].Should().Be(0);
        beat[1].Should().Be(0);
        beat[2].Should().Be(1);
        beat.ToString().Should().Be("0:0/1");
        ((int[])beat).Should().Equal(0, 0, 1);
        (beat + new Beat(1d)).Should().Be(new Beat(1d));
    }

    [Fact]
    public void Constructor_WithInvalidLength_ThrowsArgumentException()
    {
        var act = () => new Beat(new[] { 1, 2 });

        act.Should().Throw<ArgumentException>().WithMessage("*3 elements*");
    }

    [Fact]
    public void Constructor_WithZeroDenominator_ThrowsArgumentException()
    {
        var act = () => new Beat(new[] { 1, 0, 0 });

        act.Should().Throw<ArgumentException>().WithMessage("*denominator*cannot be zero*");
    }

    [Fact]
    public void Constructor_WithNegativeDenominator_ThrowsArgumentException()
    {
        var act = () => new Beat(new[] { 1, 1, -2 });

        act.Should().Throw<ArgumentException>().WithMessage("*denominator*positive*");
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_WithNonFiniteValue_ThrowsArgumentOutOfRangeException(double value)
    {
        var act = () => new Beat(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithDouble_ConvertsToFraction()
    {
        var beat = new Beat(1.5);

        ((double)beat).Should().BeApproximately(1.5, 1e-9);
        beat[0].Should().Be(1);
    }

    [Fact]
    public void Constructor_WithDouble_HandlesWholeNumbers()
    {
        var beat = new Beat(3.0);

        ((double)beat).Should().BeApproximately(3.0, 1e-9);
        beat[0].Should().Be(3);
        beat[1].Should().Be(0);
        beat[2].Should().Be(1);
    }

    [Fact]
    public void Constructor_WithDouble_HandlesZero()
    {
        var beat = new Beat(0.0);

        ((double)beat).Should().Be(0.0);
    }

    [Theory]
    [InlineData(0.25, 0, 1, 4)]
    [InlineData(0.5, 0, 1, 2)]
    [InlineData(0.75, 0, 3, 4)]
    [InlineData(0.125, 0, 1, 8)]
    public void Constructor_WithDouble_FindsBestFractionApproximation(
        double input,
        int expectedWhole,
        int expectedNum,
        int expectedDen
    )
    {
        var beat = new Beat(input);

        beat[0].Should().Be(expectedWhole);
        beat[1].Should().Be(expectedNum);
        beat[2].Should().Be(expectedDen);
    }

    #endregion

    #region Indexer Tests

    [Fact]
    public void Indexer_Get_ValidIndex_ReturnsValue()
    {
        var beat = new Beat(new[] { 2, 3, 4 });

        beat[0].Should().Be(2);
        beat[1].Should().Be(3);
        beat[2].Should().Be(4);
    }

    [Fact]
    public void Indexer_Get_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        var beat = new Beat(new[] { 1, 0, 1 });

        var act = () => beat[-1];

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Indexer_Get_IndexTooLarge_ThrowsArgumentOutOfRangeException()
    {
        var beat = new Beat(new[] { 1, 0, 1 });

        var act = () => beat[3];

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Indexer_Get_ValidIndex_ReturnsCorrectValues()
    {
        var beat = new Beat(new[] { 1, 3, 4 });

        beat[0].Should().Be(1);
        beat[1].Should().Be(3);
        beat[2].Should().Be(4);
        ((double)beat).Should().Be(1.75);
    }

    [Fact]
    public void Indexer_Get_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        var beat = new Beat(new[] { 1, 0, 1 });

        var act = () =>
        {
            var _ = beat[-1];
        };

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Arithmetic Operator Tests

    [Fact]
    public void Add_TwoBeats_ReturnsCorrectSum()
    {
        var a = new Beat(new[] { 1, 1, 2 }); // 1.5
        var b = new Beat(new[] { 0, 1, 4 }); // 0.25

        var result = a + b;

        ((double)result).Should().BeApproximately(1.75, 1e-9);
    }

    [Fact]
    public void Add_WithCarry_HandlesCorrectly()
    {
        var a = new Beat(new[] { 0, 3, 4 }); // 0.75
        var b = new Beat(new[] { 0, 3, 4 }); // 0.75

        var result = a + b;

        ((double)result).Should().BeApproximately(1.5, 1e-9);
        result[0].Should().Be(1);
    }

    [Fact]
    public void Subtract_TwoBeats_ReturnsCorrectDifference()
    {
        var a = new Beat(new[] { 2, 1, 2 }); // 2.5
        var b = new Beat(new[] { 1, 1, 4 }); // 1.25

        var result = a - b;

        ((double)result).Should().BeApproximately(1.25, 1e-9);
    }

    [Fact]
    public void Subtract_WithBorrow_HandlesCorrectly()
    {
        var a = new Beat(new[] { 1, 0, 1 }); // 1.0
        var b = new Beat(new[] { 0, 3, 4 }); // 0.75

        var result = a - b;

        ((double)result).Should().BeApproximately(0.25, 1e-9);
    }

    [Fact]
    public void Add_ResultInZero_ReturnsSimplifiedBeat()
    {
        var a = new Beat(new[] { 1, 1, 2 }); // 1.5
        var b = new Beat(new[] { -1, -1, 2 }); // -1.5

        var result = a + b;

        ((double)result).Should().Be(0.0);
        result[1].Should().Be(0);
        result[2].Should().Be(1);
    }

    #endregion

    #region Comparison Operator Tests

    [Fact]
    public void Equal_SameValues_ReturnsTrue()
    {
        var a = new Beat(new[] { 1, 1, 2 });
        var b = new Beat(new[] { 1, 1, 2 });

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equal_DifferentValues_ReturnsFalse()
    {
        var a = new Beat(new[] { 1, 1, 2 });
        var b = new Beat(new[] { 1, 1, 3 });

        (a == b).Should().BeFalse();
    }

    [Fact]
    public void Equal_SameReference_ReturnsTrue()
    {
        var a = new Beat(new[] { 1, 1, 2 });
        var b = a; // Same reference

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equal_WithNull_ReturnsFalse()
    {
        var a = new Beat(new[] { 1, 1, 2 });

        (a == null).Should().BeFalse();
        (null == a).Should().BeFalse();
    }

    [Fact]
    public void NotEqual_DifferentValues_ReturnsTrue()
    {
        var a = new Beat(new[] { 1, 1, 2 });
        var b = new Beat(new[] { 1, 1, 3 });

        (a != b).Should().BeTrue();
    }

    [Fact]
    public void LessThan_ComparesCorrectly()
    {
        var a = new Beat(new[] { 0, 1, 2 }); // 0.5
        var b = new Beat(new[] { 1, 0, 1 }); // 1.0

        (a < b).Should().BeTrue();
        (b < a).Should().BeFalse();
    }

    [Fact]
    public void GreaterThan_ComparesCorrectly()
    {
        var a = new Beat(new[] { 2, 0, 1 }); // 2.0
        var b = new Beat(new[] { 1, 1, 2 }); // 1.5

        (a > b).Should().BeTrue();
        (b > a).Should().BeFalse();
    }

    [Fact]
    public void LessThanOrEqual_ComparesCorrectly()
    {
        var a = new Beat(new[] { 1, 1, 2 }); // 1.5
        var b = new Beat(new[] { 1, 1, 2 }); // 1.5
        var c = new Beat(new[] { 1, 1, 3 }); // 1.333...

        (a <= b).Should().BeTrue();
        (c <= a).Should().BeTrue();
    }

    [Fact]
    public void GreaterThanOrEqual_ComparesCorrectly()
    {
        var a = new Beat(new[] { 1, 1, 2 }); // 1.5
        var b = new Beat(new[] { 1, 1, 2 }); // 1.5
        var c = new Beat(new[] { 1, 1, 3 }); // 1.333...

        (a >= b).Should().BeTrue();
        (a >= c).Should().BeTrue();
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_ToDouble_ReturnsCurBeat()
    {
        var beat = new Beat(new[] { 1, 1, 2 });

        double value = beat;

        value.Should().Be(1.5);
    }

    [Fact]
    public void ImplicitConversion_ToFloat_ReturnsCurBeat()
    {
        var beat = new Beat(new[] { 1, 1, 2 });

        float value = beat;

        value.Should().Be(1.5f);
    }

    [Fact]
    public void ImplicitConversion_ToIntArray_ReturnsClone()
    {
        var beat = new Beat(new[] { 1, 2, 3 });

        int[] array = beat;

        array.Should().Equal(1, 2, 3);

        // Verify it's a clone (modifying doesn't affect original)
        array[0] = 99;
        beat[0].Should().Be(1);
    }

    #endregion

    #region Standard Method Tests

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        var beat = new Beat(new[] { 2, 3, 4 });

        beat.ToString().Should().Be("2:3/4");
    }

    [Fact]
    public void Equals_WithEqualBeat_ReturnsTrue()
    {
        var a = new Beat(new[] { 1, 1, 2 });
        var b = new Beat(new[] { 1, 1, 2 });

        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentBeat_ReturnsFalse()
    {
        var a = new Beat(new[] { 1, 1, 2 });
        var b = new Beat(new[] { 1, 1, 3 });

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var a = new Beat(new[] { 1, 1, 2 });

        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNonBeatObject_ReturnsFalse()
    {
        var a = new Beat(new[] { 1, 1, 2 });

        a.Equals("not a beat").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        var a = new Beat(new[] { 1, 1, 2 });
        var b = new Beat(new[] { 1, 1, 2 });

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void CompareTo_WithSmaller_ReturnsPositive()
    {
        var a = new Beat(new[] { 2, 0, 1 }); // 2.0
        var b = new Beat(new[] { 1, 0, 1 }); // 1.0

        a.CompareTo(b).Should().BePositive();
    }

    [Fact]
    public void CompareTo_WithLarger_ReturnsNegative()
    {
        var a = new Beat(new[] { 1, 0, 1 }); // 1.0
        var b = new Beat(new[] { 2, 0, 1 }); // 2.0

        a.CompareTo(b).Should().BeNegative();
    }

    [Fact]
    public void CompareTo_WithEqual_ReturnsZero()
    {
        var a = new Beat(new[] { 1, 1, 2 });
        var b = new Beat(new[] { 1, 1, 2 });

        a.CompareTo(b).Should().Be(0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void LargeDenominator_HandlesCorrectly()
    {
        var beat = new Beat(new[] { 0, 999, 1000 });

        ((double)beat).Should().BeApproximately(0.999, 1e-6);
    }

    [Fact]
    public void NegativeWholePart_HandlesCorrectly()
    {
        var beat = new Beat(new[] { -2, 1, 4 });

        ((double)beat).Should().BeApproximately(-1.75, 1e-9);
    }

    [Fact]
    public void Add_RepeatedOperations_DoesNotOverflow()
    {
        var beat = new Beat(new[] { 0, 1, 1000 });
        var increment = new Beat(new[] { 0, 1, 1000 });

        for (int i = 0; i < 100; i++)
        {
            beat += increment;
        }

        // Repeated fractional additions accumulate rounding error
        ((double)beat)
            .Should()
            .BeApproximately(0.1, 0.01);
    }

    #endregion
}
