using System.Reflection;
using System.Text;
using KaedePhi.Core.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kpc = KaedePhi.Core.KaedePhi;
using Pc = KaedePhi.Core.PhiChain.v6;
using Pe = KaedePhi.Core.PhiEdit;
using Pf = KaedePhi.Core.PhiFans;
using Ph = KaedePhi.Core.Phigros.v3;
using Rpe = KaedePhi.Core.RePhiEdit;

namespace KaedePhi.Tests.Serialization;

public class NoteEndBeatDeserializationTests
{
    [Theory]
    [InlineData("{\"type\":3,\"beat\":[0,0,1]}")]
    [InlineData("{\"type\":3,\"beat\":[0,0,1],\"holdEndBeat\":[0,0,1]}")]
    [InlineData("{\"type\":3,\"beat\":[0,0,1],\"holdEndBeat\":[-1,0,1]}")]
    public void PhiFansHold_WithMissingOrNonPositiveDuration_Throws(string json)
    {
        Action act = () => JsonConvert.DeserializeObject<Pf.Note>(json);

        ShouldThrowCallbackJsonException(act);
    }

    [Theory]
    [InlineData("{\"type\":3,\"time\":0}")]
    [InlineData("{\"type\":3,\"time\":0,\"holdTime\":0}")]
    [InlineData("{\"type\":3,\"time\":0,\"holdTime\":-1}")]
    public void PhigrosHold_WithMissingOrNonPositiveDuration_Throws(string json)
    {
        Action act = () => JsonConvert.DeserializeObject<Ph.Note>(json);

        ShouldThrowCallbackJsonException(act);
    }

    [Theory]
    [InlineData("{\"type\":2,\"startTime\":[0,0,1]}")]
    [InlineData("{\"type\":2,\"startTime\":[0,0,1],\"endTime\":[0,0,1]}")]
    [InlineData("{\"type\":2,\"startTime\":[0,0,1],\"endTime\":[-1,0,1]}")]
    public void RePhiEditHold_WithMissingOrNonPositiveDuration_Throws(string json)
    {
        Action act = () => JsonConvert.DeserializeObject<Rpe.Note>(json);

        ShouldThrowCallbackJsonException(act);
    }

    [Theory]
    [InlineData("{\"kind\":\"hold\",\"beat\":[0,0,1]}")]
    [InlineData("{\"kind\":\"hold\",\"beat\":[0,0,1],\"hold_beat\":[0,0,1]}")]
    [InlineData("{\"kind\":\"hold\",\"beat\":[0,0,1],\"hold_beat\":[-1,0,1]}")]
    public void PhiChainHold_WithMissingOrNonPositiveDuration_Throws(string json)
    {
        var act = () => JsonConvert.DeserializeObject<Pc.Note>(json);

        act.Should().Throw<JsonSerializationException>();
    }

    [Theory]
    [InlineData("{\"from\":0,\"to\":1,\"kind\":\"hold\"}")]
    [InlineData("{\"from\":0,\"to\":1,\"kind\":\"hold\",\"hold_beat\":null}")]
    [InlineData("{\"from\":0,\"to\":1,\"kind\":\"hold\",\"hold_beat\":[0,0,1]}")]
    [InlineData("{\"from\":0,\"to\":1,\"kind\":\"hold\",\"hold_beat\":[-1,0,1]}")]
    public void PhiChainCurveHold_WithMissingOrNonPositiveDuration_Throws(string json)
    {
        Action act = () => JsonConvert.DeserializeObject<Pc.CurveNoteTrack>(json);

        ShouldThrowCallbackJsonException(act);
    }

