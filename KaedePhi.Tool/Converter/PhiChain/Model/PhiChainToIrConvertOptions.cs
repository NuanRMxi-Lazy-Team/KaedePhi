namespace KaedePhi.Tool.Converter.PhiChain.Model;

/// <summary>
/// Phichain 转 IR 的转换选项。
/// </summary>
public class PhiChainToIrConvertOptions
{
    /// <summary>
    /// 不支持的缓动切段精度（每拍细分数量），默认 64。
    /// </summary>
    public int UnsupportedEasingPrecision { get; set; } = 64;
}
