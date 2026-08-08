using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Event.KaedePhi;
using EventLayer = KaedePhi.Core.KaedePhi.Events.EventLayer;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;

namespace KaedePhi.Tool.JudgeLines.KaedePhi.Utils;

/// <summary>
/// KPC 父子解绑共用辅助方法：缓存表、坐标计算、通道合并、范围统计、采样算法、结果写回。
/// </summary>
public static class FatherUnbindHelpers
{
    private static readonly AsyncLocal<CoordinateProfile?> RenderProfileContext = new();

    public static CoordinateProfile CurrentRenderProfile =>
        RenderProfileContext.Value ?? CoordinateProfile.DefaultRenderProfile;

    public static IDisposable UseRenderProfile(CoordinateProfile renderProfile) =>
        new RenderProfileScope(renderProfile);

    private sealed class RenderProfileScope : IDisposable
    {
        private readonly CoordinateProfile? _previous;

        public RenderProfileScope(CoordinateProfile nextProfile)
        {
            _previous = RenderProfileContext.Value;
            RenderProfileContext.Value = nextProfile;
        }

        public void Dispose() => RenderProfileContext.Value = _previous;
    }

    /// <summary>
    /// 以 allJudgeLines 实例为 key 自动隔离缓存：
    /// 同一谱面的所有解绑调用共享同一份缓存，allJudgeLines 被 GC 后自动释放。
    /// </summary>
    internal static readonly ConditionalWeakTable<
        List<JudgeLine>,
        ConcurrentDictionary<int, JudgeLine>
    > JudgeLineCacheTable = new();

    /// <summary>
    /// 根据父线绝对坐标和旋转角度，计算子线的绝对坐标。
    /// </summary>
    public static (double X, double Y) GetLinePos(
        double fatherLineX,
        double fatherLineY,
        double angleDegrees,
        double lineX,
        double lineY
    )
    {
        return CoordinateGeometry.GetKpcAbsolutePos(
            fatherLineX,
            fatherLineY,
            angleDegrees,
            lineX,
            lineY,
            CurrentRenderProfile
        );
    }

    /// <summary>
    /// 判断线段是否需要进一步切割以满足精度要求。
    /// </summary>
    /// <param name="segmentStart">线段起点坐标</param>
    /// <param name="next">下一个采样点坐标</param>
    /// <param name="intervalEnd">区间终点坐标</param>
    /// <param name="segmentStartBeat">线段起始拍</param>
    /// <param name="intervalEndBeat">区间结束拍</param>
    /// <param name="nextBeat">下一采样点拍</param>
    /// <param name="tolerance">容差百分比</param>
    /// <returns>需要切割则返回 true</returns>
    public static bool NeedsAdaptiveCut(
        (double X, double Y) segmentStart,
        (double X, double Y) next,
        (double X, double Y) intervalEnd,
        Beat segmentStartBeat,
        Beat intervalEndBeat,
        Beat nextBeat,
        double tolerance
    )
    {
        var segmentLength = (double)(intervalEndBeat - segmentStartBeat);
        var progress = segmentLength > 1e-12 ? (nextBeat - segmentStartBeat) / segmentLength : 1.0;
        var predicted = (
            X: segmentStart.X + (intervalEnd.X - segmentStart.X) * progress,
            Y: segmentStart.Y + (intervalEnd.Y - segmentStart.Y) * progress
        );

        var profile = CurrentRenderProfile;
        var tolFraction = tolerance / 100.0;

        // 逐轴屏幕空间误差：测试点相对线性预测的各轴偏差
        var screenErrorX = CoordinateGeometry.GetKpcScreenDistance(
            (next.X, predicted.Y),
            predicted,
            profile
        );
        var screenErrorY = CoordinateGeometry.GetKpcScreenDistance(
            (predicted.X, next.Y),
            predicted,
            profile
        );

        // 逐轴屏幕空间位移尺度：局部运动范围
        var segEndXScale = CoordinateGeometry.GetKpcScreenDistance(
            (intervalEnd.X, segmentStart.Y),
            segmentStart,
            profile
        );
        var nextXScale = CoordinateGeometry.GetKpcScreenDistance(
            (next.X, segmentStart.Y),
            segmentStart,
            profile
        );
        var segEndYScale = CoordinateGeometry.GetKpcScreenDistance(
            (segmentStart.X, intervalEnd.Y),
            segmentStart,
            profile
        );
        var nextYScale = CoordinateGeometry.GetKpcScreenDistance(
            (segmentStart.X, next.Y),
            segmentStart,
            profile
        );

        var thresholdX = tolFraction * Math.Max(Math.Max(segEndXScale, nextXScale), 1e-3);
        var thresholdY = tolFraction * Math.Max(Math.Max(segEndYScale, nextYScale), 1e-3);

        return screenErrorX > thresholdX || screenErrorY > thresholdY;
    }

