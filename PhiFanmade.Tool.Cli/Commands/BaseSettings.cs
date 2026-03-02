using PhiFanmade.Tool.Cli.Infrastructure;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands;

/// <summary>
/// 所有命令的基础 Settings。
/// </summary>
public class BaseSettings : CommandSettings
{
    /// <summary>快速构建与当前 Settings 对应的 ConsoleWriter。</summary>
    public ConsoleWriter CreateWriter() => new();
}

