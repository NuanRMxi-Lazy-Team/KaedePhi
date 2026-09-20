#pragma warning disable CS0618

using KaedePhi.Tool.Common;
using KaedePhi.Tool.Compatibility;
using KaedePhi.Tool.Converter.Intermediate;
using Kpc = KaedePhi.Core.KaedePhi;

namespace KaedePhi.Tool.Converter.KaedePhi;

/// <summary>
/// 已弃用的 KPC 格式直通转换器，行为与 <see cref="IntermediateConverter"/> 一致。
/// </summary>
[Obsolete("已弃用：请迁移至 KaedePhi.Tool.Converter.Intermediate.IntermediateConverter。")]
public class KaedePhiConverter : LoggableBase, IChartConverter<Kpc.Chart, Unit?, Unit?>
{
    /// <summary>
    /// 将旧 KPC 谱面规范化并转换为 IR 谱面。
    /// </summary>
    /// <param name="input">旧 KPC 谱面。</param>
    /// <param name="options">未使用。</param>
    /// <returns>规范后的 IR 谱面。</returns>
    [Obsolete("已弃用：请迁移至 IntermediateConverter.ToIr。")]
    public Ir.Chart ToIr(Kpc.Chart input, Unit? options) =>
        IrChartNormalizer.NormalizeAndValidateNoteEndBeats(
            KpcCompatibilityMapper.ToIntermediate(input)
        );

    /// <summary>
    /// 将 IR 谱面规范化并转换为旧 KPC 谱面。
    /// </summary>
    /// <param name="input">IR 谱面。</param>
    /// <param name="options">未使用。</param>
    /// <returns>规范后的旧 KPC 谱面。</returns>
    [Obsolete("已弃用：请迁移至 IntermediateConverter.FromIr。")]
    public Kpc.Chart FromIr(Ir.Chart input, Unit? options) =>
        KpcCompatibilityMapper.ToKpc(IrChartNormalizer.NormalizeAndValidateNoteEndBeats(input));
}