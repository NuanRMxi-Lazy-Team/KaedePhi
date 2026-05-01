using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.KaedePhi;
using NewHelpers = KaedePhi.Tool.JudgeLines.KaedePhi.Utils.FatherUnbindHelpers;
using EventLayer = KaedePhi.Core.KaedePhi.EventLayer;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;

namespace KaedePhi.Tool.KaedePhi.JudgeLines.Internal;

/// <summary>
/// Kpc 父子解绑共用辅助方法：缓存表、坐标计算、通道合并、范围统计、采样算法、结果写回。
/// </summary>
[Obsolete("请使用 KaedePhi.Tool.JudgeLines.KaedePhi.Internal.FatherUnbindHelpers")]
internal static class FatherUnbindHelpers
{
    internal static CoordinateProfile CurrentRenderProfile
        => NewHelpers.CurrentRenderProfile;

    internal static IDisposable UseRenderProfile(CoordinateProfile renderProfile)
        => NewHelpers.UseRenderProfile(renderProfile);

    /// <summary>
    /// 以 allJudgeLines 实例为 key 自动隔离缓存：
    /// 同一谱面的所有解绑调用共享同一份缓存，allJudgeLines 被 GC 后自动释放。
    /// </summary>
    internal static readonly ConditionalWeakTable<List<JudgeLine>, ConcurrentDictionary<int, JudgeLine>>
        ChartCacheTable = NewHelpers.ChartCacheTable;

