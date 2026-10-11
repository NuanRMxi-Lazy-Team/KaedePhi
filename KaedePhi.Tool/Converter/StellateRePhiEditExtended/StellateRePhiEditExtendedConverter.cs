using System.Runtime.ExceptionServices;
using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using KaedePhi.Tool.Converter.RePhiEdit;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;
using IrBlockArea = KaedePhi.Core.Intermediate.Model.BlockArea;
using IrChart = KaedePhi.Core.Intermediate.Model.Chart;
using RpeChart = KaedePhi.Core.Formats.RePhiEdit.Model.Chart;
using RpeJudgeLine = KaedePhi.Core.Formats.RePhiEdit.Model.JudgeLine;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended;

/// <summary>
/// StellateRePhiEditExtended 与 IR 谱面之间的格式转换器。
/// </summary>
public sealed class StellateRePhiEditExtendedConverter
    : LoggableBase,
        IChartConverter<RpeChart, Unit?, ConvertOption>,
        ICancellableChartConverter
{
    private CancellationToken _ct;

    /// <summary>
    /// 设置转换过程中使用的取消令牌。
    /// </summary>
    /// <param name="ct">取消令牌</param>
    public void SetCancellationToken(CancellationToken ct) => _ct = ct;

    /// <summary>
    /// 将 StellateRePhiEditExtended 谱面转换为 IR 格式。
    /// </summary>
    /// <param name="source">源谱面</param>
    /// <returns>转换后的 IR 谱面</returns>
    public IrChart ToIr(RpeChart source)
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
            JudgeLineList =
            [
                .. sourceLines
                    .Select((line, index) =>
                        isBlockAreaLine[index] ? new RpeJudgeLine { Father = line.Father } : line
                    )
            ],
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
            geometryConverter.PrepareLine(item.Index);
            if (item.Line.Notes is { Count: > 0 })
                LogWarning(
                    $"StellateRePhiEditExtended 判定线 {item.Index} 的音符因转换为噪域而被忽略。"
                );
        }

        switch (blockAreaLines.Count)
        {
            case 1:
                blockAreas.AddRange(geometryConverter.ConvertLine(blockAreaLines[0].Index));
                break;
            case > 1:
            {
                var convertedBlockAreas = new List<IrBlockArea>[blockAreaLines.Count];
                var sampleParallelism = Math.Max(
                    1,
                    Environment.ProcessorCount / blockAreaLines.Count
                );
                try
                {
                    Parallel.For(
                        0,
                        blockAreaLines.Count,
                        new ParallelOptions
                        {
                            CancellationToken = _ct,
                            MaxDegreeOfParallelism = Environment.ProcessorCount,
                        },
                        index =>
                        {
                            convertedBlockAreas[index] = geometryConverter.ConvertLine(
                                blockAreaLines[index].Index,
                                sampleParallelism
                            );
                        }
                    );
                }
                catch (AggregateException exception)
                {
                    var failures = exception.Flatten().InnerExceptions;
                    if (failures.Count == 1)
                        ExceptionDispatchInfo.Capture(failures[0]).Throw();

                    throw;
                }

                foreach (var convertedBlockArea in convertedBlockAreas)
                    blockAreas.AddRange(convertedBlockArea);
                break;
            }
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

    /// <summary>
    /// 使用通用转换器接口将 StellateRePhiEditExtended 谱面转换为 IR 格式。
    /// </summary>
    /// <param name="input">源谱面</param>
    /// <param name="options">未使用的输入选项</param>
    /// <returns>转换后的 IR 谱面</returns>
    public IrChart ToIr(RpeChart input, Unit? options) => ToIr(input);

    /// <summary>
    /// 将 IR 谱面转换为 StellateRePhiEditExtended 格式。
    /// </summary>
    /// <param name="input">IR 谱面</param>
    /// <param name="options">输出转换选项</param>
    /// <returns>转换后的 StellateRePhiEditExtended 谱面</returns>
    public RpeChart FromIr(IrChart input, ConvertOption options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);
        _ct.ThrowIfCancellationRequested();

        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(input);
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
        List<RpeJudgeLine> sourceLines,
        bool[] isBlockAreaLine,
        int[] oldToNewIndex,
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
