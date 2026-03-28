using System.Collections.Concurrent;
using PhiFanmade.Core.Common;
using PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;
using PhiFanmade.Tool.PhiFanmadeNrc.Layers.Internal;

namespace PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines.Internal;

/// <summary>
/// NRC 判定线父子解绑异步处理器（async/await 版本）。
/// 缓存与同步版 <see cref="FatherUnbindProcessor"/> 共享同一张 <see cref="FatherUnbindHelpers.ChartCacheTable"/>。
/// </summary>
internal static class FatherUnbindAsyncProcessor
{
    private readonly record struct EventChannels(
        List<Nrc.Event<float>> Fx, List<Nrc.Event<float>> Fy, List<Nrc.Event<float>> Fr,
        List<Nrc.Event<float>> Tx, List<Nrc.Event<float>> Ty);

    // ─── 等间隔采样异步版 ────────────────────────────────────────────────────

    internal static async Task<Nrc.JudgeLine> FatherUnbindAsync(
        int targetJudgeLineIndex,
        List<Nrc.JudgeLine> allJudgeLines,
        double precision, double tolerance,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache,
        bool compress)
    {
        if (cache.TryGetValue(targetJudgeLineIndex, out var cached))
        {
            NrcToolLog.OnDebug($"FatherUnbindAsync[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cached.Clone();
        }

        var judgeLineCopy    = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                NrcToolLog.OnWarning($"FatherUnbindAsync[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            NrcToolLog.OnInfo($"FatherUnbindAsync[{targetJudgeLineIndex}]: 开始解绑，父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                NrcToolLog.OnDebug($"FatherUnbindAsync[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                fatherLineCopy = await FatherUnbindAsync(
                    judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache, compress);
            }

            judgeLineCopy.EventLayers  = LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers)  ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            var mergeResults = await Task.WhenAll(
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveXEvents,
                    (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveYEvents,
                    (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveXEvents,
                    (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveYEvents,
                    (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.RotateEvents,
                    (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress)))
            );

            var (txMin, txMax) = FatherUnbindHelpers.GetEventRange(mergeResults[0]);
            var (tyMin, tyMax) = FatherUnbindHelpers.GetEventRange(mergeResults[1]);
            var (fxMin, fxMax) = FatherUnbindHelpers.GetEventRange(mergeResults[2]);
            var (fyMin, fyMax) = FatherUnbindHelpers.GetEventRange(mergeResults[3]);
            var (frMin, frMax) = FatherUnbindHelpers.GetEventRange(mergeResults[4]);
            var cutLength      = new Beat(1d / precision);

            var cutResults = await Task.WhenAll(
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[0], txMin, txMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[1], tyMin, tyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[2], fxMin, fxMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[3], fyMin, fyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[4], frMin, frMax, cutLength))
            );

            var ch          = new EventChannels(cutResults[2], cutResults[3], cutResults[4], cutResults[0], cutResults[1]);
            var overallMin  = new Beat(Math.Min(Math.Min(txMin, tyMin), Math.Min(fxMin, fyMin)));
            var overallMax  = new Beat(Math.Max(Math.Max(txMax, tyMax), Math.Max(fxMax, fyMax)));
            var step        = new Beat(1d / precision);
            var beats       = BuildBeatList(overallMin, overallMax, step);

            NrcToolLog.OnDebug($"FatherUnbindAsync[{targetJudgeLineIndex}]: 等间隔采样 {beats.Count} 段，精度={precision}");

            var (sortedX, sortedY) = await RunEqualSpacingSamplingAsync(beats, step, ch);

            NrcToolLog.OnDebug($"FatherUnbindAsync[{targetJudgeLineIndex}]: 采样完成，写回");
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, sortedX, sortedY, ch.Fr, tolerance,
                (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress), compress);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            NrcToolLog.OnInfo($"FatherUnbindAsync[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (NullReferenceException ex)
        {
            NrcToolLog.OnError($"FatherUnbindAsync[{targetJudgeLineIndex}]: 存在空引用: " + ex);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            NrcToolLog.OnError($"FatherUnbindAsync[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }

    // ─── 自适应采样异步版 ────────────────────────────────────────────────────

    internal static async Task<Nrc.JudgeLine> FatherUnbindPlusAsync(
        int targetJudgeLineIndex,
        List<Nrc.JudgeLine> allJudgeLines,
        double precision, double tolerance,
        ConcurrentDictionary<int, Nrc.JudgeLine> cache)
    {
        if (cache.TryGetValue(targetJudgeLineIndex, out var cached))
        {
            NrcToolLog.OnDebug($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cached.Clone();
        }

        var judgeLineCopy    = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                NrcToolLog.OnWarning($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            NrcToolLog.OnInfo($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 开始解绑（自适应采样），父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                NrcToolLog.OnDebug($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                fatherLineCopy = await FatherUnbindPlusAsync(
                    judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache);
            }

            judgeLineCopy.EventLayers  = LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers)  ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            var mergeResults = await Task.WhenAll(
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveXEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveYEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveXEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveYEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.RotateEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b)))
            );

            var rangeResult = TryGetOverallRange(mergeResults[0], mergeResults[1], mergeResults[2], mergeResults[3]);
            if (rangeResult is null)
            {
                judgeLineCopy.Father = -1;
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            var ch                      = new EventChannels(mergeResults[2], mergeResults[3], mergeResults[4], mergeResults[0], mergeResults[1]);
            var (overallMin, overallMax) = rangeResult.Value;
            var step                    = new Beat(1d / precision);
            var keyBeats                = CollectKeyBeats(overallMin, overallMax, new[] { ch.Tx, ch.Ty, ch.Fx, ch.Fy, ch.Fr });

            NrcToolLog.OnDebug($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 自适应采样，关键帧数={keyBeats.Count}，最大精度={precision}");

            var (resultX, resultY) = await Task.Run(() => RunAdaptiveSampling(keyBeats, step, tolerance, ch));

            NrcToolLog.OnDebug($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 采样完成（生成 {resultX.Count} 段），压缩并写回");
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, resultX, resultY, ch.Fr, tolerance,
                (a, b) => EventMerger.EventMergePlus(a, b), compress: true);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            NrcToolLog.OnInfo($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (NullReferenceException ex)
        {
            NrcToolLog.OnError($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 存在空引用: " + ex);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            NrcToolLog.OnError($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }

    // ─── 等间隔采样辅助 ──────────────────────────────────────────────────────

    private static List<Beat> BuildBeatList(Beat min, Beat max, Beat step)
    {
        var beats = new List<Beat>();
        for (var b = min; b <= max; b += step) beats.Add(b);
        return beats;
    }

    private static Task<(List<Nrc.Event<float>>, List<Nrc.Event<float>>)> RunEqualSpacingSamplingAsync(
        List<Beat> beats, Beat step, EventChannels ch)
        => Task.Run(() => EqualSpacingSampling(beats, step, ch));

    private static (List<Nrc.Event<float>> x, List<Nrc.Event<float>> y) EqualSpacingSampling(
        List<Beat> beats, Beat step, EventChannels ch)
    {
        var xBag = new ConcurrentBag<(int i, Nrc.Event<float> evt)>();
        var yBag = new ConcurrentBag<(int i, Nrc.Event<float> evt)>();

        Parallel.For(0, beats.Count, i =>
        {
            var (xEvt, yEvt) = ComputeBeatSegment(i, beats[i], beats[i] + step, ch);
            xBag.Add((i, xEvt));
            yBag.Add((i, yEvt));
        });

        return (xBag.OrderBy(x => x.i).Select(x => x.evt).ToList(),
                yBag.OrderBy(x => x.i).Select(x => x.evt).ToList());
    }

    private static (Nrc.Event<float> x, Nrc.Event<float> y) ComputeBeatSegment(
        int i, Beat beat, Beat next, EventChannels ch)
    {
        var prevFx = GetPrevValue(ch.Fx, beat, i);
        var prevFy = GetPrevValue(ch.Fy, beat, i);
        var prevFr = GetPrevValue(ch.Fr, beat, i);
        var prevTx = GetPrevValue(ch.Tx, beat, i);
        var prevTy = GetPrevValue(ch.Ty, beat, i);

        var fxEvt = FindSegment(ch.Fx, beat, next);
        var fyEvt = FindSegment(ch.Fy, beat, next);
        var frEvt = FindSegment(ch.Fr, beat, next);
        var txEvt = FindSegment(ch.Tx, beat, next);
        var tyEvt = FindSegment(ch.Ty, beat, next);

        var (startAbsX, startAbsY) = FatherUnbindHelpers.GetLinePos(
            fxEvt?.StartValue ?? prevFx, fyEvt?.StartValue ?? prevFy, frEvt?.StartValue ?? prevFr,
            txEvt?.StartValue ?? prevTx, tyEvt?.StartValue ?? prevTy);
        var (endAbsX, endAbsY) = FatherUnbindHelpers.GetLinePos(
            fxEvt?.EndValue ?? prevFx, fyEvt?.EndValue ?? prevFy, frEvt?.EndValue ?? prevFr,
            txEvt?.EndValue ?? prevTx, tyEvt?.EndValue ?? prevTy);

        return (
            new Nrc.Event<float> { StartBeat = beat, EndBeat = next, StartValue = (float)startAbsX, EndValue = (float)endAbsX },
            new Nrc.Event<float> { StartBeat = beat, EndBeat = next, StartValue = (float)startAbsY, EndValue = (float)endAbsY }
        );
    }

    private static float GetPrevValue(List<Nrc.Event<float>> events, Beat beat, int i)
        => i > 0 ? events.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;

    private static Nrc.Event<float>? FindSegment(List<Nrc.Event<float>> events, Beat start, Beat end)
        => events.FirstOrDefault(e => e.StartBeat == start && e.EndBeat == end);

    // ─── 自适应采样辅助 ──────────────────────────────────────────────────────

    private static (Beat min, Beat max)? TryGetOverallRange(
        List<Nrc.Event<float>> tX, List<Nrc.Event<float>> tY,
        List<Nrc.Event<float>> fX, List<Nrc.Event<float>> fY)
    {
        Beat overallMin = new(0), overallMax = new(0);
        var hasEvents = false;
        foreach (var list in new[] { tX, tY, fX, fY })
        {
            if (list.Count == 0) continue;
            var (mn, mx) = FatherUnbindHelpers.GetEventRange(list);
            if (!hasEvents) { overallMin = mn; overallMax = mx; hasEvents = true; }
            else { if (mn < overallMin) overallMin = mn; if (mx > overallMax) overallMax = mx; }
        }

        return hasEvents ? (overallMin, overallMax) : null;
    }

    private static List<Beat> CollectKeyBeats(Beat overallMin, Beat overallMax,
        IEnumerable<List<Nrc.Event<float>>> channels)
    {
        var keyBeatsList = new List<Beat> { overallMin, overallMax };
        foreach (var list in channels)
        foreach (var e in list)
        {
            if (e.StartBeat >= overallMin && e.StartBeat <= overallMax) keyBeatsList.Add(e.StartBeat);
            if (e.EndBeat   >= overallMin && e.EndBeat   <= overallMax) keyBeatsList.Add(e.EndBeat);
        }

        return keyBeatsList.Distinct().OrderBy(b => b).ToList();
    }

    private static (List<Nrc.Event<float>> x, List<Nrc.Event<float>> y) RunAdaptiveSampling(
        List<Beat> keyBeats, Beat step, double tolerance, EventChannels ch)
    {
        (double X, double Y) AbsPosIn(Beat beat) => FatherUnbindHelpers.GetLinePos(
            FatherUnbindHelpers.GetValIn(ch.Fx, beat), FatherUnbindHelpers.GetValIn(ch.Fy, beat),
            FatherUnbindHelpers.GetValIn(ch.Fr, beat),
            FatherUnbindHelpers.GetValIn(ch.Tx, beat), FatherUnbindHelpers.GetValIn(ch.Ty, beat));

        (double X, double Y) AbsPosOut(Beat beat) => FatherUnbindHelpers.GetLinePos(
            FatherUnbindHelpers.GetValOut(ch.Fx, beat), FatherUnbindHelpers.GetValOut(ch.Fy, beat),
            FatherUnbindHelpers.GetValOut(ch.Fr, beat),
            FatherUnbindHelpers.GetValOut(ch.Tx, beat), FatherUnbindHelpers.GetValOut(ch.Ty, beat));

        var segmentCount = keyBeats.Count - 1;
        var segmentsX    = new List<Nrc.Event<float>>[segmentCount];
        var segmentsY    = new List<Nrc.Event<float>>[segmentCount];
        for (var i = 0; i < segmentCount; i++) { segmentsX[i] = []; segmentsY[i] = []; }

        Parallel.For(0, segmentCount, ki =>
        {
            if (keyBeats[ki] >= keyBeats[ki + 1]) return;
            var (sx, sy) = AdaptiveSampleInterval(
                keyBeats[ki], keyBeats[ki + 1], step, tolerance, AbsPosIn, AbsPosOut);
            segmentsX[ki].AddRange(sx);
            segmentsY[ki].AddRange(sy);
        });

        var resX = new List<Nrc.Event<float>>();
        var resY = new List<Nrc.Event<float>>();
        foreach (var seg in segmentsX) resX.AddRange(seg);
        foreach (var seg in segmentsY) resY.AddRange(seg);
        return (resX, resY);
    }

    private static (List<Nrc.Event<float>> x, List<Nrc.Event<float>> y) AdaptiveSampleInterval(
        Beat iStart, Beat iEnd, Beat step, double tolerance,
        Func<Beat, (double X, double Y)> absPosIn,
        Func<Beat, (double X, double Y)> absPosOut)
    {
        var localX = new List<Nrc.Event<float>>();
        var localY = new List<Nrc.Event<float>>();

        var end      = absPosOut(iEnd);
        var segStart = iStart;
        var seg      = absPosIn(iStart);

        for (var cur = iStart; cur < iEnd;)
        {
            var next   = cur + step > iEnd ? iEnd : cur + step;
            var isLast = next >= iEnd;
            var nextPos = isLast ? end : absPosIn(next);

            if (isLast || NeedsAdaptiveCut(seg, nextPos, end, segStart, iEnd, next, tolerance))
            {
                localX.Add(new Nrc.Event<float>
                    { StartBeat = segStart, EndBeat = next, StartValue = (float)seg.X, EndValue = (float)nextPos.X });
                localY.Add(new Nrc.Event<float>
                    { StartBeat = segStart, EndBeat = next, StartValue = (float)seg.Y, EndValue = (float)nextPos.Y });
                segStart = next;
                seg      = nextPos;
            }

            cur = next;
        }

        return (localX, localY);
    }

    private static bool NeedsAdaptiveCut(
        (double X, double Y) seg, (double X, double Y) next,
        (double X, double Y) end, Beat segStart, Beat iEnd, Beat nextBeat, double tolerance)
    {
        var segLen   = (double)(iEnd - segStart);
        var progress = segLen > 1e-12 ? (double)(nextBeat - segStart) / segLen : 1.0;
        var predX    = seg.X + (end.X - seg.X) * progress;
        var predY    = seg.Y + (end.Y - seg.Y) * progress;
        var thrX     = tolerance / 100.0 * ((Math.Abs(seg.X) + Math.Abs(next.X)) / 2.0 + 1e-9);
        var thrY     = tolerance / 100.0 * ((Math.Abs(seg.Y) + Math.Abs(next.Y)) / 2.0 + 1e-9);
        return Math.Abs(next.X - predX) > thrX || Math.Abs(next.Y - predY) > thrY;
    }
}

