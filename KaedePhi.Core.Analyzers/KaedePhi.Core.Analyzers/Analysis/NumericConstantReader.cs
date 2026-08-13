using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KaedePhi.Core.Analyzers.Analysis;

/// <summary>
/// 在编译期将任意数值表达式求值为 double 常量，供各类诊断分析复用。
/// </summary>
internal static class NumericConstantReader
{
    /// <summary>
    /// 尝试将表达式求值为编译期常量数值。
    /// </summary>
    /// <param name="semanticModel">表达式所在语义模型</param>
    /// <param name="expression">待求值的表达式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="value">解析得到的数值</param>
    /// <returns>是否成功解析出数值</returns>
    public static bool TryGetDouble(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        CancellationToken cancellationToken,
        out double value
    )
    {
        value = 0;
        // 仅当表达式在编译期具有确定的常量值时才能成功
        var constant = semanticModel.GetConstantValue(expression, cancellationToken);
        return constant.HasValue && TryConvertNumeric(constant.Value, out value);
    }

    /// <summary>
    /// 将各种数值类型统一转为 double 供比较。
    /// </summary>
    /// <param name="rawValue">原始常量值</param>
    /// <param name="value">转换后的数值</param>
    /// <returns>是否为可转换的数值类型</returns>
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
