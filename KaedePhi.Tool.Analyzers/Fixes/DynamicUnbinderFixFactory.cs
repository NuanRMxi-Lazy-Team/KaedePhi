using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace KaedePhi.Tool.Analyzers;

internal static class DynamicUnbinderFixFactory
{
    public static InvocationExpressionSyntax Create(
        InvocationExpressionSyntax invocation,
        IInvocationOperation operation)
    {
        var removedIndices = new HashSet<int>(
            operation.Arguments
                .Select((argument, index) => (argument, index))
                .Where(item => item.argument.Parameter?.Name is "tolerance" or "mergeTolerance")
                .Select(item => item.index));
        var arguments = invocation.ArgumentList.Arguments;
        for (var i = arguments.Count - 1; i >= 0; i--)
        {
            if (removedIndices.Contains(i))
                arguments = arguments.RemoveAt(i);
        }

        var expression = invocation.Expression;
        if (expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "FatherUnbindDynamic")
        {
            expression = memberAccess.WithName(
                SyntaxFactory.IdentifierName("FatherUnbind").WithTriviaFrom(memberAccess.Name));
        }

        return invocation
            .WithExpression(expression)
            .WithArgumentList(invocation.ArgumentList.WithArguments(arguments));
    }
}
