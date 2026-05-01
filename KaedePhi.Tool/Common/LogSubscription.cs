namespace KaedePhi.Tool.Common;

/// <summary>
/// 日志订阅句柄，释放时自动取消订阅。
/// </summary>
public sealed class LogSubscription : IDisposable
{
    private readonly Action _unsubscribe;
    private bool _disposed;

    public LogSubscription(Action unsubscribe)
    {
        _unsubscribe = unsubscribe;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _unsubscribe();
        _disposed = true;
    }
}
