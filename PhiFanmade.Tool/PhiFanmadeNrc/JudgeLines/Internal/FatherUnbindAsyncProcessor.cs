using System.Collections.Concurrent;
using PhiFanmade.Core.Common;
using PhiFanmade.Tool.PhiFanmadeNrc.Events.Internal;
using PhiFanmade.Tool.PhiFanmadeNrc.Layers.Internal;

namespace PhiFanmade.Tool.PhiFanmadeNrc.JudgeLines.Internal;

/// <summary>
/// NRC 判定线父子解绑异步处理器（async/await 版本）。
/// 所有采样算法均委托给 <see cref="FatherUnbindHelpers"/> 中的共享实现，
/// 本类只负责缓存检查、父线递归解绑、通道合并及日志记录。
/// 缓存与同步版 <see cref="FatherUnbindProcessor"/> 共享同一张 <see cref="FatherUnbindHelpers.ChartCacheTable"/>。
/// </summary>
internal static class FatherUnbindAsyncProcessor
{
    // ─── 等间隔采样异步版 ────────────────────────────────────────────────────

    /// <summary>
    /// 等间隔采样解绑（异步版）：将判定线与父线解绑，以等间隔拍步长采样保持原始行为。
    /// <para>
    /// 流程：缓存命中则直接返回 → 递归异步解绑父链 → 并行合并各通道 → 并行切割 → 异步等间隔采样 → 写回。
    /// </para>
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">每拍内的采样步数；越大精度越高，计算量越大。</param>
    /// <param name="tolerance">误差容差百分比，用于事件压缩。</param>
    /// <param name="cache">同一谱面所有调用共享的解绑结果缓存。</param>
    /// <param name="compress">是否在写回前压缩冗余事件。</param>
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

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
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

