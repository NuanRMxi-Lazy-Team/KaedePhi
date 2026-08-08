namespace KaedePhi.Tool.App.Config;

/// <summary>
/// 解绑、层级合并、切割工具的默认参数。
/// </summary>
public sealed class ToolDefaultsConfig
{
    /// <summary>
    /// 精度
    /// </summary>
    public double Precision { get; set; } = 64;

    /// <summary>
    /// 容差
    /// </summary>
    public double Tolerance { get; set; } = 0.1;

    /// <summary>
    /// 经典模式
    /// </summary>
    public bool ClassicMode { get; set; }

    /// <summary>
    /// 禁用压缩
    /// </summary>
    public bool DisableCompress { get; set; }

    /// <summary>
    /// 是否为干运行（仅计算不写入，CLI 使用）。
    /// </summary>
    public bool DryRun { get; set; }
}