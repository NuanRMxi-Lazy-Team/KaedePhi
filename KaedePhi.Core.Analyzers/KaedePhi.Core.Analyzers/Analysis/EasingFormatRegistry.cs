using System.Linq;
using Microsoft.CodeAnalysis;

namespace KaedePhi.Core.Analyzers.Analysis;

/// <summary>
/// 各格式缓动编号有效范围的注册表，新增格式时在此登记即可。
/// </summary>
internal static class EasingFormatRegistry
{
    private static readonly EasingFormatRange[] Ranges =
    [
        new("KaedePhi.Core.KaedePhi.Easing", "KPC", 1, 31),
        new("KaedePhi.Core.PhiEdit.Easing", "PE", 1, 29),
        new("KaedePhi.Core.RePhiEdit.Easing", "RePhiEdit", 1, 29),
    ];

    /// <summary>
    /// 按缓动类型查找其所属格式的范围。
    /// </summary>
    /// <param name="type">缓动类型符号</param>
    /// <param name="range">匹配到的格式范围</param>
    /// <returns>是否找到对应的格式范围</returns>
    public static bool TryGetRange(INamedTypeSymbol type, out EasingFormatRange range)
    {
        range = Ranges.FirstOrDefault(r => r.TypeFullName == type.ToDisplayString());
        return range is not null;
    }
}
