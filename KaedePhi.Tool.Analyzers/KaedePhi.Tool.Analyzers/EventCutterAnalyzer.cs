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
        if (
            context.Operation is not IInvocationOperation invocation
            || !EventCutterApi.TryGetCutLengthArgument(invocation, out var argument)
        )
            return;

        // 仅报告编译期可确定且疑似忘记取倒数（大于等于 1）的常量值
        if (
            !ConstantExpressionEvaluator.TryGetValue(
                context.Compilation,
                argument,
                context.CancellationToken,
                out var value
            )
            || double.IsNaN(value)
            || double.IsInfinity(value)
            || value < 1.0
        )
            return;

        var expression = argument.Value.Syntax;
        if (expression is null)
            return;

        var display = NumericValueFormatter.Format(value);
        // 恰好等于 1 使用专门规则，其余过大值使用默认规则
        var rule = value == 1.0 ? EventCutterDiagnostic.EqualOneRule : EventCutterDiagnostic.Rule;
        context.ReportDiagnostic(Diagnostic.Create(rule, expression.GetLocation(), display));
    }
}
