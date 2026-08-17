using System.Collections.Immutable;
using KaedePhi.Core.Analyzers.Analysis;
using KaedePhi.Core.Analyzers.Diagnostics;
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
    public const string LengthDiagnosticId = BeatConstructorDiagnostic.LengthId;
    public const string DenominatorZeroDiagnosticId = BeatConstructorDiagnostic.DenominatorZeroId;
    public const string DenominatorNegativeDiagnosticId =
        BeatConstructorDiagnostic.DenominatorNegativeId;
    public const string NonFiniteDiagnosticId = BeatConstructorDiagnostic.NonFiniteId;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        BeatConstructorDiagnostic.LengthRule,
        BeatConstructorDiagnostic.DenominatorZeroRule,
        BeatConstructorDiagnostic.DenominatorNegativeRule,
        BeatConstructorDiagnostic.NonFiniteRule,
    ];

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
                    BeatConstructorDiagnostic.LengthRule,
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
                Diagnostic.Create(
                    BeatConstructorDiagnostic.DenominatorZeroRule,
                    denominator.Expression.GetLocation()
                )
            );
        }
        else if (denominator.Value < 0)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    BeatConstructorDiagnostic.DenominatorNegativeRule,
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

        context.ReportDiagnostic(
            Diagnostic.Create(BeatConstructorDiagnostic.NonFiniteRule, expression.GetLocation())
        );
    }
}
