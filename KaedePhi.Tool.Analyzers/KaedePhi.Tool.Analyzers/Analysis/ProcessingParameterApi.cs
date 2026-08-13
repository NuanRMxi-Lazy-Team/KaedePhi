using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers.Analysis;

/// <summary>
/// 识别谱面处理 API 中携带 precision 或 tolerance 实参的调用。
/// <para>
/// 覆盖事件切割、合并、拟合与判定线解绑等公开接口及其实现，
/// 按命名空间与元数据名称识别类型，避免依赖具体程序集引用。
/// </para>
/// </summary>
internal static class ProcessingParameterApi
{
    // 各目标类型的命名空间与元数据名称
    private const string JudgeLineNamespace = "KaedePhi.Tool.JudgeLines";
    private const string JudgeLineImplementationNamespace = "KaedePhi.Tool.JudgeLines.KaedePhi";
    private const string LayerNamespace = "KaedePhi.Tool.Layer";
    private const string LayerImplementationNamespace = "KaedePhi.Tool.Layer.KaedePhi";
    private const string EventNamespace = "KaedePhi.Tool.Event";
    private const string EventImplementationNamespace = "KaedePhi.Tool.Event.KaedePhi";
    private const string JudgeLineUnbinderInterface = "IJudgeLineUnbinder`1";
    private const string JudgeLineUnbinderImplementation = "JudgeLineUnbinder";
    private const string LayerProcessorInterface = "ILayerProcessor`1";
    private const string LayerProcessorImplementation = "LayerProcessor";
    private const string EventListMergerInterface = "IEventListMerger`1";
    private const string EventListMergerImplementation = "EventListMerger`1";
    private const string EventFitInterface = "IEventFit`1";
    private const string EventFitImplementation = "EventFit`1";
    private const string EventCompressorInterface = "IEventCompressor`1";
    private const string EventCompressorImplementation = "EventCompressor`1";

    // 携带 precision 形参的方法规格表
    private static readonly ImmutableArray<MethodSpec> PrecisionMethods = ImmutableArray.Create(
        new MethodSpec(JudgeLineNamespace, JudgeLineUnbinderInterface, "FatherUnbind"),
        new MethodSpec(JudgeLineNamespace, JudgeLineUnbinderInterface, "FatherUnbindDynamic"),
        new MethodSpec(
            JudgeLineImplementationNamespace,
            JudgeLineUnbinderImplementation,
            "FatherUnbind"
        ),
        new MethodSpec(
            JudgeLineImplementationNamespace,
            JudgeLineUnbinderImplementation,
            "FatherUnbindDynamic"
        ),
        new MethodSpec(LayerNamespace, LayerProcessorInterface, "LayerMerge"),
        new MethodSpec(LayerNamespace, LayerProcessorInterface, "LayerMergePlus"),
        new MethodSpec(LayerNamespace, LayerProcessorInterface, "CutLayerEvents"),
        new MethodSpec(LayerImplementationNamespace, LayerProcessorImplementation, "LayerMerge"),
        new MethodSpec(
            LayerImplementationNamespace,
            LayerProcessorImplementation,
            "LayerMergePlus"
        ),
        new MethodSpec(
            LayerImplementationNamespace,
            LayerProcessorImplementation,
            "CutLayerEvents"
        ),
        new MethodSpec(EventNamespace, EventListMergerInterface, "EventListMerge"),
        new MethodSpec(
            EventImplementationNamespace,
            EventListMergerImplementation,
            "EventListMerge"
        )
    );

    // 携带 tolerance 形参的方法规格表。
    // 父线解绑的容差已由 JudgeLineUnbinderAnalyzer 单独处理，此处不重复覆盖。
    private static readonly ImmutableArray<MethodSpec> ToleranceMethods = ImmutableArray.Create(
        new MethodSpec(EventNamespace, EventFitInterface, "FitEvents"),
        new MethodSpec(EventImplementationNamespace, EventFitImplementation, "FitEvents"),
        new MethodSpec(EventNamespace, EventCompressorInterface, "EventListCompressSqrt"),
        new MethodSpec(EventNamespace, EventCompressorInterface, "EventListCompressSlope"),
        new MethodSpec(
            EventImplementationNamespace,
            EventCompressorImplementation,
            "EventListCompressSqrt"
        ),
        new MethodSpec(
            EventImplementationNamespace,
            EventCompressorImplementation,
            "EventListCompressSlope"
        ),
        new MethodSpec(LayerNamespace, LayerProcessorInterface, "LayerMergePlus"),
        new MethodSpec(LayerNamespace, LayerProcessorInterface, "LayerEventsCompress"),
        new MethodSpec(
            LayerImplementationNamespace,
            LayerProcessorImplementation,
            "LayerMergePlus"
        ),
        new MethodSpec(
            LayerImplementationNamespace,
            LayerProcessorImplementation,
            "LayerEventsCompress"
        )
    );

