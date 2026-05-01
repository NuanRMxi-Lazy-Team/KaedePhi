namespace KaedePhi.Tool.Common;

/// <summary>
/// 可订阅日志的工具接口。
/// </summary>
public interface ILoggable
{
    /// <summary>
    /// 信息级别日志回调。
    /// </summary>
    Action<string>? OnInfo { get; set; }

    /// <summary>
    /// 警告级别日志回调。
    /// </summary>
    Action<string>? OnWarning { get; set; }

    /// <summary>
    /// 错误级别日志回调。
    /// </summary>
    Action<string>? OnError { get; set; }

    /// <summary>
    /// 调试级别日志回调。
    /// </summary>
    Action<string>? OnDebug { get; set; }

    /// <summary>
    /// 订阅日志事件。返回的 <see cref="IDisposable"/> 在释放时自动取消订阅。
    /// </summary>
    /// <param name="info">信息日志回调。</param>
    /// <param name="warning">警告日志回调。</param>
    /// <param name="error">错误日志回调。</param>
    /// <param name="debug">调试日志回调。</param>
    /// <returns>订阅句柄，释放时取消订阅。</returns>
    IDisposable SubscribeLog(
        Action<string>? info = null,
        Action<string>? warning = null,
        Action<string>? error = null,
        Action<string>? debug = null);
}
