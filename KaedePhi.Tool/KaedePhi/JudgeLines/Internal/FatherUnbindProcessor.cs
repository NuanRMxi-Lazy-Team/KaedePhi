using System.Collections.Concurrent;
using KaedePhi.Tool.KaedePhi;
using NewProcessor = KaedePhi.Tool.JudgeLines.KaedePhi.Utils.FatherUnbindProcessor;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;

namespace KaedePhi.Tool.KaedePhi.JudgeLines.Internal;

/// <summary>
/// NRC 判定线父子解绑同步处理器。
/// </summary>
[Obsolete("请使用 KaedePhi.Tool.JudgeLines.KaedePhi.Internal.FatherUnbindProcessor")]
internal static class FatherUnbindProcessor
{
    /// <summary>
    /// 等间隔采样解绑（同步版）：将判定线与父线解绑，以等间隔拍步长采样保持原始行为。
    /// </summary>
    [Obsolete("请使用 KaedePhi.Tool.JudgeLines.KaedePhi.Internal.FatherUnbindProcessor.FatherUnbind")]
    internal static JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        double precision,
        ConcurrentDictionary<int, JudgeLine> cache)
        => NewProcessor.FatherUnbind(targetJudgeLineIndex, allJudgeLines, precision, cache,
            KpcToolLog.OnInfo, KpcToolLog.OnWarning, KpcToolLog.OnError, KpcToolLog.OnDebug);

    /// <summary>
    /// 自适应采样解绑（同步版）：以事件边界为强制切割点，仅在误差超过容差时插入新采样段，
    /// 相较等间隔版可减少冗余段数。
    /// </summary>
    [Obsolete("请使用 KaedePhi.Tool.JudgeLines.KaedePhi.Internal.FatherUnbindProcessor.FatherUnbindPlus")]
    internal static JudgeLine FatherUnbindPlus(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        double precision, double tolerance,
        ConcurrentDictionary<int, JudgeLine> cache)
        => NewProcessor.FatherUnbindPlus(targetJudgeLineIndex, allJudgeLines, precision, tolerance, cache,
            KpcToolLog.OnInfo, KpcToolLog.OnWarning, KpcToolLog.OnError, KpcToolLog.OnDebug);
}
