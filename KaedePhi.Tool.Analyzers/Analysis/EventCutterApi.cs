using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers.Analysis;

/// <summary>
/// 事件切割 API 的类型识别与 cutLength 实参检索。
/// </summary>
internal static class EventCutterApi
{
    // 按命名空间与元数据名称识别目标类型，避免依赖具体程序集引用
    private const string EventNamespace = "KaedePhi.Tool.Event";
    private const string ImplementationNamespace = "KaedePhi.Tool.Event.KaedePhi";
    private const string InterfaceMetadataName = "IEventCutter`2";
    private const string ImplementationMetadataName = "EventCutter`1";
    private const string BeatNamespace = "KaedePhi.Core.Common";
    private const string BeatMetadataName = "Beat";
    private const string CutLengthParameterName = "cutLength";

    /// <summary>
    /// 尝试从调用中检索受支持的 cutLength 实参。
    /// </summary>
    /// <param name="invocation">待检查的调用操作</param>
    /// <param name="argument">匹配到的 cutLength 实参</param>
    /// <returns>是否找到受支持的 cutLength 实参</returns>
    public static bool TryGetCutLengthArgument(
        IInvocationOperation invocation,
        out IArgumentOperation argument)
    {
        argument = null!;
        if (!IsSupportedMethod(invocation.TargetMethod))
            return false;

        // 形参名与形参类型双重匹配，避免同名实参误判
        foreach (var candidate in invocation.Arguments.Where(candidate =>
                     candidate.Parameter?.Name == CutLengthParameterName &&
                     IsSupportedCutLengthType(candidate.Parameter.Type)))
        {
            argument = candidate;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 判断类型符号是否为 Beat 类型。
    /// </summary>
    /// <param name="type">待判断的类型符号</param>
    /// <returns>是否为 Beat 类型</returns>
    public static bool IsBeat(ITypeSymbol? type) =>
        // 与原始定义比对，避免泛型实例化带来的符号差异
        type is INamedTypeSymbol namedType &&
        namedType.OriginalDefinition.ContainingNamespace?.ToDisplayString() == BeatNamespace &&
        namedType.OriginalDefinition.MetadataName == BeatMetadataName;

    private static bool IsSupportedMethod(IMethodSymbol method)
    {
        // 仅匹配事件切割接口及实现中的普通切割方法
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

        // 实现类型可能通过间接接口继承事件切割能力，需遍历全部接口
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