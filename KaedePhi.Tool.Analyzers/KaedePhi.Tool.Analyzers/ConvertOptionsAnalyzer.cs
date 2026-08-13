using System.Collections.Immutable;
using KaedePhi.Tool.Analyzers.Analysis;
using KaedePhi.Tool.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers;

/// <summary>
/// 检查转换选项数值属性在对象初始化或赋值时的常量越界值。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConvertOptionsAnalyzer : DiagnosticAnalyzer
{
    public const string PositiveDiagnosticId = ConvertOptionsDiagnostic.PositiveId;
    public const string ToleranceDiagnosticId = ConvertOptionsDiagnostic.ToleranceId;
    public const string NonNegativeDiagnosticId = ConvertOptionsDiagnostic.NonNegativeId;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        ConvertOptionsDiagnostic.PositiveRule,
        ConvertOptionsDiagnostic.ToleranceRule,
        ConvertOptionsDiagnostic.NonNegativeRule,
    ];

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
    /// 分析转换选项属性赋值中的常量越界值。
    /// </summary>
    /// <param name="context">操作分析上下文</param>
    private static void AnalyzeAssignment(OperationAnalysisContext context)
    {
        // 非转换选项数值属性的赋值直接忽略
        if (
            context.Operation
                is not ISimpleAssignmentOperation
                {
                    Target: IPropertyReferenceOperation { Property: { } property }
                } assignment
            || !ConvertOptionsApi.IsConvertOptionType(property.ContainingType)
            || assignment.Value.Syntax is not ExpressionSyntax expression
        )
            return;

        var kind = ConvertOptionsApi.ClassifyProperty(property);
        if (kind == ConvertOptionsApi.PropertyKind.None)
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

        // 越界判断：非有限数值一律视为非法，其余按取值约束比较
        var outOfRange = kind switch
        {
            ConvertOptionsApi.PropertyKind.Positive => double.IsNaN(value)
                || double.IsInfinity(value)
                || value <= 0,
            ConvertOptionsApi.PropertyKind.Tolerance => double.IsNaN(value)
                || double.IsInfinity(value)
                || value < 0
                || value > 100,
            ConvertOptionsApi.PropertyKind.NonNegative => double.IsNaN(value)
                || double.IsInfinity(value)
                || value < 0,
            _ => false,
        };
        if (!outOfRange)
            return;

        var diagnostic = kind switch
        {
            ConvertOptionsApi.PropertyKind.Positive => ConvertOptionsDiagnostic.PositiveRule,
            ConvertOptionsApi.PropertyKind.Tolerance => ConvertOptionsDiagnostic.ToleranceRule,
            _ => ConvertOptionsDiagnostic.NonNegativeRule,
        };
        context.ReportDiagnostic(
            Diagnostic.Create(
                diagnostic,
                expression.GetLocation(),
                property.Name,
                NumericValueFormatter.Format(value)
            )
        );
    }
}
