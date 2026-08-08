using KaedePhi.Tool.App.Cli.Infrastructure;

#if Debug
using KaedePhi.Tool.Common;
#endif

namespace KaedePhi.Tool.App.Cli.Commands.Test;

public static class GetTypeTestCommand
{
    private static readonly Option<string?> InputOpt = new("--input", "-i")
    {
        Description = "需要推算的文件",
        Arity = ArgumentArity.ZeroOrOne,
    };

    public static Command Create()
    {
        var cmd = new Command("test", "Test command")
        {
            Hidden = true
        };
        cmd.Add(InputOpt);

        cmd.SetAction(
            async (result, ct) =>
            {
#if Debug
                var input = result.GetValue(InputOpt);
                var inputText = input is null ? "" : await File.ReadAllTextAsync(input, ct);
                var type = ChartGetType.GetType(inputText);
                ConsoleWriter.Info($"Type: {type}");
#else
                ConsoleWriter.Warn("This command can only be executed on Debug builds.");
                await Task.CompletedTask;
#endif
                return 0;
            }
        );

        return cmd;
    }
}
