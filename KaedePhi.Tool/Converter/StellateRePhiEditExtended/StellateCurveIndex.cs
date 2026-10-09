using System;
using System.Collections.Generic;
using System.Linq;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using RpeEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<float>;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended;

internal sealed class StellateCurveIndex
{
    private readonly RpeEvent[] _events;
    private readonly double[] _startTimes;
    private readonly double[] _endTimes;
    private readonly double[] _sortedStartTimes;
    private readonly int[] _dominantByPrefix;
    private readonly int[] _previousByPrefix;

    internal StellateCurveIndex(
        IReadOnlyList<RpeEvent> events,
        PhigrosV3TimeMapper timeMapper
    )
    {
        _events = new RpeEvent[events.Count];
        _startTimes = new double[events.Count];
        _endTimes = new double[events.Count];

        var firstIndex = 0;
        for (var index = 0; index < events.Count; index++)
        {
            var curveEvent = events[index];
            _events[index] = curveEvent;
            _startTimes[index] = timeMapper.ToMusicTime(curveEvent.StartBeat);
            _endTimes[index] = timeMapper.ToMusicTime(curveEvent.EndBeat);
            if (ComparePriority(index, firstIndex) < 0)
                firstIndex = index;
        }

        FirstIndex = firstIndex;

        var order = Enumerable.Range(0, events.Count).ToArray();
        Array.Sort(order, (left, right) =>
        {
            var comparison = _startTimes[left].CompareTo(_startTimes[right]);
            return comparison != 0 ? comparison : left.CompareTo(right);
        });

        _sortedStartTimes = new double[events.Count];
        _dominantByPrefix = new int[events.Count];
        _previousByPrefix = new int[events.Count];

        var dominant = -1;
        var previous = -1;
        for (var index = 0; index < order.Length; index++)
        {
            var candidate = order[index];
            _sortedStartTimes[index] = _startTimes[candidate];

            if (dominant < 0 || ComparePriority(candidate, dominant) > 0)
            {
                previous = dominant;
                dominant = candidate;
            }
            else if (previous < 0 || ComparePriority(candidate, previous) > 0)
            {
                previous = candidate;
            }

            _dominantByPrefix[index] = dominant;
            _previousByPrefix[index] = previous;
        }
    }

    internal int FirstIndex { get; }

    internal RpeEvent GetEvent(int index) => _events[index];

    internal double GetStartTime(int index) => _startTimes[index];

    internal double GetEndTime(int index) => _endTimes[index];

    internal (int Dominant, int Previous) FindDominant(double time, double epsilon)
    {
        var limit = time + epsilon;
        var lower = 0;
        var upper = _sortedStartTimes.Length;
        while (lower < upper)
        {
            var middle = lower + ((upper - lower) >> 1);
            if (_sortedStartTimes[middle] <= limit)
                lower = middle + 1;
            else
                upper = middle;
        }

        return lower == 0
            ? (-1, -1)
            : (_dominantByPrefix[lower - 1], _previousByPrefix[lower - 1]);
    }

    private int ComparePriority(int leftIndex, int rightIndex)
    {
        var left = _events[leftIndex];
        var right = _events[rightIndex];
        var comparison = left.StartBeat.CompareTo(right.StartBeat);
        if (comparison != 0)
            return comparison;

        comparison = left.EndBeat.CompareTo(right.EndBeat);
        return comparison != 0 ? comparison : leftIndex.CompareTo(rightIndex);
    }
}
