using Microsoft.CodeAnalysis;

namespace KaedePhi.Core.Analyzers.Diagnostics;

/// <summary>
/// 缓动编号相关诊断规则的描述符集合。
/// </summary>
internal static class EasingNumberDiagnostic
{
    // 诊断 ID 与资源键一一对应，前缀区分严重级别，编号区分规则
    public const string Id = "KPCE0001";

    // 标题、消息与描述均来自本地化资源，随资源文件切换语言
    private static readonly LocalizableString Title = new LocalizableResourceString(
        nameof(Resources.kpce_0001_title),
        Resources.ResourceManager,
        typeof(Resources)
    );

    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
        nameof(Resources.kpce_0001_message_format),
        Resources.ResourceManager,
        typeof(Resources)
    );

    private static readonly LocalizableString Description = new LocalizableResourceString(
        nameof(Resources.kpce_0001_description),
        Resources.ResourceManager,
        typeof(Resources)
    );

    /// <summary>
    /// 缓动编号超出格式有效范围时报告错误。
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
}
