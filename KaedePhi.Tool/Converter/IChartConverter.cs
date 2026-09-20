using KaedePhi.Tool.Common;
using KpcChart = KaedePhi.Core.KaedePhi.Chart;

namespace KaedePhi.Tool.Converter;

/// <summary>
/// 谱面格式转换器接口。
/// </summary>
/// <typeparam name="TPayload">目标谱面格式</typeparam>
/// <typeparam name="TInOptions">输入转换选项</typeparam>
/// <typeparam name="TOutOptions">输出转换选项</typeparam>
public interface IChartConverter<TPayload, in TInOptions, in TOutOptions> : ILoggable
{
    /// <summary>
    /// 将外部格式转换为 IR 内部格式。
    /// </summary>
    /// <param name="input">源谱面</param>
    /// <param name="options">转换选项</param>
    /// <returns>IR 谱面</returns>
    Ir.Chart ToIr(TPayload input, TInOptions options);

    /// <summary>
    /// 将 IR 内部格式转换为外部格式。
    /// </summary>
    /// <param name="input">IR 谱面</param>
    /// <param name="options">转换选项</param>
    /// <returns>目标格式谱面</returns>
    TPayload FromIr(Ir.Chart input, TOutOptions options);

    /// <summary>
    /// 将外部格式转换为已弃用的 KPC 内部格式。
    /// </summary>
    /// <param name="input">源谱面</param>
    /// <param name="options">转换选项</param>
    /// <returns>KPC 谱面</returns>
    [Obsolete("已弃用：请迁移至 ToIr。")]
    KpcChart ToKpc(TPayload input, TInOptions options) =>
        Compatibility.KpcCompatibilityMapper.ToKpc(ToIr(input, options));

    /// <summary>
    /// 将已弃用的 KPC 内部格式转换为外部格式。
    /// </summary>
    /// <param name="input">KPC 谱面</param>
    /// <param name="options">转换选项</param>
    /// <returns>目标格式谱面</returns>
    [Obsolete("已弃用：请迁移至 FromIr。")]
    TPayload FromKpc(KpcChart input, TOutOptions options) =>
        FromIr(Compatibility.KpcCompatibilityMapper.ToIntermediate(input), options);
}