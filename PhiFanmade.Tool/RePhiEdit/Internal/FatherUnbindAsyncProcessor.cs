using System.Collections.Concurrent;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Tool.RePhiEdit.Internal;

/// <summary>
/// 判定线父子关系异步处理器（async/await 版本）
/// 缓存与同步版 <see cref="FatherUnbindProcessor"/> 共享同一张 <c>ChartCacheTable</c>，
/// 同步/异步混用时不会重复解绑同一父线。
/// </summary>
internal static class FatherUnbindAsyncProcessor
{
    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。
    /// 策略：等间隔采样（精度由 precision 决定），各通道层级合并并行执行，主采样循环异步卸载至线程池。
    /// </summary>
    internal static async Task<Rpe.JudgeLine> FatherUnbindAsync(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision, double tolerance,
        ConcurrentDictionary<int, Rpe.JudgeLine> cache, bool compress)
    {
        // ── 缓存命中：该线已被解绑过，直接返回副本，避免重复计算 ──
        if (cache.TryGetValue(targetJudgeLineIndex, out var cachedResult))
        {
            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbindAsync[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cachedResult.Clone();
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RePhiEditHelper.OnWarning.Invoke($"FatherUnbindAsync[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            RePhiEditHelper.OnInfo.Invoke(
                $"FatherUnbindAsync[{targetJudgeLineIndex}]: 开始解绑，父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                RePhiEditHelper.OnDebug.Invoke(
                    $"FatherUnbindAsync[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                // 传入共享缓存：若父线已被其他调用解绑过，直接复用缓存结果
                fatherLineCopy = await FatherUnbindAsync(judgeLineCopy.Father, allJudgeLinesCopy,
                    precision, tolerance, cache, compress);
            }

            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            // 各通道按层顺序串行叠加（层间叠加不满足交换律；不同通道间互不依赖，并行执行）
            var mergeResults = await Task.WhenAll(
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    tLayers, l => l.MoveXEvents,
                    (a, b) => EventProcessor.EventListMerge(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    tLayers, l => l.MoveYEvents,
                    (a, b) => EventProcessor.EventListMerge(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    fLayers, l => l.MoveXEvents,
                    (a, b) => EventProcessor.EventListMerge(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    fLayers, l => l.MoveYEvents,
                    (a, b) => EventProcessor.EventListMerge(a, b, precision, tolerance, compress))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    fLayers, l => l.RotateEvents,
                    (a, b) => EventProcessor.EventListMerge(a, b, precision, tolerance, compress)))
            );

            var tX = mergeResults[0];
            var tY = mergeResults[1];
            var fX = mergeResults[2];
            var fY = mergeResults[3];
            var fR = mergeResults[4];

            var (txMin, txMax) = FatherUnbindProcessor.GetEventRange(tX);
            var (tyMin, tyMax) = FatherUnbindProcessor.GetEventRange(tY);
            var (fxMin, fxMax) = FatherUnbindProcessor.GetEventRange(fX);
            var (fyMin, fyMax) = FatherUnbindProcessor.GetEventRange(fY);
            var (frMin, frMax) = FatherUnbindProcessor.GetEventRange(fR);
            var cutLength = new Beat(1d / precision);
            // 5 个通道互不依赖，并行切割为等长小段
            var cutResults = await Task.WhenAll(
                Task.Run(() => EventProcessor.CutEventsInRange(tX, txMin, txMax, cutLength)),
                Task.Run(() => EventProcessor.CutEventsInRange(tY, tyMin, tyMax, cutLength)),
                Task.Run(() => EventProcessor.CutEventsInRange(fX, fxMin, fxMax, cutLength)),
                Task.Run(() => EventProcessor.CutEventsInRange(fY, fyMin, fyMax, cutLength)),
                Task.Run(() => EventProcessor.CutEventsInRange(fR, frMin, frMax, cutLength))
            );

            tX = cutResults[0];
            tY = cutResults[1];
            fX = cutResults[2];
            fY = cutResults[3];
            fR = cutResults[4];

            // 采样范围仅由 X/Y 移动事件决定（旋转事件不影响范围边界）
            var overallMin = new Beat(Math.Min(Math.Min(txMin, tyMin), Math.Min(fxMin, fyMin)));
            var overallMax = new Beat(Math.Max(Math.Max(txMax, tyMax), Math.Max(fxMax, fyMax)));
            var step = new Beat(1d / precision);

            var beats = new List<Beat>();
            for (var b = overallMin; b <= overallMax; b += step)
                beats.Add(b);

            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbindAsync[{targetJudgeLineIndex}]: 等间隔采样 {beats.Count} 段，精度={precision}");

            // CPU 密集型主循环：卸载至线程池，内部用 Parallel.For 多核加速
            var (sortedX, sortedY) = await Task.Run(() =>
            {
                var xBag = new ConcurrentBag<(int i, Rpe.Event<float> evt)>();
                var yBag = new ConcurrentBag<(int i, Rpe.Event<float> evt)>();

                Parallel.For(0, beats.Count, i =>
                {
                    var beat = beats[i];
                    var next = beat + step;

                    var prevFX = i > 0 ? fX.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
                    var prevFY = i > 0 ? fY.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
                    var prevFR = i > 0 ? fR.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
                    var prevTX = i > 0 ? tX.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;
                    var prevTY = i > 0 ? tY.LastOrDefault(e => e.EndBeat <= beat)?.EndValue ?? 0f : 0f;

                    var fxEvt = fX.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
                    var fyEvt = fY.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
                    var frEvt = fR.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
                    var txEvt = tX.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);
                    var tyEvt = tY.FirstOrDefault(e => e.StartBeat == beat && e.EndBeat == next);

                    var (startAbsX, startAbsY) = FatherUnbindProcessor.GetLinePos(
                        fxEvt?.StartValue ?? prevFX, fyEvt?.StartValue ?? prevFY, frEvt?.StartValue ?? prevFR,
                        txEvt?.StartValue ?? prevTX, tyEvt?.StartValue ?? prevTY);
                    var (endAbsX, endAbsY) = FatherUnbindProcessor.GetLinePos(
                        fxEvt?.EndValue ?? prevFX, fyEvt?.EndValue ?? prevFY, frEvt?.EndValue ?? prevFR,
                        txEvt?.EndValue ?? prevTX, tyEvt?.EndValue ?? prevTY);

                    xBag.Add((i, new Rpe.Event<float>
                    {
                        StartBeat = beat, EndBeat = next, StartValue = (float)startAbsX, EndValue = (float)endAbsX
                    }));
                    yBag.Add((i, new Rpe.Event<float>
                    {
                        StartBeat = beat, EndBeat = next, StartValue = (float)startAbsY, EndValue = (float)endAbsY
                    }));
                });

                return (
                    xBag.OrderBy(x => x.i).Select(x => x.evt).ToList(),
                    yBag.OrderBy(x => x.i).Select(x => x.evt).ToList()
                );
            });


            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbindAsync[{targetJudgeLineIndex}]: 采样完成，写回");
            FatherUnbindProcessor.WriteResultToLine(judgeLineCopy, sortedX, sortedY, fR, tolerance,
                (a, b) => EventProcessor.EventListMerge(a, b, precision, tolerance, compress), compress);


            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            RePhiEditHelper.OnInfo.Invoke($"FatherUnbindAsync[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (NullReferenceException ex)
        {
            RePhiEditHelper.OnError.Invoke(
                $"FatherUnbindAsync[{targetJudgeLineIndex}]: 存在空引用: " + ex);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            RePhiEditHelper.OnError.Invoke(
                $"FatherUnbindAsync[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致。
    /// 策略：自适应采样——以事件边界为强制切割点，仅在误差超过容差时才插入新采样段。
    /// 各通道层级合并并行执行，自适应采样主循环异步卸载至线程池。
    /// </summary>
    internal static async Task<Rpe.JudgeLine> FatherUnbindPlusAsync(int targetJudgeLineIndex,
        List<Rpe.JudgeLine> allJudgeLines, double precision, double tolerance,
        ConcurrentDictionary<int, Rpe.JudgeLine> cache)
    {
        // ── 缓存命中：该线已被解绑过，直接返回副本，避免重复计算 ──
        if (cache.TryGetValue(targetJudgeLineIndex, out var cachedResult))
        {
            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 命中缓存，直接返回已解绑结果");
            return cachedResult.Clone();
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                RePhiEditHelper.OnWarning.Invoke(
                    $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            RePhiEditHelper.OnInfo.Invoke(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 开始解绑（自适应采样），父线索引={judgeLineCopy.Father}");

            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                RePhiEditHelper.OnDebug.Invoke(
                    $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                // 传入共享缓存：若父线已被其他调用解绑过，直接复用缓存结果
                fatherLineCopy = await FatherUnbindPlusAsync(judgeLineCopy.Father, allJudgeLinesCopy,
                    precision, tolerance, cache);
            }

            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            // 各通道按层顺序串行叠加（层间叠加不满足交换律；不同通道间互不依赖，并行执行）
            var mergeResults = await Task.WhenAll(
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    tLayers, l => l.MoveXEvents, (a, b) => EventProcessor.EventMergePlus(a, b))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    tLayers, l => l.MoveYEvents, (a, b) => EventProcessor.EventMergePlus(a, b))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    fLayers, l => l.MoveXEvents, (a, b) => EventProcessor.EventMergePlus(a, b))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    fLayers, l => l.MoveYEvents, (a, b) => EventProcessor.EventMergePlus(a, b))),
                Task.Run(() => FatherUnbindProcessor.MergeLayerChannel(
                    fLayers, l => l.RotateEvents, (a, b) => EventProcessor.EventMergePlus(a, b)))
            );

            var tX = mergeResults[0];
            var tY = mergeResults[1];
            var fX = mergeResults[2];
            var fY = mergeResults[3];
            var fR = mergeResults[4];

            // 采样范围仅由 X/Y 移动事件决定（旋转事件不影响范围边界）
            Beat overallMin = new(0), overallMax = new(0);
            var hasEvents = false;
            foreach (var list in new[] { tX, tY, fX, fY })
            {
                if (list.Count == 0) continue;
                var (mn, mx) = FatherUnbindProcessor.GetEventRange(list);
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

            if (!hasEvents)
            {
                judgeLineCopy.Father = -1;
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            var step = new Beat(1d / precision);

            // 以所有通道的事件边界拍作为强制切割点，防止跳变被忽略
            var keyBeatsList = new List<Beat> { overallMin, overallMax };
            foreach (var list in new[] { tX, tY, fX, fY, fR })
            foreach (var e in list)
            {
                if (e.StartBeat >= overallMin && e.StartBeat <= overallMax) keyBeatsList.Add(e.StartBeat);
                if (e.EndBeat >= overallMin && e.EndBeat <= overallMax) keyBeatsList.Add(e.EndBeat);
            }

            var keyBeats = keyBeatsList.Distinct().OrderBy(b => b).ToList();

            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 自适应采样，关键帧数={keyBeats.Count}，最大精度={precision}");

            // CPU 密集型自适应采样主循环：卸载至线程池，各 key interval 并行处理
            var (resultX, resultY) = await Task.Run(() =>
            {
                // AbsPosIn/AbsPosOut 捕获通道变量，本地定义；
                // GetValIn/GetValOut 使用 FatherUnbindProcessor 中 O(log n) 二分查找静态方法。
                (double X, double Y) AbsPosIn(Beat beat) => FatherUnbindProcessor.GetLinePos(
                    FatherUnbindProcessor.GetValIn(fX, beat), FatherUnbindProcessor.GetValIn(fY, beat),
                    FatherUnbindProcessor.GetValIn(fR, beat),
                    FatherUnbindProcessor.GetValIn(tX, beat), FatherUnbindProcessor.GetValIn(tY, beat));

                (double X, double Y) AbsPosOut(Beat beat) => FatherUnbindProcessor.GetLinePos(
                    FatherUnbindProcessor.GetValOut(fX, beat), FatherUnbindProcessor.GetValOut(fY, beat),
                    FatherUnbindProcessor.GetValOut(fR, beat),
                    FatherUnbindProcessor.GetValOut(tX, beat), FatherUnbindProcessor.GetValOut(tY, beat));

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
                    var iStart = keyBeats[ki];
                    var iEnd = keyBeats[ki + 1];
                    if (iStart >= iEnd) return;

                    var localX = segmentsX[ki];
                    var localY = segmentsY[ki];

                    var (endX, endY) = AbsPosOut(iEnd);
                    var segStart = iStart;
                    var (segX, segY) = AbsPosIn(iStart);

                    for (var cur = iStart; cur < iEnd;)
                    {
                        var next = cur + step;
                        if (next > iEnd) next = iEnd;
                        var isLast = next >= iEnd;

                        var (nextX, nextY) = isLast ? (endX, endY) : AbsPosIn(next);
                        var shouldCut = isLast;

                        if (!isLast)
                        {
                            // 将当前段起点到区间终点做线性预测，误差超出容差时切割
                            var segLen = (double)(iEnd - segStart);
                            var progress = segLen > 1e-12 ? (double)(next - segStart) / segLen : 1.0;
                            var predX = segX + (endX - segX) * progress;
                            var predY = segY + (endY - segY) * progress;
                            var thrX = tolerance / 100.0 * ((Math.Abs(segX) + Math.Abs(nextX)) / 2.0 + 1e-9);
                            var thrY = tolerance / 100.0 * ((Math.Abs(segY) + Math.Abs(nextY)) / 2.0 + 1e-9);
                            shouldCut = Math.Abs(nextX - predX) > thrX || Math.Abs(nextY - predY) > thrY;
                        }

                        if (shouldCut)
                        {
                            localX.Add(new Rpe.Event<float>
                            {
                                StartBeat = segStart, EndBeat = next, StartValue = (float)segX, EndValue = (float)nextX
                            });
                            localY.Add(new Rpe.Event<float>
                            {
                                StartBeat = segStart, EndBeat = next, StartValue = (float)segY, EndValue = (float)nextY
                            });
                            segStart = next;
                            segX = nextX;
                            segY = nextY;
                        }

                        cur = next;
                    }
                });

                // 各段按 key interval 顺序合并，保证事件时序正确
                var resX = new List<Rpe.Event<float>>();
                var resY = new List<Rpe.Event<float>>();
                foreach (var seg in segmentsX) resX.AddRange(seg);
                foreach (var seg in segmentsY) resY.AddRange(seg);
                return (resX, resY);
            });


            // 压缩一下写回
            RePhiEditHelper.OnDebug.Invoke(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 采样完成（生成 {resultX.Count} 段），压缩并写回");
            FatherUnbindProcessor.WriteResultToLine(judgeLineCopy, resultX, resultY, fR, tolerance,
                (a, b) => EventProcessor.EventMergePlus(a, b), compress: true);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            RePhiEditHelper.OnInfo.Invoke($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (NullReferenceException ex)
        {
            RePhiEditHelper.OnError.Invoke(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 存在空引用: " + ex);
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            RePhiEditHelper.OnError.Invoke(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }
}