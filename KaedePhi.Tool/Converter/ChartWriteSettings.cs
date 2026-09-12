namespace KaedePhi.Tool.Converter;

/// <summary>
/// 谱面写出设置，描述导出时的文件写入方式。
/// </summary>
public sealed record ChartWriteSettings
{
    /// <summary>是否使用流式写入（适合大文件，降低内存占用）。</summary>
    public bool UseStream { get; init; }

    /// <summary>是否格式化输出（对文本格式无效）。</summary>
    public bool Indented { get; init; }

    /// <summary>是否为演习模式：完成序列化但跳过文件写入。</summary>
    public bool DryRun { get; init; }
}