using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KaedePhi.Tool.Analyzers;

internal static class CutLengthFixFactory
{
    public static ExpressionSyntax Create(ExpressionSyntax expression, bool isBeat)
    {
        var unwrapped = UnwrapParentheses(expression);
        if (isBeat &&
            unwrapped is ObjectCreationExpressionSyntax creation &&
            creation.ArgumentList is { Arguments.Count: 1 })
        {
            var originalArgument = creation.ArgumentList.Arguments[0].Expression;
            var fixedCreation = creation.ReplaceNode(originalArgument, CreateReciprocal(originalArgument));
            return expression.ReplaceNode(creation, fixedCreation).WithTriviaFrom(expression);
        }

        return CreateReciprocal(expression);
    }

    private static ExpressionSyntax CreateReciprocal(ExpressionSyntax expression)
    {
        var denominator = expression.WithoutTrivia();
        if (denominator is BinaryExpressionSyntax or ConditionalExpressionSyntax or AssignmentExpressionSyntax)
            denominator = SyntaxFactory.ParenthesizedExpression(denominator);

        return SyntaxFactory.BinaryExpression(
                SyntaxKind.DivideExpression,
                SyntaxFactory.ParseExpression("1d"),
                denominator)
            .WithTriviaFrom(expression);
    }

    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression;
    }
}
