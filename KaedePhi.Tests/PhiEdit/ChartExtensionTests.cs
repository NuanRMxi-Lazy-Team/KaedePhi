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
    public async Task Loaders_UseTheSameParserForAllChartCommands()
    {
        const string pec =
            "-20\n"
            + "bp 4 180\n"
            + "bp 0 120\n"
            + "cp 1 2 300 400\n"
            + "cv 1 1 2\n"
            + "cd 1 3 45\n"
            + "ca 1 4 0.5\n"
            + "cm 1 0 2 500 600 2\n"
            + "cr 1 2 3 90 3\n"
            + "cf 1 4 5 0.5\n"
            + "n2 1 3 4 100 1 0\n"
            + "# 1.25\n"
            + "& 0.5\n"
            + "n1 1 1 200 2 1 # 2 & 0.75\n"
            + "extension 1 ignored\n";

        var fromText = Chart.Load(pec);
        await using var asyncStream = new MemoryStream(Encoding.UTF8.GetBytes(pec));
        var fromAsyncStream = await Chart.LoadStreamAsync(asyncStream);
        await using var syncStream = new MemoryStream(Encoding.UTF8.GetBytes(pec));
        var fromSyncStream = Chart.LoadStream(syncStream);

        fromText.Export().Should().Be(fromAsyncStream.Export());
        fromText.Export().Should().Be(fromSyncStream.Export());
        fromText.BpmList.Select(item => item.StartBeat).Should().Equal(0, 4);
        fromText.JudgeLineList.Should().HaveCount(1);
        fromText.JudgeLineList[0].NoteList.Should().HaveCount(2);
        asyncStream.CanRead.Should().BeTrue();
        syncStream.CanRead.Should().BeTrue();
    }

    [Fact]
    public async Task Loaders_RejectTruncatedMultilineNote()
    {
        const string pec = "0\nn1 0 1 100 1 0\n# 1";

        var textAction = () => Chart.Load(pec);
        var syncAction = () => Chart.LoadStream(new MemoryStream(Encoding.UTF8.GetBytes(pec)));
        var asyncAction = () =>
            Chart.LoadStreamAsync(new MemoryStream(Encoding.UTF8.GetBytes(pec)));

        textAction.Should().Throw<FormatException>();
        syncAction.Should().Throw<FormatException>();
        await asyncAction.Should().ThrowAsync<FormatException>();
    }

    [Fact]
    public async Task StreamApis_RejectInvalidOwnershipArguments()
    {
        var loadAction = () => Chart.LoadStream(null!);
        var loadAsyncAction = () => Chart.LoadStreamAsync(null!);
        var chart = new Chart();
        var exportAction = () => chart.ExportToStream(null!);
        var exportAsyncAction = () => chart.ExportToStreamAsync(null!);

        loadAction.Should().Throw<ArgumentNullException>();
        await loadAsyncAction.Should().ThrowAsync<ArgumentNullException>();
        exportAction.Should().Throw<ArgumentNullException>();
        await exportAsyncAction.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Exporters_WriteTheSamePhysicalLines()
    {
        var chart = new Chart
        {
            Offset = 12,
            JudgeLineList =
            [
                new JudgeLine
                {
                    NoteList =
                    [
                        new Note
                        {
                            Type = NoteType.Hold,
                            StartBeat = 1,
                            EndBeat = 2,
                            PositionX = 100,
                        },
                    ],
                },
            ],
        };
        var expected = chart.Export() + Environment.NewLine;

        using var syncStream = new MemoryStream();
        chart.ExportToStream(syncStream);
        Encoding.UTF8.GetString(syncStream.ToArray()).Should().Be(expected);

        await using var asyncStream = new MemoryStream();
        await chart.ExportToStreamAsync(asyncStream);
        Encoding.UTF8.GetString(asyncStream.ToArray()).Should().Be(expected);
        syncStream.CanWrite.Should().BeTrue();
        asyncStream.CanWrite.Should().BeTrue();
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