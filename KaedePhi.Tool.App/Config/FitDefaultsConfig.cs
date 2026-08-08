namespace KaedePhi.Tool.App.Config;

/// <summary>
/// 事件拟合工具的默认参数。
/// </summary>
public sealed class FitDefaultsConfig
{
    /// <summary>
    /// 容差百分比，取值范围 [0, 100]。默认值 0.1（即 0.1%）。
    /// </summary>
    public double Tolerance { get; set; } = 0.1;

    /// <summary>是否为干运行（仅计算不写入，CLI 使用）。</summary>
    public bool DryRun { get; set; }
}