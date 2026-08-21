using System.Collections.Concurrent;
using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Event.KaedePhi;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;

namespace KaedePhi.Tool.JudgeLines.KaedePhi.Utils;

/// <summary>
/// KPC 判定线父子解绑处理器（等间隔采样）。
/// 将判定线与父线解绑，以等间隔拍步长采样保持原始行为。
/// </summary>
public class FatherUnbindProcessor : FatherUnbindProcessorBase
{
    private readonly EventListMerger<double> _merger = new();
    private readonly EventCutter<double> _cutter = new();

    public FatherUnbindProcessor(
        ConcurrentDictionary<FatherUnbindHelpers.UnbindCacheKey, JudgeLine> cache,
        Action<string>? logInfo = null,
        Action<string>? logWarning = null,
        Action<string>? logError = null,
        Action<string>? logDebug = null
    )
        : base(cache, logInfo, logWarning, logError, logDebug) { }

    /// <summary>
    /// 等间隔采样解绑（同步版）：将判定线与父线解绑，以等间隔拍步长采样保持原始行为。
    /// </summary>
    public JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        double precision,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        NumericParameterValidator.ValidatePrecision(precision);
        ct.ThrowIfCancellationRequested();
        JudgeLine judgeLineCopy;
        try
        {
            progress?.Report(new ToolProgress(0.1, "准备解绑上下文"));

            var (judgeLine, fatherLine, shouldReturn) = PrepareUnbindContext(
                targetJudgeLineIndex,
                allJudgeLines,
                logTag: "FatherUnbind",
                startAction: "开始解绑",
                cacheKeyFactory: index => new FatherUnbindHelpers.UnbindCacheKey(
                    index,
                    precision,
                    0d,
                    0d,
                    false,
                    FatherUnbindHelpers.CurrentRenderProfile
                ),
                recursiveUnbind: (idx, lines) => FatherUnbind(idx, lines, precision, progress, ct)
            );

            judgeLineCopy = judgeLine;
            if (shouldReturn || fatherLine is null)
            {
                progress?.Report(new ToolProgress(1.0));
                return judgeLineCopy;
            }

            progress?.Report(new ToolProgress(0.2, "合并通道"));
            var mergedChannels = FatherUnbindHelpers.MergeChannels(
                judgeLineCopy.EventLayers,
                fatherLine.EventLayers,
                Merge
            );

            progress?.Report(new ToolProgress(0.4, "切割事件"));
            var cutLength = new Beat(1d / precision);
            var (txMin, txMax) = FatherUnbindHelpers.GetEventRange(mergedChannels.Tx);
            var (tyMin, tyMax) = FatherUnbindHelpers.GetEventRange(mergedChannels.Ty);
            var (fxMin, fxMax) = FatherUnbindHelpers.GetEventRange(mergedChannels.Fx);
            var (fyMin, fyMax) = FatherUnbindHelpers.GetEventRange(mergedChannels.Fy);
            var (frMin, frMax) = FatherUnbindHelpers.GetEventRange(mergedChannels.Fr);

            var cutTasks = new[]
            {
                Task.Run(() =>
                    _cutter.CutEventsInRange(mergedChannels.Tx, txMin, txMax, cutLength)
                ),
                Task.Run(() =>
                    _cutter.CutEventsInRange(mergedChannels.Ty, tyMin, tyMax, cutLength)
                ),
                Task.Run(() =>
                    _cutter.CutEventsInRange(mergedChannels.Fx, fxMin, fxMax, cutLength)
                ),
                Task.Run(() =>
                    _cutter.CutEventsInRange(mergedChannels.Fy, fyMin, fyMax, cutLength)
                ),
                Task.Run(() =>
                    _cutter.CutEventsInRange(mergedChannels.Fr, frMin, frMax, cutLength)
                ),
            };
            // ReSharper disable once CoVariantArrayConversion
            Task.WaitAll(cutTasks);

            ct.ThrowIfCancellationRequested();
            var cutChannels = new FatherUnbindHelpers.EventChannels(
                Fx: cutTasks[2].Result,
                Fy: cutTasks[3].Result,
                Fr: cutTasks[4].Result,
                Tx: cutTasks[0].Result,
                Ty: cutTasks[1].Result
            );

            var overallMin = new Beat(
                Math.Min(Math.Min(Math.Min(txMin, tyMin), Math.Min(fxMin, fyMin)), frMin)
            );
            var overallMax = new Beat(
                Math.Max(Math.Max(Math.Max(txMax, tyMax), Math.Max(fxMax, fyMax)), frMax)
            );
            var step = new Beat(1d / precision);
            var beats = FatherUnbindHelpers.BuildBeatList(overallMin, overallMax, step, ct);

            progress?.Report(new ToolProgress(0.6, "等间隔采样"));
            LogDebug?.Invoke(
                $"FatherUnbind[{targetJudgeLineIndex}]: 等间隔采样 {beats.Count} 段，精度={precision}"
            );
            var (sortedX, sortedY) = FatherUnbindHelpers.EqualSpacingSampling(
                beats,
                overallMax,
                step,
                cutChannels,
                ct
            );

            progress?.Report(new ToolProgress(0.9, "写回结果"));
            LogDebug?.Invoke($"FatherUnbind[{targetJudgeLineIndex}]: 采样完成，写回");
            FatherUnbindHelpers.WriteResultToLine(
                judgeLineCopy,
                sortedX,
                sortedY,
                cutChannels.Fr,
                Merge
            );

            Cache.TryAdd(
                new FatherUnbindHelpers.UnbindCacheKey(
                    targetJudgeLineIndex,
                    precision,
                    0d,
                    0d,
                    false,
                    FatherUnbindHelpers.CurrentRenderProfile
                ),
                judgeLineCopy
            );
            LogInfo?.Invoke($"FatherUnbind[{targetJudgeLineIndex}]: 解绑完成");
            progress?.Report(new ToolProgress(1.0));
            return judgeLineCopy;

            List<KpcEvents.Event<double>> Merge(
                List<KpcEvents.Event<double>> a,
                List<KpcEvents.Event<double>> b
            ) => _merger.EventListMerge(a, b, precision);
        }
        catch (Exception ex)
        {
            LogError?.Invoke($"FatherUnbind[{targetJudgeLineIndex}]: 解绑失败: " + ex.Message);
            throw;
        }
    }
}
