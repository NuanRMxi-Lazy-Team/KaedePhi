using System.Collections.Concurrent;
using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Event.KaedePhi;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;

namespace KaedePhi.Tool.JudgeLines.KaedePhi.Utils;

/// <summary>
/// KPC 判定线父子解绑处理器（自适应采样）。
/// 以事件边界为强制切割点，仅在误差超过容差时插入新采样段，
/// 相较 <see cref="FatherUnbindProcessor"/> 可减少冗余段数。
/// </summary>
public class FatherUnbindPlusProcessor : FatherUnbindProcessorBase
{
    private readonly double _tolerance;
    private readonly double _mergeTolerance;
    private readonly EventListMergerPlus<double> _merger = new();

    [Obsolete("请改用包含 mergeTolerance 参数的构造函数。")]
    public FatherUnbindPlusProcessor(
        ConcurrentDictionary<FatherUnbindHelpers.UnbindCacheKey, JudgeLine> cache,
        double tolerance,
        Action<string>? logInfo = null,
        Action<string>? logWarning = null,
        Action<string>? logError = null,
        Action<string>? logDebug = null
    )
        : this(
            cache,
            tolerance,
            tolerance,
            logInfo,
            logWarning,
            logError,
            logDebug
        ) { }

    public FatherUnbindPlusProcessor(
        ConcurrentDictionary<FatherUnbindHelpers.UnbindCacheKey, JudgeLine> cache,
        double tolerance,
        double mergeTolerance,
        Action<string>? logInfo = null,
        Action<string>? logWarning = null,
        Action<string>? logError = null,
        Action<string>? logDebug = null
    )
        : base(cache, logInfo, logWarning, logError, logDebug)
    {
        _tolerance = tolerance;
        _mergeTolerance = mergeTolerance;
    }

    /// <summary>
    /// 自适应采样解绑：将判定线与父线解绑，以事件边界为强制切割点，
    /// 仅在误差超过容差时插入新采样段。相较 <see cref="FatherUnbindProcessor"/> 可减少冗余段数。
    /// </summary>
    public JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        double precision,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        ChartProcessingValidator.ValidatePrecision(precision);
        ChartProcessingValidator.ValidateTolerance(_tolerance);
        ChartProcessingValidator.ValidateTolerance(_mergeTolerance);
        ct.ThrowIfCancellationRequested();
        JudgeLine judgeLineCopy;
        try
        {
            progress?.Report(new ToolProgress(0.1, "准备解绑上下文"));

            var (judgeLine, fatherLine, shouldReturn) = PrepareUnbindContext(
                targetJudgeLineIndex,
                allJudgeLines,
                logTag: "FatherUnbindPlus",
                startAction: "开始解绑（自适应采样）",
                cacheKeyFactory: index =>
                    new FatherUnbindHelpers.UnbindCacheKey(
                        index,
                        precision,
                        _tolerance,
                        _mergeTolerance,
                        true,
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
            var ch = FatherUnbindHelpers.MergeChannels(
                judgeLineCopy.EventLayers,
                fatherLine.EventLayers,
                Merge
            );

            var rangeResult = FatherUnbindHelpers.TryGetOverallRange(ch);
            if (rangeResult is null)
            {
                judgeLineCopy.Father = -1;
                Cache.TryAdd(
                    new FatherUnbindHelpers.UnbindCacheKey(
                        targetJudgeLineIndex,
                        precision,
                        _tolerance,
                        _mergeTolerance,
                        true,
                        FatherUnbindHelpers.CurrentRenderProfile
                    ),
                    judgeLineCopy
                );
                progress?.Report(new ToolProgress(1.0));
                return judgeLineCopy;
            }

            var (overallMin, overallMax) = rangeResult.Value;
            var step = new Beat(1d / precision);

            progress?.Report(new ToolProgress(0.4, "收集关键帧"));
            var keyBeats = FatherUnbindHelpers.CollectKeyBeats(overallMin, overallMax, ch);

            progress?.Report(new ToolProgress(0.6, "自适应采样"));
            LogDebug?.Invoke(
                $"FatherUnbindPlus[{targetJudgeLineIndex}]: 自适应采样，关键帧数={keyBeats.Count}，最大精度={precision}"
            );
            var (resultX, resultY) = FatherUnbindHelpers.RunAdaptiveSampling(
                keyBeats,
                step,
                _tolerance,
                ch,
                ct
            );

            progress?.Report(new ToolProgress(0.9, "写回结果"));
            LogDebug?.Invoke(
                $"FatherUnbindPlus[{targetJudgeLineIndex}]: 采样完成（生成 {resultX.Count} 段），写回结果"
            );
            FatherUnbindHelpers.WriteResultToLine(judgeLineCopy, resultX, resultY, ch.Fr, Merge);

            Cache.TryAdd(
                new FatherUnbindHelpers.UnbindCacheKey(
                    targetJudgeLineIndex,
                    precision,
                    _tolerance,
                    _mergeTolerance,
                    true,
                    FatherUnbindHelpers.CurrentRenderProfile
                ),
                judgeLineCopy
            );
            LogInfo?.Invoke($"FatherUnbindPlus[{targetJudgeLineIndex}]: 解绑完成");
            progress?.Report(new ToolProgress(1.0));
            return judgeLineCopy;

            List<KpcEvents.Event<double>> Merge(
                List<KpcEvents.Event<double>> a,
                List<KpcEvents.Event<double>> b
            ) => _merger.EventListMerge(a, b, precision, _mergeTolerance);
        }
        catch (Exception ex)
        {
            LogError?.Invoke($"FatherUnbindPlus[{targetJudgeLineIndex}]: 解绑失败: " + ex.Message);
            throw;
        }
    }
}
