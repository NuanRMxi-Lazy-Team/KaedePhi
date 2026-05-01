using KaedePhi.Tool.Common;
using KaedePhi.Tool.JudgeLines.KaedePhi.Utils;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;

namespace KaedePhi.Tool.JudgeLines.KaedePhi;

/// <summary>
/// NRC（KaedePhi）谱面判定线父子解绑器。
/// </summary>
public class KpcJudgeLineUnbinder : LoggableBase, IJudgeLineUnbinder<JudgeLine>
{

    /// <inheritdoc/>
    public (double X, double Y) GetLinePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
        => FatherUnbindHelpers.GetLinePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY);

    /// <inheritdoc/>
    public (double X, double Y) GetLinePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY, CoordinateProfile renderProfile)
    {
        using var _ = FatherUnbindHelpers.UseRenderProfile(renderProfile);
        return FatherUnbindHelpers.GetLinePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY);
    }

    /// <inheritdoc/>
    public JudgeLine FatherUnbind(
        int targetJudgeLineIndex, List<JudgeLine> allJudgeLines,
        double precision)
        => FatherUnbindProcessor.FatherUnbind(
            targetJudgeLineIndex, allJudgeLines, precision,
            FatherUnbindHelpers.ChartCacheTable.GetOrCreateValue(allJudgeLines),
            LogInfo, LogWarning, LogError, LogDebug);

    /// <inheritdoc/>
    public JudgeLine FatherUnbind(
        int targetJudgeLineIndex, List<JudgeLine> allJudgeLines, CoordinateProfile renderProfile,
        double precision)
    {
        using var _ = FatherUnbindHelpers.UseRenderProfile(renderProfile);
        return FatherUnbind(targetJudgeLineIndex, allJudgeLines, precision);
    }

    /// <inheritdoc/>
    public JudgeLine FatherUnbindPlus(
        int targetJudgeLineIndex, List<JudgeLine> allJudgeLines,
        double precision, double tolerance)
        => FatherUnbindProcessor.FatherUnbindPlus(
            targetJudgeLineIndex, allJudgeLines, precision, tolerance,
            FatherUnbindHelpers.ChartCacheTable.GetOrCreateValue(allJudgeLines),
            LogInfo, LogWarning, LogError, LogDebug);

    /// <inheritdoc/>
    public JudgeLine FatherUnbindPlus(
        int targetJudgeLineIndex, List<JudgeLine> allJudgeLines, CoordinateProfile renderProfile,
        double precision, double tolerance)
    {
        using var _ = FatherUnbindHelpers.UseRenderProfile(renderProfile);
        return FatherUnbindPlus(targetJudgeLineIndex, allJudgeLines, precision, tolerance);
    }
}
