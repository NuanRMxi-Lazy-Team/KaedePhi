using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.App.Cli.Infrastructure;

/// <summary>
/// 控制台输出封装。
/// 说明（i18n/l10n）：此类仅负责输出，不持有具体文案；
/// 文案应由调用方通过本地化后传入。
/// </summary>
public static class ConsoleWriter
{
    /// <summary>
    /// 当前日志级别。0 = 关闭, 1 = Debug, 2 = Info, 3 = Warning, 4 = Error。
    /// 只输出级别 >= 此值的消息；0 表示全部关闭。
    /// </summary>
    public static uint LogLevel { get; set; } = 3;

    /// <summary>
    /// Debug 级输出（级别 1）。
    /// </summary>
    public static void Debug(string message)
    {
        if (!ShouldLog(1))
            return;
        WriteColoredLine(message, ConsoleColor.Gray, false);
    }

    /// <summary>
    /// 信息级输出（级别 2）。
    /// </summary>
    public static void Info(string message)
    {
        if (!ShouldLog(2))
            return;
        WriteColoredLine(message, ConsoleColor.Green, false);
    }

    /// <summary>
    /// 警告级输出（级别 3）。
    /// </summary>
    public static void Warn(string message)
    {
        if (!ShouldLog(3))
            return;
        WriteColoredLine(message, ConsoleColor.Yellow, true);
    }

    /// <summary>
    /// 错误级输出（级别 4）。
    /// </summary>
    public static void Error(string message)
    {
        if (!ShouldLog(4))
            return;
        WriteColoredLine(message, ConsoleColor.Red, true);
    }

    /// <summary>
    /// 判断指定级别的消息是否应当输出。
    /// </summary>
    private static bool ShouldLog(uint level) => LogLevel != 0 && level >= LogLevel;

    /// <summary>
    /// 创建向标准错误输出节流进度的回调。
    /// </summary>
    /// <returns>进度回调实例。</returns>
    public static IProgress<ToolProgress> CreateProgress()
    {
        var lastPercentage = -1;
        return new Progress<ToolProgress>(progress =>
        {
            var rawPercentage =
                progress.OverallPercentage >= 0 ? progress.OverallPercentage : progress.Percentage;
            var percentage = (int)(Math.Clamp(rawPercentage, 0, 1) * 100);
            if (percentage == lastPercentage && string.IsNullOrWhiteSpace(progress.Detail))
                return;
            lastPercentage = percentage;
            Console.Error.WriteLine($"[{percentage, 3}%] {progress.Detail}");
        });
    }

    private static void WriteColoredLine(string message, ConsoleColor color, bool isError)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        if (isError)
            Console.Error.WriteLine(message);
        else
            Console.WriteLine(message);
        Console.ForegroundColor = prev;
    }
}
