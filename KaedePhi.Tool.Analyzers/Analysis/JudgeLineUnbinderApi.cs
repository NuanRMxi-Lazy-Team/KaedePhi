using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers.Analysis;

internal static class JudgeLineUnbinderApi
{
    private const string JudgeLineNamespace = "KaedePhi.Tool.JudgeLines";
    private const string ImplementationNamespace = "KaedePhi.Tool.JudgeLines.KaedePhi";
    private const string InterfaceMetadataName = "IJudgeLineUnbinder`1";
    private const string ImplementationMetadataName = "JudgeLineUnbinder";
    private const string ToleranceParameterName = "tolerance";
    private const string MergeToleranceParameterName = "mergeTolerance";

    public static bool TryGetToleranceArguments(
        IInvocationOperation invocation,
        out ImmutableArray<IArgumentOperation> arguments)
    {
        arguments = [];
        if (!IsSupportedMethod(invocation.TargetMethod))
            return false;

        var builder = ImmutableArray.CreateBuilder<IArgumentOperation>();
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter is not { } parameter ||
                !IsToleranceParameter(parameter) ||
                parameter.Type.SpecialType != SpecialType.System_Double)
                continue;

            builder.Add(argument);
        }

        arguments = builder.ToImmutable();
        return !arguments.IsDefaultOrEmpty;
    }

    public static bool IsDynamicMethod(IMethodSymbol method) =>
        method.MethodKind == MethodKind.Ordinary &&
        method.Name == "FatherUnbindDynamic" &&
        IsUnbinderType(method.ContainingType);

    private static bool IsSupportedMethod(IMethodSymbol method)
    {
        if (method.MethodKind != MethodKind.Ordinary ||
            method.Name is not ("FatherUnbind" or "FatherUnbindDynamic"))
            return false;

        return IsUnbinderType(method.ContainingType);
    }

    private static bool IsToleranceParameter(IParameterSymbol parameter) =>
        parameter.Name is ToleranceParameterName or MergeToleranceParameterName;

    private static bool IsUnbinderType(INamedTypeSymbol? type)
    {
        if (type is null)
            return false;

        var definition = type.OriginalDefinition;
        if (IsUnbinderInterface(definition) || IsUnbinderImplementation(definition))
            return true;

        foreach (var interfaceType in type.AllInterfaces)
        {
            if (IsUnbinderInterface(interfaceType.OriginalDefinition))
                return true;
        }

        return false;
    }

    private static bool IsUnbinderInterface(INamedTypeSymbol type) =>
        type.ContainingNamespace?.ToDisplayString() == JudgeLineNamespace &&
        type.MetadataName == InterfaceMetadataName;

    private static bool IsUnbinderImplementation(INamedTypeSymbol type) =>
        type.ContainingNamespace?.ToDisplayString() == ImplementationNamespace &&
        type.MetadataName == ImplementationMetadataName;
}
