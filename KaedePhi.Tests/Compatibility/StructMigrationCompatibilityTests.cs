using KaedePhi.Core.PhiEdit;
using KaedePhi.Core.Phigros.v3;
using Newtonsoft.Json;
using PhigrosEvent = KaedePhi.Core.Phigros.v3.Event;
using PhigrosSpeedEvent = KaedePhi.Core.Phigros.v3.SpeedEvent;

namespace KaedePhi.Tests.Compatibility;

/// <summary>
/// 验证结构体迁移的兼容性：旧版类可隐式映射到结构体，且 JSON 序列化行为保持一致。
/// </summary>
public class StructMigrationCompatibilityTests
{
#pragma warning disable CS0618

    [Fact]
    public void LegacyFrame_ImplicitlyConvertsToFrame()
    {
        var legacy = new LegacyFrame { Beat = 2.5f, Value = 3.5f };

        Frame frame = legacy;

        frame.Beat.Should().Be(2.5f);
        frame.Value.Should().Be(3.5f);
    }

    [Fact]
    public void Frame_ImplicitlyConvertsToLegacyFrame()
    {
        var frame = new Frame { Beat = 1.0f, Value = 4.0f };

        LegacyFrame legacy = frame;

        legacy.Beat.Should().Be(1.0f);
        legacy.Value.Should().Be(4.0f);
    }

    [Fact]
    public void LegacyMoveFrame_ImplicitlyConvertsToMoveFrame()
    {
        var legacy = new LegacyMoveFrame { Beat = 2.5f, XValue = 0.5f, YValue = -1.5f };

        MoveFrame frame = legacy;

        frame.XValue.Should().Be(0.5f);
        frame.YValue.Should().Be(-1.5f);
    }

    [Fact]
    public void LegacySpeedEvent_ImplicitlyConvertsToSpeedEvent()
    {
        var legacy = new LegacySpeedEvent { StartTime = 1f, EndTime = 2f, Value = 3f };

        SpeedEvent speedEvent = legacy;

        speedEvent.StartTime.Should().Be(1f);
        speedEvent.EndTime.Should().Be(2f);
        speedEvent.Value.Should().Be(3f);
    }

    [Fact]
    public void LegacyEvent_ImplicitlyConvertsToEvent()
    {
        var legacy = new LegacyEvent
        {
            StartTime = 1f,
            EndTime = 2f,
            Start = 3f,
            End = 4f,
            Start2 = 5f,
            End2 = 6f,
        };

        PhigrosEvent evt = legacy;

        evt.Start.Should().Be(3f);
        evt.End.Should().Be(4f);
        evt.Start2.Should().Be(5f);
        evt.End2.Should().Be(6f);
    }

    [Fact]
    public void Frame_ObjectInitializer_ValuesPreserved()
    {
        var frame = new Frame { Beat = 7.25f, Value = 8.5f };

        frame.Beat.Should().Be(7.25f);
        frame.Value.Should().Be(8.5f);
        frame.Clone().Should().Be(frame);
    }

    [Fact]
    public void PhigrosEvent_JsonRoundTrip_KeepsValues()
    {
        const string json =
            "[{\"startTime\":0.0,\"endTime\":1.0,\"start\":2.0,\"end\":3.0,\"start2\":4.0,\"end2\":5.0}]";

        var events = JsonConvert.DeserializeObject<List<PhigrosEvent>>(json);

        events.Should().HaveCount(1);
        events![0].StartTime.Should().Be(0f);
        events[0].EndTime.Should().Be(1f);
        events[0].Start.Should().Be(2f);
        events[0].End.Should().Be(3f);
        events[0].Start2.Should().Be(4f);
        events[0].End2.Should().Be(5f);
    }

    [Fact]
    public void PhigrosSpeedEvent_JsonRoundTrip_KeepsValues()
    {
        const string json = "[{\"startTime\":1.0,\"endTime\":2.0,\"value\":3.0}]";

        var speedEvents = JsonConvert.DeserializeObject<List<PhigrosSpeedEvent>>(json);

        speedEvents.Should().HaveCount(1);
        speedEvents![0].StartTime.Should().Be(1f);
        speedEvents[0].EndTime.Should().Be(2f);
        speedEvents[0].Value.Should().Be(3f);
    }

    [Fact]
    public void LegacyPhigrosEvent_JsonRoundTrip_StillWorks()
    {
        const string json =
            "[{\"startTime\":0.0,\"endTime\":1.0,\"start\":2.0,\"end\":3.0,\"start2\":4.0,\"end2\":5.0}]";

        var events = JsonConvert.DeserializeObject<List<LegacyEvent>>(json);

        events.Should().HaveCount(1);
        events![0].Start.Should().Be(2f);
        events[0].End2.Should().Be(5f);
    }

#pragma warning restore CS0618
}
