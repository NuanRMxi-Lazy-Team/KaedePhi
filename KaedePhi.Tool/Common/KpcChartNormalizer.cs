#pragma warning disable CS0618

using KaedePhi.Tool.Compatibility;
using Kpc = KaedePhi.Core.KaedePhi;

namespace KaedePhi.Tool.Common;

/// <summary>
/// 已弃用的 KPC 谱面规范化入口，行为与 <see cref="IrChartNormalizer"/> 一致。
/// </summary>
[Obsolete("已弃用：请迁移至 KaedePhi.Tool.Common.IrChartNormalizer。")]
public static class KpcChartNormalizer
{
    /// <summary>
    /// 复制 KPC 谱面并规范非 Hold 音符的结束拍。
    /// </summary>
    /// <param name="chart">待复制的 KPC 谱面。</param>
    /// <returns>音符结束拍已规范的独立谱面副本。</returns>
    [Obsolete("已弃用：请迁移至 IrChartNormalizer.NormalizeAndValidateNoteEndBeats。")]
    public static Kpc.Chart NormalizeAndValidateNoteEndBeats(Kpc.Chart chart) =>
        KpcCompatibilityMapper.ToKpc(
            IrChartNormalizer.NormalizeAndValidateNoteEndBeats(
                KpcCompatibilityMapper.ToIntermediate(chart)
            )
        );
}
