using KaedePhi.Tool.Common;
using KaedePhi.Tool.JudgeLines.KaedePhi;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;

namespace KaedePhi.Tool.KaedePhi.JudgeLines;

/// <summary>
/// NRC 格式判定线操作工具。提供父子关系解绑功能。
/// </summary>
[Obsolete("请改用 KaedePhi.Tool.JudgeLines.KaedePhi.KpcJudgeLineUnbinder")]
public static class KpcJudgeLineTools
{
    private static readonly KpcJudgeLineUnbinder Unbinder = new();

    [Obsolete("请改用 KpcJudgeLineUnbinder.GetLinePos")]
    public static (double X, double Y) GetLinePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
        => Unbinder.GetLinePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY);

    [Obsolete("请改用 KpcJudgeLineUnbinder.GetLinePos")]
    public static (double X, double Y) GetLinePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY, CoordinateProfile renderProfile)
        => Unbinder.GetLinePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY, renderProfile);

    #region 经典（固定采样）

    [Obsolete("请改用 KpcJudgeLineUnbinder.FatherUnbind")]
    public static JudgeLine FatherUnbind(
        int targetJudgeLineIndex, List<JudgeLine> allJudgeLines,
        double precision = 64d)
        => Unbinder.FatherUnbind(targetJudgeLineIndex, allJudgeLines, precision);

    [Obsolete("请改用 KpcJudgeLineUnbinder.FatherUnbind")]
    public static JudgeLine FatherUnbind(
        int targetJudgeLineIndex, List<JudgeLine> allJudgeLines, CoordinateProfile renderProfile,
        double precision = 64d, double tolerance = 5d, bool compress = true)
        => Unbinder.FatherUnbind(targetJudgeLineIndex, allJudgeLines, renderProfile, precision);

    #endregion

    #region Plus（自适应采样）

    [Obsolete("请改用 KpcJudgeLineUnbinder.FatherUnbindPlus")]
    public static JudgeLine FatherUnbindPlus(
        int targetJudgeLineIndex, List<JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d)
        => Unbinder.FatherUnbindPlus(targetJudgeLineIndex, allJudgeLines, precision, tolerance);

    [Obsolete("请改用 KpcJudgeLineUnbinder.FatherUnbindPlus")]
    public static JudgeLine FatherUnbindPlus(
        int targetJudgeLineIndex, List<JudgeLine> allJudgeLines, CoordinateProfile renderProfile,
        double precision = 64d, double tolerance = 5d)
        => Unbinder.FatherUnbindPlus(targetJudgeLineIndex, allJudgeLines, renderProfile, precision, tolerance);

    #endregion
}
