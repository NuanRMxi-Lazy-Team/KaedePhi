using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KaedePhi.Tool.Analyzers;

/// <summary>
/// 生成 cutLength 倒数修复所需的替换表达式。
/// </summary>
internal static class CutLengthFixFactory
{
    /// <summary>
    /// 创建将切割长度替换为倒数的表达式。
    /// </summary>
    /// <param name="expression">原切割长度表达式</param>
    /// <param name="isBeat">切割长度是否为 Beat 对象</param>
    /// <returns>取倒数后的替换表达式</returns>
    public static ExpressionSyntax Create(ExpressionSyntax expression, bool isBeat)
    {
        var unwrapped = UnwrapParentheses(expression);
        // Beat 实参需要改写其构造参数，而不是整个 Beat 构造表达式
        if (
            isBeat
            && unwrapped is ObjectCreationExpressionSyntax creation
            && creation.ArgumentList is { Arguments.Count: 1 }
        )
        {
            var originalArgument = creation.ArgumentList.Arguments[0].Expression;
            var fixedCreation = creation.ReplaceNode(
                originalArgument,
                CreateReciprocal(originalArgument)
            );
            return expression.ReplaceNode(creation, fixedCreation).WithTriviaFrom(expression);
        }

        return CreateReciprocal(expression);
    }

    private static ExpressionSyntax CreateReciprocal(ExpressionSyntax expression)
    {
        var denominator = expression.WithoutTrivia();
        // 复合表达式需加括号，避免 1d / a + b 改变运算顺序
        if (
            denominator
            is BinaryExpressionSyntax
                or ConditionalExpressionSyntax
                or AssignmentExpressionSyntax
        )
            denominator = SyntaxFactory.ParenthesizedExpression(denominator);

        return SyntaxFactory
            .BinaryExpression(
                SyntaxKind.DivideExpression,
                SyntaxFactory.ParseExpression("1d"),
                denominator
            )
            .WithTriviaFrom(expression);
    }

    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression;
    }
}
