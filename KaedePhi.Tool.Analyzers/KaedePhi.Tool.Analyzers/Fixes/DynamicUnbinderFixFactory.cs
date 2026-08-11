using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers;

/// <summary>
/// 生成将动态解绑调用改写为普通解绑调用的替换表达式。
/// </summary>
internal static class DynamicUnbinderFixFactory
{
    /// <summary>
    /// 创建移除容差实参并改用普通解绑方法的调用表达式。
    /// </summary>
    /// <param name="invocation">原始调用表达式</param>
    /// <param name="operation">原始调用的操作信息</param>
    /// <returns>改写后的调用表达式</returns>
    public static InvocationExpressionSyntax Create(
        InvocationExpressionSyntax invocation,
        IInvocationOperation operation
    )
    {
        // 先记录需要移除的容差实参在实参列表中的索引
        var removedIndices = new HashSet<int>(
            operation
                .Arguments.Select((argument, index) => (argument, index))
                .Where(item => item.argument.Parameter?.Name is "tolerance" or "mergeTolerance")
                .Select(item => item.index)
        );
        var arguments = invocation.ArgumentList.Arguments;
        // 从后向前移除，避免前面的索引发生偏移
        for (var i = arguments.Count - 1; i >= 0; i--)
        {
            if (removedIndices.Contains(i))
                arguments = arguments.RemoveAt(i);
        }

        var expression = invocation.Expression;
        // 将方法名 FatherUnbindDynamic 替换为普通解绑方法 FatherUnbind
        if (
            expression is MemberAccessExpressionSyntax memberAccess
            && memberAccess.Name.Identifier.Text == "FatherUnbindDynamic"
        )
        {
            expression = memberAccess.WithName(
                SyntaxFactory.IdentifierName("FatherUnbind").WithTriviaFrom(memberAccess.Name)
            );
        }

        return invocation
            .WithExpression(expression)
            .WithArgumentList(invocation.ArgumentList.WithArguments(arguments));
    }
}
