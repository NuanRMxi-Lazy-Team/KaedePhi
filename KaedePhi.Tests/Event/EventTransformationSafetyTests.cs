using KaedePhi.Core.Common;
using KaedePhi.Core.KaedePhi;
using KaedePhi.Tool.Event.KaedePhi;
using KaedePhi.Tool.Layer.KaedePhi;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;

namespace KaedePhi.Tests.Event;

public class EventTransformationSafetyTests
{
    private readonly EventCompressor<double> _compressor = new();
    private readonly EventFit<double> _fit = new();

    [Fact]
    public void CompressSqrt_ContinuousBezierEvents_PreservesBothEventsAndFields()
    {
        var events = new List<KpcEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 50, isBezier: true, bezierPoints: [0.1f, 0.2f, 0.3f, 0.4f]),
            CreateEvent(1, 2, 50, 100, isBezier: true, bezierPoints: [0.5f, 0.6f, 0.7f, 0.8f]),
        };

        var result = _compressor.EventListCompressSqrt(events, 100);

        result.Should().HaveCount(2);
        result[0].IsBezier.Should().BeTrue();
        result[0].BezierPoints.Should().Equal(0.1f, 0.2f, 0.3f, 0.4f);
        result[1].IsBezier.Should().BeTrue();
        result[1].BezierPoints.Should().Equal(0.5f, 0.6f, 0.7f, 0.8f);
    }

    [Fact]
    public void CompressSlope_ContinuousBezierEvents_PreservesBothEventsAndFields()
    {
        var events = new List<KpcEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 50, isBezier: true, bezierPoints: [0.1f, 0.2f, 0.3f, 0.4f]),
            CreateEvent(1, 2, 50, 100, isBezier: true, bezierPoints: [0.5f, 0.6f, 0.7f, 0.8f]),
        };

        var result = _compressor.EventListCompressSlope(events, 100);

        result.Should().HaveCount(2);
        result[0].BezierPoints.Should().Equal(0.1f, 0.2f, 0.3f, 0.4f);
        result[1].BezierPoints.Should().Equal(0.5f, 0.6f, 0.7f, 0.8f);
    }

    [Fact]
    public void CompressSqrt_EventsWithAdditionalFields_PreservesBothEventsAndFields()
    {
        var events = new List<KpcEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 50, font: "first.ttf", startTime: 1, endTime: 2, floorPosition: 3, bezierPoints: [0.1f, 0, 0, 0]),
            CreateEvent(1, 2, 50, 100, font: "second.ttf", startTime: 4, endTime: 5, floorPosition: 6),
        };

        var result = _compressor.EventListCompressSqrt(events, 100);

        result.Should().HaveCount(2);
        AssertFields(result[0], "first.ttf", 1, 2, 3, [0.1f, 0, 0, 0]);
        AssertFields(result[1], "second.ttf", 4, 5, 6, [0, 0, 0, 0]);
    }

    [Fact]
    public void CompressSlope_CroppedLinearEasingEvents_CanMerge()
    {
        var events = new List<KpcEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 50, easingLeft: 0.1f),
            CreateEvent(1, 2, 50, 100, easingRight: 0.9f),
        };

        var result = _compressor.EventListCompressSlope(events, 100);

        result.Should().ContainSingle();
        result[0].EndBeat.Should().Be(new Beat(2));
    }

    [Fact]
    public void FitEvents_BezierFollowedByLinearEvent_PreservesBezierEvent()
    {
        var events = new List<KpcEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 50, isBezier: true, bezierPoints: [0.1f, 0.2f, 0.3f, 0.4f]),
            CreateEvent(1, 2, 50, 100),
        };

        var result = _fit.FitEvents(events, 100);

        result.Should().HaveCount(2);
        result[0].IsBezier.Should().BeTrue();
        result[0].BezierPoints.Should().Equal(0.1f, 0.2f, 0.3f, 0.4f);
    }

    [Fact]
    public void FitEvents_EventsWithAdditionalFields_PreservesBothEventsAndFields()
    {
        var events = new List<KpcEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 50, font: "first.ttf", startTime: 1, endTime: 2, floorPosition: 3, bezierPoints: [0.1f, 0, 0, 0]),
            CreateEvent(1, 2, 50, 100, font: "second.ttf", startTime: 4, endTime: 5, floorPosition: 6),
        };

        var result = _fit.FitEvents(events, 100);

        result.Should().HaveCount(2);
        AssertFields(result[0], "first.ttf", 1, 2, 3, [0.1f, 0, 0, 0]);
        AssertFields(result[1], "second.ttf", 4, 5, 6, [0, 0, 0, 0]);
    }

    [Fact]
    public void FitEvents_CroppedLinearEasingEvents_CanMerge()
    {
        var events = new List<KpcEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 50, easingLeft: 0.1f),
            CreateEvent(1, 2, 50, 100, easingRight: 0.9f),
        };

        var result = _fit.FitEvents(events, 100);

        result.Should().ContainSingle();
        result[0].EndBeat.Should().Be(new Beat(2));
    }

    [Fact]
    public void LayerEventsCompress_AlignedBezierPositionEvents_PreservesBothAxes()
    {
        var firstX = CreateEvent(
            0,
            1,
            0,
            50,
            isBezier: true,
            bezierPoints: [0.1f, 0.2f, 0.3f, 0.4f]
        );
        var layer = new KpcEvents.EventLayer
        {
            MoveXEvents = [firstX, CreateEvent(1, 2, 50, 100)],
            MoveYEvents = [CreateEvent(0, 1, 0, 50), CreateEvent(1, 2, 50, 100)],
        };

        new LayerProcessor().LayerEventsCompress(layer, 100);

        layer.MoveXEvents.Should().HaveCount(2);
        layer.MoveYEvents.Should().HaveCount(2);
        layer.MoveXEvents![0].IsBezier.Should().BeTrue();
        layer.MoveXEvents[0].BezierPoints.Should().Equal(0.1f, 0.2f, 0.3f, 0.4f);
    }

    [Fact]
    public void LayerEventsCompress_AlignedFontPositionEvents_PreservesBothAxes()
    {
        var layer = new KpcEvents.EventLayer
        {
            MoveXEvents = [CreateEvent(0, 1, 0, 50, font: "line.ttf"), CreateEvent(1, 2, 50, 100)],
            MoveYEvents = [CreateEvent(0, 1, 0, 50), CreateEvent(1, 2, 50, 100)],
        };

        new LayerProcessor().LayerEventsCompress(layer, 100);

        layer.MoveXEvents.Should().HaveCount(2);
        layer.MoveYEvents.Should().HaveCount(2);
        layer.MoveXEvents![0].Font.Should().Be("line.ttf");
    }

    [Fact]
    public void LayerEventsCompress_AlignedCleanLinearPositionEvents_CompressesBothAxes()
    {
        var layer = new KpcEvents.EventLayer
        {
            MoveXEvents = [CreateEvent(0, 1, 0, 50), CreateEvent(1, 2, 50, 100)],
            MoveYEvents = [CreateEvent(0, 1, 0, 25), CreateEvent(1, 2, 25, 50)],
        };

        new LayerProcessor().LayerEventsCompress(layer, 100);

        layer.MoveXEvents.Should().ContainSingle();
        layer.MoveYEvents.Should().ContainSingle();
        layer.MoveXEvents![0].EndValue.Should().Be(100);
        layer.MoveYEvents![0].EndValue.Should().Be(50);
    }

    [Fact]
    public void CompressSqrt_LinearEvents_MergesWithoutMutatingInputOrSharingInstances()
    {
        var first = CreateEvent(0, 1, 0, 50);
        var second = CreateEvent(1, 2, 50, 100);

        var result = _compressor.EventListCompressSqrt([first, second], 100);

        result.Should().ContainSingle();
        result[0].Should().NotBeSameAs(first);
        first.EndBeat.Should().Be(new Beat(1));
        first.EndValue.Should().Be(50);
        result[0].EndBeat.Should().Be(new Beat(2));
        result[0].EndValue.Should().Be(100);
    }

    [Fact]
    public void FitEvents_LinearEvents_ReturnsIndependentFittedEvent()
    {
        var first = CreateEvent(0, 1, 0, 50);
        var second = CreateEvent(1, 2, 50, 100);

        var result = _fit.FitEvents([first, second], 100);

        result.Should().ContainSingle();
        result[0].Should().NotBeSameAs(first);
        result[0].StartBeat.Should().Be(new Beat(0));
        result[0].EndBeat.Should().Be(new Beat(2));
    }

    [Fact]
    public void RemoveUselessEvent_KeptEvent_ReturnsIndependentEvent()
    {
        var source = CreateEvent(0, 1, 0, 50);

        var result = _compressor.RemoveUselessEvent([source]);

        result.Should().ContainSingle();
        result[0].Should().NotBeSameAs(source);
        result[0].EndValue.Should().Be(50);
    }

    private static KpcEvents.Event<double> CreateEvent(
        double startBeat,
        double endBeat,
        double startValue,
        double endValue,
        bool isBezier = false,
        float[]? bezierPoints = null,
        string? font = null,
        float startTime = 0,
        float endTime = 0,
        float floorPosition = 0,
        float easingLeft = 0,
        float easingRight = 1
    )
    {
        return new KpcEvents.Event<double>
        {
            StartBeat = new Beat(startBeat),
            EndBeat = new Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue,
            Easing = new Easing(1),
            EasingLeft = easingLeft,
            EasingRight = easingRight,
            IsBezier = isBezier,
            BezierPoints = bezierPoints ?? [0, 0, 0, 0],
            Font = font,
            StartTime = startTime,
            EndTime = endTime,
            FloorPosition = floorPosition,
        };
    }

    private static void AssertFields(
        KpcEvents.Event<double> evt,
        string? font,
        float startTime,
        float endTime,
        float floorPosition,
        float[] bezierPoints
    )
    {
        evt.Font.Should().Be(font);
        evt.StartTime.Should().Be(startTime);
        evt.EndTime.Should().Be(endTime);
        evt.FloorPosition.Should().Be(floorPosition);
        evt.BezierPoints.Should().Equal(bezierPoints);
    }
}
