using System;
using System.Collections.Generic;
using System.Linq;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using RpeEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<float>;
using IrBpmItem = KaedePhi.Core.Intermediate.Model.BpmItem;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;

/// <summary>
/// 为 RePhiEdit 事件曲线提供按音乐时间查询的索引。
/// </summary>
public sealed class StellateCurveIndex
{
    private readonly RpeEvent[] _events;
    private readonly double[] _startTimes;
    private readonly double[] _endTimes;
    private readonly double[] _sortedStartTimes;
    private readonly int[] _dominantByPrefix;
    private readonly int[] _previousByPrefix;

    /// <summary>
    /// 创建曲线索引。
    /// </summary>
    /// <param name="events">按拍数定义的曲线事件</param>
    /// <param name="bpmList">用于将拍数转换为音乐时间的 BPM 列表</param>
    /// <param name="cacheIndex">曲线在采样缓存中的索引</param>
    public StellateCurveIndex(
        IReadOnlyList<RpeEvent> events,
        IReadOnlyList<IrBpmItem> bpmList,
        int cacheIndex
    ) : this(events, new PhigrosV3TimeMapper(bpmList), cacheIndex) { }

    internal StellateCurveIndex(
        IReadOnlyList<RpeEvent> events,
        PhigrosV3TimeMapper timeMapper,
        int cacheIndex
    )
    {
        CacheIndex = cacheIndex;
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

    /// <summary>
    /// 获取按优先级选出的首个事件索引。
    /// </summary>
    public int FirstIndex { get; }

    /// <summary>
    /// 获取曲线在采样缓存中的索引。
    /// </summary>
    public int CacheIndex { get; }

    /// <summary>
    /// 获取指定索引处的曲线事件。
    /// </summary>
    /// <param name="index">事件索引</param>
    /// <returns>指定索引处的曲线事件</returns>
    public RpeEvent GetEvent(int index) => _events[index];

    /// <summary>
    /// 获取指定索引处事件的起始音乐时间。
    /// </summary>
    /// <param name="index">事件索引</param>
    /// <returns>事件的起始音乐时间</returns>
    public double GetStartTime(int index) => _startTimes[index];

    /// <summary>
    /// 获取指定索引处事件的结束音乐时间。
    /// </summary>
    /// <param name="index">事件索引</param>
    /// <returns>事件的结束音乐时间</returns>
    public double GetEndTime(int index) => _endTimes[index];

    /// <summary>
    /// 根据音乐时间查找当前优先事件及其前一个优先事件。
    /// </summary>
    /// <param name="time">待查询的音乐时间</param>
    /// <param name="epsilon">起始时间比较容差</param>
    /// <returns>主导事件索引及前一个事件索引；不存在时索引为负数</returns>
    public (int Dominant, int Previous) FindDominant(double time, double epsilon)
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
