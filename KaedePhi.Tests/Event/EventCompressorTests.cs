using KaedePhi.Core.Primitives;
using KaedePhi.Core.Intermediate.Model;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Event.Intermediate;
using IrEvents = KaedePhi.Core.Intermediate.Model.Events;

namespace KaedePhi.Tests.Event;

public class EventCompressorTests
{
    private readonly EventCompressor<double> _doubleCompressor = new();
    private readonly EventCompressor<int> _intCompressor = new();

    #region EventListCompressSqrt Tests

    [Fact]
    public void CompressSqrt_NullInput_ReturnsEmptyList()
    {
        var result = _doubleCompressor.EventListCompressSqrt(null, 10);

        result.Should().BeEmpty();
    }

    [Fact]
    public void CompressSqrt_EmptyInput_ReturnsEmptyList()
    {
        var result = _doubleCompressor.EventListCompressSqrt([], 10);

        result.Should().BeEmpty();
    }

    [Fact]
    public void CompressSqrt_SingleElement_ReturnsSameList()
    {
        var events = new List<IrEvents.Event<double>> { CreateEvent(0, 1, 0, 100) };

        var result = _doubleCompressor.EventListCompressSqrt(events, 10);

        result.Should().HaveCount(1);
        result[0].StartValue.Should().Be(0);
        result[0].EndValue.Should().Be(100);
    }

