using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers.Analysis;

internal static class ConstantExpressionEvaluator
{
    public static bool TryGetValue(
        Compilation compilation,
        IArgumentOperation argument,
        CancellationToken cancellationToken,
        out double value)
    {
        value = 0;
        if (argument.Parameter is null || argument.Value.Syntax is not ExpressionSyntax expression)
            return false;

        var semanticModel = compilation.GetSemanticModel(expression.SyntaxTree);
        return EventCutterApi.IsBeat(argument.Parameter.Type)
            ? TryGetBeatValue(semanticModel, expression, cancellationToken, out value)
            : TryGetNumericValue(semanticModel, expression, cancellationToken, out value);
    }

    private static bool TryGetBeatValue(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        CancellationToken cancellationToken,
        out double value)
    {
        value = 0;
        expression = UnwrapParentheses(expression);
        if (expression is not ObjectCreationExpressionSyntax creation ||
            creation.ArgumentList is not { Arguments.Count: 1 } ||
            !EventCutterApi.IsBeat(semanticModel.GetTypeInfo(creation, cancellationToken).Type))
            return false;

        return TryGetNumericValue(
            semanticModel,
            creation.ArgumentList.Arguments[0].Expression,
            cancellationToken,
            out value);
    }

    private static bool TryGetNumericValue(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        CancellationToken cancellationToken,
        out double value)
    {
        value = 0;
        var constant = semanticModel.GetConstantValue(expression, cancellationToken);
        return constant.HasValue && TryConvertNumeric(constant.Value, out value);
    }

    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression;
    }

    private static bool TryConvertNumeric(object? rawValue, out double value)
    {
        switch (rawValue)
        {
            case byte number:
                value = number;
                return true;
            case sbyte number:
                value = number;
                return true;
            case short number:
                value = number;
                return true;
            case ushort number:
                value = number;
                return true;
            case int number:
                value = number;
                return true;
            case uint number:
                value = number;
                return true;
            case long number:
                value = number;
                return true;
            case ulong number:
                value = number;
                return true;
            case float number:
                value = number;
                return true;
            case double number:
                value = number;
                return true;
            case decimal number:
                value = (double)number;
                return true;
            default:
                value = 0;
                return false;
        }
    }
}
