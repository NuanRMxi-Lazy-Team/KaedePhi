using System.ComponentModel;
using KaedePhi.Tool.Cli.Infrastructure;
using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Cli.Commands.Test;

public class OnlyStreamLoadCommand : AsyncCommand<GetTypeTestCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-i|--input <PATH>")]
        [Description("需要推算的文件")]
        public string? Input { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, GetTypeTestCommand.Settings settings,
        CancellationToken cancellationToken)
    {
        var writer = new ConsoleWriter();
#if Debug
        var input = settings.Input;
        if (string.IsNullOrWhiteSpace(input))
        {
            writer.Error("Input file path cannot be null or whitespace.");
            return 1;
        }
        
        // 创建文件流
        var stream = File.OpenRead(input);
        // 测试pec
        var chart = await Core.PhiEdit.Chart.LoadStreamAsync(stream);
        writer.Info(chart.Offset.ToString());
        Console.ReadLine();
#else
        writer.Warn("This command can only be executed on Debug builds.");
        await Task.CompletedTask;
#endif
        
        return 0;
    }
}