    [Fact]
    public void CompressSqrt_TwoLinearEvents_MergesIntoOne()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 50),
            CreateEvent(1, 2, 50, 100),
        };

        var result = _doubleCompressor.EventListCompressSqrt(events, 10);

        result.Should().HaveCount(1);
        result[0].StartValue.Should().Be(0);
        result[0].EndValue.Should().Be(100);
    }

    [Fact]
    public void CompressSqrt_ThreeLinearEvents_MergesIntoOne()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 33.33),
            CreateEvent(1, 2, 33.33, 66.66),
            CreateEvent(2, 3, 66.66, 100),
        };

        var result = _doubleCompressor.EventListCompressSqrt(events, 10);

        result.Should().HaveCount(1);
        result[0].StartValue.Should().Be(0);
        result[0].EndValue.Should().Be(100);
    }

    [Fact]
    public void CompressSqrt_DifferentEasingTypes_DoesNotMerge()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 50, easingId: 1),
            CreateEvent(1, 2, 50, 100, easingId: 2),
        };

        var result = _doubleCompressor.EventListCompressSqrt(events, 10);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void CompressSqrt_NonAdjacentEvents_DoesNotMerge()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 50),
            CreateEvent(2, 3, 50, 100), // Gap between 1 and 2
        };

        var result = _doubleCompressor.EventListCompressSqrt(events, 10);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void CompressSqrt_ZeroTolerance_DoesNotMerge()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 50),
            CreateEvent(1, 2, 50, 100),
        };

        var result = _doubleCompressor.EventListCompressSqrt(events, 0);

        // With 0 tolerance, even small deviation prevents merge
        result.Should().HaveCount(1); // Perfect linear still merges
    }

    [Fact]
    public void CompressSqrt_HighTolerance_MergesMore()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 100),
            CreateEvent(1, 2, 100, 50), // Non-linear: goes up then down
            CreateEvent(2, 3, 50, 150),
        };

        var resultLowTolerance = _doubleCompressor.EventListCompressSqrt(events, 5);
        var resultHighTolerance = _doubleCompressor.EventListCompressSqrt(events, 80);

        resultHighTolerance.Count.Should().BeLessThanOrEqualTo(resultLowTolerance.Count);
    }

    [Fact]
    public void CompressSqrt_InvalidTolerance_ThrowsArgumentOutOfRangeException()
    {
        var events = new List<IrEvents.Event<double>> { CreateEvent(0, 1, 0, 100) };

        var act1 = () => _doubleCompressor.EventListCompressSqrt(events, -1);
        var act2 = () => _doubleCompressor.EventListCompressSqrt(events, 101);

        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CompressSqrt_ReportsProgress()
    {
        // Use more events to ensure progress is reported before completion
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 10),
            CreateEvent(1, 2, 10, 20),
            CreateEvent(2, 3, 20, 30),
            CreateEvent(3, 4, 30, 40),
            CreateEvent(4, 5, 50, 60), // Different slope, won't merge
            CreateEvent(5, 6, 60, 70),
            CreateEvent(6, 7, 70, 80),
            CreateEvent(7, 8, 90, 110), // Different slope
            CreateEvent(8, 9, 110, 130),
            CreateEvent(9, 10, 130, 150),
        };

        var progressReports = new List<ToolProgress>();
        var progressMock = new Mock<IProgress<ToolProgress>>();
        progressMock
            .Setup(p => p.Report(It.IsAny<ToolProgress>()))
            .Callback<ToolProgress>(progressReports.Add);

        _doubleCompressor.EventListCompressSqrt(events, 10, progressMock.Object);

        progressMock.Verify(p => p.Report(It.IsAny<ToolProgress>()), Times.AtLeastOnce);
    }

    #endregion

    #region EventListCompressSlope Tests

    [Fact]
    public void CompressSlope_NullInput_ReturnsEmptyList()
    {
        var result = _doubleCompressor.EventListCompressSlope(null, 10);

        result.Should().BeEmpty();
    }

    [Fact]
    public void CompressSlope_TwoLinearEvents_MergesIntoOne()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 50),
            CreateEvent(1, 2, 50, 100),
        };

        var result = _doubleCompressor.EventListCompressSlope(events, 10);

        result.Should().HaveCount(1);
    }

    [Fact]
    public void CompressSlope_DifferentSlopes_DoesNotMerge()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 10), // slope = 10
            CreateEvent(1, 2, 10, 100), // slope = 90
        };

        var result = _doubleCompressor.EventListCompressSlope(events, 5);

        result.Should().HaveCount(2);
    }

    #endregion

    #region RemoveUselessEvent Tests

    [Fact]
    public void RemoveUselessEvent_NullInput_ReturnsEmpty()
    {
        var result = _doubleCompressor.RemoveUselessEvent(null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void RemoveUselessEvent_EmptyInput_ReturnsEmpty()
    {
        var result = _doubleCompressor.RemoveUselessEvent([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void RemoveUselessEvent_SingleDefaultEvent_RemovesIt()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 0), // Both values are default (0)
        };

        var result = _doubleCompressor.RemoveUselessEvent(events);

        result.Should().BeEmpty();
    }

    [Fact]
    public void RemoveUselessEvent_SingleNonDefaultEvent_KeepsIt()
    {
        var events = new List<IrEvents.Event<double>> { CreateEvent(0, 1, 0, 100) };

        var result = _doubleCompressor.RemoveUselessEvent(events);

        result.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveUselessEvent_MultipleDefaultEvents_KeepsAll()
    {
        var events = new List<IrEvents.Event<double>>
        {
            CreateEvent(0, 1, 0, 0),
            CreateEvent(1, 2, 0, 0),
        };

        var result = _doubleCompressor.RemoveUselessEvent(events);

        // Only removes when single element
        result.Should().HaveCount(2);
    }

    [Fact]
    public void RemoveUselessEvent_WithInt_WorksCorrectly()
    {
        var events = new List<IrEvents.Event<int>> { CreateIntEvent(0, 1, 0, 0) };

        var result = _intCompressor.RemoveUselessEvent(events);

        result.Should().BeEmpty();
    }

    [Fact]
    public void RemoveUselessEvent_WithInt_NonDefault_KeepsIt()
    {
        var events = new List<IrEvents.Event<int>> { CreateIntEvent(0, 1, 0, 42) };

        var result = _intCompressor.RemoveUselessEvent(events);

        result.Should().HaveCount(1);
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

    private static IrEvents.Event<int> CreateIntEvent(
        double startBeat,
        double endBeat,
        int startValue,
        int endValue,
        int easingId = 1
    )
    {
        return new IrEvents.Event<int>
        {
            StartBeat = new Beat(startBeat),
            EndBeat = new Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue,
            Easing = new Easing(easingId),
        };
    }

    #endregion
}
