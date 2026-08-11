using KaedePhi.Tool.Analyzers;
using Microsoft.CodeAnalysis;

namespace KaedePhi.Tool.Analyzers.Diagnostics;

internal static class JudgeLineUnbinderDiagnostic
{
    public const string Id = "KPTE0001";
    public const string SmallToleranceId = "KPTI0003";
    public const string ZeroToleranceId = "KPTR0001";

    private static readonly LocalizableString Title = new LocalizableResourceString(
        nameof(Resource.KPTE0001Title),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
        nameof(Resource.KPTE0001MessageFormat),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString Description = new LocalizableResourceString(
        nameof(Resource.KPTE0001Description),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString SmallToleranceTitle = new LocalizableResourceString(
        nameof(Resource.KPTI0003Title),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString SmallToleranceMessageFormat = new LocalizableResourceString(
        nameof(Resource.KPTI0003MessageFormat),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString SmallToleranceDescription = new LocalizableResourceString(
        nameof(Resource.KPTI0003Description),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString ZeroToleranceTitle = new LocalizableResourceString(
        nameof(Resource.KPTR0001Title),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString ZeroToleranceMessageFormat = new LocalizableResourceString(
        nameof(Resource.KPTR0001MessageFormat),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString ZeroToleranceDescription = new LocalizableResourceString(
        nameof(Resource.KPTR0001Description),
        Resource.ResourceManager,
        typeof(Resource));

    public static readonly DiagnosticDescriptor Rule = new(
        Id,
        Title,
        MessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    public static readonly DiagnosticDescriptor SmallToleranceRule = new(
        SmallToleranceId,
        SmallToleranceTitle,
        SmallToleranceMessageFormat,
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: SmallToleranceDescription);

    public static readonly DiagnosticDescriptor ZeroToleranceRule = new(
        ZeroToleranceId,
        ZeroToleranceTitle,
        ZeroToleranceMessageFormat,
        "Performance",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: ZeroToleranceDescription);
}
