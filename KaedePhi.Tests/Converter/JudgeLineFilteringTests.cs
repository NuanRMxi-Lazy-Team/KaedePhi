using KaedePhi.Core.Common;
using KaedePhi.Tool.Converter.PhiEdit;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Converter.Phigros.v3;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using Kpc = KaedePhi.Core.KaedePhi;

namespace KaedePhi.Tests.Converter;

public class JudgeLineFilteringTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void PhiEditFromKpc_FiltersOnlyLinesMatchingEnabledOptions(
        bool removeTextureLine,
        bool removeAttachUiLine
    )
    {
        var options = new KpcToPhiEditConvertOptions
        {
            LineFilter = new KpcToPhiEditConvertOptions.LineFilterOptions
            {
                RemoveTextureLine = removeTextureLine,
                RemoveAttachUiLine = removeAttachUiLine,
            },
        };

        var result = new PhiEditConverter().FromKpc(CreateSourceChart(), options);
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
    public void PhigrosFromKpc_FiltersOnlyLinesMatchingEnabledOptions(
        bool removeTextureLine,
        bool removeAttachUiLine
    )
    {
        var options = new KpcToPhigrosV3ConvertOptions
        {
            LineFilter = new KpcToPhigrosV3ConvertOptions.LineFilterOptions
            {
                RemoveTextureLine = removeTextureLine,
                RemoveAttachUiLine = removeAttachUiLine,
            },
        };

        var result = new PhigrosV3Converter().FromKpc(CreateSourceChart(), options);
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

    private static Kpc.Chart CreateSourceChart()
    {
        return new Kpc.Chart
        {
            JudgeLineList =
            [
                CreateLine(1),
                CreateLine(2, "custom.png"),
                CreateLine(3, attachUi: AttachUi.Pause),
            ],
        };
    }

    private static Kpc.JudgeLine CreateLine(
        int markerBeat,
        string texture = CoreConstants.DefaultTexture,
        AttachUi? attachUi = null
    )
    {
        return new Kpc.JudgeLine
        {
            Texture = texture,
            AttachUi = attachUi,
            Notes =
            [
                new Kpc.Note
                {
                    StartBeat = new Beat(markerBeat),
                    EndBeat = new Beat(markerBeat),
                },
            ],
        };
    }

    private static List<int> GetExpectedMarkerBeats(
        bool removeTextureLine,
        bool removeAttachUiLine
    )
    {
        var beats = new List<int> { 1 };
        if (!removeTextureLine)
            beats.Add(2);
        if (!removeAttachUiLine)
            beats.Add(3);
        return beats;
    }
}
