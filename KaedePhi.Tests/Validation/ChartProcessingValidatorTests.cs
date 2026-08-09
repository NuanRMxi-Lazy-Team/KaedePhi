using KaedePhi.Core.KaedePhi;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Render.KaedePhi;

namespace KaedePhi.Tests.Validation;

public class ChartProcessingValidatorTests
{
    [Fact]
    public void ValidatePrecision_RejectsNonPositiveAndNonFiniteValues()
    {
        Action zero = () => ChartProcessingValidator.ValidatePrecision(0);
        Action negative = () => ChartProcessingValidator.ValidatePrecision(-1);
        Action infinite = () => ChartProcessingValidator.ValidatePrecision(double.PositiveInfinity);

        zero.Should().Throw<ArgumentOutOfRangeException>();
        negative.Should().Throw<ArgumentOutOfRangeException>();
        infinite.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ValidateJudgeLineHierarchy_RejectsCyclesAndInvalidIndexes()
    {
        var cyclic = new List<JudgeLine>
        {
            new() { Father = 1 },
            new() { Father = 0 },
        };
        var invalid = new List<JudgeLine> { new() { Father = 2 } };

        Action cycleCheck = () => ChartProcessingValidator.ValidateJudgeLineHierarchy(cyclic);
        Action invalidCheck = () => ChartProcessingValidator.ValidateJudgeLineHierarchy(invalid);

        cycleCheck.Should().Throw<FormatException>();
        invalidCheck.Should().Throw<FormatException>();
    }

    [Fact]
    public void ValidateRender_RequiresLineForLayer()
    {
        var chart = new Chart { JudgeLineList = [new JudgeLine()] };
        var options = new KpcRenderOptions();

        Action act = () => ChartProcessingValidator.ValidateRender(chart, options, layerIndex: 0);

        act.Should().Throw<ArgumentException>();
    }
}