    /// <summary>
    /// 尝试从调用中检索受支持的 precision 实参。
    /// </summary>
    /// <param name="invocation">待检查的调用操作</param>
    /// <param name="argument">匹配到的 precision 实参</param>
    /// <returns>是否找到受支持的 precision 实参</returns>
    public static bool TryGetPrecisionArgument(
        IInvocationOperation invocation,
        out IArgumentOperation argument
    ) => TryGetArgument(invocation, PrecisionMethods, "precision", out argument);

    /// <summary>
    /// 尝试从调用中检索受支持的 tolerance 实参。
    /// </summary>
    /// <param name="invocation">待检查的调用操作</param>
    /// <param name="argument">匹配到的 tolerance 实参</param>
    /// <returns>是否找到受支持的 tolerance 实参</returns>
    public static bool TryGetToleranceArgument(
        IInvocationOperation invocation,
        out IArgumentOperation argument
    ) => TryGetArgument(invocation, ToleranceMethods, "tolerance", out argument);

    /// <summary>
    /// 按形参名与类型在规格表内匹配，并检索目标实参。
    /// </summary>
    /// <param name="invocation">待检查的调用操作</param>
    /// <param name="specs">目标方法规格表</param>
    /// <param name="parameterName">目标形参名</param>
    /// <param name="argument">匹配到的实参</param>
    /// <returns>是否找到匹配实参</returns>
    private static bool TryGetArgument(
        IInvocationOperation invocation,
        ImmutableArray<MethodSpec> specs,
        string parameterName,
        out IArgumentOperation argument
    )
    {
        argument = null!;
        if (!IsTargetMethod(invocation.TargetMethod, specs))
            return false;

        // 形参名与类型双重匹配，避免同名实参误判
        foreach (var candidate in invocation.Arguments)
        {
            if (
                candidate.Parameter?.Name == parameterName
                && candidate.Parameter.Type.SpecialType == SpecialType.System_Double
            )
            {
                argument = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断方法符号是否命中规格表中的任一目标方法。
    /// </summary>
    /// <param name="method">待判断的方法符号</param>
    /// <param name="specs">目标方法规格表</param>
    /// <returns>是否为目标方法</returns>
    private static bool IsTargetMethod(IMethodSymbol method, ImmutableArray<MethodSpec> specs)
    {
        if (method.MethodKind != MethodKind.Ordinary)
            return false;

        foreach (var spec in specs)
        {
            if (method.Name != spec.MethodName)
                continue;
            if (MatchesType(method.ContainingType, spec))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 判断类型符号是否命中规格，含对全部实现接口的匹配。
    /// </summary>
    /// <param name="type">待判断的类型符号</param>
    /// <param name="spec">目标方法规格</param>
    /// <returns>是否命中规格</returns>
    private static bool MatchesType(INamedTypeSymbol? type, MethodSpec spec)
    {
        if (type is null)
            return false;

        // 实现类型可能通过间接接口继承处理能力，需同时匹配声明类型与全部接口
        var definition = type.OriginalDefinition;
        return MatchesDefinition(definition, spec) || MatchesAnyInterface(definition, spec);
    }

    /// <summary>
    /// 遍历类型实现的全部接口，判断其中是否有命中规格的类型。
    /// </summary>
    /// <param name="type">待判断的类型符号</param>
    /// <param name="spec">目标方法规格</param>
    /// <returns>是否有接口命中规格</returns>
    private static bool MatchesAnyInterface(INamedTypeSymbol type, MethodSpec spec)
    {
        foreach (var interfaceType in type.AllInterfaces)
        {
            if (MatchesDefinition(interfaceType.OriginalDefinition, spec))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 判断类型原始定义是否与规格的命名空间与元数据名一致。
    /// </summary>
    /// <param name="type">待判断的类型符号</param>
    /// <param name="spec">目标方法规格</param>
    /// <returns>是否一致</returns>
    private static bool MatchesDefinition(INamedTypeSymbol type, MethodSpec spec) =>
        type.ContainingNamespace?.ToDisplayString() == spec.Namespace
        && type.MetadataName == spec.MetadataName;

    /// <summary>
    /// 描述一个目标方法：所属类型的命名空间与元数据名，以及方法名。
    /// </summary>
    /// <param name="Namespace">类型所在命名空间</param>
    /// <param name="MetadataName">类型的元数据名称（含泛型元数后缀）</param>
    /// <param name="MethodName">方法名</param>
    private readonly struct MethodSpec
    {
        public MethodSpec(string @namespace, string metadataName, string methodName)
        {
            Namespace = @namespace;
            MetadataName = metadataName;
            MethodName = methodName;
        }

        public string Namespace { get; }

        public string MetadataName { get; }

        public string MethodName { get; }
    }
}
