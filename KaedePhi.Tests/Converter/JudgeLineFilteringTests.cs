using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Converter.PhiEdit;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Converter.Phigros.v3;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using Ir = KaedePhi.Core.Intermediate;

namespace KaedePhi.Tests.Converter;

public class JudgeLineFilteringTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void PhiEditFromIr_FiltersOnlyLinesMatchingEnabledOptions(
        bool removeTextureLine,
        bool removeAttachUiLine
    )
    {
        var options = new IrToPhiEditConvertOptions
        {
            LineFilter = new IrToPhiEditConvertOptions.LineFilterOptions
            {
                RemoveTextureLine = removeTextureLine,
                RemoveAttachUiLine = removeAttachUiLine,
            },
        };

        var result = new PhiEditConverter().FromIr(CreateSourceChart(), options);
        var expectedBeats = GetExpectedMarkerBeats(removeTextureLine, removeAttachUiLine);

        result.JudgeLineList.Should().HaveCount(expectedBeats.Count);
        foreach (var line in result.JudgeLineList)
        {
            line.NoteList.Should().ContainSingle();
            line.AlphaFrames.Should().ContainSingle();
        }

        result
            .JudgeLineList.Select(line => line.NoteList[0].StartBeat)
            .Should()
            .Equal(expectedBeats.Select(beat => (float)beat));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void PhigrosFromIr_FiltersOnlyLinesMatchingEnabledOptions(
        bool removeTextureLine,
        bool removeAttachUiLine
    )
    {
        var options = new IrToPhigrosV3ConvertOptions
        {
            LineFilter = new IrToPhigrosV3ConvertOptions.LineFilterOptions
            {
                RemoveTextureLine = removeTextureLine,
                RemoveAttachUiLine = removeAttachUiLine,
            },
        };

        var result = new PhigrosV3Converter().FromIr(CreateSourceChart(), options);
        var expectedBeats = GetExpectedMarkerBeats(removeTextureLine, removeAttachUiLine);

        result.JudgeLineList.Should().HaveCount(expectedBeats.Count);
        foreach (var line in result.JudgeLineList)
        {
            line.NotesAbove.Should().ContainSingle();
            line.NotesBelow.Should().BeEmpty();
            line.JudgeLineDisappearEvents.Should().ContainSingle();
        }

        result
            .JudgeLineList.Select(line => line.NotesAbove[0].Time)
            .Should()
            .Equal(expectedBeats.Select(beat => beat * 32));
    }

    private static Ir.Chart CreateSourceChart()
    {
        return new Ir.Chart
        {
            JudgeLineList =
            [
                CreateLine(1),
                CreateLine(2, "custom.png"),
                CreateLine(3, attachUi: AttachUi.Pause),
            ],
        };
    }

    private static Ir.JudgeLine CreateLine(
        int markerBeat,
        string texture = CoreConstants.DefaultTexture,
        AttachUi? attachUi = null
    )
    {
        return new Ir.JudgeLine
        {
            Texture = texture,
            AttachUi = attachUi,
            Notes =
            [
                new Ir.Note { StartBeat = new Beat(markerBeat), EndBeat = new Beat(markerBeat) },
            ],
        };
    }

    private static List<int> GetExpectedMarkerBeats(bool removeTextureLine, bool removeAttachUiLine)
    {
        var beats = new List<int> { 1 };
        if (!removeTextureLine)
            beats.Add(2);
        if (!removeAttachUiLine)
            beats.Add(3);
        return beats;
    }
}
