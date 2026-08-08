using KaedePhi.Core.Common;
using KaedePhi.Core.KaedePhi;
using KaedePhi.Tool.Event.KaedePhi;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;

namespace KaedePhi.Tests.Event;

public class EventListMergerPlusOverlapTests
{
    private readonly EventListMergerPlus<double> _merger = new();
    private readonly EventListMerger<double> _basicMerger = new();

    #region 基本合并行为

    [Fact]
    public void Merge_SameStartBeat_LaterIndexWins_InAdaptiveSampling()
    {
        var toEvents = new List<KpcEvents.Event<double>> { CreateEvent(0, 10, 0, 100) };
        var fromEvents = new List<KpcEvents.Event<double>> { CreateEvent(0, 5, 0, 200) };

        var result = _merger.EventListMerge(toEvents, fromEvents, 4, 1.0);

        result.Should().NotBeEmpty();

        var valueAt0 = QueryResult(result, Beat(0));
        valueAt0.Should().BeApproximately(0.0, 1e-6);

        var valueAt2_5 = QueryResult(result, Beat(2.5));
        valueAt2_5.Should().BeApproximately(125.0, 1.0);

        var valueAt5 = QueryResult(result, Beat(5));
        valueAt5.Should().BeApproximately(250.0, 1.0);
    }

    [Fact]
    public void Merge_NoOverlap_SimpleAddition()
    {
        var toEvents = new List<KpcEvents.Event<double>> { CreateEvent(0, 5, 0, 100) };
        var fromEvents = new List<KpcEvents.Event<double>> { CreateEvent(5, 10, 50, 150) };

        var result = _merger.EventListMerge(toEvents, fromEvents, 4, 1.0);

        result.Should().NotBeEmpty();

        var valueAt2_5 = QueryResult(result, Beat(2.5));
        valueAt2_5.Should().BeApproximately(50.0, 1.0);

        var valueAt7_5 = QueryResult(result, Beat(7.5));
        valueAt7_5.Should().BeApproximately(200.0, 1.0);
    }

    [Fact]
    public void Merge_PartialOverlap_LaterTruncatesCorrectly()
    {
        var toEvents = new List<KpcEvents.Event<double>> { CreateEvent(0, 5, 0, 100) };
        var fromEvents = new List<KpcEvents.Event<double>> { CreateEvent(3, 8, 0, 200) };

        var result = _merger.EventListMerge(toEvents, fromEvents, 4, 1.0);

        result.Should().NotBeEmpty();

        var valueAt1 = QueryResult(result, Beat(1));
        valueAt1.Should().BeApproximately(20.0, 1.0);

        var valueAt4 = QueryResult(result, Beat(4));
        valueAt4.Should().BeApproximately(120.0, 2.0);

        var valueAt6 = QueryResult(result, Beat(6));
        valueAt6.Should().BeApproximately(220.0, 2.0);
    }

    #endregion

    #region HasOverlap 覆盖测试

