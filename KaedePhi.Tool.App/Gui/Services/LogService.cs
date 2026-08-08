using Serilog;
using Serilog.Core;
using ILogger = Serilog.ILogger;

namespace KaedePhi.Tool.App.Gui.Services;

/// <summary>
/// 应用程序日志服务。负责 Serilog 全局 Logger 的生命周期管理。
/// 业务类应通过 <see cref="ForContext{T}"/> 获取携带来源信息的专属 <see cref="ILogger"/>，
/// 而非直接使用本类的方法，以便在日志文件中准确追踪每条记录来自哪个类。
/// </summary>
public sealed class LogService : IDisposable
{
    private readonly int _maxLogFiles;
    private Logger? _logger;

    public LogService(int maxLogFiles = 5)
    {
        _maxLogFiles = maxLogFiles;
    }

    /// <summary>日志目录路径</summary>
    public string LogDirectory => AppPaths.GetDirectory("logs");

    /// <summary>当前会话日志文件路径</summary>
    public string CurrentLogFile { get; private set; } = string.Empty;

    /// <summary>
    /// 为指定类型创建携带 SourceContext 的专属 <see cref="ILogger"/>。
    /// 输出到日志文件时，每行都会显示 <c>[完整类名]</c>，便于定位日志来源。
    /// <para>
    /// 注意：此方法必须在 <see cref="StartSession"/> 调用之后才能返回有效 logger。
    /// 若在会话启动前调用，返回 <see cref="Serilog.Core.Logger.None"/>（静默丢弃所有日志）。
    /// </para>
    /// </summary>
    public ILogger ForContext<T>() => _logger?.ForContext<T>() ?? Logger.None;

    /// <summary>
    /// 启动一个新的日志会话：创建带时间戳的日志文件，配置全局 <see cref="Log.Logger"/>。
    /// </summary>
    public void StartSession()
    {
        _logger?.Dispose();

        var fileName = $"session_{DateTime.Now:yyyyMMdd_HHmmss}.log";
        CurrentLogFile = Path.Combine(LogDirectory, fileName);

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                CurrentLogFile,
                outputTemplate: "[{Level:u4} {Timestamp:HH:mm:ss}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                flushToDiskInterval: TimeSpan.FromSeconds(1)
            );

#if Debug
        // Debug 构建将日志同步镜像到控制台，便于实时观察
        loggerConfiguration.WriteTo.Console(
            outputTemplate: "[{Level:u4} {Timestamp:HH:mm:ss}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
        );
#endif

        _logger = loggerConfiguration.CreateLogger();

        // 将全局静态 Logger 指向同一实例，供 ForContext<T>() 使用
        Log.Logger = _logger;

        Log.ForContext<LogService>().Information("=== KaedePhi GUI Session Started ===");

        CleanupOldLogs();
    }

    /// <summary>刷新缓冲并释放文件句柄</summary>
    public void Dispose()
    {
        _logger?.Dispose();
        _logger = null;
    }

    private void CleanupOldLogs()
    {
        try
        {
            var files = new DirectoryInfo(LogDirectory)
                .GetFiles("session_*.log")
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            for (var i = _maxLogFiles; i < files.Count; i++)
                files[i].Delete();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 清理失败不应导致应用程序崩溃
            Log.ForContext<LogService>().Warning(ex, "清理日志文件失败。");
        }
    }
}
