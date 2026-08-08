namespace KaedePhi.Tool.App.Config;

/// <summary>
/// 解绑、层级合并、切割工具的默认参数。
/// </summary>
public sealed class ToolDefaultsConfig
{
    public double Precision { get; set; } = 64;
    public double Tolerance { get; set; } = 0.1;
    public bool ClassicMode { get; set; }
    public bool DisableCompress { get; set; }

    /// <summary>是否为干运行（仅计算不写入，CLI 使用）。</summary>
    public bool DryRun { get; set; }
}