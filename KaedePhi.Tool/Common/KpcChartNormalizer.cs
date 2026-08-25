using KaedePhi.Core.Common;
using KaedePhi.Core.KaedePhi;

namespace KaedePhi.Tool.Common;

/// <summary>
/// 创建音符结束拍已规范的谱面副本。
/// </summary>
public static class KpcChartNormalizer
{
    /// <summary>
    /// 复制 KPC 谱面并规范非 Hold 音符的结束拍。
    /// </summary>
    /// <param name="chart">待复制的 KPC 谱面。</param>
    /// <returns>音符结束拍已规范的独立谱面副本。</returns>
    public static Chart NormalizeAndValidateNoteEndBeats(Chart chart)
    {
        ArgumentNullException.ThrowIfNull(chart);
        KpcChartValidator.ValidateBpmAndBpmFactors(chart);

        var normalized = chart.Clone();
        NormalizeJudgeLineNotes(normalized.JudgeLineList);
        return normalized;
    }

    private static void NormalizeJudgeLineNotes(IReadOnlyList<JudgeLine> judgeLines)
    {
        foreach (var line in judgeLines)
            NormalizeNotes(line.Notes);
    }

    private static void NormalizeNotes(IReadOnlyList<Note> notes)
    {
        foreach (var note in notes)
            NormalizeNote(note);
    }

    private static void NormalizeNote(Note note)
    {
        if (note.Type == NoteType.Hold)
            return;

        note.EndBeat = new Beat((int[])note.StartBeat);
    }
}
