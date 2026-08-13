using System.Collections.Immutable;
using KaedePhi.Tool.Analyzers.Analysis;
using KaedePhi.Tool.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers;

/// <summary>
/// 检查 KpcRenderOptions 数值属性在对象初始化或赋值时的常量越界值。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RenderOptionsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = RenderOptionsDiagnostic.Id;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [RenderOptionsDiagnostic.Rule];

    public override void Initialize(AnalysisContext context)
    {
        // 不分析自动生成的代码，避免误报
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // 启用并发执行以提升构建性能
        context.EnableConcurrentExecution();
        // 简单赋值同时覆盖对象初始化器赋值与普通赋值语句
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
    }

    /// <summary>
    /// 分析渲染配置属性赋值中的常量越界值。
    /// </summary>
    /// <param name="context">操作分析上下文</param>
    private static void AnalyzeAssignment(OperationAnalysisContext context)
    {
        // 非渲染配置数值属性的赋值直接忽略
        if (
            context.Operation
                is not ISimpleAssignmentOperation
                {
                    Target: IPropertyReferenceOperation { Property: { } property }
                } assignment
            || !RenderOptionsApi.TryGetBound(property, out var bound)
            || assignment.Value.Syntax is not ExpressionSyntax expression
        )
            return;

        // 仅报告编译期可确定的常量值
        if (
            context.Operation.SemanticModel is not { } semanticModel
            || !NumericConstantReader.TryGetDouble(
                semanticModel,
                expression,
                context.CancellationToken,
                out var value
            )
        )
            return;

        // 非有限数值一律视为非法，其余按区间的上下限比较
        var outOfRange =
            double.IsNaN(value)
            || double.IsInfinity(value)
            || (bound.MinExclusive ? value <= bound.Min : value < bound.Min)
            || (bound.Max != double.MaxValue && value > bound.Max);
        if (!outOfRange)
            return;

        context.ReportDiagnostic(
            Diagnostic.Create(
                RenderOptionsDiagnostic.Rule,
                expression.GetLocation(),
                property.Name,
                NumericValueFormatter.Format(value),
                bound.Display
            )
        );
    }
}
