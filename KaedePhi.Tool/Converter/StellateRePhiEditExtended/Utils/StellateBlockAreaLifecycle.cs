using KaedePhi.Core.Primitives;
using RpeChart = KaedePhi.Core.Formats.RePhiEdit.Model.Chart;
using RpeEventLayer = KaedePhi.Core.Formats.RePhiEdit.Model.Events.EventLayer;
using RpeJudgeLine = KaedePhi.Core.Formats.RePhiEdit.Model.JudgeLine;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;

/// <summary>
/// 提供噪域标记识别、生命周期生成和谱面结束拍计算功能。
/// </summary>
public static class StellateBlockAreaLifecycle
{
    /// <summary>
    /// 从判定线中提取噪域生命周期标记。
    /// </summary>
    /// <param name="line">包含生命周期标记的判定线</param>
    /// <param name="cancellationToken">用于取消处理的令牌</param>
    public static List<(int Code, Beat Beat, int Order)> GetMarkers(
        RpeJudgeLine line,
        CancellationToken cancellationToken
    )
    {
        var result = new List<(int Code, Beat Beat, int Order)>();
        var order = 0;
        foreach (var layer in line.EventLayers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (layer?.SpeedEvents is not { Count: > 0 } events)
                continue;
            foreach (var evt in events)
            {
                if (!float.IsFinite(evt.StartValue))
                    continue;
                var code = (int)Math.Round(evt.StartValue);
                if (code is >= 1 and <= 4 && Math.Abs(evt.StartValue - code) <= 1e-6)
                    result.Add((code, evt.StartBeat, order++));
            }
        }

        return
        [
            .. result
                .OrderBy(marker => marker.Beat)
                .ThenBy(marker => marker.Order),
        ];
    }

    /// <summary>
    /// 根据有序标记生成噪域显示阶段。
    /// </summary>
    /// <param name="markers">按拍数排列的生命周期标记</param>
    /// <param name="chartEndBeat">谱面结束拍</param>
    /// <param name="cancellationToken">用于取消处理的令牌</param>
    public static List<(Beat AppearBeat, Beat EnableBeat, Beat DisableBeat, Beat DisappearBeat)> BuildLifeCycles(
        List<(int Code, Beat Beat, int Order)> markers,
        Beat chartEndBeat,
        CancellationToken cancellationToken
    )
    {
        if (markers.Count == 0)
            return [];

        var groups = new List<List<(int Code, Beat Beat, int Order)>> { new() };
        foreach (var marker in markers)
        {
            var currentGroup = groups[^1];
            if (currentGroup.Count > 0 && marker.Code <= currentGroup[^1].Code)
                groups.Add([]);
            groups[^1].Add(marker);
        }

        var result = new List<(Beat, Beat, Beat, Beat)>(groups.Count);
        for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var group = groups[groupIndex];
            var groupEndBeat = groupIndex + 1 < groups.Count
                ? groups[groupIndex + 1][0].Beat
                : chartEndBeat;
            if (groupEndBeat < group[^1].Beat)
                groupEndBeat = group[^1].Beat;

            var phaseBeats = new Beat[4];
            for (var phase = 1; phase <= 4; phase++)
            {
                var markerIndex = 0;
                while (markerIndex < group.Count && group[markerIndex].Code < phase)
                    markerIndex++;
                phaseBeats[phase - 1] = markerIndex < group.Count
                    ? group[markerIndex].Beat
                    : groupEndBeat;
            }

            result.Add((phaseBeats[0], phaseBeats[1], phaseBeats[2], phaseBeats[3]));
        }

        return result;
    }

    /// <summary>
    /// 获取谱面中事件和音符的最晚结束拍。
    /// </summary>
    /// <param name="source">源 RePhiEdit 谱面</param>
    public static Beat CalculateChartEndBeat(RpeChart source)
    {
        var endBeat = new Beat(0);
        foreach (var line in source.JudgeLineList)
        {
            foreach (var layer in line.EventLayers.OfType<RpeEventLayer>())
            {
                UpdateEndBeat(layer.MoveXEvents, ref endBeat);
                UpdateEndBeat(layer.MoveYEvents, ref endBeat);
                UpdateEndBeat(layer.RotateEvents, ref endBeat);
                UpdateEndBeat(layer.AlphaEvents, ref endBeat);
                UpdateEndBeat(layer.SpeedEvents, ref endBeat);
            }

            if (line.Extended is { } extended)
            {
                UpdateEndBeat(extended.ColorEvents, ref endBeat);
                UpdateEndBeat(extended.ScaleXEvents, ref endBeat);
                UpdateEndBeat(extended.ScaleYEvents, ref endBeat);
                UpdateEndBeat(extended.TextEvents, ref endBeat);
                UpdateEndBeat(extended.PaintEvents, ref endBeat);
                UpdateEndBeat(extended.GifEvents, ref endBeat);
                UpdateEndBeat(extended.InclineEvents, ref endBeat);
            }

            foreach (var note in line.Notes ?? [])
            {
                if (note.StartBeat > endBeat)
                    endBeat = note.StartBeat;
                if (note.EndBeat > endBeat)
                    endBeat = note.EndBeat;
            }
        }

        return endBeat;
    }

    private static void UpdateEndBeat<T>(
        List<RpeEvents.Event<T>>? events,
        ref Beat endBeat
    )
        where T : notnull
    {
        if (events is null)
            return;
        foreach (var evt in events)
        {
            if (evt.StartBeat > endBeat)
                endBeat = evt.StartBeat;
            if (evt.EndBeat > endBeat)
                endBeat = evt.EndBeat;
        }
    }
}
