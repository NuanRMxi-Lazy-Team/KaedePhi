using PhiFanmade.Tool.Localization;
using Spectre.Console.Cli;

namespace PhiFanmade.Tool.Cli.Commands.RePhiEdit;

[Obsolete("后期将不再支持直接操作RPE谱面")]
public sealed class RpeConvertCommand : Command<BaseSettings>
{
    public override int Execute(CommandContext context, BaseSettings settings, CancellationToken cancellationToken)
    {
        settings.CreateWriter().Warn(Strings.cli_warn_rpe_convert);
        return 2;
    }
}