    /// <summary>
    /// 获取指定拍点上正在生效的事件插值（用于段起点）。
    /// </summary>
    /// <param name="events">事件列表</param>
    /// <param name="beat">目标拍点</param>
    /// <returns>插值结果</returns>
    public static double GetValIn(List<KpcEvents.Event<double>> events, Beat beat)
    {
        if (events.Count == 0)
            return 0f;
        int lo = 0,
            hi = events.Count - 1,
            idx = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (events[mid].StartBeat <= beat)
            {
                idx = mid;
                lo = mid + 1;
            }
            else
                hi = mid - 1;
        }

        if (idx < 0)
            return 0f;
        var e = events[idx];
        return e.EndBeat > beat ? e.GetValueAtBeat(beat) : e.EndValue;
    }

    /// <summary>
    /// 获取指定拍点上即将结束的事件插值（用于段终点）。
    /// </summary>
    /// <param name="events">事件列表</param>
    /// <param name="beat">目标拍点</param>
    /// <returns>插值结果</returns>
    public static double GetValOut(List<KpcEvents.Event<double>> events, Beat beat)
    {
        if (events.Count == 0)
            return 0f;
        int lo = 0,
            hi = events.Count - 1,
            idx = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (events[mid].StartBeat < beat)
            {
                idx = mid;
                lo = mid + 1;
            }
            else
                hi = mid - 1;
        }

        if (idx < 0)
            return 0f;
        var e = events[idx];
        return e.EndBeat >= beat ? e.GetValueAtBeat(beat) : e.EndValue;
    }

    /// <summary>
    /// 按层顺序将某一类型的事件列表串行叠加合并。
    /// </summary>
    public static List<KpcEvents.Event<double>> MergeLayerChannel(
        List<EventLayer> layers,
        Func<EventLayer, List<KpcEvents.Event<double>>?> selector,
        Func<
            List<KpcEvents.Event<double>>,
            List<KpcEvents.Event<double>>,
            List<KpcEvents.Event<double>>
        > merge
    )
    {
        var result = new List<KpcEvents.Event<double>>();
        return layers
            .Select(selector)
            .Where(ch => ch is { Count: > 0 })
            .Aggregate(
                result,
                (current, ch) =>
                    merge(
                        current,
                        ch ?? throw new InvalidOperationException("Channel selector returned null")
                    )
            );
    }

    /// <summary>
    /// 命中缓存时返回克隆结果，避免调用方直接持有缓存实例。
    /// </summary>
    public static bool TryGetCachedClone(
        int targetJudgeLineIndex,
        ConcurrentDictionary<int, JudgeLine> cache,
        string logTag,
        [NotNullWhen(true)] out JudgeLine? cachedClone,
        Action<string>? logDebug = null
    )
    {
        if (cache.TryGetValue(targetJudgeLineIndex, out var cached))
        {
            logDebug?.Invoke($"{logTag}[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            cachedClone = cached.Clone();
            return true;
        }

        cachedClone = null;
        return false;
    }

    /// <summary>
    /// 判定线无父线时直接缓存并返回，统一同步/异步处理器的短路分支。
    /// </summary>
    public static bool TryReturnWhenNoFather(
        int targetJudgeLineIndex,
        JudgeLine judgeLineCopy,
        ConcurrentDictionary<int, JudgeLine> cache,
        string logTag,
        Action<string>? logWarning = null
    )
    {
        if (judgeLineCopy.Father > -1)
            return false;
        logWarning?.Invoke($"{logTag}[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
        cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
        return true;
    }

    /// <summary>
    /// 清理判定线与父线的全零事件层，减少后续通道合并计算量。
    /// </summary>
    public static void CleanupRedundantLayers(JudgeLine judgeLineCopy, JudgeLine fatherLineCopy)
    {
        judgeLineCopy.EventLayers =
            RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
        fatherLineCopy.EventLayers =
            RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;
    }

    /// <summary>移除无用层级（所有事件都为默认值的层级）。</summary>
    private static List<EventLayer>? RemoveUnlessLayer(List<EventLayer>? layers)
    {
        if (layers is not { Count: > 1 })
            return layers;
        var layersCopy = layers.Select(l => l.Clone()).ToList();
        var intCompressor = new EventCompressor<int>();
        var doubleCompressor = new EventCompressor<double>();
        foreach (var layer in layersCopy)
        {
            layer.AlphaEvents = intCompressor.RemoveUselessEvent(layer.AlphaEvents);
            layer.MoveXEvents = doubleCompressor.RemoveUselessEvent(layer.MoveXEvents);
            layer.MoveYEvents = doubleCompressor.RemoveUselessEvent(layer.MoveYEvents);
            layer.RotateEvents = doubleCompressor.RemoveUselessEvent(layer.RotateEvents);
        }

        return layersCopy;
    }

    /// <summary>获取事件列表的拍范围（最小 StartBeat，最大 EndBeat）。列表为空时返回 (0, 0)。</summary>
    public static (Beat Min, Beat Max) GetEventRange(List<KpcEvents.Event<double>> events) =>
        events.Count == 0
            ? (new Beat(0), new Beat(0))
            : (events.Min(e => e.StartBeat), events.Max(e => e.EndBeat));

    /// <summary>
    /// 将计算结果写回判定线：清除第 1 层及以上的 X/Y 事件，将结果写入第 0 层。
    /// RotateWithFather 为 true 时叠加父线旋转事件；最后置 Father = -1 完成解绑。
    /// </summary>
    public static void WriteResultToLine(
        JudgeLine line,
        List<KpcEvents.Event<double>> newXEvents,
        List<KpcEvents.Event<double>> newYEvents,
        List<KpcEvents.Event<double>> fatherRotateEvents,
        Func<
            List<KpcEvents.Event<double>>,
            List<KpcEvents.Event<double>>,
            List<KpcEvents.Event<double>>
        > merge
    )
    {
        for (var i = 1; i < line.EventLayers.Count; i++)
        {
            line.EventLayers[i].MoveXEvents?.Clear();
            line.EventLayers[i].MoveYEvents?.Clear();
        }

        if (line.EventLayers.Count == 0)
            line.EventLayers.Add(new EventLayer());

        line.EventLayers[0].MoveXEvents = newXEvents;
        line.EventLayers[0].MoveYEvents = newYEvents;

        if (line.RotateWithFather)
        {
            var merged = merge(line.EventLayers[0].RotateEvents ?? [], fatherRotateEvents);
            line.EventLayers[0].RotateEvents = merged;
        }

        line.Father = -1;
    }

    #region 共享数据结构

    /// <summary>
    /// 封装解绑计算所需的五个事件通道：父线 X/Y/旋转 和子线 X/Y。
    /// 使用 readonly record struct 保证值语义，可安全在多线程闭包中捕获。
    /// </summary>
    public readonly record struct EventChannels(
        List<KpcEvents.Event<double>> Fx,
        List<KpcEvents.Event<double>> Fy,
        List<KpcEvents.Event<double>> Fr,
        List<KpcEvents.Event<double>> Tx,
        List<KpcEvents.Event<double>> Ty
    );

    /// <summary>
    /// 合并父子线五个通道事件，统一 EventChannels 拼装顺序。
    /// </summary>
    public static EventChannels MergeChannels(
        List<EventLayer> targetLayers,
        List<EventLayer> fatherLayers,
        Func<
            List<KpcEvents.Event<double>>,
            List<KpcEvents.Event<double>>,
            List<KpcEvents.Event<double>>
        > merge
    )
    {
        return new EventChannels(
            Fx: MergeLayerChannel(fatherLayers, l => l.MoveXEvents, merge),
            Fy: MergeLayerChannel(fatherLayers, l => l.MoveYEvents, merge),
            Fr: MergeLayerChannel(fatherLayers, l => l.RotateEvents, merge),
            Tx: MergeLayerChannel(targetLayers, l => l.MoveXEvents, merge),
            Ty: MergeLayerChannel(targetLayers, l => l.MoveYEvents, merge)
        );
    }

    #endregion

    #region 等间隔采样算法

    /// <summary>
    /// 生成从 <paramref name="min"/> 到 <paramref name="max"/>（不含）以 <paramref name="step"/> 为步长的拍列表。
    /// </summary>
    public static List<Beat> BuildBeatList(
        Beat min,
        Beat max,
        Beat step,
        CancellationToken ct = default
    )
    {
        // 预计算容量，避免多次内部扩容
        var range = (double)(max - min);
        var stepSize = (double)step;
        var estimatedCount = stepSize > 0 ? (int)Math.Ceiling(range / stepSize) : 0;
        var beats = new List<Beat>(Math.Max(0, estimatedCount));
        for (var b = min; b < max; b += step)
        {
            ct.ThrowIfCancellationRequested();
            beats.Add(b);
        }
        return beats;
    }

    /// <summary>
    /// 并行等间隔采样：对 <paramref name="beats"/> 中每一段计算绝对坐标，返回按顺序排列的 X/Y 事件列表。
    /// </summary>
    public static (
        List<KpcEvents.Event<double>> x,
        List<KpcEvents.Event<double>> y
    ) EqualSpacingSampling(
        List<Beat> beats,
        Beat max,
        Beat step,
        EventChannels ch,
        CancellationToken ct = default
    )
    {
        var xBag = new ConcurrentBag<(int i, KpcEvents.Event<double> evt)>();
        var yBag = new ConcurrentBag<(int i, KpcEvents.Event<double> evt)>();

        Parallel.For(
            0,
            beats.Count,
            new ParallelOptions { CancellationToken = ct },
            (int i) =>
            {
                ct.ThrowIfCancellationRequested();
                var beat = beats[i];
                var next = beat + step > max ? max : beat + step;
                var (xEvt, yEvt) = ComputeBeatSegment(beat, next, ch);
                xBag.Add((i, xEvt));
                yBag.Add((i, yEvt));
            }
        );

        return (
            xBag.OrderBy(x => x.i).Select(x => x.evt).ToList(),
            yBag.OrderBy(x => x.i).Select(x => x.evt).ToList()
        );
    }

    /// <summary>
    /// 计算单个采样段 [<paramref name="beat"/>, <paramref name="next"/>] 的 X/Y 绝对坐标事件。
    /// 段起点取 GetValIn（正在生效的插值），段终点取 GetValOut（即将结束的插值）。
    /// </summary>
    public static (KpcEvents.Event<double> x, KpcEvents.Event<double> y) ComputeBeatSegment(
        Beat beat,
        Beat next,
        EventChannels ch
    )
    {
        var (startAbsX, startAbsY) = GetLinePos(
            GetValIn(ch.Fx, beat),
            GetValIn(ch.Fy, beat),
            GetValIn(ch.Fr, beat),
            GetValIn(ch.Tx, beat),
            GetValIn(ch.Ty, beat)
        );
        var (endAbsX, endAbsY) = GetLinePos(
            GetValOut(ch.Fx, next),
            GetValOut(ch.Fy, next),
            GetValOut(ch.Fr, next),
            GetValOut(ch.Tx, next),
            GetValOut(ch.Ty, next)
        );

        return (
            new KpcEvents.Event<double>
            {
                StartBeat = beat,
                EndBeat = next,
                StartValue = startAbsX,
                EndValue = endAbsX,
            },
            new KpcEvents.Event<double>
            {
                StartBeat = beat,
                EndBeat = next,
                StartValue = startAbsY,
                EndValue = endAbsY,
            }
        );
    }

    #endregion

    #region 自适应采样算法

    /// <summary>
    /// 尝试计算五个通道的总体拍范围。若所有通道均为空则返回 <see langword="null"/>。
    /// </summary>
    public static (Beat min, Beat max)? TryGetOverallRange(EventChannels ch)
    {
        Beat overallMin = new(0),
            overallMax = new(0);
        var hasEvents = false;
        foreach (var list in new[] { ch.Tx, ch.Ty, ch.Fx, ch.Fy, ch.Fr })
        {
            if (list.Count == 0)
                continue;
            var (mn, mx) = GetEventRange(list);
            if (!hasEvents)
            {
                overallMin = mn;
                overallMax = mx;
                hasEvents = true;
            }
            else
            {
                if (mn < overallMin)
                    overallMin = mn;
                if (mx > overallMax)
                    overallMax = mx;
            }
        }

        return hasEvents ? (overallMin, overallMax) : null;
    }

    /// <summary>
    /// 收集所有通道事件的起止拍作为关键帧，在 [<paramref name="overallMin"/>, <paramref name="overallMax"/>]
    /// 范围内去重排序后返回。关键帧是自适应采样的强制切割点。
    /// </summary>
    public static List<Beat> CollectKeyBeats(Beat overallMin, Beat overallMax, EventChannels ch)
    {
        var keyBeatsList = new List<Beat> { overallMin, overallMax };
        foreach (var list in new[] { ch.Tx, ch.Ty, ch.Fx, ch.Fy, ch.Fr })
        {
            foreach (var e in list)
            {
                if (e.StartBeat >= overallMin && e.StartBeat <= overallMax)
                    keyBeatsList.Add(e.StartBeat);
                if (e.EndBeat >= overallMin && e.EndBeat <= overallMax)
                    keyBeatsList.Add(e.EndBeat);
            }
        }

        return keyBeatsList.Distinct().OrderBy(b => b).ToList();
    }

    /// <summary>
    /// 并行自适应采样：对 <paramref name="keyBeats"/> 中的每个区间调用
    /// <see cref="AdaptiveSampleInterval"/>，汇总后返回 X/Y 事件列表。
    /// </summary>
    public static (
        List<KpcEvents.Event<double>> x,
        List<KpcEvents.Event<double>> y
    ) RunAdaptiveSampling(
        List<Beat> keyBeats,
        Beat step,
        double tolerance,
        EventChannels ch,
        CancellationToken ct = default
    )
    {
        var segmentCount = keyBeats.Count - 1;
        var segmentsX = new List<KpcEvents.Event<double>>[segmentCount];
        var segmentsY = new List<KpcEvents.Event<double>>[segmentCount];
        for (var i = 0; i < segmentCount; i++)
        {
            segmentsX[i] = [];
            segmentsY[i] = [];
        }

        Parallel.For(
            0,
            segmentCount,
            new ParallelOptions { CancellationToken = ct },
            (int ki) =>
            {
                ct.ThrowIfCancellationRequested();
                if (keyBeats[ki] >= keyBeats[ki + 1])
                    return;
                var (sx, sy) = AdaptiveSampleInterval(
                    keyBeats[ki],
                    keyBeats[ki + 1],
                    step,
                    tolerance,
                    AbsPosIn,
                    AbsPosOut,
                    ct
                );
                segmentsX[ki].AddRange(sx);
                segmentsY[ki].AddRange(sy);
            }
        );

        var resX = new List<KpcEvents.Event<double>>();
        var resY = new List<KpcEvents.Event<double>>();
        foreach (var seg in segmentsX)
            resX.AddRange(seg);
        foreach (var seg in segmentsY)
            resY.AddRange(seg);
        return (resX, resY);

        (double X, double Y) AbsPosIn(Beat b) =>
            GetLinePos(
                GetValIn(ch.Fx, b),
                GetValIn(ch.Fy, b),
                GetValIn(ch.Fr, b),
                GetValIn(ch.Tx, b),
                GetValIn(ch.Ty, b)
            );

        (double X, double Y) AbsPosOut(Beat b) =>
            GetLinePos(
                GetValOut(ch.Fx, b),
                GetValOut(ch.Fy, b),
                GetValOut(ch.Fr, b),
                GetValOut(ch.Tx, b),
                GetValOut(ch.Ty, b)
            );
    }

    /// <summary>
    /// 对单个区间 [<paramref name="iStart"/>, <paramref name="iEnd"/>] 进行自适应分段采样：
    /// 以 <paramref name="step"/> 推进，当相邻采样点误差超出容差时插入切割点，否则延续当前段。
    /// </summary>
    private static (
        List<KpcEvents.Event<double>> x,
        List<KpcEvents.Event<double>> y
    ) AdaptiveSampleInterval(
        Beat iStart,
        Beat iEnd,
        Beat step,
        double tolerance,
        Func<Beat, (double X, double Y)> absPosIn,
        Func<Beat, (double X, double Y)> absPosOut,
        CancellationToken ct
    )
    {
        var localX = new List<KpcEvents.Event<double>>();
        var localY = new List<KpcEvents.Event<double>>();

        var end = absPosOut(iEnd);
        var segStart = iStart;
        var seg = absPosIn(iStart);

        for (var cur = iStart; cur < iEnd; )
        {
            ct.ThrowIfCancellationRequested();
            var next = cur + step > iEnd ? iEnd : cur + step;
            var isLast = next >= iEnd;
            var nextPos = isLast ? end : absPosIn(next);

            if (isLast || NeedsAdaptiveCut(seg, nextPos, end, segStart, iEnd, next, tolerance))
            {
                localX.Add(
                    new KpcEvents.Event<double>
                    {
                        StartBeat = segStart,
                        EndBeat = next,
                        StartValue = seg.X,
                        EndValue = nextPos.X,
                    }
                );
                localY.Add(
                    new KpcEvents.Event<double>
                    {
                        StartBeat = segStart,
                        EndBeat = next,
                        StartValue = seg.Y,
                        EndValue = nextPos.Y,
                    }
                );
                segStart = next;
                seg = nextPos;
            }

            cur = next;
        }

        return (localX, localY);
    }

    #endregion
}
