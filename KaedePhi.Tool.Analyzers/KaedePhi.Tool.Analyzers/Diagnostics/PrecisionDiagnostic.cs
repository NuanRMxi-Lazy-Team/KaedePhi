using Microsoft.CodeAnalysis;

namespace KaedePhi.Tool.Analyzers.Diagnostics;

/// <summary>
/// 采样精度参数相关诊断规则的描述符集合。
/// </summary>
internal static class PrecisionDiagnostic
{
    // 诊断 ID 与资源键一一对应，前缀区分分析器，编号区分规则
    public const string Id = "KPTE0002";
    public const string ExcessiveId = "KPTE0003";
    public const string HighPrecisionId = "KPTR0002";

    // 标题、消息与描述均来自本地化资源，随资源文件切换语言
    private static readonly LocalizableString Title = new LocalizableResourceString(
        nameof(Resource.kpte_0002_title),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
        nameof(Resource.kpte_0002_message_format),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString Description = new LocalizableResourceString(
        nameof(Resource.kpte_0002_description),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString ExcessiveTitle = new LocalizableResourceString(
        nameof(Resource.kpte_0003_title),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString ExcessiveMessageFormat =
        new LocalizableResourceString(
            nameof(Resource.kpte_0003_message_format),
            Resource.ResourceManager,
            typeof(Resource)
        );

    private static readonly LocalizableString ExcessiveDescription = new LocalizableResourceString(
        nameof(Resource.kpte_0003_description),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString HighPrecisionTitle = new LocalizableResourceString(
        nameof(Resource.kptr_0002_title),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString HighPrecisionMessageFormat =
        new LocalizableResourceString(
            nameof(Resource.kptr_0002_message_format),
            Resource.ResourceManager,
            typeof(Resource)
        );

    private static readonly LocalizableString HighPrecisionDescription =
        new LocalizableResourceString(
            nameof(Resource.kptr_0002_description),
            Resource.ResourceManager,
            typeof(Resource)
        );

    /// <summary>
    /// precision 小于等于 0 时报告错误。
    /// </summary>
    public static readonly DiagnosticDescriptor Rule = new(
        Id,
        Title,
        MessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description
    );

    /// <summary>
    /// precision 超过运行时上限时报告错误。
    /// </summary>
    public static readonly DiagnosticDescriptor ExcessiveRule = new(
        ExcessiveId,
        ExcessiveTitle,
        ExcessiveMessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: ExcessiveDescription
    );

    /// <summary>
    /// precision 偏高但仍合法时报告性能警告。
    /// </summary>
    public static readonly DiagnosticDescriptor HighPrecisionRule = new(
        HighPrecisionId,
        HighPrecisionTitle,
        HighPrecisionMessageFormat,
        "Performance",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: HighPrecisionDescription
    );
}
