using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers.Analysis;

/// <summary>
/// 在编译期尝试将调用实参求值为常量数值，供诊断与修复判断使用。
/// </summary>
internal static class ConstantExpressionEvaluator
{
    /// <summary>
    /// 尝试将调用实参解析为编译期常量数值。
    /// </summary>
    /// <param name="compilation">当前编译单元</param>
    /// <param name="argument">待求值的调用实参</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="value">解析得到的常量数值</param>
    /// <returns>是否成功解析出常量数值</returns>
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
        // Beat 实参需要先取出其构造参数中的数值，其余类型直接取常量值
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
        // 仅接受构造参数唯一且类型确为 Beat 的构造表达式
        if (expression is not ObjectCreationExpressionSyntax creation ||
            creation.ArgumentList is not { Arguments.Count: 1 } ||
            !EventCutterApi.IsBeat(semanticModel.GetTypeInfo(creation, cancellationToken).Type))
            return false;

        // 构造参数即 Beat 内部封装的数值，递归求值该参数
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
        // 仅当表达式在编译期具有确定的常量值时才能成功
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
        // 覆盖所有数值类型以避免精度损失式的意外转换，decimal 转为 double 供统一比较
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
