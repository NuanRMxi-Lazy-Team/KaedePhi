using KaedePhi.Core.Common;
using KaedePhi.Core.RePhiEdit;
using KaedePhi.Core.RePhiEdit.Events;

namespace KaedePhi.Tests.Serialization;

public class RePhiEditSerializationTests
{
    #region LoadFromJson Tests

    [Fact]
    public void LoadFromJson_ValidJson_ReturnsChart()
    {
        var json = CreateMinimalJson();

        var chart = Chart.LoadFromJson(json);

        chart.Should().NotBeNull();
        chart.BpmList.Should().NotBeNull();
        chart.JudgeLineList.Should().NotBeNull();
        chart.Meta.Should().NotBeNull();
    }

    [Fact]
    public void LoadFromJson_PreservesBpmList()
    {
        var json = CreateMinimalJson();

        var chart = Chart.LoadFromJson(json);

        chart.BpmList.Should().HaveCount(1);
        chart.BpmList[0].Bpm.Should().Be(120);
    }

    [Fact]
    public void LoadFromJson_PreservesMeta()
    {
        var json = CreateMinimalJson();

        var chart = Chart.LoadFromJson(json);

        chart.Meta.Should().NotBeNull();
        chart.Meta.Name.Should().Be("Test Chart");
    }

    [Fact]
    public void LoadFromJson_PreservesJudgeLineList()
    {
        var json = CreateMinimalJson();

        var chart = Chart.LoadFromJson(json);

        chart.JudgeLineList.Should().HaveCount(1);
    }

