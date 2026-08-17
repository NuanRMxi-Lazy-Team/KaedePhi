using System.Collections.Immutable;
using KaedePhi.Core.Analyzers.Analysis;
using KaedePhi.Core.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Core.Analyzers;

/// <summary>
/// 检查各格式缓动对象的创建，缓动编号超出对应格式的有效范围时报告错误。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EasingNumberAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = EasingNumberDiagnostic.Id;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [EasingNumberDiagnostic.Rule];

    public override void Initialize(AnalysisContext context)
    {
        // 不分析自动生成的代码，避免误报
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // 启用并发执行以提升构建性能
        context.EnableConcurrentExecution();

        // 对象创建（含 new(...) 目标类型推断形式）都会产生 ObjectCreation 操作
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context)
    {
        // 仅处理注册表中已知的缓动类型，且构造参数恰好为一个
        if (
            context.Operation is not IObjectCreationOperation creation
            || creation.Type is not INamedTypeSymbol type
            || !EasingFormatRegistry.TryGetRange(type, out var range)
            || creation.Arguments.Length != 1
        )
            return;

        // 只检查编译期可确定的缓动编号，动态值无法在编译期判定。
        if (
            creation.Arguments[0].Value.ConstantValue
            is not { HasValue: true, Value: int easingNumber }
        )
            return;

        // 编号位于有效范围内则无需诊断
        if (easingNumber >= range.Min && easingNumber <= range.Max)
            return;

        var location = creation.Arguments[0].Value.Syntax.GetLocation();
        context.ReportDiagnostic(
            Diagnostic.Create(
                EasingNumberDiagnostic.Rule,
                location,
                easingNumber,
                range.DisplayName,
                range.Min,
                range.Max
            )
        );
    }
}
