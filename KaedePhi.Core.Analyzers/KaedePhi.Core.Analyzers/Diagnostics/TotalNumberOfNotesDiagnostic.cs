using Microsoft.CodeAnalysis;

namespace KaedePhi.Core.Analyzers.Diagnostics;

/// <summary>
/// RePhiEdit 判定线音符总数属性相关诊断规则的描述符集合。
/// </summary>
internal static class TotalNumberOfNotesDiagnostic
{
    // 诊断 ID 与资源键一一对应，前缀区分严重级别，编号区分规则
    public const string Id = "KPCR0006";

    // 标题、消息与描述均来自本地化资源，随资源文件切换语言
    private static readonly LocalizableString Title = new LocalizableResourceString(
        nameof(Resources.kpcr_0006_title),
        Resources.ResourceManager,
        typeof(Resources)
    );

    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
        nameof(Resources.kpcr_0006_message_format),
        Resources.ResourceManager,
        typeof(Resources)
    );

    private static readonly LocalizableString Description = new LocalizableResourceString(
        nameof(Resources.kpcr_0006_description),
        Resources.ResourceManager,
        typeof(Resources)
    );

    /// <summary>
    /// 访问 TotalNumberOfNotes 时报告警告。
    /// </summary>
    public static readonly DiagnosticDescriptor Rule = new(
        Id,
        Title,
        MessageFormat,
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description
    );
}
