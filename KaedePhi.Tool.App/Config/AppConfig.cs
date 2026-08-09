namespace KaedePhi.Tool.App.Config;

/// <summary>
/// 应用全局配置，CLI 与 GUI 共用同一模型与同一配置文件。
/// </summary>
public sealed class AppConfig
{
    /// <summary>
    /// CLI 日志级别：0 = 关闭, 1 = Debug, 2 = Info, 3 = Warning, 4 = Error。
    /// </summary>
    public uint LogLevel { get; set; } = 3;

    /// <summary>
    /// GUI 日志文件保留数量。
    /// </summary>
    public int MaxLogFiles { get; set; } = 5;

    public ToolDefaultsConfig Unbind { get; set; } =
        new()
        {
            Precision = 64,
            Tolerance = 1,
            ClassicMode = false,
            DisableCompress = false,
        };

    public ToolDefaultsConfig LayerMerge { get; set; } =
        new()
        {
            Precision = 64,
            Tolerance = 0.2,
            ClassicMode = false,
            DisableCompress = false,
        };

    public ToolDefaultsConfig Cut { get; set; } =
        new()
        {
            Precision = 64,
            Tolerance = 0.1,
            DisableCompress = false,
        };

    public FitDefaultsConfig Fit { get; set; } = new();
    public RenderDefaultsConfig Render { get; set; } = new();
    public ConvertDefaultsConfig Convert { get; set; } = new();
}
