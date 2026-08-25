using System.Reflection;
using KaedePhi.Core.Common;
using KaedePhi.Core.KaedePhi;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.KaedePhi;
using KaedePhi.Tool.Render.KaedePhi;

namespace KaedePhi.Tests.Validation;

public class ValidationComponentTests
{
    [Fact]
    public void ValidatePrecision_RejectsNonPositiveAndNonFiniteValues()
    {
        Action zero = () => NumericParameterValidator.ValidatePrecision(0);
        Action negative = () => NumericParameterValidator.ValidatePrecision(-1);
        Action infinite = () =>
            NumericParameterValidator.ValidatePrecision(double.PositiveInfinity);

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

        Action cycleCheck = () => KpcChartValidator.ValidateJudgeLineHierarchy(cyclic);
        Action invalidCheck = () => KpcChartValidator.ValidateJudgeLineHierarchy(invalid);

        cycleCheck.Should().Throw<FormatException>();
        invalidCheck.Should().Throw<FormatException>();
    }

    [Fact]
    public void ValidateRender_RequiresLineForLayer()
    {
        var chart = new Chart { JudgeLineList = [new JudgeLine()] };
        var options = new KpcRenderOptions();

        Action act = () => KpcRenderValidator.Validate(chart, options, layerIndex: 0);

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

        Action act = () => KpcChartNormalizer.NormalizeAndValidateNoteEndBeats(chart);

        act.Should().Throw<FormatException>().WithMessage("*BPM*0*");
    }

    [Fact]
    public void NormalizeAndValidateNoteEndBeats_AllowsNegativeBpmStartBeat()
    {
        var chart = new Chart { BpmList = [new BpmItem { StartBeat = new Beat(-1), Bpm = 120f }] };

        Action act = () => KpcChartNormalizer.NormalizeAndValidateNoteEndBeats(chart);

        act.Should().NotThrow();
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

        Action act = () => KpcChartNormalizer.NormalizeAndValidateNoteEndBeats(chart);

        act.Should().Throw<FormatException>().WithMessage("*判定线 0*BPM*");
    }

    [Theory]
    [InlineData("Notes")]
    [InlineData("EventLayers")]
    [InlineData("Layer")]
    public void NormalizeAndValidateNoteEndBeats_AllowsNullCollections(string malformedMember)
    {
        var chart = CreateChartWithMalformedStructure(malformedMember);

        Action act = () => KpcChartNormalizer.NormalizeAndValidateNoteEndBeats(chart);

        act.Should().NotThrow();
    }

    [Fact]
    public void KaedePhiConverter_FromKpcAllowsNullCollections()
    {
        var chart = CreateChartWithMalformedStructure("Notes");

        Action act = () => new KaedePhiConverter().FromKpc(chart, null);

        act.Should().NotThrow();
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

    private static Chart CreateChartWithMalformedStructure(string malformedMember)
    {
        var line = new JudgeLine();
        switch (malformedMember)
        {
            case "Notes":
                line.Notes = null!;
                break;
            case "EventLayers":
                line.EventLayers = null!;
                break;
            case "Layer":
                line.EventLayers = [null!];
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(malformedMember));
        }

        return new Chart { JudgeLineList = [line] };
    }

    private static void SetBpmFactorBypassingValidation(JudgeLine line, float bpmFactor)
    {
        var field = typeof(JudgeLine)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(field => field.Name.Contains("BpmFactor", StringComparison.OrdinalIgnoreCase));
        field.SetValue(line, bpmFactor);
    }
}
