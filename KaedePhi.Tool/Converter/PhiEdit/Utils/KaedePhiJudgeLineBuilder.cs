#pragma warning disable CS0618

using KaedePhi.Tool.Compatibility;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using Kpc = KaedePhi.Core.KaedePhi;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

/// <summary>
/// 已弃用的 PE 判定线到 KPC 判定线的构建器，行为与 <see cref="IntermediateJudgeLineBuilder"/> 一致。
/// </summary>
[Obsolete(
    "已弃用：请迁移至 KaedePhi.Tool.Converter.PhiEdit.Utils.IntermediateJudgeLineBuilder。"
)]
public class KaedePhiJudgeLineBuilder
{
    private readonly IntermediateJudgeLineBuilder _builder;

    /// <summary>
    /// 使用转换选项创建构建器。
    /// </summary>
    /// <param name="options">PhiEdit 转 IR 转换选项。</param>
    /// <param name="ct">取消令牌。</param>
    [Obsolete("已弃用：请迁移至 IntermediateJudgeLineBuilder。")]
    public KaedePhiJudgeLineBuilder(
        PhiEditToKpcConvertOptions options,
        CancellationToken ct = default
    )
    {
        _builder = new IntermediateJudgeLineBuilder(options, ct);
    }

    /// <summary>
    /// 转换全部判定线。
    /// </summary>
    /// <param name="judgeLines">待转换的 PE 判定线列表。</param>
    /// <returns>转换后的旧 KPC 判定线列表。</returns>
    [Obsolete("已弃用：请迁移至 IntermediateJudgeLineBuilder.ConvertJudgeLines。")]
    public List<Kpc.JudgeLine> ConvertJudgeLines(List<Pe.JudgeLine>? judgeLines) =>
        KpcCompatibilityMapper.ToKpc(_builder.ConvertJudgeLines(judgeLines));

    /// <summary>
    /// 转换单条判定线。
    /// </summary>
    /// <param name="src">待转换的 PE 判定线。</param>
    /// <param name="index">判定线索引。</param>
    /// <returns>转换后的旧 KPC 判定线。</returns>
    [Obsolete("已弃用：请迁移至 IntermediateJudgeLineBuilder.ConvertJudgeLine。")]
    public Kpc.JudgeLine ConvertJudgeLine(Pe.JudgeLine src, int index) =>
        KpcCompatibilityMapper.ToKpc(_builder.ConvertJudgeLine(src, index));
}