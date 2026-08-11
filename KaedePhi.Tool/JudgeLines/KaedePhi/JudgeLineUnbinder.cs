using KaedePhi.Tool.Common;
using KaedePhi.Tool.JudgeLines.KaedePhi.Utils;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;

namespace KaedePhi.Tool.JudgeLines.KaedePhi;

/// <summary>
/// KPC 谱面判定线父子解绑器。
/// <para>不带 Dynamic 的方法使用等间隔采样；带 Dynamic 的方法使用自适应采样。</para>
/// </summary>
public class JudgeLineUnbinder : LoggableBase, IJudgeLineUnbinder<JudgeLine>
{
    /// <inheritdoc/>
    public (double X, double Y) GetLinePos(
        double fatherLineX,
        double fatherLineY,
        double angleDegrees,
        double lineX,
        double lineY
    ) => FatherUnbindHelpers.GetLinePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY);

    /// <inheritdoc/>
    public (double X, double Y) GetLinePos(
        double fatherLineX,
        double fatherLineY,
        double angleDegrees,
        double lineX,
        double lineY,
        CoordinateProfile renderProfile
    )
    {
        using var profileScope = FatherUnbindHelpers.UseRenderProfile(renderProfile);
        return FatherUnbindHelpers.GetLinePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY);
    }

    #region 处理器创建

    private FatherUnbindProcessor CreateProcessor(List<JudgeLine> allJudgeLines) =>
        new(
            FatherUnbindHelpers.JudgeLineCacheTable.GetOrCreateValue(allJudgeLines),
            LogInfo,
            LogWarning,
            LogError,
            LogDebug
        );

    private FatherUnbindPlusProcessor CreatePlusProcessor(
        List<JudgeLine> allJudgeLines,
        double tolerance,
        double mergeTolerance
    ) =>
        new(
            FatherUnbindHelpers.JudgeLineCacheTable.GetOrCreateValue(allJudgeLines),
            tolerance,
            mergeTolerance,
            LogInfo,
            LogWarning,
            LogError,
            LogDebug
        );

    #endregion

    #region 等间隔采样

    /// <inheritdoc/>
    public JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        double precision,
        IProgress<ToolProgress>? progress = null
    ) =>
        UnbindEqualSpacing(
            targetJudgeLineIndex,
            allJudgeLines,
            precision,
            progress,
            CancellationToken.None
        );

    /// <inheritdoc/>
    public JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        double precision,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    ) =>
        UnbindEqualSpacing(
            targetJudgeLineIndex,
            allJudgeLines,
            precision,
            progress,
            cancellationToken
        );

    /// <inheritdoc/>
    public JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        CoordinateProfile renderProfile,
        double precision,
        IProgress<ToolProgress>? progress = null
    ) =>
        FatherUnbind(
            targetJudgeLineIndex,
            allJudgeLines,
            renderProfile,
            precision,
            progress,
            CancellationToken.None
        );

    /// <inheritdoc/>
    public JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        CoordinateProfile renderProfile,
        double precision,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    )
    {
        using var profileScope = FatherUnbindHelpers.UseRenderProfile(renderProfile);
        return UnbindEqualSpacing(
            targetJudgeLineIndex,
            allJudgeLines,
            precision,
            progress,
            cancellationToken
        );
    }

    private JudgeLine UnbindEqualSpacing(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        double precision,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    ) =>
        ValidateInput(targetJudgeLineIndex, allJudgeLines, precision, null)
            .CreateProcessor(allJudgeLines)
            .FatherUnbind(
                targetJudgeLineIndex,
                allJudgeLines,
                precision,
                progress,
                cancellationToken
            );

    #endregion

    #region 自适应采样

    /// <inheritdoc/>
    public JudgeLine FatherUnbindDynamic(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        double precision,
        double tolerance,
        double mergeTolerance,
        IProgress<ToolProgress>? progress = null
    ) =>
        UnbindAdaptive(
            targetJudgeLineIndex,
            allJudgeLines,
            precision,
            tolerance,
            mergeTolerance,
            progress,
            CancellationToken.None
        );

    /// <inheritdoc/>
    public JudgeLine FatherUnbindDynamic(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        double precision,
        double tolerance,
        double mergeTolerance,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    ) =>
        UnbindAdaptive(
            targetJudgeLineIndex,
            allJudgeLines,
            precision,
            tolerance,
            mergeTolerance,
            progress,
            cancellationToken
        );

    /// <inheritdoc/>
    public JudgeLine FatherUnbindDynamic(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        CoordinateProfile renderProfile,
        double precision,
        double tolerance,
        double mergeTolerance,
        IProgress<ToolProgress>? progress = null
    ) =>
        FatherUnbindDynamic(
            targetJudgeLineIndex,
            allJudgeLines,
            renderProfile,
            precision,
            tolerance,
            mergeTolerance,
            progress,
            CancellationToken.None
        );

    /// <inheritdoc/>
    public JudgeLine FatherUnbindDynamic(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        CoordinateProfile renderProfile,
        double precision,
        double tolerance,
        double mergeTolerance,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    )
    {
        using var profileScope = FatherUnbindHelpers.UseRenderProfile(renderProfile);
        return UnbindAdaptive(
            targetJudgeLineIndex,
            allJudgeLines,
            precision,
            tolerance,
            mergeTolerance,
            progress,
            cancellationToken
        );
    }

    private JudgeLine UnbindAdaptive(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        double precision,
        double tolerance,
        double mergeTolerance,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    ) =>
        ValidateInput(
                targetJudgeLineIndex,
                allJudgeLines,
                precision,
                tolerance,
                mergeTolerance
            )
            .CreatePlusProcessor(allJudgeLines, tolerance, mergeTolerance)
            .FatherUnbind(
                targetJudgeLineIndex,
                allJudgeLines,
                precision,
                progress,
                cancellationToken
            );

    #endregion

    private JudgeLineUnbinder ValidateInput(
        int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines,
        double precision,
        double? tolerance,
        double? mergeTolerance = null
    )
    {
        ArgumentNullException.ThrowIfNull(allJudgeLines);
        if (targetJudgeLineIndex < 0 || targetJudgeLineIndex >= allJudgeLines.Count)
            throw new ArgumentOutOfRangeException(nameof(targetJudgeLineIndex));
        ChartProcessingValidator.ValidatePrecision(precision);
        if (tolerance is not null)
            ChartProcessingValidator.ValidateTolerance(tolerance.Value);
        if (mergeTolerance is not null)
            ChartProcessingValidator.ValidateTolerance(mergeTolerance.Value);
        ChartProcessingValidator.ValidateJudgeLineHierarchy(allJudgeLines);
        return this;
    }
}
