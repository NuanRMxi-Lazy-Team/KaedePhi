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
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation ||
            !JudgeLineUnbinderApi.TryGetToleranceArguments(invocation, out var arguments))
            return;

        var isDynamicMethod = JudgeLineUnbinderApi.IsDynamicMethod(invocation.TargetMethod);
        foreach (var argument in arguments)
        {
            if (!ConstantExpressionEvaluator.TryGetValue(
                    context.Compilation,
                    argument,
                    context.CancellationToken,
                    out var value) ||
                double.IsNaN(value) ||
                double.IsInfinity(value) ||
                argument.Value.Syntax is not SyntaxNode expression)
                continue;

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
