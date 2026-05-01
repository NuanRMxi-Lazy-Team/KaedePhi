using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.JudgeLines;

/// <summary>
/// 判定线父子解绑器：将子判定线从父判定线的坐标系中解绑，转换为绝对坐标。
/// </summary>
public interface IJudgeLineUnbinder<TJudgeLine> : ILoggable
{
    /// <summary>
    /// 根据父线位置与旋转角度，计算子线在绝对坐标系中的位置。
    /// </summary>
    /// <param name="fatherLineX">父线 X 坐标。</param>
    /// <param name="fatherLineY">父线 Y 坐标。</param>
    /// <param name="angleDegrees">父线旋转角度（度）。</param>
    /// <param name="lineX">子线相对于父线的 X 坐标。</param>
    /// <param name="lineY">子线相对于父线的 Y 坐标。</param>
    /// <returns>子线在绝对坐标系中的 (X, Y) 坐标。</returns>
    (double X, double Y) GetLinePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY);

    /// <summary>
    /// 根据父线位置与旋转角度，计算子线在指定渲染坐标系中的位置。
    /// </summary>
    /// <param name="fatherLineX">父线 X 坐标。</param>
    /// <param name="fatherLineY">父线 Y 坐标。</param>
    /// <param name="angleDegrees">父线旋转角度（度）。</param>
    /// <param name="lineX">子线相对于父线的 X 坐标。</param>
    /// <param name="lineY">子线相对于父线的 Y 坐标。</param>
    /// <param name="renderProfile">渲染坐标系配置。</param>
    /// <returns>子线在指定坐标系中的 (X, Y) 坐标。</returns>
    (double X, double Y) GetLinePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY, CoordinateProfile renderProfile);

    /// <summary>
    /// 将判定线与父判定线解绑（等间隔采样）。
    /// 若父线仍有父线则递归解绑，确保父线已为绝对坐标后再解绑目标线。
    /// </summary>
    /// <param name="targetTJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allTJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">每拍内的采样步数；越大精度越高，计算量越大。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    TJudgeLine FatherUnbind(
        int targetTJudgeLineIndex, List<TJudgeLine> allTJudgeLines,
        double precision);

    /// <summary>
    /// 将判定线与父判定线解绑（等间隔采样，指定渲染坐标系）。
    /// </summary>
    /// <param name="targetTJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allTJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="renderProfile">渲染坐标系配置。</param>
    /// <param name="precision">每拍内的采样步数。</param>
    /// <returns>解绑后的判定线。</returns>
    TJudgeLine FatherUnbind(
        int targetTJudgeLineIndex, List<TJudgeLine> allTJudgeLines, CoordinateProfile renderProfile,
        double precision);

    /// <summary>
    /// 将判定线与父判定线解绑（自适应采样）。
    /// 以事件边界为强制切割点，仅在误差超过容差时插入新采样段，相较等间隔版可减少冗余段数。
    /// </summary>
    /// <param name="targetTJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allTJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">自适应采样的最大步数上限（同时作为事件合并精度）。</param>
    /// <param name="tolerance">误差容差百分比，决定何时插入额外切割点及压缩阈值。</param>
    /// <returns>解绑后的判定线。</returns>
    TJudgeLine FatherUnbindPlus(
        int targetTJudgeLineIndex, List<TJudgeLine> allTJudgeLines,
        double precision, double tolerance);

    /// <summary>
    /// 将判定线与父判定线解绑（自适应采样，指定渲染坐标系）。
    /// </summary>
    /// <param name="targetTJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allTJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="renderProfile">渲染坐标系配置。</param>
    /// <param name="precision">自适应采样的最大步数上限。</param>
    /// <param name="tolerance">误差容差百分比。</param>
    /// <returns>解绑后的判定线。</returns>
    TJudgeLine FatherUnbindPlus(
        int targetTJudgeLineIndex, List<TJudgeLine> allTJudgeLines, CoordinateProfile renderProfile,
        double precision, double tolerance);
}
