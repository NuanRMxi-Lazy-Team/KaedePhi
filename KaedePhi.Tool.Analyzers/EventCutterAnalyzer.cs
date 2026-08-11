using System.Collections.Immutable;
using KaedePhi.Tool.Analyzers.Analysis;
using KaedePhi.Tool.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers;

/// <summary>
/// 检查事件切割 API 的切割长度是否疑似忘记取倒数。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventCutterAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = EventCutterDiagnostic.Id;
    public const string EqualOneDiagnosticId = EventCutterDiagnostic.EqualOneId;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [EventCutterDiagnostic.Rule, EventCutterDiagnostic.EqualOneRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation ||
            !EventCutterApi.TryGetCutLengthArgument(invocation, out var argument))
            return;

        if (!ConstantExpressionEvaluator.TryGetValue(
                context.Compilation,
                argument,
                context.CancellationToken,
                out var value) ||
            double.IsNaN(value) ||
            double.IsInfinity(value) ||
            value < 1.0)
            return;

        var expression = argument.Value.Syntax;
        if (expression is null)
            return;

        var display = NumericValueFormatter.Format(value);
        var rule = value == 1.0
            ? EventCutterDiagnostic.EqualOneRule
            : EventCutterDiagnostic.Rule;
        context.ReportDiagnostic(
            Diagnostic.Create(rule, expression.GetLocation(), display));
    }
}
