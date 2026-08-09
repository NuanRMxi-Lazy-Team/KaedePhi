namespace KaedePhi.Tool.Converter.PhiChain.Model;

/// <summary>
/// KPC 转 Phichain 的转换选项。
/// </summary>
public class KpcToPhiChainConvertOptions
{
    /// <summary>
    /// 是否对 rotateWithFather 为 false 的判定线进行父子线解绑。
    /// </summary>
    public bool UnbindNonRotatingChildren { get; set; } = true;

    /// <summary>
    /// 父子线解绑精度（每拍采样数），默认 64。
    /// </summary>
    public double UnbindPrecision { get; set; } = 64d;

    /// <summary>
    /// 父子线解绑相对原始运动范围的几何容差百分比，默认 0.1%。
    /// </summary>
    public double UnbindTolerance { get; set; } = 0.1d;

    /// <summary>
    /// 父子线解绑是否使用经典模式（等间隔采样），默认 false（自适应采样）。
    /// </summary>
    public bool UnbindClassicMode { get; set; } = false;

    /// <summary>
    /// 多层级合并精度（每拍采样数），默认 64。
    /// </summary>
    public double MultiLayerMergePrecision { get; set; } = 64d;

    /// <summary>
    /// 多层级合并自适应采样容差，默认 0.1。
    /// </summary>
    public double MultiLayerMergeTolerance { get; set; } = 0.1d;

    /// <summary>
    /// 多层级合并是否使用经典模式（等间隔采样），默认 false（自适应采样）。
    /// </summary>
    public bool MultiLayerMergeClassicMode { get; set; } = false;

    #region 缓动截取切割选项

    /// <summary>
    /// 缓动截取切割精度（每拍细分数量），默认 64。
    /// </summary>
    public double EasingCutPrecision { get; set; } = 64d;

    /// <summary>
    /// 缓动截取切割后是否压缩，默认 true。
    /// </summary>
    public bool EasingCutCompress { get; set; } = true;

    /// <summary>
    /// 缓动截取切割后压缩容差百分比，默认 0.1。
    /// </summary>
    public double EasingCutTolerance { get; set; } = 0.1d;

    #endregion
}
