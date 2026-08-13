using System.Collections.Immutable;
using KaedePhi.Tool.Analyzers.Analysis;
using KaedePhi.Tool.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers;

/// <summary>
/// 检查拟合与压缩 API 的容差参数是否越界或退化为不生效。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ToleranceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = ToleranceDiagnostic.Id;
    public const string NegativeToleranceDiagnosticId = ToleranceDiagnostic.NegativeId;
    public const string ZeroToleranceDiagnosticId = ToleranceDiagnostic.ZeroId;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [ToleranceDiagnostic.Rule, ToleranceDiagnostic.NegativeRule, ToleranceDiagnostic.ZeroRule];

    public override void Initialize(AnalysisContext context)
    {
        // 不分析自动生成的代码，避免误报
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // 启用并发执行以提升构建性能
        context.EnableConcurrentExecution();
        // 仅注册调用操作的分析，将分析范围控制在最小
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    /// <summary>
    /// 分析目标调用中的 tolerance 常量值，按阈值报告相应诊断。
    /// </summary>
    /// <param name="context">操作分析上下文</param>
    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        // 非目标调用直接忽略
        if (
            context.Operation is not IInvocationOperation invocation
            || !ProcessingParameterApi.TryGetToleranceArgument(invocation, out var argument)
        )
            return;

        // 仅报告编译期可确定的常量值
        if (
            !ConstantExpressionEvaluator.TryGetValue(
                context.Compilation,
                argument,
                context.CancellationToken,
                out var value
            )
            || double.IsNaN(value)
            || double.IsInfinity(value)
            || argument.Value.Syntax is not SyntaxNode expression
        )
            return;

        // 依次按阈值匹配规则：过大、为负、为零退化
        var diagnostic =
            value >= 100 ? ToleranceDiagnostic.Rule
            : value < 0 ? ToleranceDiagnostic.NegativeRule
            : value == 0 ? ToleranceDiagnostic.ZeroRule
            : null;
        if (diagnostic is null)
            return;

        context.ReportDiagnostic(
            Diagnostic.Create(
                diagnostic,
                expression.GetLocation(),
                NumericValueFormatter.Format(value)
            )
        );
    }
}
