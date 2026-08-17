using System.Collections.Immutable;
using KaedePhi.Core.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Core.Analyzers;

/// <summary>
/// 检查 RePhiEdit 判定线音符总数属性的访问，该值遵循 RePhiEdit 规范而非真实数量。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TotalNumberOfNotesAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = TotalNumberOfNotesDiagnostic.Id;

    private const string JudgeLineNamespace = "KaedePhi.Core.RePhiEdit";
    private const string JudgeLineMetadataName = "JudgeLine";
    private const string PropertyMetadataName = "TotalNumberOfNotes";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [TotalNumberOfNotesDiagnostic.Rule];

    public override void Initialize(AnalysisContext context)
    {
        // 不分析自动生成的代码，避免误报
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // 启用并发执行以提升构建性能
        context.EnableConcurrentExecution();
        // 仅注册属性访问操作的分析，将分析范围控制在最小
        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
    }

    /// <summary>
    /// 分析 TotalNumberOfNotes 属性访问并报告警告。
    /// </summary>
    /// <param name="context">操作分析上下文</param>
    private static void AnalyzePropertyReference(OperationAnalysisContext context)
    {
        // 非目标属性的访问直接忽略
        if (
            context.Operation is not IPropertyReferenceOperation { Property: { } property }
            || !IsTargetProperty(property)
        )
            return;

        context.ReportDiagnostic(
            Diagnostic.Create(TotalNumberOfNotesDiagnostic.Rule, context.Operation.Syntax.GetLocation())
        );
    }

    /// <summary>
    /// 判断属性符号是否为 RePhiEdit 判定线的音符总数属性。
    /// </summary>
    /// <param name="property">待判断的属性符号</param>
    /// <returns>是否为目标属性</returns>
    private static bool IsTargetProperty(IPropertySymbol property) =>
        property.MetadataName == PropertyMetadataName
        && property.ContainingType is { } type
        && type.OriginalDefinition.MetadataName == JudgeLineMetadataName
        && type.OriginalDefinition.ContainingNamespace?.ToDisplayString() == JudgeLineNamespace;
}
