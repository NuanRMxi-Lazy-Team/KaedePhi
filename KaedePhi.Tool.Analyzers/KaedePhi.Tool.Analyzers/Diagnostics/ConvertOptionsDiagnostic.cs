using Microsoft.CodeAnalysis;

namespace KaedePhi.Tool.Analyzers.Diagnostics;

/// <summary>
/// 转换选项数值属性相关诊断规则的描述符集合。
/// </summary>
internal static class ConvertOptionsDiagnostic
{
    // 诊断 ID 与资源键一一对应，前缀区分分析器，编号区分规则
    public const string PositiveId = "KPTE0010";
    public const string ToleranceId = "KPTE0011";
    public const string NonNegativeId = "KPTE0012";

    // 标题、消息与描述均来自本地化资源，随资源文件切换语言
    private static readonly LocalizableString PositiveTitle = new LocalizableResourceString(
        nameof(Resource.kpte_0010_title),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString PositiveMessageFormat = new LocalizableResourceString(
        nameof(Resource.kpte_0010_message_format),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString PositiveDescription = new LocalizableResourceString(
        nameof(Resource.kpte_0010_description),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString ToleranceTitle = new LocalizableResourceString(
        nameof(Resource.kpte_0011_title),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString ToleranceMessageFormat =
        new LocalizableResourceString(
            nameof(Resource.kpte_0011_message_format),
            Resource.ResourceManager,
            typeof(Resource)
        );

    private static readonly LocalizableString ToleranceDescription = new LocalizableResourceString(
        nameof(Resource.kpte_0011_description),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString NonNegativeTitle = new LocalizableResourceString(
        nameof(Resource.kpte_0012_title),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString NonNegativeMessageFormat =
        new LocalizableResourceString(
            nameof(Resource.kpte_0012_message_format),
            Resource.ResourceManager,
            typeof(Resource)
        );

    private static readonly LocalizableString NonNegativeDescription =
        new LocalizableResourceString(
            nameof(Resource.kpte_0012_description),
            Resource.ResourceManager,
            typeof(Resource)
        );

    /// <summary>
    /// 必须为正数的属性（精度、比率等）不大于 0 时报告错误。
    /// </summary>
    public static readonly DiagnosticDescriptor PositiveRule = new(
        PositiveId,
        PositiveTitle,
        PositiveMessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: PositiveDescription
    );

    /// <summary>
    /// 容差属性超出 0 到 100 时报告错误。
    /// </summary>
    public static readonly DiagnosticDescriptor ToleranceRule = new(
        ToleranceId,
        ToleranceTitle,
        ToleranceMessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: ToleranceDescription
    );

    /// <summary>
    /// 非负属性为负数时报告错误。
    /// </summary>
    public static readonly DiagnosticDescriptor NonNegativeRule = new(
        NonNegativeId,
        NonNegativeTitle,
        NonNegativeMessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: NonNegativeDescription
    );
}