    [Theory]
    [InlineData("0\nn2 0 1 0 1 0\n# 1\n& 1")]
    [InlineData("0\nn2 0 1 1 0 1 0\n# 1\n& 1")]
    [InlineData("0\nn2 0 1 0 0 1 0\n# 1\n& 1")]
    public void PhiEditHold_WithMissingEqualOrReversedEnd_Throws(string pec)
    {
        var act = () => Pe.Chart.Load(pec);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void KpcNote_EndBeatSetterRecordsExplicitMarker()
    {
        var note = new Kpc.Note();

        ReadExplicitEndBeatMarker(note).Should().BeFalse();
        note.EndBeat = new Beat([2, 0, 1]);

        ReadExplicitEndBeatMarker(note).Should().BeTrue();
        JObject.Parse(JsonConvert.SerializeObject(note)).Property("HasExplicitEndBeat")
            .Should()
            .BeNull();
    }

    [Fact]
    public void KpcNoteClone_PreservesExplicitEndBeatMarker()
    {
        var implicitEnd = new Kpc.Note();
        var explicitEnd = new Kpc.Note { EndBeat = new Beat([2, 0, 1]) };

        ReadExplicitEndBeatMarker(implicitEnd.Clone()).Should().BeFalse();
        ReadExplicitEndBeatMarker(explicitEnd.Clone()).Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PhiFansChartEntry_WithHoldMissingEnd_Throws(bool useStream)
    {
        const string json =
            "{\"info\":{},\"offset\":0,\"bpm\":[],\"lines\":[{\"props\":{},\"notes\":[{\"type\":3,\"beat\":[0,0,1]}]}]}";
        using var stream = CreateStream(json);
        Action act = useStream
            ? () => Pf.Chart.LoadFromStream(stream)
            : () => Pf.Chart.LoadFromJson(json);

        ShouldThrowCallbackJsonException(act);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PhigrosChartEntry_WithHoldMissingDuration_Throws(bool useStream)
    {
        const string json =
            "{\"formatVersion\":3,\"offset\":0,\"judgeLineList\":[{\"bpm\":120,\"notesAbove\":[{\"type\":3,\"time\":0}],\"notesBelow\":[],\"speedEvents\":[],\"judgeLineMoveEvents\":[],\"judgeLineRotateEvents\":[],\"judgeLineDisappearEvents\":[]}]}";
        using var stream = CreateStream(json);
        Action act = useStream
            ? () => Ph.Chart.LoadFromStream(stream)
            : () => Ph.Chart.LoadFromJson(json);

        ShouldThrowCallbackJsonException(act);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RePhiEditChartEntry_WithHoldMissingEnd_Throws(bool useStream)
    {
        const string json =
            "{\"BPMList\":[],\"META\":{},\"judgeLineList\":[{\"eventLayers\":[],\"notes\":[{\"type\":2,\"startTime\":[0,0,1]}]}],\"chartTime\":0,\"judgeLineGroup\":[\"Default\"],\"multiLineString\":\"1\",\"multiScale\":1,\"timeTags\":[],\"xybind\":true}";
        using var stream = CreateStream(json);
        Action act = useStream
            ? () => Rpe.Chart.LoadFromStream(stream)
            : () => Rpe.Chart.LoadFromJson(json);

        ShouldThrowCallbackJsonException(act);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PhiChainChartEntry_WithHoldMissingDuration_Throws(bool useStream)
    {
        const string json =
            "{\"format\":6,\"offset\":0,\"bpm_list\":[{\"beat\":[0,0,1],\"bpm\":120}],\"lines\":[{\"name\":\"line\",\"notes\":[{\"kind\":\"hold\",\"beat\":[0,0,1]}],\"events\":[],\"children\":[],\"curve_note_tracks\":[]}]}";
        using var stream = CreateStream(json);
        Action act = useStream
            ? () => Pc.Chart.LoadFromJsonStream(stream)
            : () => Pc.Chart.LoadFromJson(json);

        act.Should().Throw<InvalidOperationException>().Which.InnerException
            .Should().BeOfType<JsonSerializationException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PhiEditChartEntry_WithHoldMissingEnd_Throws(bool useStream)
    {
        const string pec = "0\nn2 0 1 0 1 0\n# 1\n& 1";
        using var stream = CreateStream(pec);
        Action act = useStream ? () => Pe.Chart.LoadStream(stream) : () => Pe.Chart.Load(pec);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void JsonNonHold_WithoutEndField_Deserializes()
    {
        JsonConvert.DeserializeObject<Pf.Note>("{\"type\":1,\"beat\":[1,0,1]}")
            .Should()
            .NotBeNull();
        JsonConvert.DeserializeObject<Ph.Note>("{\"type\":1,\"time\":32}")
            .Should()
            .NotBeNull();
        JsonConvert.DeserializeObject<Rpe.Note>("{\"type\":1,\"startTime\":[1,0,1]}")
            .Should()
            .NotBeNull();
        JsonConvert.DeserializeObject<Pc.Note>("{\"kind\":\"tap\",\"beat\":[1,0,1]}")
            .Should()
            .NotBeNull();
        JsonConvert
            .DeserializeObject<Pc.CurveNoteTrack>(
                "{\"from\":0,\"to\":1,\"kind\":\"drag\"}"
            )
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void PhiEditNonHold_WithoutEndBeat_UsesStartBeat()
    {
        const string pec = "0\nn1 0 2 10 1 0\n# 1\n& 1";

        var note = Pe.Chart.Load(pec).JudgeLineList.Single().NoteList.Single();

        note.EndBeat.Should().Be(2f);
    }

    [Fact]
    public void ValidHolds_DeserializeWithTheirEndValues()
    {
        JsonConvert
            .DeserializeObject<Pf.Note>(
                "{\"type\":3,\"beat\":[1,0,1],\"holdEndBeat\":[2,0,1]}"
            )!
            .HoldEndBeat.Should()
            .Be(new Beat([2, 0, 1]));
        JsonConvert.DeserializeObject<Ph.Note>("{\"type\":3,\"time\":32,\"holdTime\":32}")!
            .HoldTime.Should()
            .Be(32f);
        JsonConvert
            .DeserializeObject<Rpe.Note>(
                "{\"type\":2,\"startTime\":[1,0,1],\"endTime\":[2,0,1]}"
            )!
            .EndBeat.Should()
            .Be(new Beat([2, 0, 1]));
        JsonConvert
            .DeserializeObject<Pc.Note>(
                "{\"kind\":\"hold\",\"beat\":[1,0,1],\"hold_beat\":[1,0,1]}"
            )!
            .HoldBeat.Should()
            .Be(new Beat([1, 0, 1]));
        JsonConvert
            .DeserializeObject<Pc.CurveNoteTrack>(
                "{\"from\":0,\"to\":1,\"kind\":\"hold\",\"hold_beat\":[1,0,1]}"
            )!
            .HoldBeat.Should()
            .Be(new Beat([1, 0, 1]));

        var phiEditNote = Pe.Chart
            .Load("0\nn2 0 1 2 0 1 0\n# 1\n& 1")
            .JudgeLineList.Single()
            .NoteList.Single();
        phiEditNote.EndBeat.Should().Be(2f);
    }

    [Fact]
    public void HoldSerialization_PreservesEndFieldNames()
    {
        var phiFans = JObject.Parse(
            JsonConvert.SerializeObject(
                new Pf.Note
                {
                    Type = Pf.NoteType.Hold,
                    Beat = new Beat([1, 0, 1]),
                    HoldEndBeat = new Beat([2, 0, 1]),
                }
            )
        );
        var phigros = JObject.Parse(
            JsonConvert.SerializeObject(new Ph.Note { Type = Ph.NoteType.Hold, HoldTime = 32 })
        );
        var rePhiEdit = JObject.Parse(
            JsonConvert.SerializeObject(
                new Rpe.Note
                {
                    Type = NoteType.Hold,
                    StartBeat = new Beat([1, 0, 1]),
                    EndBeat = new Beat([2, 0, 1]),
                }
            )
        );
        var phiChain = JObject.Parse(
            JsonConvert.SerializeObject(
                new Pc.Note { Type = Pc.NoteType.Hold, HoldBeat = new Beat([1, 0, 1]) }
            )
        );
        var curveTrack = JObject.Parse(
            JsonConvert.SerializeObject(
                new Pc.CurveNoteTrack
                {
                    NoteType = Pc.NoteType.Hold,
                    HoldBeat = new Beat([1, 0, 1]),
                }
            )
        );

        phiFans.Property("holdEndBeat").Should().NotBeNull();
        phigros.Property("holdTime").Should().NotBeNull();
        rePhiEdit.Property("endTime").Should().NotBeNull();
        phiChain.Property("hold_beat").Should().NotBeNull();
        curveTrack.Property("hold_beat").Should().NotBeNull();
    }

    private static MemoryStream CreateStream(string value) =>
        new(Encoding.UTF8.GetBytes(value));

    private static bool ReadExplicitEndBeatMarker(Kpc.Note note)
    {
        var property = typeof(Kpc.Note).GetProperty(
            "HasExplicitEndBeat",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        property.Should().NotBeNull();
        return (bool)property!.GetValue(note)!;
    }

    private static void ShouldThrowCallbackJsonException(Action act)
    {
        var exception = act.Should().Throw<TargetInvocationException>().Which;
        exception.InnerException.Should().BeOfType<JsonSerializationException>();
    }
}
