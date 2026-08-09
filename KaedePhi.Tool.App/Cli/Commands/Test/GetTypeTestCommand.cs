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
        var cmd = new Command("test", "Test command") { Hidden = true };
        cmd.Add(InputOpt);

        cmd.SetAction(
            async (result, ct) =>
            {
#if Debug
                var input = result.GetValue(InputOpt);
                if (input is null)
                {
                    ConsoleWriter.Info("Type: Unknown");
                    return 0;
                }

                await using var inputStream = new FileStream(
                    input,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    65536,
                    useAsync: true
                );
                var type = await ChartGetType.GetTypeAsync(inputStream, ct);
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
