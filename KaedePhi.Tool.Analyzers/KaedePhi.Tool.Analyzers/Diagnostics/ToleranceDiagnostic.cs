using Microsoft.CodeAnalysis;

namespace KaedePhi.Tool.Analyzers.Diagnostics;

/// <summary>
/// 拟合与压缩容差参数相关诊断规则的描述符集合。
/// </summary>
internal static class ToleranceDiagnostic
{
    // 诊断 ID 与资源键一一对应，前缀区分分析器，编号区分规则
    public const string Id = "KPTE0004";
    public const string NegativeId = "KPTE0005";
    public const string ZeroId = "KPTI0004";

    // 标题、消息与描述均来自本地化资源，随资源文件切换语言
    private static readonly LocalizableString Title = new LocalizableResourceString(
        nameof(Resource.kpte_0004_title),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
        nameof(Resource.kpte_0004_message_format),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString Description = new LocalizableResourceString(
        nameof(Resource.kpte_0004_description),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString NegativeTitle = new LocalizableResourceString(
        nameof(Resource.kpte_0005_title),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString NegativeMessageFormat = new LocalizableResourceString(
        nameof(Resource.kpte_0005_message_format),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString NegativeDescription = new LocalizableResourceString(
        nameof(Resource.kpte_0005_description),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString ZeroTitle = new LocalizableResourceString(
        nameof(Resource.kpti_0004_title),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString ZeroMessageFormat = new LocalizableResourceString(
        nameof(Resource.kpti_0004_message_format),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString ZeroDescription = new LocalizableResourceString(
        nameof(Resource.kpti_0004_description),
        Resource.ResourceManager,
        typeof(Resource)
    );

    /// <summary>
    /// tolerance 大于等于 100 时报告错误（百分比语义下失去意义）。
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
    /// tolerance 为负数时报告错误。
    /// </summary>
    public static readonly DiagnosticDescriptor NegativeRule = new(
        NegativeId,
        NegativeTitle,
        NegativeMessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: NegativeDescription
    );

    /// <summary>
    /// tolerance 为 0 时报告提示（拟合或压缩退化为不生效）。
    /// </summary>
    public static readonly DiagnosticDescriptor ZeroRule = new(
        ZeroId,
        ZeroTitle,
        ZeroMessageFormat,
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: ZeroDescription
    );
}
