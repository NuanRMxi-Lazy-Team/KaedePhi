#pragma warning disable CS0618

using KaedePhi.Tool.Common;
using KaedePhi.Tool.Compatibility;
using Kpc = KaedePhi.Core.KaedePhi;

namespace KaedePhi.Tool.JudgeLines.KaedePhi;

/// <summary>
/// 已弃用的 KPC 判定线父子解绑器，行为与 <see cref="Intermediate.JudgeLineUnbinder"/> 一致。
/// </summary>
[Obsolete("已弃用：请迁移至 KaedePhi.Tool.JudgeLines.Intermediate.JudgeLineUnbinder。")]
public class JudgeLineUnbinder
    : Intermediate.JudgeLineUnbinder,
        IJudgeLineUnbinder<Kpc.JudgeLine>
{
    /// <summary>
    /// 使用等间隔采样将判定线与父判定线解绑。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">每拍内的采样步数。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    [Obsolete("已弃用：请迁移至 JudgeLineUnbinder.FatherUnbind。")]
    public Kpc.JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<Kpc.JudgeLine> allJudgeLines,
        double precision,
        IProgress<ToolProgress>? progress = null
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.FatherUnbind(
                targetJudgeLineIndex,
                KpcCompatibilityMapper.ToIntermediate(allJudgeLines),
                precision,
                progress
            )
        );

    /// <summary>
    /// 使用等间隔采样将判定线与父判定线解绑，并支持取消长时间采样。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">每拍内的采样步数。</param>
    /// <param name="progress">进度回调。</param>
    /// <param name="cancellationToken">取消解绑操作的令牌。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    [Obsolete("已弃用：请迁移至 JudgeLineUnbinder.FatherUnbind。")]
    public Kpc.JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<Kpc.JudgeLine> allJudgeLines,
        double precision,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.FatherUnbind(
                targetJudgeLineIndex,
                KpcCompatibilityMapper.ToIntermediate(allJudgeLines),
                precision,
                progress,
                cancellationToken
            )
        );

    /// <summary>
    /// 使用等间隔采样在指定渲染坐标系中解绑判定线。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="renderProfile">渲染坐标系配置。</param>
    /// <param name="precision">每拍内的采样步数。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    [Obsolete("已弃用：请迁移至 JudgeLineUnbinder.FatherUnbind。")]
    public Kpc.JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<Kpc.JudgeLine> allJudgeLines,
        CoordinateProfile renderProfile,
        double precision,
        IProgress<ToolProgress>? progress = null
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.FatherUnbind(
                targetJudgeLineIndex,
                KpcCompatibilityMapper.ToIntermediate(allJudgeLines),
                renderProfile,
                precision,
                progress
            )
        );

    /// <summary>
    /// 使用等间隔采样在指定渲染坐标系中解绑判定线，并支持取消长时间采样。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="renderProfile">渲染坐标系配置。</param>
    /// <param name="precision">每拍内的采样步数。</param>
    /// <param name="progress">进度回调。</param>
    /// <param name="cancellationToken">取消解绑操作的令牌。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    [Obsolete("已弃用：请迁移至 JudgeLineUnbinder.FatherUnbind。")]
    public Kpc.JudgeLine FatherUnbind(
        int targetJudgeLineIndex,
        List<Kpc.JudgeLine> allJudgeLines,
        CoordinateProfile renderProfile,
        double precision,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.FatherUnbind(
                targetJudgeLineIndex,
                KpcCompatibilityMapper.ToIntermediate(allJudgeLines),
                renderProfile,
                precision,
                progress,
                cancellationToken
            )
        );

    /// <summary>
    /// 使用自适应采样将判定线与父判定线解绑。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">每拍内的最大采样步数。</param>
    /// <param name="tolerance">几何拟合容差百分比。</param>
    /// <param name="mergeTolerance">事件通道合并容差百分比。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    [Obsolete("已弃用：请迁移至 JudgeLineUnbinder.FatherUnbindDynamic。")]
    public Kpc.JudgeLine FatherUnbindDynamic(
        int targetJudgeLineIndex,
        List<Kpc.JudgeLine> allJudgeLines,
        double precision,
        double tolerance,
        double mergeTolerance,
        IProgress<ToolProgress>? progress = null
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.FatherUnbindDynamic(
                targetJudgeLineIndex,
                KpcCompatibilityMapper.ToIntermediate(allJudgeLines),
                precision,
                tolerance,
                mergeTolerance,
                progress
            )
        );

    /// <summary>
    /// 使用自适应采样将判定线与父判定线解绑，并支持取消长时间采样。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="precision">每拍内的最大采样步数。</param>
    /// <param name="tolerance">几何拟合容差百分比。</param>
    /// <param name="mergeTolerance">事件通道合并容差百分比。</param>
    /// <param name="progress">进度回调。</param>
    /// <param name="cancellationToken">取消解绑操作的令牌。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    [Obsolete("已弃用：请迁移至 JudgeLineUnbinder.FatherUnbindDynamic。")]
    public Kpc.JudgeLine FatherUnbindDynamic(
        int targetJudgeLineIndex,
        List<Kpc.JudgeLine> allJudgeLines,
        double precision,
        double tolerance,
        double mergeTolerance,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.FatherUnbindDynamic(
                targetJudgeLineIndex,
                KpcCompatibilityMapper.ToIntermediate(allJudgeLines),
                precision,
                tolerance,
                mergeTolerance,
                progress,
                cancellationToken
            )
        );

    /// <summary>
    /// 使用自适应采样在指定渲染坐标系中解绑判定线。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="renderProfile">渲染坐标系配置。</param>
    /// <param name="precision">每拍内的最大采样步数。</param>
    /// <param name="tolerance">几何拟合容差百分比。</param>
    /// <param name="mergeTolerance">事件通道合并容差百分比。</param>
    /// <param name="progress">进度回调。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    [Obsolete("已弃用：请迁移至 JudgeLineUnbinder.FatherUnbindDynamic。")]
    public Kpc.JudgeLine FatherUnbindDynamic(
        int targetJudgeLineIndex,
        List<Kpc.JudgeLine> allJudgeLines,
        CoordinateProfile renderProfile,
        double precision,
        double tolerance,
        double mergeTolerance,
        IProgress<ToolProgress>? progress = null
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.FatherUnbindDynamic(
                targetJudgeLineIndex,
                KpcCompatibilityMapper.ToIntermediate(allJudgeLines),
                renderProfile,
                precision,
                tolerance,
                mergeTolerance,
                progress
            )
        );

    /// <summary>
    /// 使用自适应采样在指定渲染坐标系中解绑判定线，并支持取消长时间采样。
    /// </summary>
    /// <param name="targetJudgeLineIndex">目标判定线在列表中的索引。</param>
    /// <param name="allJudgeLines">当前谱面的全部判定线。</param>
    /// <param name="renderProfile">渲染坐标系配置。</param>
    /// <param name="precision">每拍内的最大采样步数。</param>
    /// <param name="tolerance">几何拟合容差百分比。</param>
    /// <param name="mergeTolerance">事件通道合并容差百分比。</param>
    /// <param name="progress">进度回调。</param>
    /// <param name="cancellationToken">取消解绑操作的令牌。</param>
    /// <returns>解绑后的判定线（已转换为绝对坐标）。</returns>
    [Obsolete("已弃用：请迁移至 JudgeLineUnbinder.FatherUnbindDynamic。")]
    public Kpc.JudgeLine FatherUnbindDynamic(
        int targetJudgeLineIndex,
        List<Kpc.JudgeLine> allJudgeLines,
        CoordinateProfile renderProfile,
        double precision,
        double tolerance,
        double mergeTolerance,
        IProgress<ToolProgress>? progress,
        CancellationToken cancellationToken
    ) =>
        KpcCompatibilityMapper.ToKpc(
            base.FatherUnbindDynamic(
                targetJudgeLineIndex,
                KpcCompatibilityMapper.ToIntermediate(allJudgeLines),
                renderProfile,
                precision,
                tolerance,
                mergeTolerance,
                progress,
                cancellationToken
            )
        );
}
