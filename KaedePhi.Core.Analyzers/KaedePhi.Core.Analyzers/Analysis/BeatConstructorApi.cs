using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KaedePhi.Core.Analyzers.Analysis;

/// <summary>
/// Beat 类型的识别与构造实参的编译期求值。
/// </summary>
internal static class BeatConstructorApi
{
    private const string BeatNamespace = "KaedePhi.Core.Common";
    private const string BeatMetadataName = "Beat";

    /// <summary>
    /// 判断类型符号是否为 Beat 类型。
    /// </summary>
    /// <param name="type">待判断的类型符号</param>
    /// <returns>是否为 Beat 类型</returns>
    public static bool IsBeatType(ITypeSymbol? type) =>
        type is INamedTypeSymbol namedType
        && namedType.OriginalDefinition.ContainingNamespace?.ToDisplayString() == BeatNamespace
        && namedType.OriginalDefinition.MetadataName == BeatMetadataName;

    /// <summary>
    /// 判断构造形参是否为 int[] 数组类型。
    /// </summary>
    /// <param name="parameter">待判断的构造形参</param>
    /// <returns>是否为 int[] 类型</returns>
    public static bool IsIntArrayParameter(IParameterSymbol parameter) =>
        parameter.Type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Int32 };

    /// <summary>
    /// 判断构造形参是否为 double 类型。
    /// </summary>
    /// <param name="parameter">待判断的构造形参</param>
    /// <returns>是否为 double 类型</returns>
    public static bool IsDoubleParameter(IParameterSymbol parameter) =>
        parameter.Type.SpecialType == SpecialType.System_Double;

    /// <summary>
    /// 从数组创建表达式中提取编译期可确定的元素。
    /// </summary>
    /// <param name="semanticModel">表达式所在语义模型</param>
    /// <param name="expression">数组创建表达式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="elements">元素表达式与值的列表</param>
    /// <returns>是否成功提取全部元素（任一元素非常量则视为失败）</returns>
    public static bool TryGetArrayElements(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        CancellationToken cancellationToken,
        out ImmutableArray<(ExpressionSyntax Expression, double Value)> elements
    )
    {
        elements = [];
        var builder = ImmutableArray.CreateBuilder<(ExpressionSyntax, double)>();
        if (!TryCollectElementExpressions(expression, out var expressions))
            return false;

        foreach (var element in expressions)
        {
            if (
                !NumericConstantReader.TryGetDouble(
                    semanticModel,
                    element,
                    cancellationToken,
                    out var value
                )
            )
                return false;
            builder.Add((element, value));
        }

        elements = builder.ToImmutable();
        return true;
    }

    /// <summary>
    /// 从数组创建表达式中收集元素表达式，支持显式、隐式与集合表达式三种形式。
    /// </summary>
    /// <param name="expression">数组创建表达式</param>
    /// <param name="expressions">收集到的元素表达式</param>
    /// <returns>是否为可静态展开的数组形式</returns>
    private static bool TryCollectElementExpressions(
        ExpressionSyntax expression,
        out ImmutableArray<ExpressionSyntax> expressions
    )
    {
        expressions = [];
        var builder = ImmutableArray.CreateBuilder<ExpressionSyntax>();
        switch (expression)
        {
            // new int[] { a, b, c }
            case ArrayCreationExpressionSyntax array when array.Initializer is not null:
                builder.AddRange(array.Initializer.Expressions);
                break;
            // new[] { a, b, c }
            case ImplicitArrayCreationExpressionSyntax implicitArray:
                builder.AddRange(implicitArray.Initializer.Expressions);
                break;
            // [a, b, c]
            case CollectionExpressionSyntax collection:
                foreach (var element in collection.Elements)
                {
                    // 展开元素在编译期无法确定，视为无法提取
                    if (element is not ExpressionElementSyntax expressionElement)
                        return false;
                    builder.Add(expressionElement.Expression);
                }
                break;
            default:
                return false;
        }

        expressions = builder.ToImmutable();
        return true;
    }
}
