using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using PhiFanmade.Core.Common;
using PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;
using PhiFanmade.Tool.PhiFanmadeNrc.Layers.Internal;

namespace PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines.Internal;

/// <summary>
/// NRC 父子解绑共用辅助方法：缓存表、坐标计算、通道合并、范围统计、结果写回。
/// 同步处理器（<see cref="FatherUnbindProcessor"/>）与异步处理器（<see cref="FatherUnbindAsyncProcessor"/>）共享此类。
/// </summary>
internal static class FatherUnbindHelpers
{
    /// <summary>
    /// 以 allJudgeLines 实例为 key 自动隔离缓存：
    /// 同一谱面的所有解绑调用共享同一份缓存，allJudgeLines 被 GC 后自动释放。
    /// </summary>
    internal static readonly ConditionalWeakTable<List<Nrc.JudgeLine>, ConcurrentDictionary<int, Nrc.JudgeLine>>
        ChartCacheTable = new();

    /// <summary>
    /// 根据父线绝对坐标和旋转角度，计算子线的绝对坐标。
    /// </summary>
    internal static (double X, double Y) GetLinePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
    {
        var rad  = angleDegrees % 360 * Math.PI / 180d;
        var cos  = Math.Cos(rad);
        var sin  = Math.Sin(rad);
        var rotX = lineX * cos - lineY * sin;
        var rotY = lineX * sin + lineY * cos;
        return (fatherLineX + rotX, fatherLineY + rotY);
    }

    /// <summary>
    /// 传入语义：取 beat 时刻正在生效的事件插值（用于段起点）。O(log n) 二分查找。
    /// </summary>
    internal static float GetValIn(List<Nrc.Event<float>> events, Beat beat)
    {
        if (events.Count == 0) return 0f;
        int lo = 0, hi = events.Count - 1, idx = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (events[mid].StartBeat <= beat) { idx = mid; lo = mid + 1; }
            else hi = mid - 1;
        }

        if (idx < 0) return 0f;
        var e = events[idx];
        return e.EndBeat > beat ? e.GetValueAtBeat(beat) : e.EndValue;
    }

    /// <summary>
    /// 传出语义：取 beat 时刻即将结束的事件插值（用于段终点）。O(log n) 二分查找。
    /// </summary>
    internal static float GetValOut(List<Nrc.Event<float>> events, Beat beat)
    {
        if (events.Count == 0) return 0f;
        int lo = 0, hi = events.Count - 1, idx = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (events[mid].StartBeat < beat) { idx = mid; lo = mid + 1; }
            else hi = mid - 1;
        }

        if (idx < 0) return 0f;
        var e = events[idx];
        return e.EndBeat >= beat ? e.GetValueAtBeat(beat) : e.EndValue;
    }

    /// <summary>
    /// 按层顺序将某一通道的事件列表串行叠加合并。层间叠加不满足交换律，必须顺序处理。
    /// </summary>
    internal static List<Nrc.Event<float>> MergeLayerChannel(
        List<Nrc.EventLayer> layers,
        Func<Nrc.EventLayer, List<Nrc.Event<float>>?> selector,
        Func<List<Nrc.Event<float>>, List<Nrc.Event<float>>, List<Nrc.Event<float>>> merge)
    {
        var result = new List<Nrc.Event<float>>();
        return layers.Select(selector)
            .Where(ch => ch is { Count: > 0 })
            .Aggregate(result, (current, ch) => merge(current, ch!));
    }

    /// <summary>获取事件列表的拍范围（最小 StartBeat，最大 EndBeat）。列表为空时返回 (0, 0)。</summary>
    internal static (Beat Min, Beat Max) GetEventRange(List<Nrc.Event<float>> events)
        => events.Count == 0
            ? (new Beat(0), new Beat(0))
            : (events.Min(e => e.StartBeat), events.Max(e => e.EndBeat));

    /// <summary>
    /// 将计算结果写回判定线：清除第 1 层及以上的 X/Y 事件，将压缩后的结果写入第 0 层。
    /// RotateWithFather 为 true 时叠加父线旋转事件；最后置 Father = -1 完成解绑。
    /// </summary>
    internal static void WriteResultToLine(
        Nrc.JudgeLine line,
        List<Nrc.Event<float>> newXEvents,
        List<Nrc.Event<float>> newYEvents,
        List<Nrc.Event<float>> fatherRotateEvents,
        double tolerance,
        Func<List<Nrc.Event<float>>, List<Nrc.Event<float>>, List<Nrc.Event<float>>> merge,
        bool compress = true)
    {
        for (var i = 1; i < line.EventLayers.Count; i++)
        {
            line.EventLayers[i].MoveXEvents.Clear();
            line.EventLayers[i].MoveYEvents.Clear();
        }

        if (line.EventLayers.Count == 0)
            line.EventLayers.Add(new Nrc.EventLayer());

        line.EventLayers[0].MoveXEvents = compress
            ? EventCompressor.EventListCompress(newXEvents, tolerance)
            : newXEvents;
        line.EventLayers[0].MoveYEvents = compress
            ? EventCompressor.EventListCompress(newYEvents, tolerance)
            : newYEvents;

        if (line.RotateWithFather)
        {
            var merged = merge(line.EventLayers[0].RotateEvents, fatherRotateEvents);
            line.EventLayers[0].RotateEvents = compress
                ? EventCompressor.EventListCompress(merged, tolerance)
                : merged;
        }

        line.Father = -1;
    }
}

