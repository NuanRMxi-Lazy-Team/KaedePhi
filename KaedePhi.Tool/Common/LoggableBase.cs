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
        Action<string>? debug = null
    )
    {
        if (info != null)
            OnInfo += info;
        if (warning != null)
            OnWarning += warning;
        if (error != null)
            OnError += error;
        if (debug != null)
            OnDebug += debug;

        return new LogSubscription(() =>
        {
            if (info != null)
                OnInfo -= info;
            if (warning != null)
                OnWarning -= warning;
            if (error != null)
                OnError -= error;
            if (debug != null)
                OnDebug -= debug;
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

    /// <summary>
    /// 检查 IR Meta 中不被目标格式支持的字段，逐个发出警告。
    /// </summary>
    /// <param name="formatName">目标格式名称</param>
    /// <param name="src">源 Meta 对象</param>
    protected void WarnIfUnsupportedMeta(string formatName, KaedePhi.Core.Intermediate.Model.Meta src)
    {
        var defaults = new KaedePhi.Core.Intermediate.Model.Meta();
        if (src.Background != defaults.Background)
            LogWarning($"{formatName} 不支持 Meta.Background（值='{src.Background}'）");
        if (src.Author != defaults.Author)
            LogWarning($"{formatName} 不支持 Meta.Author（值='{src.Author}'）");
        if (src.Composer != defaults.Composer)
            LogWarning($"{formatName} 不支持 Meta.Composer（值='{src.Composer}'）");
        if (src.Artist != defaults.Artist)
            LogWarning($"{formatName} 不支持 Meta.Artist（值='{src.Artist}'）");
        if (src.Level != defaults.Level)
            LogWarning($"{formatName} 不支持 Meta.Level（值='{src.Level}'）");
        if (src.Name != defaults.Name)
            LogWarning($"{formatName} 不支持 Meta.Name（值='{src.Name}'）");
        if (src.Song != defaults.Song)
            LogWarning($"{formatName} 不支持 Meta.Song（值='{src.Song}'）");
    }
}
