namespace PhiFanmade.Tool.PhiFanmadeNrc;

/// <summary>
/// NRC 格式工具的日志回调，可从外部订阅以接收处理信息。
/// </summary>
public static class NrcToolLog
{
    public static Action<string> OnInfo    = _ => { };
    public static Action<string> OnWarning = _ => { };
    public static Action<string> OnError   = _ => { };
    public static Action<string> OnDebug   = _ => { };
}

