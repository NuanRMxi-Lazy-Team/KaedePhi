using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.JudgeLines;

/// <summary>
/// 判定线父子解绑器：将子判定线从父判定线的坐标系中解绑，转换为绝对坐标。
/// <para>
/// 不带 Dynamic 的方法使用等间隔采样；带 Dynamic 的方法使用自适应采样。
/// </para>
/// </summary>
public interface IJudgeLineUnbinder<TJudgeLine> : ILoggable
{
    /// <summary>
    /// 根据父线位置与旋转角度，计算子线在绝对坐标系中的位置。
    /// </summary>
    /// <param name="fatherLineX">父线 X 坐标。</param>
    /// <param name="fatherLineY">父线 Y 坐标。</param>
    /// <param name="angleDegrees">旋转角度（度）。</param>
    /// <param name="lineX">子线 X 坐标。</param>
    /// <param name="lineY">子线 Y 坐标。</param>
    /// <returns>绝对坐标系中的位置。</returns>
    (double X, double Y) GetLinePos(
        double fatherLineX,
        double fatherLineY,
        double angleDegrees,
        double lineX,
        double lineY
    );

    /// <summary>
    /// 根据父线位置与旋转角度，计算子线在指定渲染坐标系中的位置。
    /// </summary>
    /// <param name="fatherLineX">父线 X 坐标。</param>
    /// <param name="fatherLineY">父线 Y 坐标。</param>
    /// <param name="angleDegrees">旋转角度（度）。</param>
    /// <param name="lineX">子线 X 坐标。</param>
    /// <param name="lineY">子线 Y 坐标。</param>
    /// <param name="renderProfile">渲染坐标系配置。</param>
    /// <returns>渲染坐标系中的位置。</returns>
    (double X, double Y) GetLinePos(
        double fatherLineX,
        double fatherLineY,
        double angleDegrees,
        double lineX,
        double lineY,
        CoordinateProfile renderProfile
    );

    #region 等间隔采样

    /// <summary>
    /// 使用等间隔采样将判定线与父判定线解绑。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allTJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">每拍内的采样步数；越大精度越高，计算量越大。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    TJudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<TJudgeLine> allTJudgeLines,
        double precision,
        IProgress<ToolProgress>? progress = null
    );

    /// <summary>
    /// 使用等间隔采样将判定线与父判定线解绑，并支持取消长时间采样。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allTJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">每拍内的采样步数；越大精度越高，计算量越大。</param>
    /// <param name="progress">进度回调。</param>
    /// <param name="cancellationToken">取消解绑操作的令牌。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    TJudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<TJudgeLine> allTJudgeLines,
        double precision,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// 使用等间隔采样在指定渲染坐标系中解绑判定线。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allTJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="renderProfile">渲染坐标系配置。</param>
    /// <param name="precision">每拍内的采样步数；越大精度越高，计算量越大。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    TJudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<TJudgeLine> allTJudgeLines,
        CoordinateProfile renderProfile,
        double precision,
        IProgress<ToolProgress>? progress = null
    );

    /// <summary>
    /// 使用等间隔采样在指定渲染坐标系中解绑判定线，并支持取消长时间采样。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allTJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="renderProfile">渲染坐标系配置。</param>
    /// <param name="precision">每拍内的采样步数；越大精度越高，计算量越大。</param>
    /// <param name="progress">进度回调。</param>
    /// <param name="cancellationToken">取消解绑操作的令牌。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    TJudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<TJudgeLine> allTJudgeLines,
        CoordinateProfile renderProfile,
        double precision,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    );

    #endregion

    #region 自适应采样

    /// <summary>
    /// 使用自适应采样将判定线与父判定线解绑。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allTJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">每拍内的最大采样步数；越大精度越高，计算量越大。</param>
    /// <param name="tolerance">几何拟合容差百分比。</param>
    /// <param name="mergeTolerance">事件通道合并容差百分比。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    TJudgeLine FatherUnbindDynamic(
        int targetJudgeLineIndex,
        List<TJudgeLine> allTJudgeLines,
        double precision,
        double tolerance,
        double mergeTolerance,
        IProgress<ToolProgress>? progress = null
    );

    /// <summary>
    /// 使用自适应采样将判定线与父判定线解绑，并支持取消长时间采样。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allTJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">每拍内的最大采样步数；越大精度越高，计算量越大。</param>
    /// <param name="tolerance">几何拟合容差百分比。</param>
    /// <param name="mergeTolerance">事件通道合并容差百分比。</param>
    /// <param name="progress">进度回调。</param>
    /// <param name="cancellationToken">取消解绑操作的令牌。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    TJudgeLine FatherUnbindDynamic(
        int targetJudgeLineIndex,
        List<TJudgeLine> allTJudgeLines,
        double precision,
        double tolerance,
        double mergeTolerance,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// 使用自适应采样在指定渲染坐标系中解绑判定线。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allTJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="renderProfile">渲染坐标系配置。</param>
    /// <param name="precision">每拍内的最大采样步数；越大精度越高，计算量越大。</param>
    /// <param name="tolerance">几何拟合容差百分比。</param>
    /// <param name="mergeTolerance">事件通道合并容差百分比。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    TJudgeLine FatherUnbindDynamic(
        int targetJudgeLineIndex,
        List<TJudgeLine> allTJudgeLines,
        CoordinateProfile renderProfile,
        double precision,
        double tolerance,
        double mergeTolerance,
        IProgress<ToolProgress>? progress = null
    );

    /// <summary>
    /// 使用自适应采样在指定渲染坐标系中解绑判定线，并支持取消长时间采样。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allTJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="renderProfile">渲染坐标系配置。</param>
    /// <param name="precision">每拍内的最大采样步数；越大精度越高，计算量越大。</param>
    /// <param name="tolerance">几何拟合容差百分比。</param>
    /// <param name="mergeTolerance">事件通道合并容差百分比。</param>
    /// <param name="progress">进度回调。</param>
    /// <param name="cancellationToken">取消解绑操作的令牌。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    TJudgeLine FatherUnbindDynamic(
        int targetJudgeLineIndex,
        List<TJudgeLine> allTJudgeLines,
        CoordinateProfile renderProfile,
        double precision,
        double tolerance,
        double mergeTolerance,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    );

    #endregion
}
