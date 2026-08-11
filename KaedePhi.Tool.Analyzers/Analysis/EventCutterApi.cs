using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers.Analysis;

internal static class EventCutterApi
{
    private const string EventNamespace = "KaedePhi.Tool.Event";
    private const string ImplementationNamespace = "KaedePhi.Tool.Event.KaedePhi";
    private const string InterfaceMetadataName = "IEventCutter`2";
    private const string ImplementationMetadataName = "EventCutter`1";
    private const string BeatNamespace = "KaedePhi.Core.Common";
    private const string BeatMetadataName = "Beat";
    private const string CutLengthParameterName = "cutLength";

    public static bool TryGetCutLengthArgument(
        IInvocationOperation invocation,
        out IArgumentOperation argument)
    {
        argument = null!;
        if (!IsSupportedMethod(invocation.TargetMethod))
            return false;

        foreach (var candidate in invocation.Arguments.Where(candidate =>
                     candidate.Parameter?.Name == CutLengthParameterName &&
                     IsSupportedCutLengthType(candidate.Parameter.Type)))
        {
            argument = candidate;
            return true;
        }

        return false;
    }

    public static bool IsBeat(ITypeSymbol? type) =>
        type is INamedTypeSymbol namedType &&
        namedType.OriginalDefinition.ContainingNamespace?.ToDisplayString() == BeatNamespace &&
        namedType.OriginalDefinition.MetadataName == BeatMetadataName;

    private static bool IsSupportedMethod(IMethodSymbol method)
    {
        if (method.MethodKind != MethodKind.Ordinary ||
            method.Name is not ("CutEventToLinear" or "CutEventsInRange"))
            return false;

        return IsEventCutterType(method.ContainingType);
    }

    private static bool IsSupportedCutLengthType(ITypeSymbol type) =>
        type.SpecialType == SpecialType.System_Double || IsBeat(type);

    private static bool IsEventCutterType(INamedTypeSymbol? type)
    {
        if (type is null)
            return false;

        var definition = type.OriginalDefinition;
        if (IsEventCutterInterface(definition) || IsEventCutterImplementation(definition))
            return true;

        foreach (var interfaceType in type.AllInterfaces)
        {
            if (IsEventCutterInterface(interfaceType.OriginalDefinition))
                return true;
        }

        return false;
    }

    private static bool IsEventCutterInterface(INamedTypeSymbol type) =>
        type.ContainingNamespace?.ToDisplayString() == EventNamespace &&
        type.MetadataName == InterfaceMetadataName;

    private static bool IsEventCutterImplementation(INamedTypeSymbol type) =>
        type.ContainingNamespace?.ToDisplayString() == ImplementationNamespace &&
        type.MetadataName == ImplementationMetadataName;
}