    [Fact]
    public void Merge_CompletelySeparate_NoOverlap()
    {
        // A: [0,2], B: [5,8] — 无重叠
        var to = new List<KpcEvents.Event<double>> { CreateEvent(0, 2, 0, 100) };
        var from = new List<KpcEvents.Event<double>> { CreateEvent(5, 8, 0, 50) };

        var result = _basicMerger.EventListMerge(to, from, 64);

        // 无重叠走 MergeWithoutOverlap 路径
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Merge_Adjacent_NoOverlap()
    {
        // A: [0,5], B: [5,10] — 相邻但不重叠
        var to = new List<KpcEvents.Event<double>> { CreateEvent(0, 5, 0, 100) };
        var from = new List<KpcEvents.Event<double>> { CreateEvent(5, 10, 0, 50) };

        var result = _basicMerger.EventListMerge(to, from, 64);

        // 相邻事件不算重叠
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Merge_PartialOverlap_DetectsOverlap()
    {
        // A: [0,5], B: [3,8] — 部分重叠 [3,5]
        var to = new List<KpcEvents.Event<double>> { CreateEvent(0, 5, 0, 100) };
        var from = new List<KpcEvents.Event<double>> { CreateEvent(3, 8, 0, 50) };

        var result = _basicMerger.EventListMerge(to, from, 64);

        // 有重叠走 MergeWithOverlap 路径，结果应包含合并后的事件
        result.Should().NotBeEmpty();
        result.Count.Should().BeGreaterThan(2);
    }

    [Fact]
    public void Merge_CompleteWrap_DetectsOverlap()
    {
        // A: [0,10], B: [3,6] — B 完全在 A 内
        var to = new List<KpcEvents.Event<double>> { CreateEvent(0, 10, 0, 100) };
        var from = new List<KpcEvents.Event<double>> { CreateEvent(3, 6, 0, 50) };

        var result = _basicMerger.EventListMerge(to, from, 64);

        result.Should().NotBeEmpty();
        result.Count.Should().BeGreaterThan(2);
    }

    [Fact]
    public void Merge_MultipleOverlapIntervals_DetectsAll()
    {
        // A: [0,3], [6,9]  B: [2,7] — 两个重叠区间 [2,3] 和 [6,7]
        var to = new List<KpcEvents.Event<double>>
        {
            CreateEvent(0, 3, 0, 100),
            CreateEvent(6, 9, 0, 100),
        };
        var from = new List<KpcEvents.Event<double>> { CreateEvent(2, 7, 0, 50) };

        var result = _basicMerger.EventListMerge(to, from, 64);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public void Merge_EmptyToEvents_ReturnsFromClone()
    {
        var to = new List<KpcEvents.Event<double>>();
        var from = new List<KpcEvents.Event<double>> { CreateEvent(0, 5, 0, 100) };

        var result = _basicMerger.EventListMerge(to, from, 64);

        result.Should().HaveCount(1);
        result[0].StartValue.Should().Be(0.0);
        result[0].EndValue.Should().Be(100.0);
    }

    [Fact]
    public void Merge_EmptyFromEvents_ReturnsToClone()
    {
        var to = new List<KpcEvents.Event<double>> { CreateEvent(0, 5, 0, 100) };
        var from = new List<KpcEvents.Event<double>>();

        var result = _basicMerger.EventListMerge(to, from, 64);

        result.Should().HaveCount(1);
    }

    [Fact]
    public void Merge_UnsortedInputs_ReturnsEventsInStartBeatOrder()
    {
        var to = new List<KpcEvents.Event<double>>
        {
            CreateEvent(5, 7, 0, 20),
            CreateEvent(0, 2, 0, 10),
        };
        var from = new List<KpcEvents.Event<double>> { CreateEvent(2, 4, 0, 30) };

        var result = _basicMerger.EventListMerge(to, from, 64);

        result.Select(e => (double)e.StartBeat).Should().BeInAscendingOrder();
        result[0].StartBeat.Should().Be(new Beat(0d));
        result[1].StartBeat.Should().Be(new Beat(2d));
        result[2].StartBeat.Should().Be(new Beat(5d));
    }

    [Fact]
    public void Merge_UnsortedOverlappingInputs_UsesSortedOffsetLookup()
    {
        var to = new List<KpcEvents.Event<double>>
        {
            CreateEvent(4, 6, 40, 60),
            CreateEvent(0, 3, 0, 30),
        };
        var from = new List<KpcEvents.Event<double>> { CreateEvent(2, 5, 20, 50) };

        var result = _basicMerger.EventListMerge(to, from, 64);

        result.Select(e => (double)e.StartBeat).Should().BeInAscendingOrder();
    }

    [Fact]
    public void Merge_BothEmpty_ReturnsEmpty()
    {
        var result = _basicMerger.EventListMerge(
            new List<KpcEvents.Event<double>>(),
            new List<KpcEvents.Event<double>>(),
            64
        );

        result.Should().BeEmpty();
    }

    #endregion

    #region 自适应采样覆盖测试

    [Fact]
    public void AdaptiveMerge_NoOverlap_PreservesValues()
    {
        var to = new List<KpcEvents.Event<double>> { CreateEvent(0, 5, 0, 100) };
        var from = new List<KpcEvents.Event<double>> { CreateEvent(5, 10, 0, 50) };

        var result = _merger.EventListMerge(to, from, 64, 0.1);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void AdaptiveMerge_PartialOverlap_MergesCorrectly()
    {
        var to = new List<KpcEvents.Event<double>> { CreateEvent(0, 5, 0, 100) };
        var from = new List<KpcEvents.Event<double>> { CreateEvent(3, 8, 0, 200) };

        var result = _merger.EventListMerge(to, from, 64, 0.1);

        result.Should().NotBeEmpty();

        // 重叠前：A 独立
        var valueAt1 = QueryResult(result, Beat(1));
        valueAt1.Should().BeApproximately(20.0, 1.0);

        // 重叠中：A + B
        var valueAt4 = QueryResult(result, Beat(4));
        valueAt4.Should().BeApproximately(120.0, 5.0);
    }

    [Fact]
    public void AdaptiveMerge_MultipleEvents_MergesCorrectly()
    {
        var to = new List<KpcEvents.Event<double>>
        {
            CreateEvent(0, 3, 0, 60),
            CreateEvent(3, 6, 60, 120),
        };
        var from = new List<KpcEvents.Event<double>>
        {
            CreateEvent(1, 4, 0, 30),
            CreateEvent(4, 7, 30, 60),
        };

        var result = _merger.EventListMerge(to, from, 64, 1.0);

        result.Should().NotBeEmpty();

        // beat 0: A(0) + 0 = 0
        var valueAt0 = QueryResult(result, Beat(0));
        valueAt0.Should().BeApproximately(0.0, 1.0);

        // beat 5: to(100) + from(40) = 140
        var valueAt5 = QueryResult(result, Beat(5));
        valueAt5.Should().BeApproximately(140.0, 5.0);
    }

    #endregion

    #region Helper Methods

    private static KpcEvents.Event<double> CreateEvent(
        double startBeat,
        double endBeat,
        double startValue,
        double endValue
    )
    {
        return new KpcEvents.Event<double>
        {
            StartBeat = new Beat(startBeat),
            EndBeat = new Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue,
            Easing = Easing.Linear,
        };
    }

    private static Beat Beat(double value) => new(value);

    private static double QueryResult(List<KpcEvents.Event<double>> events, Beat beat)
    {
        return KpcEvents.EventLayer.GetValueAtBeat(events, beat);
    }

    #endregion
}
