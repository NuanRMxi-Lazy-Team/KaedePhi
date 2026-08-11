using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers.Analysis;

/// <summary>
/// 父线解绑 API 的类型识别与容差实参检索。
/// </summary>
internal static class JudgeLineUnbinderApi
{
    // 按命名空间与元数据名称识别目标类型，避免依赖具体程序集引用
    private const string JudgeLineNamespace = "KaedePhi.Tool.JudgeLines";
    private const string ImplementationNamespace = "KaedePhi.Tool.JudgeLines.KaedePhi";
    private const string InterfaceMetadataName = "IJudgeLineUnbinder`1";
    private const string ImplementationMetadataName = "JudgeLineUnbinder";
    private const string ToleranceParameterName = "tolerance";
    private const string MergeToleranceParameterName = "mergeTolerance";

    /// <summary>
    /// 尝试从调用中检索全部受支持的容差实参。
    /// </summary>
    /// <param name="invocation">待检查的调用操作</param>
    /// <param name="arguments">匹配到的容差实参集合</param>
    /// <returns>是否找到至少一个容差实参</returns>
    public static bool TryGetToleranceArguments(
        IInvocationOperation invocation,
        out ImmutableArray<IArgumentOperation> arguments)
    {
        arguments = [];
        if (!IsSupportedMethod(invocation.TargetMethod))
            return false;

        // 仅收集形参名为 tolerance/mergeTolerance 且类型为 double 的实参
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

    /// <summary>
    /// 判断方法是否为动态解绑方法。
    /// </summary>
    /// <param name="method">待判断的方法符号</param>
    /// <returns>是否为解绑接口或实现中的 FatherUnbindDynamic 方法</returns>
    public static bool IsDynamicMethod(IMethodSymbol method) =>
        method.MethodKind == MethodKind.Ordinary &&
        method.Name == "FatherUnbindDynamic" &&
        IsUnbinderType(method.ContainingType);

    private static bool IsSupportedMethod(IMethodSymbol method)
    {
        // 仅匹配解绑接口及实现中的普通解绑与动态解绑方法
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

        // 实现类型可能通过间接接口继承解绑能力，需遍历全部接口
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
