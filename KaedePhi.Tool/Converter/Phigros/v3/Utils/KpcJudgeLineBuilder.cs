#pragma warning disable CS0618

using KaedePhi.Tool.Compatibility;
using KaedePhi.Core.Formats.Phigros.v3;
using Kpc = KaedePhi.Core.KaedePhi;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

/// <summary>
/// 已弃用的 PhigrosV3 判定线到 KPC 判定线的构建器，行为与 <see cref="IrJudgeLineBuilder"/> 一致。
/// </summary>
[Obsolete("已弃用：请迁移至 KaedePhi.Tool.Converter.Phigros.v3.Utils.IrJudgeLineBuilder。")]
public static class KpcJudgeLineBuilder
{
    /// <summary>
    /// 转换单条判定线。
    /// </summary>
    /// <param name="src">待转换的 PhigrosV3 判定线。</param>
    /// <param name="index">判定线索引。</param>
    /// <param name="defaultBpm">默认 BPM。</param>
    /// <returns>转换后的旧 KPC 判定线。</returns>
    [Obsolete("已弃用：请迁移至 IrJudgeLineBuilder.ConvertJudgeLine。")]
    public static Kpc.JudgeLine ConvertJudgeLine(
        JudgeLine src,
        int index,
        float defaultBpm
    ) => KpcCompatibilityMapper.ToKpc(IrJudgeLineBuilder.ConvertJudgeLine(src, index, defaultBpm));
}