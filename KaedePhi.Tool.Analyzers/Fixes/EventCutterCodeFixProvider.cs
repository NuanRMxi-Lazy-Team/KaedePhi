using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KaedePhi.Tool.Analyzers.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace KaedePhi.Tool.Analyzers;

/// <summary>
/// 为事件切割长度诊断提供倒数修复建议。
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EventCutterCodeFixProvider)), Shared]
public sealed class EventCutterCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(EventCutterAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic is null)
            return;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var node = root?.FindNode(diagnostic.Location.SourceSpan);
        var invocation = node?.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
            .ConfigureAwait(false);
        if (semanticModel?.GetOperation(invocation, context.CancellationToken) is not IInvocationOperation operation ||
            !EventCutterApi.TryGetCutLengthArgument(operation, out var argument) ||
            argument.Value.Syntax is not ExpressionSyntax expression ||
            argument.Parameter is null)
            return;

        if (!ConstantExpressionEvaluator.TryGetValue(
                semanticModel.Compilation,
                argument,
                context.CancellationToken,
                out var value) ||
            double.IsNaN(value) ||
            double.IsInfinity(value) ||
            value < 1.0)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                Resource.KPTI0001CodeFixTitle,
                cancellationToken => ApplyFixAsync(
                    context.Document,
                    diagnostic.Location.SourceSpan,
                    cancellationToken),
                nameof(EventCutterCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> ApplyFixAsync(
        Document document,
        TextSpan diagnosticSpan,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root?.FindNode(diagnosticSpan) is not SyntaxNode node)
            return document;

        var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation is null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation ||
            !EventCutterApi.TryGetCutLengthArgument(operation, out var argument) ||
            argument.Value.Syntax is not ExpressionSyntax expression ||
            argument.Parameter is null)
            return document;

        var isBeat = EventCutterApi.IsBeat(argument.Parameter.Type);
        var replacement = CutLengthFixFactory.Create(expression, isBeat);
        return document.WithSyntaxRoot(root.ReplaceNode(expression, replacement));
    }
}
