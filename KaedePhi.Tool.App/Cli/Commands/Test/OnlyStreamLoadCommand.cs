using KaedePhi.Tool.App.Cli.Infrastructure;
#if Debug
using KaedePhi.Core.PhiEdit;
#endif

namespace KaedePhi.Tool.App.Cli.Commands.Test;

public static class OnlyStreamLoadCommand
{
    private static readonly Option<string?> InputOpt = new("--input", "-i")
    {
        Description = "需要推算的文件",
        Arity = ArgumentArity.ZeroOrOne,
    };

    public static Command Create()
    {
        var cmd = new Command("pestream", "PE stream test") { Hidden = true };
        cmd.Add(InputOpt);

        cmd.SetAction(
            async (result, _) =>
            {
#if Debug
                var input = result.GetValue(InputOpt);
                if (string.IsNullOrWhiteSpace(input))
                {
                    ConsoleWriter.Error("Input file path cannot be null or whitespace.");
                    return 1;
                }

                await using var stream = File.OpenRead(input);
                var chart = await Chart.LoadStreamAsync(stream);
                ConsoleWriter.Info(chart.Offset.ToString());
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
