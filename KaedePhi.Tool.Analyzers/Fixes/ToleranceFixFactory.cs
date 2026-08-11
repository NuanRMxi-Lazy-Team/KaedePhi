using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KaedePhi.Tool.Analyzers;

internal static class ToleranceFixFactory
{
    public static ExpressionSyntax CreateDivideByHundred(ExpressionSyntax expression) =>
        Create(expression, SyntaxKind.DivideExpression, "100d");

    public static ExpressionSyntax CreateMultiplyByHundred(ExpressionSyntax expression) =>
        Create(expression, SyntaxKind.MultiplyExpression, "100d");

    private static ExpressionSyntax Create(
        ExpressionSyntax expression,
        SyntaxKind operatorKind,
        string operandText)
    {
        var numerator = expression.WithoutTrivia();
        if (numerator is BinaryExpressionSyntax or ConditionalExpressionSyntax or AssignmentExpressionSyntax)
            numerator = SyntaxFactory.ParenthesizedExpression(numerator);

        return SyntaxFactory.BinaryExpression(
                operatorKind,
                numerator,
                SyntaxFactory.ParseExpression(operandText))
            .WithTriviaFrom(expression);
    }
}
