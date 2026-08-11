using KaedePhi.Tool.Analyzers;
using Microsoft.CodeAnalysis;

namespace KaedePhi.Tool.Analyzers.Diagnostics;

/// <summary>
/// 父线解绑相关诊断规则的描述符集合。
/// </summary>
internal static class JudgeLineUnbinderDiagnostic
{
    // 诊断 ID 与资源键一一对应，前缀区分分析器，编号区分规则
    public const string Id = "KPTE0001";
    public const string SmallToleranceId = "KPTI0003";
    public const string ZeroToleranceId = "KPTR0001";

    // 标题、消息与描述均来自本地化资源，随资源文件切换语言
    private static readonly LocalizableString Title = new LocalizableResourceString(
        nameof(Resource.kpte_0001_title),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
        nameof(Resource.kpte_0001_message_format),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString Description = new LocalizableResourceString(
        nameof(Resource.kpte_0001_description),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString SmallToleranceTitle = new LocalizableResourceString(
        nameof(Resource.kpti_0003_title),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString SmallToleranceMessageFormat = new LocalizableResourceString(
        nameof(Resource.kpti_0003_message_format),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString SmallToleranceDescription = new LocalizableResourceString(
        nameof(Resource.kpti_0003_description),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString ZeroToleranceTitle = new LocalizableResourceString(
        nameof(Resource.kptr_0001_title),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString ZeroToleranceMessageFormat = new LocalizableResourceString(
        nameof(Resource.kptr_0001_message_format),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString ZeroToleranceDescription = new LocalizableResourceString(
        nameof(Resource.kptr_0001_description),
        Resource.ResourceManager,
        typeof(Resource));

    /// <summary>
    /// 容差大于等于阈值（过大）时报告错误。
    /// </summary>
    public static readonly DiagnosticDescriptor Rule = new(
        Id,
        Title,
        MessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    /// <summary>
    /// 容差过小时报告提示。
    /// </summary>
    public static readonly DiagnosticDescriptor SmallToleranceRule = new(
        SmallToleranceId,
        SmallToleranceTitle,
        SmallToleranceMessageFormat,
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: SmallToleranceDescription);

    /// <summary>
    /// 动态解绑容差为 0 时报告性能警告。
    /// </summary>
    public static readonly DiagnosticDescriptor ZeroToleranceRule = new(
        ZeroToleranceId,
        ZeroToleranceTitle,
        ZeroToleranceMessageFormat,
        "Performance",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ZeroToleranceDescription);
}
