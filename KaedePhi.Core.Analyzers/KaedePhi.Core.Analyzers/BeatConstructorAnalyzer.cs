using System.Collections.Immutable;
using KaedePhi.Core.Analyzers.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Core.Analyzers;

/// <summary>
/// 检查 Beat 构造参数是否在编译期就能确定的非法值。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BeatConstructorAnalyzer : DiagnosticAnalyzer
{
    public const string LengthDiagnosticId = "KPCE0002";
    public const string DenominatorZeroDiagnosticId = "KPCE0003";
    public const string DenominatorNegativeDiagnosticId = "KPCE0004";
    public const string NonFiniteDiagnosticId = "KPCE0005";

    // 标题、消息与描述均来自本地化资源
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
    private static readonly DiagnosticDescriptor LengthRule = new(
        LengthDiagnosticId,
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
    private static readonly DiagnosticDescriptor DenominatorZeroRule = new(
        DenominatorZeroDiagnosticId,
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
    private static readonly DiagnosticDescriptor DenominatorNegativeRule = new(
        DenominatorNegativeDiagnosticId,
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
    private static readonly DiagnosticDescriptor NonFiniteRule = new(
        NonFiniteDiagnosticId,
        NonFiniteTitle,
        NonFiniteMessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: NonFiniteDescription
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [LengthRule, DenominatorZeroRule, DenominatorNegativeRule, NonFiniteRule];

    public override void Initialize(AnalysisContext context)
    {
        // 不分析自动生成的代码，避免误报
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // 启用并发执行以提升构建性能
        context.EnableConcurrentExecution();
        // 仅注册对象创建操作的分析，将分析范围控制在最小
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
    }

    /// <summary>
    /// 分析 Beat 构造操作，按构造形参类型分发到对应的校验分支。
    /// </summary>
    /// <param name="context">操作分析上下文</param>
    private static void AnalyzeObjectCreation(OperationAnalysisContext context)
    {
        // 非 Beat 构造或参数数量不符的创建直接忽略
        if (
            context.Operation is not IObjectCreationOperation creation
            || !BeatConstructorApi.IsBeatType(creation.Type)
            || creation.Arguments.Length != 1
            || creation.Arguments[0] is not { Parameter: { } parameter } argument
            || argument.Value.Syntax is not ExpressionSyntax expression
        )
            return;

        var semanticModel = context.Operation.SemanticModel;
        if (semanticModel is null)
            return;

        // 按构造形参类型分发：int[] 构造检查数组元素，double 构造检查数值有限性
        if (BeatConstructorApi.IsIntArrayParameter(parameter))
            AnalyzeIntArrayConstructor(context, semanticModel, expression);
        else if (BeatConstructorApi.IsDoubleParameter(parameter))
            AnalyzeDoubleConstructor(context, semanticModel, expression);
    }

    /// <summary>
    /// 校验 int[] 构造的数组长度与分母元素。
    /// </summary>
    /// <param name="context">操作分析上下文</param>
    /// <param name="semanticModel">构造表达式所在语义模型</param>
    /// <param name="expression">数组构造实参表达式</param>
    private static void AnalyzeIntArrayConstructor(
        OperationAnalysisContext context,
        SemanticModel semanticModel,
        ExpressionSyntax expression
    )
    {
        // 数组元素存在非常量时无法静态判断，直接忽略
        if (
            !BeatConstructorApi.TryGetArrayElements(
                semanticModel,
                expression,
                context.CancellationToken,
                out var elements
            )
        )
            return;

        if (elements.Length != 3)
        {
            // 长度不是 3：运行时必然抛出 ArgumentException
            context.ReportDiagnostic(
                Diagnostic.Create(
                    LengthRule,
                    expression.GetLocation(),
                    NumericValueFormatter.Format(elements.Length)
                )
            );
            return;
        }

        var denominator = elements[2];
        // 分母校验仅指向数组中具体的分母元素，便于定位
        if (denominator.Value == 0)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(DenominatorZeroRule, denominator.Expression.GetLocation())
            );
        }
        else if (denominator.Value < 0)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DenominatorNegativeRule,
                    denominator.Expression.GetLocation(),
                    NumericValueFormatter.Format(denominator.Value)
                )
            );
        }
    }

    /// <summary>
    /// 校验 double 构造参数的数值有限性。
    /// </summary>
    /// <param name="context">操作分析上下文</param>
    /// <param name="semanticModel">构造表达式所在语义模型</param>
    /// <param name="expression">数值构造实参表达式</param>
    private static void AnalyzeDoubleConstructor(
        OperationAnalysisContext context,
        SemanticModel semanticModel,
        ExpressionSyntax expression
    )
    {
        // 仅报告编译期可确定的非有限常量值
        if (
            !NumericConstantReader.TryGetDouble(
                semanticModel,
                expression,
                context.CancellationToken,
                out var value
            ) || (!double.IsNaN(value) && !double.IsInfinity(value))
        )
            return;

        context.ReportDiagnostic(Diagnostic.Create(NonFiniteRule, expression.GetLocation()));
    }
}
