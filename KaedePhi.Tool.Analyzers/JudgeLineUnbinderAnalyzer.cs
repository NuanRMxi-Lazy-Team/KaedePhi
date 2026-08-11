using System.Collections.Immutable;
using KaedePhi.Tool.Analyzers.Analysis;
using KaedePhi.Tool.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers;

/// <summary>
/// 检查父线解绑接口中的百分比容差参数是否超出安全范围。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class JudgeLineUnbinderAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = JudgeLineUnbinderDiagnostic.Id;
    public const string SmallToleranceDiagnosticId = JudgeLineUnbinderDiagnostic.SmallToleranceId;
    public const string ZeroToleranceDiagnosticId = JudgeLineUnbinderDiagnostic.ZeroToleranceId;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [
            JudgeLineUnbinderDiagnostic.Rule,
            JudgeLineUnbinderDiagnostic.SmallToleranceRule,
            JudgeLineUnbinderDiagnostic.ZeroToleranceRule,
        ];

    public override void Initialize(AnalysisContext context)
    {
        // 不分析自动生成的代码，避免误报
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // 启用并发执行以提升构建性能
        context.EnableConcurrentExecution();
        // 仅注册调用操作的分析，将分析范围控制在最小
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        // 非目标调用直接忽略
        if (context.Operation is not IInvocationOperation invocation ||
            !JudgeLineUnbinderApi.TryGetToleranceArguments(invocation, out var arguments))
            return;

        // 零容差诊断仅针对动态解绑方法的 tolerance 实参
        var isDynamicMethod = JudgeLineUnbinderApi.IsDynamicMethod(invocation.TargetMethod);
        foreach (var argument in arguments)
        {
            // 仅分析编译期可确定的常量值
            if (!ConstantExpressionEvaluator.TryGetValue(
                    context.Compilation,
                    argument,
                    context.CancellationToken,
                    out var value) ||
                double.IsNaN(value) ||
                double.IsInfinity(value) ||
                argument.Value.Syntax is not SyntaxNode expression)
                continue;

            // 依次按阈值匹配规则：动态零容差、过大、过小
            var diagnostic = isDynamicMethod &&
                argument.Parameter?.Name == "tolerance" &&
                value == 0.0
                ? JudgeLineUnbinderDiagnostic.ZeroToleranceRule
                : value >= JudgeLineUnbinderTolerance.ErrorThreshold
                ? JudgeLineUnbinderDiagnostic.Rule
                : value > 0.0 && value < JudgeLineUnbinderTolerance.SmallToleranceThreshold
                    ? JudgeLineUnbinderDiagnostic.SmallToleranceRule
                    : null;
            if (diagnostic is null)
                continue;

            context.ReportDiagnostic(
                Diagnostic.Create(
                    diagnostic,
                    expression.GetLocation(),
                    NumericValueFormatter.Format(value)));
        }
    }
}
