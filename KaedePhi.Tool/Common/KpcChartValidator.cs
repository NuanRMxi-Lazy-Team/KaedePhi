using KaedePhi.Core.Common;
using KaedePhi.Core.KaedePhi;

namespace KaedePhi.Tool.Common;

/// <summary>
/// 校验 KPC 谱面的数据结构和处理安全边界。
/// </summary>
public static class KpcChartValidator
{
    private const int RootFatherIndex = -1;
    private const byte Unvisited = 0;
    private const byte Visiting = 1;
    private const byte Visited = 2;
    private static readonly Beat MinimumBpmStartBeat = new(0);

    /// <summary>
    /// 允许处理的最大判定线数量。
    /// </summary>
    public const int MaximumJudgeLines = 10_000;

    /// <summary>
    /// 允许处理的最大事件和音符总数。
    /// </summary>
    public const long MaximumChartItems = 1_000_000_000_000L;

    /// <summary>
    /// 校验 KPC 谱面的 BPM 节点和判定线 BPM 因子。
    /// </summary>
    /// <param name="chart">待校验的 KPC 谱面。</param>
    /// <returns>无返回值。</returns>
    public static void ValidateBpmAndBpmFactors(Chart chart)
    {
        ArgumentNullException.ThrowIfNull(chart);
        ValidateChartCollections(chart);
        ValidateBpmItems(chart.BpmList);
        ValidateBpmFactors(chart.JudgeLineList);
    }

    /// <summary>
    /// 校验父子线索引、父线循环和谱面项目数量。
    /// </summary>
    /// <param name="judgeLines">待检查的判定线列表。</param>
    /// <returns>无返回值。</returns>
    public static void ValidateJudgeLineHierarchy(IReadOnlyList<JudgeLine> judgeLines)
    {
        ArgumentNullException.ThrowIfNull(judgeLines);
        ValidateJudgeLineCount(judgeLines.Count);
        ValidateCloneableJudgeLineStructure(judgeLines);
        ValidateChartItemCount(judgeLines);
        ValidateFatherIndexes(judgeLines);
        ValidateAcyclicHierarchy(judgeLines);
    }

    internal static void ValidateCloneableJudgeLineStructure(IReadOnlyList<JudgeLine> judgeLines)
    {
        foreach (var line in judgeLines)
        {
            if ((object?)line is null || line.EventLayers is null || line.Notes is null)
                throw new FormatException("谱面包含空的判定线或事件集合。");
            if (line.EventLayers.Any(layer => (object?)layer is null))
                throw new FormatException("谱面包含空的事件层。");
        }
    }

    private static void ValidateChartCollections(Chart chart)
    {
        if (chart.BpmList is null)
            throw new FormatException("谱面 BPM 列表不能为 null。");
        if (chart.JudgeLineList is null)
            throw new FormatException("谱面判定线列表不能为 null。");
    }

    private static void ValidateBpmItems(IReadOnlyList<BpmItem> bpmItems)
    {
        for (var index = 0; index < bpmItems.Count; index++)
        {
            var bpm = bpmItems[index];
            if (bpm is null || !float.IsFinite(bpm.Bpm) || bpm.Bpm <= 0)
                throw new FormatException($"谱面 BPM 节点 {index} 必须是有限正数。");
            if (bpm.StartBeat < MinimumBpmStartBeat)
                throw new FormatException($"谱面 BPM 节点 {index} 的起始拍不能小于 0。");
        }
    }

    private static void ValidateBpmFactors(IReadOnlyList<JudgeLine> judgeLines)
    {
        for (var index = 0; index < judgeLines.Count; index++)
        {
            var line = judgeLines[index];
            if (line is null || !float.IsFinite(line.BpmFactor) || line.BpmFactor <= 0)
                throw new FormatException($"判定线 {index} 的 BPM 因子必须是有限正数。");
        }
    }

    private static void ValidateJudgeLineCount(int count)
    {
        if (count > MaximumJudgeLines)
            throw new FormatException("谱面判定线数量超过安全上限。");
    }

    private static void ValidateChartItemCount(IReadOnlyList<JudgeLine> judgeLines)
    {
        long itemCount = 0;
        foreach (var line in judgeLines)
        {
            itemCount += line.Notes.Count;
            foreach (var layer in line.EventLayers)
            {
                itemCount += CountEvents(layer);
                if (itemCount > MaximumChartItems)
                    throw new FormatException("谱面事件和音符数量超过安全上限。");
            }
        }
    }

    private static long CountEvents(KpcEvents.EventLayer layer) =>
        (layer.MoveXEvents?.Count ?? 0L)
        + (layer.MoveYEvents?.Count ?? 0L)
        + (layer.RotateEvents?.Count ?? 0L)
        + (layer.AlphaEvents?.Count ?? 0L)
        + (layer.SpeedEvents?.Count ?? 0L);

    private static void ValidateFatherIndexes(IReadOnlyList<JudgeLine> judgeLines)
    {
        for (var index = 0; index < judgeLines.Count; index++)
        {
            var father = judgeLines[index].Father;
            if (father < RootFatherIndex || father >= judgeLines.Count)
                throw new FormatException($"判定线 {index} 的父线索引 {father} 超出范围。");
        }
    }

    private static void ValidateAcyclicHierarchy(IReadOnlyList<JudgeLine> judgeLines)
    {
        var states = new byte[judgeLines.Count];
        for (var start = 0; start < judgeLines.Count; start++)
            VisitFatherPath(judgeLines, states, start);
    }

    private static void VisitFatherPath(
        IReadOnlyList<JudgeLine> judgeLines,
        byte[] states,
        int start
    )
    {
        var current = start;
        while (current >= 0 && states[current] == Unvisited)
        {
            states[current] = Visiting;
            current = judgeLines[current].Father;
        }

        if (current >= 0 && states[current] == Visiting)
            throw new FormatException($"判定线父子关系包含循环，循环起点为 {current}。");

        current = start;
        while (current >= 0 && states[current] == Visiting)
        {
            states[current] = Visited;
            current = judgeLines[current].Father;
        }
    }
}
