using KaedePhi.Core.Common;
using KaedePhi.Core.KaedePhi;

namespace KaedePhi.Tool.Common;

/// <summary>
/// 创建满足 KPC 音符结束拍约束的谱面副本。
/// </summary>
public static class KpcChartNormalizer
{
    /// <summary>
    /// 复制 KPC 谱面，校验 Hold 结束拍并规范非 Hold 结束拍。
    /// </summary>
    /// <param name="chart">待复制和校验的 KPC 谱面。</param>
    /// <returns>音符结束拍已校验并规范的独立谱面副本。</returns>
    public static Chart NormalizeAndValidateNoteEndBeats(Chart chart)
    {
        ArgumentNullException.ThrowIfNull(chart);
        KpcChartValidator.ValidateBpmAndBpmFactors(chart);
        KpcChartValidator.ValidateCloneableJudgeLineStructure(chart.JudgeLineList);

        var normalized = chart.Clone();
        NormalizeJudgeLineNotes(normalized.JudgeLineList);
        return normalized;
    }

    private static void NormalizeJudgeLineNotes(IReadOnlyList<JudgeLine> judgeLines)
    {
        for (var lineIndex = 0; lineIndex < judgeLines.Count; lineIndex++)
            NormalizeNotes(judgeLines[lineIndex].Notes, lineIndex);
    }

    private static void NormalizeNotes(IReadOnlyList<Note> notes, int lineIndex)
    {
        for (var noteIndex = 0; noteIndex < notes.Count; noteIndex++)
            NormalizeNote(notes[noteIndex], lineIndex, noteIndex);
    }

    private static void NormalizeNote(Note note, int lineIndex, int noteIndex)
    {
        if (note.Type == NoteType.Hold)
        {
            ValidateHoldEndBeat(note, lineIndex, noteIndex);
            return;
        }

        note.EndBeat = new Beat((int[])note.StartBeat);
    }

    private static void ValidateHoldEndBeat(Note note, int lineIndex, int noteIndex)
    {
        if (!note.HasExplicitEndBeat || note.EndBeat <= note.StartBeat)
            throw new FormatException(
                $"判定线 {lineIndex} 的音符 {noteIndex} 缺少有效的 Hold 结束拍。"
            );
    }
}
