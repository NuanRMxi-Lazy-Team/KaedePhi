namespace PhiFanmade.Tool.PhiFanmadeNrc;

/// <summary>
/// NRC 格式工具的日志回调，可从外部订阅以接收处理信息。
/// </summary>
public static class NrcToolLog
{
    public static Action<string> OnInfo { get; set; } = _ => { };
    public static Action<string> OnWarning { get; set; } = _ => { };
    public static Action<string> OnError { get; set; } = _ => { };
    public static Action<string> OnDebug { get; set; } = _ => { };

    public static IDisposable Subscribe(
        Action<string>? info = null,
        Action<string>? warning = null,
        Action<string>? error = null,
        Action<string>? debug = null)
    {
        if (info != null) OnInfo += info;
        if (warning != null) OnWarning += warning;
        if (error != null) OnError += error;
        if (debug != null) OnDebug += debug;

        return new Subscription(info, warning, error, debug);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action<string>? _info;
        private readonly Action<string>? _warning;
        private readonly Action<string>? _error;
        private readonly Action<string>? _debug;
        private bool _disposed;

        public Subscription(Action<string>? info, Action<string>? warning, Action<string>? error, Action<string>? debug)
        {
            _info = info;
            _warning = warning;
            _error = error;
            _debug = debug;
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_info != null) OnInfo = (Action<string>?)Delegate.Remove(OnInfo, _info) ?? (_ => { });
            if (_warning != null) OnWarning = (Action<string>?)Delegate.Remove(OnWarning, _warning) ?? (_ => { });
            if (_error != null) OnError = (Action<string>?)Delegate.Remove(OnError, _error) ?? (_ => { });
            if (_debug != null) OnDebug = (Action<string>?)Delegate.Remove(OnDebug, _debug) ?? (_ => { });
            _disposed = true;
        }
    }
}