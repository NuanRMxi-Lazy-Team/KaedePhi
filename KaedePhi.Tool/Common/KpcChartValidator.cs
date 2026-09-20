#pragma warning disable CS0618

using KaedePhi.Tool.Compatibility;
using Kpc = KaedePhi.Core.KaedePhi;

namespace KaedePhi.Tool.Common;

/// <summary>
/// 已弃用的 KPC 谱面校验入口，行为与 <see cref="IrChartValidator"/> 一致。
/// </summary>
[Obsolete("已弃用：请迁移至 KaedePhi.Tool.Common.IrChartValidator。")]
public static class KpcChartValidator
{
    /// <summary>
    /// 校验 KPC 谱面的 BPM 节点和判定线 BPM 因子。
    /// </summary>
    /// <param name="chart">待校验的 KPC 谱面。</param>
    [Obsolete("已弃用：请迁移至 IrChartValidator.ValidateBpmAndBpmFactors。")]
    public static void ValidateBpmAndBpmFactors(Kpc.Chart chart) =>
        IrChartValidator.ValidateBpmAndBpmFactors(KpcCompatibilityMapper.ToIntermediate(chart));

    /// <summary>
    /// 校验父子线索引与父线环路。
    /// </summary>
    /// <param name="judgeLines">待检查的判定线列表。</param>
    [Obsolete("已弃用：请迁移至 IrChartValidator.ValidateJudgeLineHierarchy。")]
    public static void ValidateJudgeLineHierarchy(IReadOnlyList<Kpc.JudgeLine> judgeLines) =>
        IrChartValidator.ValidateJudgeLineHierarchy(
            KpcCompatibilityMapper.ToIntermediate(judgeLines)
        );
}
