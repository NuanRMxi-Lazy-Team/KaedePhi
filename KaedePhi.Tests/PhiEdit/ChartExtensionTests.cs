using System.Globalization;
using System.Text;
using KaedePhi.Core.PhiEdit;

namespace KaedePhi.Tests.PhiEdit;

public class ChartExtensionTests
{
    [Fact]
    public void Load_UsesInvariantCultureAndSplitsWhitespace()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var chart = Chart.Load(
                "0\r\n  bp\t0.5   120.5\r"
                    + "cv\t0\t1.5\t2.5\n"
                    + "n1\t0\t2.5\t0.25\t1\t0\n"
                    + "#\t1.5\n"
                    + "&\t0.75"
            );

            chart.BpmList[0].StartBeat.Should().Be(0.5f);
            chart.BpmList[0].Bpm.Should().Be(120.5f);
            chart.JudgeLineList.Should().HaveCount(1);
            chart.JudgeLineList[0].SpeedFrames[0].Beat.Should().Be(1.5f);
            chart.JudgeLineList[0].NoteList[0].SpeedMultiplier.Should().Be(1.5f);
            chart.JudgeLineList[0].NoteList[0].WidthRatio.Should().Be(0.75f);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public async Task LoadStreamAsync_ParsesWithInvariantCulture()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("0\nbp 1.25 90.5\n"));

        var chart = await Chart.LoadStreamAsync(stream);

        chart.BpmList[0].StartBeat.Should().Be(1.25f);
        chart.BpmList[0].Bpm.Should().Be(90.5f);
    }

    [Fact]
    public void Load_ParsesBelowSideFlag()
    {
        var chart = Chart.Load("0\nn1 0 1 0.25 2 0\n# 1\n& 1");

        chart.JudgeLineList[0].NoteList[0].Above.Should().BeFalse();
    }

    [Fact]
    public void Load_WithInvalidNumericField_ThrowsFormatException()
    {
        var act = () => Chart.Load("0\ncv 0 invalid 1");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Export_UsesInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var chart = new Chart
            {
                BpmList = [new BpmItem { StartBeat = 1.5f, Bpm = 120.5f }],
                JudgeLineList =
                [
                    new JudgeLine { SpeedFrames = [new Frame { Beat = 2.5f, Value = 3.5f }] },
                ],
            };

            var exported = chart.Export();

            exported.Should().Contain("bp 1.5 120.5");
            exported.Should().Contain("cv 0 2.5 3.5");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
