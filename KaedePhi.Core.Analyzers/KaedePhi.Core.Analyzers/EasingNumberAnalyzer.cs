using System.Collections.Immutable;
using KaedePhi.Core.Analyzers.Analysis;
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
    public const string DiagnosticId = "KPCE0001";

    // 标题、消息与描述均来自本地化资源，随资源文件切换语言
    private static readonly LocalizableString Title =
        new LocalizableResourceString(nameof(Resources.kpce_0001_title), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString MessageFormat =
        new LocalizableResourceString(nameof(Resources.kpce_0001_message_format), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString Description =
        new LocalizableResourceString(nameof(Resources.kpce_0001_description), Resources.ResourceManager, typeof(Resources));

    /// <summary>
    /// 缓动编号超出格式有效范围时报告错误。
    /// </summary>
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rule];

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
        if (context.Operation is not IObjectCreationOperation creation ||
            creation.Type is not INamedTypeSymbol type ||
            !EasingFormatRegistry.TryGetRange(type, out var range) ||
            creation.Arguments.Length != 1)
            return;

        // 只检查编译期可确定的缓动编号，动态值无法在编译期判定。
        if (creation.Arguments[0].Value.ConstantValue is not { HasValue: true, Value: int easingNumber })
            return;

        // 编号位于有效范围内则无需诊断
        if (easingNumber >= range.Min && easingNumber <= range.Max)
            return;

        var location = creation.Arguments[0].Value.Syntax.GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            location,
            easingNumber,
            range.DisplayName,
            range.Min,
            range.Max));
    }
}
