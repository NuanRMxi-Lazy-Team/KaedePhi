using System.Reflection;
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

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void NormalizeAndValidateNoteEndBeats_RejectsInvalidBpmAtProcessingBoundary(float bpm)
    {
        var chart = new Chart { BpmList = [CreateBpmItemBypassingValidation(bpm)] };

        Action act = () => ChartProcessingValidator.NormalizeAndValidateNoteEndBeats(chart);

        act.Should().Throw<FormatException>().WithMessage("*BPM*0*");
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(0f)]
    public void NormalizeAndValidateNoteEndBeats_RejectsInvalidBpmFactorAtProcessingBoundary(
        float bpmFactor
    )
    {
        var line = new JudgeLine();
        SetBpmFactorBypassingValidation(line, bpmFactor);
        var chart = new Chart { JudgeLineList = [line] };

        Action act = () => ChartProcessingValidator.NormalizeAndValidateNoteEndBeats(chart);

        act.Should().Throw<FormatException>().WithMessage("*判定线 0*BPM*");
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ValidateRender_RejectsNonFiniteBpmFactorAtProcessingBoundary(float bpmFactor)
    {
        var line = new JudgeLine();
        SetBpmFactorBypassingValidation(line, bpmFactor);
        var chart = new Chart { JudgeLineList = [line] };

        Action act = () => ChartProcessingValidator.ValidateRender(chart, new KpcRenderOptions());

        act.Should().Throw<FormatException>().WithMessage("*判定线 0*BPM*");
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void BpmItem_BpmRejectsNonFiniteOrNonPositiveValue(float bpm)
    {
        var item = new BpmItem();

        Action act = () => item.Bpm = bpm;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void JudgeLine_BpmFactorRejectsNonFiniteOrNonPositiveValue(float bpmFactor)
    {
        var line = new JudgeLine();

        Action act = () => line.BpmFactor = bpmFactor;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static BpmItem CreateBpmItemBypassingValidation(float bpm)
    {
        var item = new BpmItem();
        typeof(BpmItem)
            .GetField("_bpm", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(item, bpm);
        return item;
    }

    private static void SetBpmFactorBypassingValidation(JudgeLine line, float bpmFactor)
    {
        var field = typeof(JudgeLine)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(field => field.Name.Contains("BpmFactor", StringComparison.OrdinalIgnoreCase));
        field.SetValue(line, bpmFactor);
    }
}
