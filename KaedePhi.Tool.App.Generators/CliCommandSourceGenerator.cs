using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KaedePhi.Tool.App.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class CliCommandSourceGenerator : IIncrementalGenerator
{
    #region 入口

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var commandInfos = context
            .SyntaxProvider.CreateSyntaxProvider(IsCommandClass, ExtractCommandInfo)
            .Where(x => x is not null)
            .Select((x, _) => x ?? throw new InvalidOperationException("Unexpected null CommandInfo"))
            .Collect();

        context.RegisterSourceOutput(commandInfos, GenerateCommands);
    }

    #endregion

    #region 语法谓词与转换

    private static bool IsCommandClass(SyntaxNode node, CancellationToken _) =>
        node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

    private static CommandInfo? ExtractCommandInfo(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        var classDecl = (ClassDeclarationSyntax)ctx.Node;
        var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl, ct);

        var cmdAttr = classSymbol?.GetAttributes().FirstOrDefault(IsCliCommandAttribute);
        if (cmdAttr is null)
            return null;

        var name = (string?)cmdAttr.ConstructorArguments[0].Value ?? classDecl.Identifier.Text;
        var aliases = ExtractStringArrayNamedArg(cmdAttr, "Aliases");
        var hidden = ExtractBoolNamedArg(cmdAttr, "Hidden");
        var hasDescription =
            classSymbol
                ?.GetMembers()
                .OfType<IPropertySymbol>()
                .Any(p => p.IsStatic && p.Name == "Description") == true;

        var options = new List<string>();
        var handlerMethod = default(string?);

        foreach (var member in classDecl.Members)
        {
            switch (member)
            {
                case FieldDeclarationSyntax field:
                {
                    // 以类型识别选项字段，选项本身在字段初始化器中直接构造
                    foreach (var variable in field.Declaration.Variables)
                    {
                        if (
                            ctx.SemanticModel.GetDeclaredSymbol(variable, ct) is IFieldSymbol
                            {
                                Type.Name: "Option"
                            }
                        )
                            options.Add(variable.Identifier.Text);
                    }

                    break;
                }
                case MethodDeclarationSyntax method:
                {
                    if (HasCliHandlerAttribute(ctx.SemanticModel, method, ct))
                        handlerMethod = method.Identifier.Text;
                    break;
                }
            }
        }

        return new CommandInfo(
            classDecl.Identifier.Text,
            classSymbol?.ContainingNamespace?.ToDisplayString() ?? "",
            name,
            aliases,
            hidden,
            hasDescription,
            options.ToImmutableArray(),
            handlerMethod
        );
    }

    #endregion

    #region 代码生成

    private static void GenerateCommands(
        SourceProductionContext ctx,
        ImmutableArray<CommandInfo> commands
    )
    {
        foreach (var cmd in commands)
        {
            var source = GenerateCommandSource(cmd);
            ctx.AddSource($"{cmd.ClassName}.g.cs", source);
        }
    }

    private static string GenerateCommandSource(CommandInfo cmd)
    {
        var sb = new StringBuilder();

        sb.Append($"    var cmd = new Command(\"{cmd.Name}\"");
        if (cmd.HasDescription)
            sb.Append(", Description");
        sb.AppendLine(");");

        if (cmd.Hidden)
            sb.AppendLine("    cmd.IsHidden = true;");

        if (cmd.Aliases is { Length: > 0 })
        {
            foreach (var alias in cmd.Aliases)
                sb.AppendLine($"    cmd.Aliases.Add(\"{alias}\");");
        }

        foreach (var option in cmd.Options)
            sb.AppendLine($"    cmd.Add({option});");

        if (cmd.HandlerMethod is not null)
            sb.AppendLine($"    cmd.SetAction({cmd.ClassName}.{cmd.HandlerMethod});");

        sb.AppendLine("    return cmd;");

        return $$"""
                 using System.CommandLine;

                 namespace {{cmd.Namespace}};

                 public static partial class {{cmd.ClassName}}
                 {
                     public static Command Create()
                     {
                 {{sb}}
                     }
                 }
                 """;
    }

    #endregion

    #region 属性判断辅助

    private static bool IsCliCommandAttribute(AttributeData attr) =>
        attr.AttributeClass?.Name == "CliCommandAttribute";

    private static bool HasCliHandlerAttribute(
        SemanticModel model,
        MethodDeclarationSyntax method,
        CancellationToken ct
    ) =>
        method.AttributeLists.SelectMany(al => al.Attributes).Any()
        && model
            .GetDeclaredSymbol(method, ct)
            ?.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "CliHandlerAttribute") == true;

    #endregion

    #region 属性参数提取

    private static string[]? ExtractStringArrayNamedArg(AttributeData attr, string name)
    {
        var arg = attr.NamedArguments.FirstOrDefault(kv => kv.Key == name);
        if (arg.Value.Kind != TypedConstantKind.Array || arg.Value.Values.IsDefaultOrEmpty)
            return null;
        return [.. arg.Value.Values.Select(cv => cv.Value).OfType<string>()];
    }

    private static bool ExtractBoolNamedArg(AttributeData attr, string name) =>
        attr.NamedArguments.FirstOrDefault(kv => kv.Key == name).Value.Value as bool? ?? false;

    #endregion

    #region 数据模型

    private readonly record struct CommandInfo(
        string ClassName,
        string Namespace,
        string Name,
        string[]? Aliases,
        bool Hidden,
        bool HasDescription,
        ImmutableArray<string> Options,
        string? HandlerMethod
    );

    #endregion
}