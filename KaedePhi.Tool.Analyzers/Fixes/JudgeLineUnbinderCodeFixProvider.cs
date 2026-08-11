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
/// 为父线解绑容差诊断提供百分比换算修复建议。
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(JudgeLineUnbinderCodeFixProvider)), Shared]
public sealed class JudgeLineUnbinderCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [
            JudgeLineUnbinderAnalyzer.DiagnosticId,
            JudgeLineUnbinderAnalyzer.SmallToleranceDiagnosticId,
            JudgeLineUnbinderAnalyzer.ZeroToleranceDiagnosticId,
        ];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic is null)
            return;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);
        var node = root?.FindNode(diagnostic.Location.SourceSpan);
        var invocation = node?.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
            .ConfigureAwait(false);
        if (semanticModel?.GetOperation(invocation, context.CancellationToken) is not IInvocationOperation operation ||
            !JudgeLineUnbinderApi.TryGetToleranceArguments(operation, out var arguments) ||
            FindArgument(arguments, diagnostic.Location.SourceSpan) is not { } argument ||
            argument.Value.Syntax is not ExpressionSyntax expression)
            return;

        if (!ConstantExpressionEvaluator.TryGetValue(
                semanticModel.Compilation,
                argument,
                context.CancellationToken,
                out var value) ||
            double.IsNaN(value) ||
            double.IsInfinity(value) ||
            !CanFix(diagnostic.Id, value) ||
            (diagnostic.Id == JudgeLineUnbinderAnalyzer.ZeroToleranceDiagnosticId &&
             !JudgeLineUnbinderApi.IsDynamicMethod(operation.TargetMethod)))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                GetCodeFixTitle(diagnostic.Id),
                cancellationToken => diagnostic.Id == JudgeLineUnbinderAnalyzer.ZeroToleranceDiagnosticId
                    ? ApplyDynamicFixAsync(
                        context.Document,
                        invocation,
                        operation,
                        cancellationToken)
                    : ApplyToleranceFixAsync(
                        context.Document,
                        diagnostic.Location.SourceSpan,
                        diagnostic.Id,
                        cancellationToken),
                nameof(JudgeLineUnbinderCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> ApplyToleranceFixAsync(
        Document document,
        TextSpan diagnosticSpan,
        string diagnosticId,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root?.FindNode(diagnosticSpan) is not SyntaxNode node)
            return document;

        var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation is null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken)
            .ConfigureAwait(false);
        if (semanticModel?.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation ||
            !JudgeLineUnbinderApi.TryGetToleranceArguments(operation, out var arguments) ||
            FindArgument(arguments, diagnosticSpan) is not { Value.Syntax: ExpressionSyntax expression })
            return document;

        var replacement = diagnosticId == JudgeLineUnbinderAnalyzer.SmallToleranceDiagnosticId
            ? ToleranceFixFactory.CreateMultiplyByHundred(expression)
            : ToleranceFixFactory.CreateDivideByHundred(expression);
        return document.WithSyntaxRoot(root.ReplaceNode(expression, replacement));
    }

    private static string GetCodeFixTitle(string diagnosticId) =>
        diagnosticId switch
        {
            var id when id == JudgeLineUnbinderAnalyzer.SmallToleranceDiagnosticId =>
                Resource.KPTI0003CodeFixTitle,
            var id when id == JudgeLineUnbinderAnalyzer.ZeroToleranceDiagnosticId =>
                Resource.KPTR0001CodeFixTitle,
            _ => Resource.KPTE0001CodeFixTitle,
        };

    private static bool CanFix(string diagnosticId, double value) =>
        diagnosticId == JudgeLineUnbinderAnalyzer.SmallToleranceDiagnosticId
            ? value > 0.0 && value < JudgeLineUnbinderTolerance.SmallToleranceThreshold
            : diagnosticId == JudgeLineUnbinderAnalyzer.ZeroToleranceDiagnosticId
                ? value == 0.0
            : value >= JudgeLineUnbinderTolerance.ErrorThreshold;

    private static async Task<Document> ApplyDynamicFixAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        IInvocationOperation operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fixedInvocation = DynamicUnbinderFixFactory.Create(invocation, operation);
        return document.WithSyntaxRoot(
            (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!
                .ReplaceNode(invocation, fixedInvocation));
    }

    private static IArgumentOperation? FindArgument(
        ImmutableArray<IArgumentOperation> arguments,
        TextSpan diagnosticSpan) =>
        arguments.FirstOrDefault(argument => argument.Value.Syntax.Span == diagnosticSpan);
}
