using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using KaedePhi.Tool.Converter.RePhiEdit;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using IrBlockArea = KaedePhi.Core.Intermediate.Model.BlockArea;
using IrChart = KaedePhi.Core.Intermediate.Model.Chart;
using RpeChart = KaedePhi.Core.Formats.RePhiEdit.Model.Chart;
using RpeJudgeLine = KaedePhi.Core.Formats.RePhiEdit.Model.JudgeLine;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended;

internal sealed class StellateRePhiEditExtendedConverter : LoggableBase, ICancellableChartConverter
{
    private CancellationToken _ct;

    public void SetCancellationToken(CancellationToken ct) => _ct = ct;

    internal IrChart ToIr(RpeChart source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _ct.ThrowIfCancellationRequested();

        var sourceLines = source.JudgeLineList;
        var blockAreaLines = sourceLines
            .Select((line, index) => (Line: line, Index: index))
            .Where(item => StellateRePhiEditExtendedTexture.IsBlockAreaTexture(item.Line.Texture))
            .ToList();
        var isBlockAreaLine = new bool[sourceLines.Count];
        foreach (var item in blockAreaLines)
            isBlockAreaLine[item.Index] = true;

        var baseSource = new RpeChart
        {
            BpmList = source.BpmList,
            Meta = source.Meta,
            JudgeLineList = sourceLines
                .Select((line, index) =>
                    isBlockAreaLine[index] ? new RpeJudgeLine { Father = line.Father } : line
                )
                .ToList(),
        };
        var baseConverter = new RePhiEditConverter
        {
            OnInfo = OnInfo,
            OnWarning = OnWarning,
            OnError = OnError,
            OnDebug = OnDebug,
        };
        baseConverter.SetCancellationToken(_ct);
        var converted = baseConverter.ToIr(baseSource, null);
        var convertedLines = converted.JudgeLineList;

        var beatMapper = CreateBeatMapper(converted);
        var geometryConverter = new StellateRePhiEditGeometry(source, beatMapper, _ct);
        var blockAreas = new List<IrBlockArea>(blockAreaLines.Count);
        foreach (var item in blockAreaLines)
        {
            _ct.ThrowIfCancellationRequested();
            if (item.Line.Notes is { Count: > 0 })
                LogWarning(
                    $"StellateRePhiEditExtended 判定线 {item.Index} 的音符因转换为噪域而被忽略。"
                );

            blockAreas.AddRange(geometryConverter.ConvertLine(item.Index));
        }

        var oldToNewIndex = new int[sourceLines.Count];
        Array.Fill(oldToNewIndex, -1);
        var retainedLines = new List<Ir.JudgeLine>(sourceLines.Count - blockAreaLines.Count);
        var retainedSourceIndices = new List<int>(retainedLines.Capacity);
        for (var index = 0; index < convertedLines.Count; index++)
        {
            _ct.ThrowIfCancellationRequested();
            if (isBlockAreaLine[index])
                continue;
            oldToNewIndex[index] = retainedLines.Count;
            retainedSourceIndices.Add(index);
            retainedLines.Add(convertedLines[index]);
        }

        for (var index = 0; index < retainedLines.Count; index++)
        {
            _ct.ThrowIfCancellationRequested();
            var sourceIndex = retainedSourceIndices[index];
            retainedLines[index].Father = MapFather(
                sourceLines,
                isBlockAreaLine,
                oldToNewIndex,
                sourceIndex,
                out var skippedBlockAreaParent
            );
            if (skippedBlockAreaParent)
                LogWarning(
                    $"StellateRePhiEditExtended 判定线 {sourceIndex} 的父级已转换为噪域，其父子变换无法保留。"
                );
        }

        return IrChartNormalizer.NormalizeAndValidateNoteEndBeats(
            new IrChart
            {
                BpmList = converted.BpmList,
                Meta = converted.Meta,
                JudgeLineList = retainedLines,
                BlockAreaList = [.. converted.BlockAreaList, .. blockAreas],
            }
        );
    }

    internal RpeChart FromIr(IrChart source, ConvertOption options)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);
        _ct.ThrowIfCancellationRequested();

        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(source);
        var baseConverter = new RePhiEditConverter
        {
            OnInfo = OnInfo,
            OnWarning = OnWarning,
            OnError = OnError,
            OnDebug = OnDebug,
        };
        baseConverter.SetCancellationToken(_ct);
        var converted = baseConverter.FromIr(normalized, options);

        for (var index = 0; index < normalized.JudgeLineList.Count; index++)
        {
            _ct.ThrowIfCancellationRequested();
            if (
                StellateRePhiEditExtendedTexture.IsBlockAreaTexture(
                    normalized.JudgeLineList[index].Texture
                )
            )
                LogWarning(
                    $"IR 判定线 {index} 使用了噪域标记纹理，导出后重新导入时会被转换为噪域。"
                );
        }

        for (var index = 0; index < normalized.BlockAreaList.Count; index++)
        {
            _ct.ThrowIfCancellationRequested();
            var blockArea = normalized.BlockAreaList[index];
            if (blockArea is null)
                throw new FormatException("IR 噪域列表不能包含 null。");

            converted.JudgeLineList.Add(
                StellateRePhiEditExtendedBlockAreaBuilder.ConvertBlockArea(
                    blockArea,
                    index,
                    options.Cutting,
                    LogWarning,
                    _ct
                )
            );
        }

        return converted;
    }

    private static PhigrosV3TimeMapper CreateBeatMapper(IrChart chart)
    {
        var bpmList =
            chart.BpmList.Count > 0
                ? chart.BpmList
                : [new Ir.BpmItem { Bpm = 120f, StartBeat = new Beat(0) }];
        return new PhigrosV3TimeMapper(bpmList);
    }

    private static int MapFather(
        IReadOnlyList<RpeJudgeLine> sourceLines,
        IReadOnlyList<bool> isBlockAreaLine,
        IReadOnlyList<int> oldToNewIndex,
        int sourceIndex,
        out bool skippedBlockAreaParent
    )
    {
        skippedBlockAreaParent = false;
        var father = sourceLines[sourceIndex].Father;
        if (father < -1 || father >= sourceLines.Count)
            throw new FormatException($"RePhiEdit 判定线 {sourceIndex} 的父线索引无效。");

        var visited = new HashSet<int>();
        while (father >= 0 && isBlockAreaLine[father])
        {
            if (!visited.Add(father))
                throw new FormatException("RePhiEdit 判定线父级关系存在循环。");
            skippedBlockAreaParent = true;
            father = sourceLines[father].Father;
            if (father < -1 || father >= sourceLines.Count)
                throw new FormatException($"RePhiEdit 判定线 {sourceIndex} 的父线索引无效。");
        }

        if (father < 0)
            return -1;
        if (oldToNewIndex[father] < 0)
            throw new FormatException($"RePhiEdit 判定线 {sourceIndex} 的父线索引无法映射。");
        return oldToNewIndex[father];
    }
}
