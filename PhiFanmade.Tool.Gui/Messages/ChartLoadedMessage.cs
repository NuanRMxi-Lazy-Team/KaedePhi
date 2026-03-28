namespace PhiFanmade.Tool.Gui.Messages;

/// <summary>谱面加载完成时通过 WeakReferenceMessenger 广播的消息。</summary>
public sealed class ChartLoadedMessage(CoreRpe.Chart chart, string workspaceId, string? sourcePath = null)
{
    /// <summary>已加载的谱面实例。</summary>
    public CoreRpe.Chart Chart { get; } = chart;

    /// <summary>工作区 ID（其他页面可直接使用此 ID 读取谱面）。</summary>
    public string WorkspaceId { get; } = workspaceId;

    /// <summary>原始文件路径（可选）。</summary>
    public string? SourcePath { get; } = sourcePath;
}

