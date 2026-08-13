using Microsoft.CodeAnalysis;

namespace KaedePhi.Tool.Analyzers.Analysis;

/// <summary>
/// 转换选项属性的识别与取值规则分类。
/// </summary>
internal static class ConvertOptionsApi
{
    // 转换选项类型均位于 Converter 各格式的 Model 命名空间下
    private const string ConverterNamespacePrefix = "KaedePhi.Tool.Converter.";
    private const string ModelNamespaceSuffix = ".Model";

    /// <summary>
    /// 转换选项属性的取值约束种类。
    /// </summary>
    public enum PropertyKind
    {
        /// <summary>不参与校验的属性。</summary>
        None,

        /// <summary>必须为正数的属性（精度、比率、BPM 等）。</summary>
        Positive,

        /// <summary>必须落在 0 到 100 之间的容差属性。</summary>
        Tolerance,

        /// <summary>必须为非负数的属性。</summary>
        NonNegative,
    }

    /// <summary>
    /// 判断类型是否为转换选项类型（根类型或以 Options 结尾的嵌套类型）。
    /// </summary>
    /// <param name="type">待判断的类型符号</param>
    /// <returns>是否为转换选项类型</returns>
    public static bool IsConvertOptionType(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol namedType || namedType.ContainingNamespace is null)
            return false;

        var namespaceName = namedType.ContainingNamespace.ToDisplayString();
        var name = namedType.MetadataName;
        // 仅命中 Model 命名空间且类型名符合选项命名约定的类型
        return namespaceName.StartsWith(ConverterNamespacePrefix, StringComparison.Ordinal)
            && namespaceName.EndsWith(ModelNamespaceSuffix, StringComparison.Ordinal)
            && (name.EndsWith("Options", StringComparison.Ordinal) || name == "ConvertOption");
    }

    /// <summary>
    /// 按属性名对转换选项属性进行分类。
    /// <para>命名约定：以 Precision 结尾或含义为正数的属性要求正值；
    /// 名称包含 Tolerance 的属性使用百分比语义；TrailingBeatPadding 要求非负。</para>
    /// </summary>
    /// <param name="property">待分类的属性符号</param>
    /// <returns>属性的取值约束种类</returns>
    public static PropertyKind ClassifyProperty(IPropertySymbol property) =>
        property.Name switch
        {
            // 容差与精度命名互不重叠，可先按名称特征判断
            var name when name.Contains("Tolerance", StringComparison.Ordinal) =>
                PropertyKind.Tolerance,
            var name when name.Contains("Precision", StringComparison.Ordinal) =>
                PropertyKind.Positive,
            "SpeedConversionRatio"
            or "DefaultBpm"
            or "ElevationStep"
            or "DiscontinuityBeatPrecision"
            or "FrameDurationBeat" => PropertyKind.Positive,
            "TrailingBeatPadding" => PropertyKind.NonNegative,
            _ => PropertyKind.None,
        };
}
