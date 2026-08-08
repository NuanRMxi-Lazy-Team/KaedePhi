namespace KaedePhi.Tool.App.Cli;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CliCommandAttribute : Attribute
{
    public string Name { get; }
    public string[]? Aliases { get; init; }
    public bool Hidden { get; init; }

    public CliCommandAttribute(string name) => Name = name;
}
