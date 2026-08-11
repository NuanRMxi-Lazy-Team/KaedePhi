using KaedePhi.Core.PhiChain.v6;
using KaedePhi.Tool.Converter.PhiChain.Model;
using KaedePhi.Tool.JudgeLines.KaedePhi;
using KaedePhi.Tool.Layer.KaedePhi;

namespace KaedePhi.Tool.Converter.PhiChain.Utils;

/// <summary>
/// PhiChain 与 KPC 判定线之间的双向转换工具。
/// 处理树形线结构与扁平索引结构之间的转换。
/// </summary>
public static class JudgeLineBuilder
{
    /// <summary>
    /// 将 PhiChain 树形线结构展开为 KPC 扁平判定线列表。
    /// </summary>
    /// <param name="line">PhiChain 序列化线</param>
    /// <param name="fatherIndex">父级线索引，-1 表示无父级</param>
    /// <param name="result">结果列表</param>
    /// <param name="currentIndex">当前线索引（引用传递）</param>
    /// <param name="options">转换选项</param>
    /// <param name="warn">警告回调</param>
    public static void FlattenLine(
        SerializedLine line,
        int fatherIndex,
        List<Kpc.JudgeLine> result,
        ref int currentIndex,
        PhiChainToKpcConvertOptions options,
        Action<string>? warn = null
    )
    {
        var kpcLine = ConvertLine(line, fatherIndex, currentIndex, options, warn);
        result.Add(kpcLine);

        var currentIdx = currentIndex;
        currentIndex++;

        // 递归处理子线
        foreach (var child in line.Children)
        {
            FlattenLine(child, currentIdx, result, ref currentIndex, options, warn);
        }
    }

    /// <summary>
    /// 将 PhiChain 序列化线转换为 KPC 判定线。
    /// </summary>
    /// <param name="src">PhiChain 序列化线</param>
    /// <param name="fatherIndex">父级线索引</param>
    /// <param name="lineIndex">当前线索引</param>
    /// <param name="options">转换选项</param>
    /// <param name="warn">警告回调</param>
    /// <returns>KPC 判定线</returns>
    public static Kpc.JudgeLine ConvertLine(
        SerializedLine src,
        int fatherIndex,
        int lineIndex,
        PhiChainToKpcConvertOptions options,
        Action<string>? warn = null
    )
    {
        var kpcLine = new Kpc.JudgeLine
        {
            Name = src.Name,
            Father = fatherIndex,
            RotateWithFather = true,
        };

        // 转换事件，处理不支持的缓动
        var allEvents = new List<LineEvent>();
        foreach (var evt in src.Events)
        {
            if (EasingConverter.NeedsLinearSlicing(evt.Value.Easing))
            {
                warn?.Invoke(
                    $"PhiChain 的 {evt.Value.Easing.EasingType} 缓动在 KPC 中不支持，已切段为线性近似"
                );
                allEvents.AddRange(
                    EventBuilder.SliceUnsupportedEasing(evt, options.UnsupportedEasingPrecision)
                );
            }
            else
            {
                allEvents.Add(evt);
            }
        }

        var eventLayer = EventBuilder.ConvertEvents(allEvents);
        kpcLine.EventLayers.Add(eventLayer);

        // 转换普通音符
        var notes = src.Notes.ConvertAll(NoteBuilder.ConvertNote);

        // 展开 CurveNoteTrack（链式音符）
        if (src.CurveNoteTracks.Count > 0)
        {
            warn?.Invoke("PhiChain 的 CurveNoteTrack 将被展开为普通音符，曲线精度可能有损");
            var expandedNotes = ExpandCurveNoteTracks(src);
            notes.AddRange(expandedNotes);
        }

        kpcLine.Notes = notes;

        return kpcLine;
    }

