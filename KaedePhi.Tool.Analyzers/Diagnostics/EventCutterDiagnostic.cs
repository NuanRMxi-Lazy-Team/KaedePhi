using KaedePhi.Tool.Analyzers;
using Microsoft.CodeAnalysis;

namespace KaedePhi.Tool.Analyzers.Diagnostics;

internal static class EventCutterDiagnostic
{
    public const string Id = "KPTI0001";
    public const string EqualOneId = "KPTI0002";

    private static readonly LocalizableString Kpa0001Title = new LocalizableResourceString(
        nameof(Resource.KPTI0001Title),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString Kpa0001MessageFormat = new LocalizableResourceString(
        nameof(Resource.KPTI0001MessageFormat),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString Kpa0001Description = new LocalizableResourceString(
        nameof(Resource.KPTI0001Description),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString Kpa0002Title = new LocalizableResourceString(
        nameof(Resource.KPTI0002Title),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString Kpa0002MessageFormat = new LocalizableResourceString(
        nameof(Resource.KPTI0002MessageFormat),
        Resource.ResourceManager,
        typeof(Resource));

    private static readonly LocalizableString Kpa0002Description = new LocalizableResourceString(
        nameof(Resource.KPTI0002Description),
        Resource.ResourceManager,
        typeof(Resource));

    public static readonly DiagnosticDescriptor Rule = new(
        Id,
        Kpa0001Title,
        Kpa0001MessageFormat,
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Kpa0001Description);

    public static readonly DiagnosticDescriptor EqualOneRule = new(
        EqualOneId,
        Kpa0002Title,
        Kpa0002MessageFormat,
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Kpa0002Description);
}
