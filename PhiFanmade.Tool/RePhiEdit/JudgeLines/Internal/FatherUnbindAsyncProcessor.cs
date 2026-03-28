using System.Collections.Concurrent;
using PhiFanmade.Core.Common;
using PhiFanmade.Tool.RePhiEdit.Events.Internal;
using PhiFanmade.Tool.RePhiEdit.Layers.Internal;

namespace PhiFanmade.Tool.RePhiEdit.JudgeLines.Internal;

/// <summary>
/// RPE 判定线父子解绑异步处理器（async/await 版本）。
/// </summary>
internal static class FatherUnbindAsyncProcessor
{
    private readonly record struct EventChannels(
        List<Rpe.Event<float>> Fx,
        List<Rpe.Event<float>> Fy,
        List<Rpe.Event<float>> Fr,
        List<Rpe.Event<float>> Tx,
        List<Rpe.Event<float>> Ty);

    internal static async Task<Rpe.JudgeLine> FatherUnbindAsync(
        int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines,
        double precision,
        double tolerance,
        ConcurrentDictionary<int, Rpe.JudgeLine> cache,
        bool compress)
    {
        if (cache.TryGetValue(targetJudgeLineIndex, out var cached))
        {
            RpeToolLog.OnDebug($"FatherUnbindAsync[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cached.Clone();
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();

        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RpeToolLog.OnWarning($"FatherUnbindAsync[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                fatherLineCopy = await FatherUnbindAsync(judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache, compress);
            }

            judgeLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            var mergeResults = await Task.WhenAll(
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveXEvents, (a, b) => EventMerger.EventListMerge<float>(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveYEvents, (a, b) => EventMerger.EventListMerge<float>(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveXEvents, (a, b) => EventMerger.EventListMerge<float>(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveYEvents, (a, b) => EventMerger.EventListMerge<float>(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.RotateEvents, (a, b) => EventMerger.EventListMerge<float>(a, b, precision, tolerance, compress)))
            );

            var (txMin, txMax) = FatherUnbindHelpers.GetEventRange(mergeResults[0]);
            var (tyMin, tyMax) = FatherUnbindHelpers.GetEventRange(mergeResults[1]);
            var (fxMin, fxMax) = FatherUnbindHelpers.GetEventRange(mergeResults[2]);
            var (fyMin, fyMax) = FatherUnbindHelpers.GetEventRange(mergeResults[3]);
            var (frMin, frMax) = FatherUnbindHelpers.GetEventRange(mergeResults[4]);
            var cutLength = new Beat(1d / precision);

            var cutResults = await Task.WhenAll(
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[0], txMin, txMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[1], tyMin, tyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[2], fxMin, fxMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[3], fyMin, fyMax, cutLength)),
                Task.Run(() => EventCutter.CutEventsInRange(mergeResults[4], frMin, frMax, cutLength))
            );

            var ch = new EventChannels(cutResults[2], cutResults[3], cutResults[4], cutResults[0], cutResults[1]);
            var overallMin = new Beat(Math.Min(Math.Min(txMin, tyMin), Math.Min(fxMin, fyMin)));
            var overallMax = new Beat(Math.Max(Math.Max(txMax, tyMax), Math.Max(fxMax, fyMax)));
            var step = new Beat(1d / precision);
            var beats = BuildBeatList(overallMin, overallMax, step);

            var (sortedX, sortedY) = await Task.Run(() => EqualSpacingSampling(beats, step, ch));
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, sortedX, sortedY, ch.Fr, tolerance,
                (a, b) => EventMerger.EventListMerge<float>(a, b, precision, tolerance, compress), compress);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            RpeToolLog.OnError($"FatherUnbindAsync[{targetJudgeLineIndex}]: 未知错误: {ex.Message}");
            return judgeLineCopy;
        }
    }

