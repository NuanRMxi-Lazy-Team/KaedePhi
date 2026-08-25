using KaedePhi.Core.KaedePhi;

namespace KaedePhi.Tool.Common;

/// <summary>
/// 校验 KPC 谱面的 BPM 数据与父子线索引。
/// </summary>
public static class KpcChartValidator
{
    private const int RootFatherIndex = -1;
    private const byte Unvisited = 0;
    private const byte Visiting = 1;
    private const byte Visited = 2;

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
    /// 校验父子线索引与父线环路。
    /// </summary>
    /// <param name="judgeLines">待检查的判定线列表。</param>
    /// <returns>无返回值。</returns>
    public static void ValidateJudgeLineHierarchy(IReadOnlyList<JudgeLine> judgeLines)
    {
        ArgumentNullException.ThrowIfNull(judgeLines);
        ValidateFatherIndexes(judgeLines);
        ValidateAcyclicHierarchy(judgeLines);
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
