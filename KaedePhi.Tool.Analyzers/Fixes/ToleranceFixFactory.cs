using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KaedePhi.Tool.Analyzers;

/// <summary>
/// 生成容差参数百分比换算修复所需的替换表达式。
/// </summary>
internal static class ToleranceFixFactory
{
    /// <summary>
    /// 创建将容差除以 100 的表达式（容差过大时使用）。
    /// </summary>
    /// <param name="expression">原容差表达式</param>
    /// <returns>除以 100 后的替换表达式</returns>
    public static ExpressionSyntax CreateDivideByHundred(ExpressionSyntax expression) =>
        Create(expression, SyntaxKind.DivideExpression, "100d");

    /// <summary>
    /// 创建将容差乘以 100 的表达式（容差过小时使用）。
    /// </summary>
    /// <param name="expression">原容差表达式</param>
    /// <returns>乘以 100 后的替换表达式</returns>
    public static ExpressionSyntax CreateMultiplyByHundred(ExpressionSyntax expression) =>
        Create(expression, SyntaxKind.MultiplyExpression, "100d");

    private static ExpressionSyntax Create(
        ExpressionSyntax expression,
        SyntaxKind operatorKind,
        string operandText)
    {
        var numerator = expression.WithoutTrivia();
        // 复合表达式需加括号，避免 a + b * 100 改变运算顺序
        if (numerator is BinaryExpressionSyntax or ConditionalExpressionSyntax or AssignmentExpressionSyntax)
            numerator = SyntaxFactory.ParenthesizedExpression(numerator);

        return SyntaxFactory.BinaryExpression(
                operatorKind,
                numerator,
                SyntaxFactory.ParseExpression(operandText))
            .WithTriviaFrom(expression);
    }
}
