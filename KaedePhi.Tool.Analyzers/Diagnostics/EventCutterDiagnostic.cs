using KaedePhi.Tool.Analyzers;
using Microsoft.CodeAnalysis;

namespace KaedePhi.Tool.Analyzers.Diagnostics;

/// <summary>
/// 事件切割相关诊断规则的描述符集合。
/// </summary>
internal static class EventCutterDiagnostic
{
    // 诊断 ID 与资源键一一对应，前缀区分分析器，编号区分规则
    public const string Id = "KPTI0001";
    public const string EqualOneId = "KPTI0002";

    // 标题、消息与描述均来自本地化资源，随资源文件切换语言
    private static readonly LocalizableString Kpa0001Title = new LocalizableResourceString(
        nameof(Resource.kpti_0001_title),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString Kpa0001MessageFormat = new LocalizableResourceString(
        nameof(Resource.kpti_0001_message_format),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString Kpa0001Description = new LocalizableResourceString(
        nameof(Resource.kpti_0001_description),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString Kpa0002Title = new LocalizableResourceString(
        nameof(Resource.kpti_0002_title),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString Kpa0002MessageFormat = new LocalizableResourceString(
        nameof(Resource.kpti_0002_message_format),
        Resource.ResourceManager,
        typeof(Resource)
    );

    private static readonly LocalizableString Kpa0002Description = new LocalizableResourceString(
        nameof(Resource.kpti_0002_description),
        Resource.ResourceManager,
        typeof(Resource)
    );

    /// <summary>
    /// cutLength 大于 1 时报告，疑似忘记取倒数。
    /// </summary>
    public static readonly DiagnosticDescriptor Rule = new(
        Id,
        Kpa0001Title,
        Kpa0001MessageFormat,
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Kpa0001Description
    );

    /// <summary>
    /// cutLength 恰好等于 1 时报告，疑似参数填入错误。
    /// </summary>
    public static readonly DiagnosticDescriptor EqualOneRule = new(
        EqualOneId,
        Kpa0002Title,
        Kpa0002MessageFormat,
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Kpa0002Description
    );
}
