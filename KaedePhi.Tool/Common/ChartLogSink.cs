namespace KaedePhi.Tool.Common;

/// <summary>
/// 日志回调集合，用于一次性将宿主日志系统接入多个 <see cref="ILoggable"/> 实现。
/// </summary>
public sealed class ChartLogSink
{
    /// <summary>不输出任何日志的空实现。</summary>
    public static ChartLogSink None { get; } = new();

    /// <summary>信息级别回调。</summary>
    public Action<string>? Info { get; init; }

    /// <summary>警告级别回调。</summary>
    public Action<string>? Warning { get; init; }

    /// <summary>错误级别回调。</summary>
    public Action<string>? Error { get; init; }

    /// <summary>调试级别回调。</summary>
    public Action<string>? Debug { get; init; }

    /// <summary>
    /// 将当前回调集合订阅到指定可记录日志对象。
    /// </summary>
    /// <param name="loggable">目标对象</param>
    /// <returns>订阅句柄，释放时取消订阅</returns>
    public IDisposable AttachTo(ILoggable loggable)
    {
        ArgumentNullException.ThrowIfNull(loggable);
        return loggable.SubscribeLog(Info, Warning, Error, Debug);
    }

    /// <summary>
    /// 将当前回调集合订阅到多个可记录日志对象。
    /// </summary>
    /// <param name="loggables">目标对象集合</param>
    /// <returns>聚合订阅句柄，释放时统一取消订阅</returns>
    public IDisposable AttachToAll(params ILoggable[] loggables)
    {
        var subscriptions = loggables.Select(AttachTo).ToArray();
        return new CompositeSubscription(subscriptions);
    }

    private sealed class CompositeSubscription : IDisposable
    {
        private readonly IDisposable[] _subscriptions;

        public CompositeSubscription(IDisposable[] subscriptions) => _subscriptions = subscriptions;

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }
    }
}