    /// <summary>
    /// 展开所有 CurveNoteTrack 为普通音符。
    /// </summary>
    /// <param name="src">PhiChain 序列化线</param>
    /// <returns>展开后的音符列表</returns>
    private static List<Kpc.Note> ExpandCurveNoteTracks(SerializedLine src)
    {
        var notes = new List<Kpc.Note>();

        foreach (
            var expanded in from track in src.CurveNoteTracks
            where
                track.From >= 0
                && track.From < src.Notes.Count
                && track.To >= 0
                && track.To < src.Notes.Count
            let fromNote = src.Notes[track.From]
            let toNote = src.Notes[track.To]
            select NoteBuilder.ExpandCurveNoteTrack(track, fromNote, toNote)
        )
            notes.AddRange(expanded);
        return notes;
    }

    /// <summary>
    /// 将 KPC 扁平判定线列表构建为 PhiChain 树形线结构。
    /// </summary>
    /// <param name="kpcLines">KPC 判定线列表</param>
    /// <param name="options">转换选项</param>
    /// <param name="warn">警告回调</param>
    /// <returns>PhiChain 序列化线列表</returns>
    public static List<SerializedLine> BuildLineTree(
        List<Kpc.JudgeLine> kpcLines,
        KpcToPhiChainConvertOptions options,
        Action<string>? warn = null
    )
    {
        // 预处理：解绑 rotateWithFather 为 false 的子线
        var processedLines = PreprocessLines(kpcLines, options);

        var result = new List<SerializedLine>();
        var childMap = new Dictionary<int, List<int>>();

        // 构建父子关系映射
        for (var i = 0; i < processedLines.Count; i++)
        {
            var father = processedLines[i].Father;
            if (father < 0)
                continue;
            if (!childMap.ContainsKey(father))
                childMap[father] = [];
            childMap[father].Add(i);
        }

        // 从根节点开始构建树
        for (var i = 0; i < processedLines.Count; i++)
        {
            if (processedLines[i].Father < 0)
            {
                result.Add(BuildLineSubtree(processedLines, i, childMap, options, warn));
            }
        }

        return result;
    }

    /// <summary>
    /// 预处理判定线列表：解绑 rotateWithFather 为 false 的子线。
    /// </summary>
    private static List<Kpc.JudgeLine> PreprocessLines(
        List<Kpc.JudgeLine> kpcLines,
        KpcToPhiChainConvertOptions options
    )
    {
        if (!options.UnbindNonRotatingChildren)
            return kpcLines;

        var unbinder = new JudgeLineUnbinder();
        var result = kpcLines.Select(l => l.Clone()).ToList();

        // 找出所有需要解绑的子线（rotateWithFather 为 false）
        var linesToUnbind = new List<int>();
        for (var i = 0; i < result.Count; i++)
        {
            if (result[i].Father >= 0 && !result[i].RotateWithFather)
            {
                linesToUnbind.Add(i);
            }
        }

        // 使用 JudgeLineUnbinder 解绑
        var compressor = options.UnbindClassicMode ? new LayerProcessor() : null;
        foreach (var lineIndex in linesToUnbind)
        {
            Kpc.JudgeLine unboundLine;
            if (options.UnbindClassicMode)
            {
                unboundLine = unbinder.FatherUnbind(lineIndex, result, options.UnbindPrecision);
            }
            else
            {
                unboundLine = unbinder.FatherUnbindDynamic(
                    lineIndex,
                    result,
                    options.UnbindPrecision,
                    options.UnbindTolerance,
                    options.UnbindMergeTolerance
                );
            }

            if (compressor is not null)
            {
                foreach (var layer in unboundLine.EventLayers)
                    compressor.LayerEventsCompress(layer, options.UnbindTolerance);
            }

            result[lineIndex] = unboundLine;
        }

        return result;
    }

    /// <summary>
    /// 递归构建子树。
    /// </summary>
    private static SerializedLine BuildLineSubtree(
        List<Kpc.JudgeLine> kpcLines,
        int lineIndex,
        Dictionary<int, List<int>> childMap,
        KpcToPhiChainConvertOptions options,
        Action<string>? warn = null
    )
    {
        var kpcLine = kpcLines[lineIndex];
        var serializedLine = ConvertLineToPhiChain(kpcLine, options, warn);

        // 递归处理子线
        if (!childMap.TryGetValue(lineIndex, out var children))
            return serializedLine;
        foreach (var childIndex in children)
        {
            serializedLine.Children.Add(
                BuildLineSubtree(kpcLines, childIndex, childMap, options, warn)
            );
        }

        return serializedLine;
    }

