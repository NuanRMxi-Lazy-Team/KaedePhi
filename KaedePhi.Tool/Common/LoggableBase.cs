namespace KaedePhi.Tool.Common;

/// <summary>
/// 提供日志回调订阅功能的抽象基类，供工具实现类继承以避免重复代码。
/// </summary>
public abstract class LoggableBase : ILoggable
{
    /// <inheritdoc/>
    public Action<string>? OnInfo { get; set; }

    /// <inheritdoc/>
    public Action<string>? OnWarning { get; set; }

    /// <inheritdoc/>
    public Action<string>? OnError { get; set; }

    /// <inheritdoc/>
    public Action<string>? OnDebug { get; set; }

    /// <inheritdoc/>
    public IDisposable SubscribeLog(
        Action<string>? info = null,
        Action<string>? warning = null,
        Action<string>? error = null,
        Action<string>? debug = null)
    {
        if (info != null) OnInfo += info;
        if (warning != null) OnWarning += warning;
        if (error != null) OnError += error;
        if (debug != null) OnDebug += debug;

        return new LogSubscription(() =>
        {
            if (info != null) OnInfo -= info;
            if (warning != null) OnWarning -= warning;
            if (error != null) OnError -= error;
            if (debug != null) OnDebug -= debug;
        });
    }

    /// <summary>发出信息日志。</summary>
    protected void LogInfo(string message) => OnInfo?.Invoke(message);

    /// <summary>发出警告日志。</summary>
    protected void LogWarning(string message) => OnWarning?.Invoke(message);

    /// <summary>发出错误日志。</summary>
    protected void LogError(string message) => OnError?.Invoke(message);

    /// <summary>发出调试日志。</summary>
    protected void LogDebug(string message) => OnDebug?.Invoke(message);
}
