using System.Linq;
using KaedePhi.Core.Primitives;
using KaedePhi.Core.Intermediate;
using IrEvents = KaedePhi.Core.Intermediate.Events;

namespace KaedePhi.Tests.Event;

/// <summary>
/// 验证事件列表时间顺序播放的重叠行为规则：
/// 1. 部分重叠 —— 后事件 B 从其 StartBeat 起截断前事件 A
/// 2. 完全包含 —— A 播放至 B.StartBeat 后切换到 B，B 结束后 A 剩余部分完全忽略
/// 3. 同起始拍 —— index 靠后者生效，靠前者被忽略
/// </summary>
public class EventOverlapBehaviorTests
{
    #region Rule 1: Partial overlap — B truncates A

    [Fact]
    public void GetValueAtBeat_PartialOverlap_BeforeBStart_ReturnsA()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 5, 0, 100),
            CreateEvent(3, 7, 200, 300),
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(1));

        result.Should().BeApproximately(20.0, 1e-6); // A at beat 1: 0→100 over [0,5] → 20
    }

    [Fact]
    public void GetValueAtBeat_PartialOverlap_AtBStart_ReturnsBStartValue()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 5, 0, 100),
            CreateEvent(3, 7, 200, 300),
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(3));

        // B is dominant at beat 3 (same StartBeat handling not relevant here, B.StartBeat=3 <= 3)
        // B at beat 3 (t=0): returns B.StartValue = 200
        result.Should().Be(200.0);
    }

    [Fact]
    public void GetValueAtBeat_PartialOverlap_AfterBStart_ReturnsB()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 5, 0, 100),
            CreateEvent(3, 7, 200, 300),
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(5));

        // B at beat 5 (t=0.5 in [3,7]): 200→300 → 250
        result.Should().Be(250.0);
    }

    [Fact]
    public void GetValueAtBeat_PartialOverlap_AfterAEnd_BStillActive_ReturnsB()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 5, 0, 100),
            CreateEvent(3, 7, 200, 300),
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(6));

        // B at beat 6 (t=0.75 in [3,7]): 200→300 → 275
        result.Should().Be(275.0);
    }

    [Fact]
    public void GetValueAtBeat_PartialOverlap_AfterBEnd_HoldsBEndValue()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 5, 0, 100),
            CreateEvent(3, 7, 200, 300),
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(8));

        // B ended at 7, hold B.EndValue = 300. A's remaining part is ignored.
        result.Should().Be(300.0);
    }

    #endregion

    #region Rule 2: Complete wrapping — B inside A, A plays until B, then B, then A ignored

    [Fact]
    public void GetValueAtBeat_Wrapping_BeforeBStart_ReturnsA()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 10, 0, 100),
            CreateEvent(3, 6, 200, 300),
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(1));

        // A at beat 1 (t=0.1 in [0,10]): 0→100 → 10
        result.Should().BeApproximately(10.0, 1e-6);
    }

    [Fact]
    public void GetValueAtBeat_Wrapping_AtBStart_ReturnsBStartValue()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 10, 0, 100),
            CreateEvent(3, 6, 200, 300),
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(3));

        // B is dominant, B at beat 3 (t=0): returns B.StartValue = 200
        result.Should().Be(200.0);
    }

    [Fact]
    public void GetValueAtBeat_Wrapping_DuringB_ReturnsB()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 10, 0, 100),
            CreateEvent(3, 6, 200, 300),
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(4.5));

        // B at beat 4.5 (t=0.5 in [3,6]): 200→300 → 250
        result.Should().Be(250.0);
    }

    [Fact]
    public void GetValueAtBeat_Wrapping_AfterBEnd_HoldsBEndValue_AIgnored()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 10, 0, 100),
            CreateEvent(3, 6, 200, 300),
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(8));

        // B ended at 6, hold B.EndValue = 300. A's remaining [6,10] is completely ignored.
        result.Should().Be(300.0);
    }

    [Fact]
    public void GetValueAtBeat_Wrapping_AtAEnd_HoldsBEndValue()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 10, 0, 100),
            CreateEvent(3, 6, 200, 300),
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(10));

        // Still holds B.EndValue = 300
        result.Should().Be(300.0);
    }

    [Fact]
    public void GetValueAtBeat_Wrapping_AfterAEnd_HoldsBEndValue()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 10, 0, 100),
            CreateEvent(3, 6, 200, 300),
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(15));

        result.Should().Be(300.0);
    }

    #endregion

    #region Rule 3: Same start point — higher index wins

    [Fact]
    public void GetValueAtBeat_SameStartBeat_DuringFirst_ReturnsSecond()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 10, 0, 100), // index 0
            CreateEvent(0, 5, 200, 300), // index 1 (higher index wins)
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(2));

        // Second event (index 1) at beat 2 (t=0.4 in [0,5]): 200→300 → 240
        result.Should().BeApproximately(240.0, 1e-6);
    }

    [Fact]
    public void GetValueAtBeat_SameStartBeat_AfterSecondEnds_HoldsSecondEndValue()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 10, 0, 100), // index 0 (longer, but lower index → ignored)
            CreateEvent(0, 5, 200, 300), // index 1 (higher index wins)
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(7));

        // Second event ended at 5, hold EndValue = 300. First event completely ignored.
        result.Should().Be(300.0);
    }

    [Fact]
    public void GetValueAtBeat_SameStartBeat_ThreeEvents_HighestIndexWins()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 10, 0, 100), // index 0
            CreateEvent(0, 8, 200, 300), // index 1
            CreateEvent(0, 4, 500, 600), // index 2 (highest index wins)
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(2));

        // Third event (index 2) at beat 2 (t=0.5 in [0,4]): 500→600 → 550
        result.Should().Be(550.0);
    }

    [Fact]
    public void GetValueAtBeat_SameStartBeat_ThreeEvents_HighestEnds_HoldsItsEndValue()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 10, 0, 100), // index 0
            CreateEvent(0, 8, 200, 300), // index 1
            CreateEvent(0, 4, 500, 600), // index 2 (highest index wins)
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(6));

        // Third event ended at 4, hold EndValue = 600. Others completely ignored.
        result.Should().Be(600.0);
    }

    [Fact]
    public void GetValueAtBeat_SameStartBeat_AtStartBeat_ReturnsHighestIndexStartValue()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 10, 0, 100),
            CreateEvent(0, 5, 200, 300),
        };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(0));

        // At beat 0, both start. Highest index (second) wins. B.StartValue = 200.
        result.Should().Be(200.0);
    }

    #endregion

    #region Combined scenarios

    [Fact]
    public void GetValueAtBeat_ChainedOverlap_A_B_C_CWins()
    {
        // A: [0,10], B: [2,5] wraps inside A, C: [4,7] overlaps B's end
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 10, 0, 100), // A
            CreateEvent(2, 5, 200, 300), // B
            CreateEvent(4, 7, 500, 600), // C
        };
        EventSort(events);

        // Before B: A active
        IrEvents.EventLayer.GetValueAtBeat(events, Beat(1)).Should().BeApproximately(10.0, 1e-6);

        // During B: B active (B at beat 3: t=(3-2)/(5-2)=1/3, 200→300 → 233.33)
        IrEvents
            .EventLayer.GetValueAtBeat(events, Beat(3))
            .Should()
            .BeApproximately(233.33, 0.01);

        // During C: C active (C at beat 5: t=(5-4)/(7-4)=1/3, 500→600 → 533.33)
        IrEvents
            .EventLayer.GetValueAtBeat(events, Beat(5))
            .Should()
            .BeApproximately(533.33, 0.01);

        // After C ends: hold C.EndValue = 600
        IrEvents.EventLayer.GetValueAtBeat(events, Beat(8)).Should().BeApproximately(600.0, 1e-6);
    }

    [Fact]
    public void GetValueAtBeat_PartialOverlap_ATruncated_AHoldsBeforeB()
    {
        // A: [0,5], B: [3,8] — partial overlap, A truncated at B.StartBeat=3
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 5, 0, 100),
            CreateEvent(3, 8, 200, 400),
        };
        EventSort(events);

        // At beat 2: A active
        IrEvents.EventLayer.GetValueAtBeat(events, Beat(2)).Should().BeApproximately(40.0, 1e-6);

        // At beat 3: B takes over (B.StartValue = 200)
        IrEvents.EventLayer.GetValueAtBeat(events, Beat(3)).Should().BeApproximately(200.0, 1e-6);

        // At beat 4: B active, A truncated (B at beat 4: t=(4-3)/(8-3)=0.2, 200→400 → 240)
        IrEvents.EventLayer.GetValueAtBeat(events, Beat(4)).Should().BeApproximately(240.0, 1e-6);

        // After B ends: hold B.EndValue = 400
        IrEvents
            .EventLayer.GetValueAtBeat(events, Beat(10))
            .Should()
            .BeApproximately(400.0, 1e-6);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void GetValueAtBeat_EmptyList_ReturnsDefault()
    {
        var events = new List<IrEvents.Event<double>>();

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(5));

        result.Should().Be(default);
    }

    [Fact]
    public void GetValueAtBeat_BeforeAllEvents_ReturnsDefault()
    {
        var events = new List<IrEvents.Event<double>> { CreateEvent(5, 10, 0, 100) };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(3));

        result.Should().Be(default);
    }

    [Fact]
    public void GetValueAtBeat_SingleEvent_AfterEnd_HoldsEndValue()
    {
        var events = new List<IrEvents.Event<double>> { CreateEvent(0, 5, 0, 100) };
        EventSort(events);

        var result = IrEvents.EventLayer.GetValueAtBeat(events, Beat(10));

        result.Should().Be(100.0);
    }

    #endregion

    #region Helper Methods

    private static IrEvents.Event<double> CreateEvent(
        double startBeat,
        double endBeat,
        double startValue,
        double endValue,
        int easingId = 1
    )
    {
        return new IrEvents.Event<double>
        {
            StartBeat = new Beat(startBeat),
            EndBeat = new Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue,
            Easing = new Easing(easingId),
        };
    }

    private static Beat Beat(double value) => new(value);

    private static void EventSort(List<IrEvents.Event<double>> events)
    {
        var sorted = events.OrderBy(e => e.StartBeat).ToList();
        events.Clear();
        events.AddRange(sorted);
    }

    #endregion
}
