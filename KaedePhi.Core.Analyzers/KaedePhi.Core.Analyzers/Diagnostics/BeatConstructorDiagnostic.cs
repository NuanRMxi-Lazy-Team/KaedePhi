using Microsoft.CodeAnalysis;

namespace KaedePhi.Core.Analyzers.Diagnostics;

/// <summary>
/// Beat 构造相关诊断规则的描述符集合。
/// </summary>
internal static class BeatConstructorDiagnostic
{
    // 诊断 ID 与资源键一一对应，前缀区分严重级别，编号区分规则
    public const string LengthId = "KPCE0002";
    public const string DenominatorZeroId = "KPCE0003";
    public const string DenominatorNegativeId = "KPCE0004";
    public const string NonFiniteId = "KPCE0005";

    // 标题、消息与描述均来自本地化资源，随资源文件切换语言
    private static readonly LocalizableString LengthTitle = new LocalizableResourceString(
        nameof(Resources.kpce_0002_title),
        Resources.ResourceManager,
        typeof(Resources)
    );

    private static readonly LocalizableString LengthMessageFormat = new LocalizableResourceString(
        nameof(Resources.kpce_0002_message_format),
        Resources.ResourceManager,
        typeof(Resources)
    );

    private static readonly LocalizableString LengthDescription = new LocalizableResourceString(
        nameof(Resources.kpce_0002_description),
        Resources.ResourceManager,
        typeof(Resources)
    );

    private static readonly LocalizableString DenominatorZeroTitle = new LocalizableResourceString(
        nameof(Resources.kpce_0003_title),
        Resources.ResourceManager,
        typeof(Resources)
    );

    private static readonly LocalizableString DenominatorZeroMessageFormat =
        new LocalizableResourceString(
            nameof(Resources.kpce_0003_message_format),
            Resources.ResourceManager,
            typeof(Resources)
        );

    private static readonly LocalizableString DenominatorZeroDescription =
        new LocalizableResourceString(
            nameof(Resources.kpce_0003_description),
            Resources.ResourceManager,
            typeof(Resources)
        );

    private static readonly LocalizableString DenominatorNegativeTitle =
        new LocalizableResourceString(
            nameof(Resources.kpce_0004_title),
            Resources.ResourceManager,
            typeof(Resources)
        );

    private static readonly LocalizableString DenominatorNegativeMessageFormat =
        new LocalizableResourceString(
            nameof(Resources.kpce_0004_message_format),
            Resources.ResourceManager,
            typeof(Resources)
        );

    private static readonly LocalizableString DenominatorNegativeDescription =
        new LocalizableResourceString(
            nameof(Resources.kpce_0004_description),
            Resources.ResourceManager,
            typeof(Resources)
        );

    private static readonly LocalizableString NonFiniteTitle = new LocalizableResourceString(
        nameof(Resources.kpce_0005_title),
        Resources.ResourceManager,
        typeof(Resources)
    );

    private static readonly LocalizableString NonFiniteMessageFormat =
        new LocalizableResourceString(
            nameof(Resources.kpce_0005_message_format),
            Resources.ResourceManager,
            typeof(Resources)
        );

    private static readonly LocalizableString NonFiniteDescription = new LocalizableResourceString(
        nameof(Resources.kpce_0005_description),
        Resources.ResourceManager,
        typeof(Resources)
    );

    /// <summary>
    /// Beat 数组长度不是 3 时报告错误。
    /// </summary>
    public static readonly DiagnosticDescriptor LengthRule = new(
        LengthId,
        LengthTitle,
        LengthMessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: LengthDescription
    );

    /// <summary>
    /// Beat 数组分母为 0 时报告错误。
    /// </summary>
    public static readonly DiagnosticDescriptor DenominatorZeroRule = new(
        DenominatorZeroId,
        DenominatorZeroTitle,
        DenominatorZeroMessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: DenominatorZeroDescription
    );

    /// <summary>
    /// Beat 数组分母为负数时报告错误。
    /// </summary>
    public static readonly DiagnosticDescriptor DenominatorNegativeRule = new(
        DenominatorNegativeId,
        DenominatorNegativeTitle,
        DenominatorNegativeMessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: DenominatorNegativeDescription
    );

    /// <summary>
    /// Beat 构造参数非有限数值时报告错误。
    /// </summary>
    public static readonly DiagnosticDescriptor NonFiniteRule = new(
        NonFiniteId,
        NonFiniteTitle,
        NonFiniteMessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: NonFiniteDescription
    );
}
