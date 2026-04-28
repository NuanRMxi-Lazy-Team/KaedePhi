namespace KaedePhi.Tool.Converter.RePhiEdit.Model;

public class ConvertOption
{
    /// <summary>
    /// 默认缓动切割精度
    /// </summary>
    public const int DefaultPrecision = 64;
    public CuttingOptions Cutting { get; set; } = new();

    public class CuttingOptions
    {
        /// <summary>
        /// 非支持缓动切割精度，默认<see cref="DefaultPrecision"/>，值越大切割越精细，建议为2的倍数
        /// </summary>
        public int UnsupportedEasingPrecision { get; set; } = DefaultPrecision;
    }
}