    [Fact]
    public void LoadFromJson_NullJson_ThrowsInvalidOperationException()
    {
        var act = () => Chart.LoadFromJson("null");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LoadFromJson_InvalidJson_ThrowsException()
    {
        var act = () => Chart.LoadFromJson("{ invalid json }");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void LoadFromJson_EmptyJson_ThrowsException()
    {
        var act = () => Chart.LoadFromJson("");

        act.Should().Throw<Exception>();
    }

    #endregion

    #region ExportToJson Tests

    [Fact]
    public void ExportToJson_ProducesValidJson()
    {
        var chart = CreateMinimalChart();

        var json = chart.ExportToJson(false);

        json.Should().NotBeNullOrEmpty();
        var deserialized = Chart.LoadFromJson(json);
        deserialized.Should().NotBeNull();
    }

    [Fact]
    public void ExportToJson_WithFormat_ProducesIndentedJson()
    {
        var chart = CreateMinimalChart();

        var jsonFormatted = chart.ExportToJson(true);
        var jsonCompact = chart.ExportToJson(false);

        jsonFormatted.Length.Should().BeGreaterThan(jsonCompact.Length);
        jsonFormatted.Should().Contain("\n");
    }

    [Fact]
    public void ExportToJson_WithoutFormat_ProducesCompactJson()
    {
        var chart = CreateMinimalChart();

        var json = chart.ExportToJson(false);

        json.Should().NotContain("\n");
    }

    #endregion

    #region RoundTrip Tests

    [Fact]
    public void RoundTrip_PreservesChartData()
    {
        var original = CreateMinimalChart();

        var json = original.ExportToJson(false);
        var deserialized = Chart.LoadFromJson(json);

        deserialized.BpmList.Should().HaveCount(original.BpmList.Count);
        deserialized.JudgeLineList.Should().HaveCount(original.JudgeLineList.Count);
        deserialized.Meta.Name.Should().Be(original.Meta.Name);
    }

    [Fact]
    public void RoundTrip_PreservesBpmValues()
    {
        var original = CreateMinimalChart();

        var json = original.ExportToJson(false);
        var deserialized = Chart.LoadFromJson(json);

        deserialized.BpmList[0].Bpm.Should().Be(original.BpmList[0].Bpm);
    }

    [Fact]
    public void RoundTrip_WithMultipleJudgeLines_PreservesAll()
    {
        var original = CreateChartWithMultipleLines();

        var json = original.ExportToJson(false);
        var deserialized = Chart.LoadFromJson(json);

        deserialized.JudgeLineList.Should().HaveCount(3);
    }

    #endregion

    #region Stream Tests

    [Fact]
    public void LoadFromStream_ValidStream_ReturnsChart()
    {
        var json = CreateMinimalJson();
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        var chart = Chart.LoadFromStream(stream);

        chart.Should().NotBeNull();
        chart.BpmList.Should().NotBeNull();
    }

    [Fact]
    public void ExportToJsonStream_WritesToStream()
    {
        var chart = CreateMinimalChart();
        using var stream = new MemoryStream();

        chart.ExportToJsonStream(stream, false);

        stream.Position = 0;
        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        json.Should().NotBeNullOrEmpty();

        var deserialized = Chart.LoadFromJson(json);
        deserialized.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportToJsonStreamAsync_WritesToStream()
    {
        var chart = CreateMinimalChart();
        using var stream = new MemoryStream();

        await chart.ExportToJsonStreamAsync(stream, false);

        stream.Position = 0;
        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        json.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoadFromJsonAsync_ReturnsChart()
    {
        var json = CreateMinimalJson();

        var chart = await Chart.LoadFromJsonAsync(json);

        chart.Should().NotBeNull();
        chart.BpmList.Should().NotBeNull();
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var original = CreateMinimalChart();

        var clone = original.Clone();

        clone.Should().NotBeSameAs(original);
        clone.Meta.Should().NotBeSameAs(original.Meta);
        clone.BpmList.Should().NotBeSameAs(original.BpmList);
        clone.JudgeLineList.Should().NotBeSameAs(original.JudgeLineList);
    }

    [Fact]
    public void Clone_PreservesValues()
    {
        var original = CreateMinimalChart();

        var clone = original.Clone();

        clone.Meta.Name.Should().Be(original.Meta.Name);
        clone.BpmList.Should().HaveCount(original.BpmList.Count);
        clone.JudgeLineList.Should().HaveCount(original.JudgeLineList.Count);
    }

    [Fact]
    public void Clone_ModificationDoesNotAffectOriginal()
    {
        var original = CreateMinimalChart();

        var clone = original.Clone();
        clone.Meta.Name = "Modified";

        original.Meta.Name.Should().Be("Test Chart");
    }

    #endregion

    #region Anticipation Tests

    [Fact]
    public void Anticipation_SetsDefaultControls()
    {
        var chart = CreateChartWithNullControls();

        chart.Anticipation();

        var firstLine = chart.JudgeLineList[0];
        firstLine.AlphaControls.Should().NotBeNull();
        firstLine.PositionControls.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private static string CreateMinimalJson()
    {
        return """
            {
                "BPMList": [{"bpm": 120, "startTime": [0, 0, 1]}],
                "META": {"name": "Test Chart", "composer": "Test", "charter": "Test", "level": "HD"},
                "judgeLineList": [{
                    "Group": 0,
                    "Name": "",
                    "Texture": "line.png",
                    "isCover": 1,
                    "eventLayers": null,
                    "father": -1,
                    "zOrder": 0
                }],
                "chartTime": 60,
                "judgeLineGroup": ["Default"],
                "multiLineString": "1",
                "multiScale": 1.0,
                "timeTags": [],
                "xybind": true
            }
            """;
    }

    private static Chart CreateMinimalChart()
    {
        return new Chart
        {
            BpmList = [new BpmItem { Bpm = 120, StartBeat = new Beat([0, 0, 1]) }],
            Meta = new Meta
            {
                Name = "Test Chart",
                Composer = "Test",
                Charter = "Test",
                Level = "HD",
            },
            JudgeLineList = [new JudgeLine { EventLayers = [new EventLayer()] }],
            ChartTime = 60,
        };
    }

    private static Chart CreateChartWithMultipleLines()
    {
        return new Chart
        {
            BpmList = [new BpmItem { Bpm = 120, StartBeat = new Beat([0, 0, 1]) }],
            Meta = new Meta { Name = "Multi Line Chart" },
            JudgeLineList =
            [
                new JudgeLine { EventLayers = [new EventLayer()] },
                new JudgeLine { EventLayers = [new EventLayer()] },
                new JudgeLine { EventLayers = [new EventLayer()] },
            ],
        };
    }

    private static Chart CreateChartWithNullControls()
    {
        return new Chart
        {
            BpmList = [new BpmItem { Bpm = 120, StartBeat = new Beat([0, 0, 1]) }],
            Meta = new Meta { Name = "Null Controls Chart" },
            JudgeLineList =
            [
                new JudgeLine
                {
                    EventLayers = [new EventLayer()],
                    AlphaControls = null!,
                    PositionControls = null!,
                    SizeControls = null!,
                    SkewControls = null!,
                    YControls = null!,
                },
            ],
        };
    }

    #endregion
}