    /// <summary>
    /// 根据父线绝对坐标和旋转角度，计算子线的绝对坐标。
    /// </summary>
    internal static (double X, double Y) GetLinePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
        => NewHelpers.GetLinePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY);

    /// <summary>
    /// Kpc 虽然以归一化坐标存储，但几何误差必须在当前渲染坐标系评估，
    /// 否则 X/Y 轴缩放不一致会导致切段阈值偏斜。
    /// </summary>
    internal static bool NeedsAdaptiveCut(
        (double X, double Y) segmentStart,
        (double X, double Y) next,
        (double X, double Y) intervalEnd,
        Beat segmentStartBeat,
        Beat intervalEndBeat,
        Beat nextBeat,
        double tolerance)
        => NewHelpers.NeedsAdaptiveCut(segmentStart, next, intervalEnd, segmentStartBeat, intervalEndBeat, nextBeat, tolerance);

    /// <summary>
    /// 传入语义：取 beat 时刻正在生效的事件插值（用于段起点）。O(log n) 二分查找。
    /// </summary>
    internal static double GetValIn(List<Kpc.Event<double>> events, Beat beat)
        => NewHelpers.GetValIn(events, beat);

    /// <summary>
    /// 传出语义：取 beat 时刻即将结束的事件插值（用于段终点）。O(log n) 二分查找。
    /// </summary>
    internal static double GetValOut(List<Kpc.Event<double>> events, Beat beat)
        => NewHelpers.GetValOut(events, beat);

    /// <summary>
    /// 按层顺序将某一类型的事件列表串行叠加合并。层间叠加不满足交换律，必须顺序处理。
    /// </summary>
    internal static List<Kpc.Event<double>> MergeLayerChannel(
        List<EventLayer> layers,
        Func<EventLayer, List<Kpc.Event<double>>?> selector,
        Func<List<Kpc.Event<double>>, List<Kpc.Event<double>>, List<Kpc.Event<double>>> merge)
        => NewHelpers.MergeLayerChannel(layers, selector, merge);

    /// <summary>
    /// 命中缓存时返回克隆结果，避免调用方直接持有缓存实例。
    /// </summary>
    internal static bool TryGetCachedClone(
        int targetJudgeLineIndex,
        ConcurrentDictionary<int, JudgeLine> cache,
        string logTag,
        out JudgeLine cachedClone)
        => NewHelpers.TryGetCachedClone(targetJudgeLineIndex, cache, logTag, out cachedClone, KpcToolLog.OnDebug);

    /// <summary>
    /// 判定线无父线时直接缓存并返回，统一同步/异步处理器的短路分支。
    /// </summary>
    internal static bool TryReturnWhenNoFather(
        int targetJudgeLineIndex,
        JudgeLine judgeLineCopy,
        ConcurrentDictionary<int, JudgeLine> cache,
        string logTag)
        => NewHelpers.TryReturnWhenNoFather(targetJudgeLineIndex, judgeLineCopy, cache, logTag, KpcToolLog.OnWarning);

    /// <summary>
    /// 清理判定线与父线的全零事件层，减少后续通道合并计算量。
    /// </summary>
    internal static void CleanupRedundantLayers(JudgeLine judgeLineCopy, JudgeLine fatherLineCopy)
        => NewHelpers.CleanupRedundantLayers(judgeLineCopy, fatherLineCopy);

    /// <summary>获取事件列表的拍范围（最小 StartBeat，最大 EndBeat）。列表为空时返回 (0, 0)。</summary>
    internal static (Beat Min, Beat Max) GetEventRange(List<Kpc.Event<double>> events)
        => NewHelpers.GetEventRange(events);

    /// <summary>
    /// 将计算结果写回判定线：清除第 1 层及以上的 X/Y 事件，将压缩后的结果写入第 0 层。
    /// RotateWithFather 为 true 时叠加父线旋转事件；最后置 Father = -1 完成解绑。
    /// </summary>
    internal static void WriteResultToLine(
        JudgeLine line,
        List<Kpc.Event<double>> newXEvents,
        List<Kpc.Event<double>> newYEvents,
        List<Kpc.Event<double>> fatherRotateEvents,
        Func<List<Kpc.Event<double>>, List<Kpc.Event<double>>, List<Kpc.Event<double>>> merge)
        => NewHelpers.WriteResultToLine(line, newXEvents, newYEvents, fatherRotateEvents, merge);

    /// <summary>
    /// 合并父子线五个通道事件，统一 EventChannels 拼装顺序。
    /// </summary>
    internal static NewHelpers.EventChannels MergeChannels(
        List<EventLayer> targetLayers,
        List<EventLayer> fatherLayers,
        Func<List<Kpc.Event<double>>, List<Kpc.Event<double>>, List<Kpc.Event<double>>> merge)
        => NewHelpers.MergeChannels(targetLayers, fatherLayers, merge);

    /// <summary>
    /// 生成从 <paramref name="min"/> 到 <paramref name="max"/>（不含）以 <paramref name="step"/> 为步长的拍列表。
    /// </summary>
    internal static List<Beat> BuildBeatList(Beat min, Beat max, Beat step)
        => NewHelpers.BuildBeatList(min, max, step);

    /// <summary>
    /// 并行等间隔采样：对 <paramref name="beats"/> 中每一段计算绝对坐标，返回按顺序排列的 X/Y 事件列表。
    /// </summary>
    internal static (List<Kpc.Event<double>> x, List<Kpc.Event<double>> y) EqualSpacingSampling(
        List<Beat> beats, Beat max, Beat step, NewHelpers.EventChannels ch)
        => NewHelpers.EqualSpacingSampling(beats, max, step, ch);

    /// <summary>
    /// 计算单个采样段 [<paramref name="beat"/>, <paramref name="next"/>] 的 X/Y 绝对坐标事件。
    /// 段起点取 GetValIn（正在生效的插值），段终点取 GetValOut（即将结束的插值）。
    /// </summary>
    internal static (Kpc.Event<double> x, Kpc.Event<double> y) ComputeBeatSegment(
        Beat beat, Beat next, NewHelpers.EventChannels ch)
        => NewHelpers.ComputeBeatSegment(beat, next, ch);

    /// <summary>
    /// 尝试计算五个通道的总体拍范围。若所有通道均为空则返回 <see langword="null"/>。
    /// </summary>
    internal static (Beat min, Beat max)? TryGetOverallRange(NewHelpers.EventChannels ch)
        => NewHelpers.TryGetOverallRange(ch);

    /// <summary>
    /// 收集所有通道事件的起止拍作为关键帧，在 [<paramref name="overallMin"/>, <paramref name="overallMax"/>]
    /// 范围内去重排序后返回。关键帧是自适应采样的强制切割点。
    /// </summary>
    internal static List<Beat> CollectKeyBeats(Beat overallMin, Beat overallMax, NewHelpers.EventChannels ch)
        => NewHelpers.CollectKeyBeats(overallMin, overallMax, ch);

    /// <summary>
    /// 并行自适应采样：对 <paramref name="keyBeats"/> 中的每个区间调用自适应采样，汇总后返回 X/Y 事件列表。
    /// </summary>
    internal static (List<Kpc.Event<double>> x, List<Kpc.Event<double>> y) RunAdaptiveSampling(
        List<Beat> keyBeats, Beat step, double tolerance, NewHelpers.EventChannels ch)
        => NewHelpers.RunAdaptiveSampling(keyBeats, step, tolerance, ch);
}