    internal static async Task<Rpe.JudgeLine> FatherUnbindPlusAsync(
        int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines,
        double precision,
        double tolerance,
        ConcurrentDictionary<int, Rpe.JudgeLine> cache)
    {
        if (cache.TryGetValue(targetJudgeLineIndex, out var cached))
        {
            RpeToolLog.OnDebug($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cached.Clone();
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();

        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RpeToolLog.OnWarning($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                fatherLineCopy = await FatherUnbindPlusAsync(judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache);
            }

            judgeLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers = LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            var mergeResults = await Task.WhenAll(
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveXEvents, (a, b) => EventMerger.EventMergePlus<float>(a, b))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveYEvents, (a, b) => EventMerger.EventMergePlus<float>(a, b))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveXEvents, (a, b) => EventMerger.EventMergePlus<float>(a, b))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveYEvents, (a, b) => EventMerger.EventMergePlus<float>(a, b))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.RotateEvents, (a, b) => EventMerger.EventMergePlus<float>(a, b)))
            );

            var rangeResult = TryGetOverallRange(mergeResults[0], mergeResults[1], mergeResults[2], mergeResults[3]);
            if (rangeResult is null)
            {
                judgeLineCopy.Father = -1;
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            var ch = new EventChannels(mergeResults[2], mergeResults[3], mergeResults[4], mergeResults[0], mergeResults[1]);
            var (overallMin, overallMax) = rangeResult.Value;
            var step = new Beat(1d / precision);
            var keyBeats = CollectKeyBeats(overallMin, overallMax, new[] { ch.Tx, ch.Ty, ch.Fx, ch.Fy, ch.Fr });

            var (resultX, resultY) = await Task.Run(() => RunAdaptiveSampling(keyBeats, step, tolerance, ch));
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, resultX, resultY, ch.Fr, tolerance,
                (a, b) => EventMerger.EventMergePlus<float>(a, b), compress: true);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            RpeToolLog.OnError($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 未知错误: {ex.Message}");
            return judgeLineCopy;
        }
    }

    private static List<Beat> BuildBeatList(Beat min, Beat max, Beat step)
    {
        var beats = new List<Beat>();
        for (var b = min; b <= max; b += step)
            beats.Add(b);
        return beats;
    }

    private static (List<Rpe.Event<float>> x, List<Rpe.Event<float>> y) EqualSpacingSampling(
        List<Beat> beats,
        Beat step,
        EventChannels ch)
    {
        var xBag = new ConcurrentBag<(int i, Rpe.Event<float> evt)>();
        var yBag = new ConcurrentBag<(int i, Rpe.Event<float> evt)>();

        Parallel.For(0, beats.Count, i =>
        {
            var beat = beats[i];
            var next = beat + step;

            var prevFx = i > 0 ? ch.Fx.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
            var prevFy = i > 0 ? ch.Fy.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
            var prevFr = i > 0 ? ch.Fr.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
            var prevTx = i > 0 ? ch.Tx.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
            var prevTy = i > 0 ? ch.Ty.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;

            var fxEvt = ch.Fx.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
            var fyEvt = ch.Fy.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
            var frEvt = ch.Fr.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
            var txEvt = ch.Tx.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
            var tyEvt = ch.Ty.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);

            var (startAbsX, startAbsY) = FatherUnbindHelpers.GetLinePos(
                fxEvt?.StartValue ?? prevFx, fyEvt?.StartValue ?? prevFy, frEvt?.StartValue ?? prevFr,
                txEvt?.StartValue ?? prevTx, tyEvt?.StartValue ?? prevTy);
            var (endAbsX, endAbsY) = FatherUnbindHelpers.GetLinePos(
                fxEvt?.EndValue ?? prevFx, fyEvt?.EndValue ?? prevFy, frEvt?.EndValue ?? prevFr,
                txEvt?.EndValue ?? prevTx, tyEvt?.EndValue ?? prevTy);

            xBag.Add((i, new Rpe.Event<float>
            {
                StartBeat = beat,
                EndBeat = next,
                StartValue = (float)startAbsX,
                EndValue = (float)endAbsX
            }));
            yBag.Add((i, new Rpe.Event<float>
            {
                StartBeat = beat,
                EndBeat = next,
                StartValue = (float)startAbsY,
                EndValue = (float)endAbsY
            }));
        });

        return (
            xBag.OrderBy(x => x.i).Select(x => x.evt).ToList(),
            yBag.OrderBy(x => x.i).Select(x => x.evt).ToList());
    }

    private static (Beat min, Beat max)? TryGetOverallRange(
        List<Rpe.Event<float>> tX,
        List<Rpe.Event<float>> tY,
        List<Rpe.Event<float>> fX,
        List<Rpe.Event<float>> fY)
    {
        Beat overallMin = new(0), overallMax = new(0);
        var hasEvents = false;
        foreach (var list in new[] { tX, tY, fX, fY })
        {
            if (list.Count == 0)
                continue;

            var (mn, mx) = FatherUnbindHelpers.GetEventRange(list);
            if (!hasEvents)
            {
                overallMin = mn;
                overallMax = mx;
                hasEvents = true;
            }
            else
            {
                if (mn < overallMin) overallMin = mn;
                if (mx > overallMax) overallMax = mx;
            }
        }

        return hasEvents ? (overallMin, overallMax) : null;
    }

    private static List<Beat> CollectKeyBeats(
        Beat overallMin,
        Beat overallMax,
        IEnumerable<List<Rpe.Event<float>>> channels)
    {
        var keyBeatsList = new List<Beat> { overallMin, overallMax };
        foreach (var list in channels)
        {
            foreach (var e in list)
            {
                if (e.StartBeat >= overallMin && e.StartBeat <= overallMax) keyBeatsList.Add(e.StartBeat);
                if (e.EndBeat >= overallMin && e.EndBeat <= overallMax) keyBeatsList.Add(e.EndBeat);
            }
        }

        return keyBeatsList.Distinct().OrderBy(b => b).ToList();
    }

    private static (List<Rpe.Event<float>> x, List<Rpe.Event<float>> y) RunAdaptiveSampling(
        List<Beat> keyBeats,
        Beat step,
        double tolerance,
        EventChannels ch)
    {
        (double X, double Y) AbsPosIn(Beat beat) => FatherUnbindHelpers.GetLinePos(
            FatherUnbindHelpers.GetValIn(ch.Fx, beat), FatherUnbindHelpers.GetValIn(ch.Fy, beat), FatherUnbindHelpers.GetValIn(ch.Fr, beat),
            FatherUnbindHelpers.GetValIn(ch.Tx, beat), FatherUnbindHelpers.GetValIn(ch.Ty, beat));

        (double X, double Y) AbsPosOut(Beat beat) => FatherUnbindHelpers.GetLinePos(
            FatherUnbindHelpers.GetValOut(ch.Fx, beat), FatherUnbindHelpers.GetValOut(ch.Fy, beat), FatherUnbindHelpers.GetValOut(ch.Fr, beat),
            FatherUnbindHelpers.GetValOut(ch.Tx, beat), FatherUnbindHelpers.GetValOut(ch.Ty, beat));

        var segmentCount = keyBeats.Count - 1;
        var segmentsX = new List<Rpe.Event<float>>[segmentCount];
        var segmentsY = new List<Rpe.Event<float>>[segmentCount];
        for (var i = 0; i < segmentCount; i++)
        {
            segmentsX[i] = [];
            segmentsY[i] = [];
        }

        Parallel.For(0, segmentCount, ki =>
        {
            if (keyBeats[ki] >= keyBeats[ki + 1])
                return;

            var start = keyBeats[ki];
            var end = keyBeats[ki + 1];
            var endPos = AbsPosOut(end);
            var segStart = start;
            var seg = AbsPosIn(start);

            for (var cur = start; cur < end;)
            {
                var next = cur + step > end ? end : cur + step;
                var isLast = next >= end;
                var nextPos = isLast ? endPos : AbsPosIn(next);

                if (isLast || NeedsAdaptiveCut(seg, nextPos, endPos, segStart, end, next, tolerance))
                {
                    segmentsX[ki].Add(new Rpe.Event<float>
                    {
                        StartBeat = segStart,
                        EndBeat = next,
                        StartValue = (float)seg.X,
                        EndValue = (float)nextPos.X
                    });
                    segmentsY[ki].Add(new Rpe.Event<float>
                    {
                        StartBeat = segStart,
                        EndBeat = next,
                        StartValue = (float)seg.Y,
                        EndValue = (float)nextPos.Y
                    });
                    segStart = next;
                    seg = nextPos;
                }

                cur = next;
            }
        });

        var resultX = new List<Rpe.Event<float>>();
        var resultY = new List<Rpe.Event<float>>();
        foreach (var seg in segmentsX) resultX.AddRange(seg);
        foreach (var seg in segmentsY) resultY.AddRange(seg);
        return (resultX, resultY);
    }

    private static bool NeedsAdaptiveCut(
        (double X, double Y) seg,
        (double X, double Y) next,
        (double X, double Y) end,
        Beat segStart,
        Beat intervalEnd,
        Beat nextBeat,
        double tolerance)
    {
        var segLen = (double)(intervalEnd - segStart);
        var progress = segLen > 1e-12 ? (double)(nextBeat - segStart) / segLen : 1.0;
        var predX = seg.X + (end.X - seg.X) * progress;
        var predY = seg.Y + (end.Y - seg.Y) * progress;
        var thrX = tolerance / 100.0 * ((Math.Abs(seg.X) + Math.Abs(next.X)) / 2.0 + 1e-9);
        var thrY = tolerance / 100.0 * ((Math.Abs(seg.Y) + Math.Abs(next.Y)) / 2.0 + 1e-9);
        return Math.Abs(next.X - predX) > thrX || Math.Abs(next.Y - predY) > thrY;
    }
}

