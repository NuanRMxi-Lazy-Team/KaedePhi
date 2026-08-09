using System.Collections.Concurrent;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;

namespace KaedePhi.Tool.JudgeLines.KaedePhi.Utils;

/// <summary>
/// KPC 判定线父子解绑处理器基类。
/// 封装缓存检查、父线递归解绑准备、通道合并及日志记录等共享逻辑，
/// 具体采样策略由子类（等间隔 / 自适应）实现。
/// </summary>
public abstract class FatherUnbindProcessorBase
{
    protected readonly ConcurrentDictionary<
        FatherUnbindHelpers.UnbindCacheKey,
        JudgeLine
    > Cache;
    protected readonly Action<string>? LogInfo;
    protected readonly Action<string>? LogWarning;
    protected readonly Action<string>? LogError;
    protected readonly Action<string>? LogDebug;

    protected FatherUnbindProcessorBase(
        ConcurrentDictionary<FatherUnbindHelpers.UnbindCacheKey, JudgeLine> cache,
        Action<string>? logInfo = null,
        Action<string>? logWarning = null,
        Action<string>? logError = null,
        Action<string>? logDebug = null
    )
    {
        Cache = cache;
        LogInfo = logInfo;
        LogWarning = logWarning;
        LogError = logError;
        LogDebug = logDebug;
    }

    protected readonly record struct PrepareResult(
        JudgeLine JudgeLine,
        JudgeLine? FatherLine,
        bool ShouldReturn
    );

    protected PrepareResult PrepareUnbindContext(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        string logTag,
        string startAction,
        Func<int, FatherUnbindHelpers.UnbindCacheKey> cacheKeyFactory,
        Func<int, List<JudgeLine>, JudgeLine> recursiveUnbind
    )
    {
        if (
            FatherUnbindHelpers.TryGetCachedClone(
                cacheKeyFactory(targetJudgeLineIndex),
                Cache,
                logTag,
                out var cached,
                LogDebug
            )
        )
        {
            return new PrepareResult(cached, null, true);
        }

        var judgeLineCopy = allJudgeLines[targetJudgeLineIndex].Clone();
        var allJudgeLinesCopy = allJudgeLines.Select(jl => jl.Clone()).ToList();

        if (
            FatherUnbindHelpers.TryReturnWhenNoFather(
                cacheKeyFactory(targetJudgeLineIndex),
                judgeLineCopy,
                Cache,
                logTag,
                LogWarning
            )
        )
        {
            return new PrepareResult(judgeLineCopy, null, true);
        }

        LogInfo?.Invoke(
            $"{logTag}[{targetJudgeLineIndex}]: {startAction}，父线索引={judgeLineCopy.Father}"
        );

        // allJudgeLinesCopy 已经是克隆列表，直接使用其中的元素，无需再次克隆
        var fatherLineCopy = allJudgeLinesCopy[judgeLineCopy.Father];
        if (fatherLineCopy.Father >= 0)
        {
            LogDebug?.Invoke(
                $"{logTag}[{targetJudgeLineIndex}]: 父线 {judgeLineCopy.Father} 仍有父线，递归解绑"
            );
            fatherLineCopy = recursiveUnbind(judgeLineCopy.Father, allJudgeLinesCopy);
        }

        FatherUnbindHelpers.CleanupRedundantLayers(judgeLineCopy, fatherLineCopy);

        return new PrepareResult(judgeLineCopy, fatherLineCopy, false);
    }
}