    /// <summary>
    /// 将 KPC 判定线转换为 PhiChain 序列化线。
    /// </summary>
    /// <param name="src">KPC 判定线</param>
    /// <param name="options">转换选项</param>
    /// <param name="warn">警告回调</param>
    /// <returns>PhiChain 序列化线</returns>
    private static SerializedLine ConvertLineToPhiChain(
        Kpc.JudgeLine src,
        KpcToPhiChainConvertOptions options,
        Action<string>? warn = null
    )
    {
        WarnIfUnsupportedLineFields(src, warn);

        var line = new SerializedLine
        {
            Name = src.Name,
            Notes = src.Notes.ConvertAll(NoteBuilder.ConvertNote),
        };

        // 使用 LayerProcessor 合并多个事件层（PhiChain 不支持多层级）
        if (src.EventLayers.Count > 0)
        {
            if (src.EventLayers.Count > 1)
                warn?.Invoke(
                    $"PhiChain 不支持多事件层，判定线 '{src.Name}' 的 {src.EventLayers.Count} 个事件层将被合并"
                );

            var processor = new LayerProcessor();
            KpcEvents.EventLayer mergedLayer;

            if (options.MultiLayerMergeClassicMode)
            {
                mergedLayer = processor.LayerMerge(
                    src.EventLayers,
                    options.MultiLayerMergePrecision
                );
            }
            else
            {
                mergedLayer = processor.LayerMergePlus(
                    src.EventLayers,
                    options.MultiLayerMergePrecision,
                    options.MultiLayerMergeTolerance
                );
            }

            // 转换合并后的事件层为 PhiChain 事件列表
            line.Events = EventBuilder.ConvertEventLayer(mergedLayer, options);
        }
        else
        {
            line.Events = [];
        }

        // 警告音符字段丢失
        foreach (var note in src.Notes)
        {
            NoteBuilder.WarnIfUnsupportedNoteFields(note, warn);
        }

        return line;
    }

    /// <summary>
    /// 检查 KPC 判定线字段是否会被 PhiChain 丢弃，发出警告。
    /// </summary>
    /// <param name="src">KPC 判定线</param>
    /// <param name="warn">警告回调</param>
    private static void WarnIfUnsupportedLineFields(Kpc.JudgeLine src, Action<string>? warn)
    {
        if (warn == null)
            return;

        var defaults = new Kpc.JudgeLine();
        if (src.Texture != defaults.Texture)
            warn($"PhiChain 不支持 JudgeLine.Texture（值='{src.Texture}'）");
        if (
            Math.Abs(src.Anchor[0] - defaults.Anchor[0]) > Common.Constants.FloatEpsilon
            || Math.Abs(src.Anchor[1] - defaults.Anchor[1]) > Common.Constants.FloatEpsilon
        )
            warn($"PhiChain 不支持 JudgeLine.Anchor（值=[{src.Anchor[0]}, {src.Anchor[1]}]）");
        if (!src.IsCover)
            warn($"PhiChain 不支持 JudgeLine.IsCover（值=false）");
        if (src.ZOrder != defaults.ZOrder)
            warn($"PhiChain 不支持 JudgeLine.ZOrder（值={src.ZOrder}）");
        if (src.AttachUi != defaults.AttachUi)
            warn($"PhiChain 不支持 JudgeLine.AttachUi（值='{src.AttachUi}'）");
        if (src.IsGif)
            warn("PhiChain 不支持 JudgeLine.IsGif（值=True）");
        if (Math.Abs(src.BpmFactor - defaults.BpmFactor) > Common.Constants.FloatEpsilon)
            warn($"PhiChain 不支持 JudgeLine.BpmFactor（值={src.BpmFactor}）");
    }
}
