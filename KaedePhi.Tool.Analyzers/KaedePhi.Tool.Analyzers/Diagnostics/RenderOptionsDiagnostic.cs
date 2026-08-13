using Microsoft.CodeAnalysis;

namespace KaedePhi.Tool.Analyzers.Diagnostics;

/// <summary>
/// KpcRenderOptions 数值属性相关诊断规则的描述符集合。
/// </summary>
internal static class RenderOptionsDiagnostic
{
    // 诊断 ID 与资源键一一对应
    public const string Id = "KPTE0013";

    // 标题、消息与描述均来自本地化资源，随资源文件切换语言
    private static readonly LocalizableString Title = new LocalizableResourceString(
        nameof(Resource.kpte_0013_title),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
        nameof(Resource.kpte_0013_message_format),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString Description = new LocalizableResourceString(
        nameof(Resource.kpte_0013_description),
        Resource.ResourceManager,
        typeof(Resource)
    );

    /// <summary>
    /// 渲染配置数值属性越界时报告错误。
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