            // 若父线仍有父线，递归异步解绑父链，确保父线已为绝对坐标
            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                NrcToolLog.OnDebug(
                    $"FatherUnbindAsync[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                fatherLineCopy = await FatherUnbindAsync(
                    judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache, compress);
            }

            // 清理冗余（全零）事件层，减少后续合并的计算量
            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            // 并行合并各通道事件（等间隔版使用 EventListMerge）；各通道独立，可安全并行
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
            // mergeResults 顺序：[0]=tx, [1]=ty, [2]=fx, [3]=fy, [4]=fr

            // 将各通道按精度步长并行切割，为等间隔采样准备
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

            // 构建事件通道并执行等间隔采样（委托给共享算法）
            var ch = new FatherUnbindHelpers.EventChannels(
                Fx: cutResults[2], Fy: cutResults[3], Fr: cutResults[4],
                Tx: cutResults[0], Ty: cutResults[1]);
            var overallMin = new Beat(Math.Min(Math.Min(Math.Min(txMin, tyMin), Math.Min(fxMin, fyMin)), frMin));
            var overallMax = new Beat(Math.Max(Math.Max(Math.Max(txMax, tyMax), Math.Max(fxMax, fyMax)), frMax));
            var step = new Beat(1d / precision);
            var beats = FatherUnbindHelpers.BuildBeatList(overallMin, overallMax, step);

            NrcToolLog.OnDebug($"FatherUnbindAsync[{targetJudgeLineIndex}]: 等间隔采样 {beats.Count} 段，精度={precision}");
            var (sortedX, sortedY) = await Task.Run(
                () => FatherUnbindHelpers.EqualSpacingSampling(beats, overallMax, step, ch));

            NrcToolLog.OnDebug($"FatherUnbindAsync[{targetJudgeLineIndex}]: 采样完成，写回");
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, sortedX, sortedY, ch.Fr, tolerance,
                (a, b) => EventMerger.EventListMerge(a, b, precision, tolerance, compress), compress);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            NrcToolLog.OnInfo($"FatherUnbindAsync[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            NrcToolLog.OnError($"FatherUnbindAsync[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }

    // ─── 自适应采样异步版 ────────────────────────────────────────────────────

    /// <summary>
    /// 自适应采样解绑（异步版）：以事件边界为强制切割点，仅在误差超过容差时插入新采样段，
    /// 相较等间隔版可减少冗余段数。
    /// <para>
    /// 流程：缓存命中则直接返回 → 递归异步解绑父链 → 并行合并各通道 → 收集关键帧 → 异步自适应采样 → 写回。
    /// </para>
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">自适应采样的最大步数上限（同时作为事件合并精度）。</param>
    /// <param name="tolerance">误差容差百分比，决定何时插入额外切割点及压缩阈值。</param>
    /// <param name="cache">同一谱面所有调用共享的解绑结果缓存。</param>
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

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();
        try
        {
            if (judgeLineCopy.Father <= -1)
            {
                NrcToolLog.OnWarning($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 判定线无父线，跳过。");
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            NrcToolLog.OnInfo(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 开始解绑（自适应采样），父线索引={judgeLineCopy.Father}");

            // 若父线仍有父线，递归异步解绑父链，确保父线已为绝对坐标
            var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father].Clone();
            if (fatherLineCopy.Father >= 0)
            {
                NrcToolLog.OnDebug(
                    $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑");
                fatherLineCopy = await FatherUnbindPlusAsync(
                    judgeLineCopy.Father, allJudgeLinesCopy, precision, tolerance, cache);
            }

            // 清理冗余（全零）事件层，减少后续合并的计算量
            judgeLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(judgeLineCopy.EventLayers) ?? judgeLineCopy.EventLayers;
            fatherLineCopy.EventLayers =
                LayerProcessor.RemoveUnlessLayer(fatherLineCopy.EventLayers) ?? fatherLineCopy.EventLayers;

            var tLayers = judgeLineCopy.EventLayers;
            var fLayers = fatherLineCopy.EventLayers;

            // 并行合并各通道事件（自适应版使用 EventMergePlus）；各通道独立，可安全并行
            var mergeResults = await Task.WhenAll(
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveXEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(tLayers, l => l.MoveYEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveXEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.MoveYEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance))),
                Task.Run(() => FatherUnbindHelpers.MergeLayerChannel(fLayers, l => l.RotateEvents,
                    (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance)))
            );
            // mergeResults 顺序：[0]=tx, [1]=ty, [2]=fx, [3]=fy, [4]=fr

            // 构建事件通道，确定总体拍范围（委托给共享算法）
            var ch = new FatherUnbindHelpers.EventChannels(
                Fx: mergeResults[2], Fy: mergeResults[3], Fr: mergeResults[4],
                Tx: mergeResults[0], Ty: mergeResults[1]);

            var rangeResult = FatherUnbindHelpers.TryGetOverallRange(ch);
            if (rangeResult is null)
            {
                // 所有通道均为空，无需采样，直接解绑
                judgeLineCopy.Father = -1;
                cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
                return judgeLineCopy;
            }

            var (overallMin, overallMax) = rangeResult.Value;
            var step = new Beat(1d / precision);
            var keyBeats = FatherUnbindHelpers.CollectKeyBeats(overallMin, overallMax, ch);

            NrcToolLog.OnDebug(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 自适应采样，关键帧数={keyBeats.Count}，最大精度={precision}");
            var (resultX, resultY) = await Task.Run(
                () => FatherUnbindHelpers.RunAdaptiveSampling(keyBeats, step, tolerance, ch));

            NrcToolLog.OnDebug(
                $"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 采样完成（生成 {resultX.Count} 段），压缩并写回");
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, resultX, resultY, ch.Fr, tolerance,
                (a, b) => EventMerger.EventMergePlus(a, b, precision, tolerance), compress: true);

            cache.TryAdd(targetJudgeLineIndex, judgeLineCopy);
            NrcToolLog.OnInfo($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 解绑完成");
            return judgeLineCopy;
        }
        catch (Exception ex)
        {
            NrcToolLog.OnError($"FatherUnbindAsyncPlus[{targetJudgeLineIndex}]: 未知错误: " + ex.Message);
            return judgeLineCopy;
        }
    }
}