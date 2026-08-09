using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter.PhiFans.Model;

/// <summary>
/// KPC 转 PhiFans 的输出选项。
/// </summary>
public class KpcToPhiFansConvertOptions
{
    /// <summary>
    /// 不支持缓动的处理选项。
    /// </summary>
    public CuttingOptions Cutting { get; set; } = new();

    /// <summary>
    /// 节点式格式中，相邻事件结束值与开始值不连续时，将后事件开始拍向后推迟的拍数精分，实际偏移为 1 / x 拍。
    /// </summary>
    public int DiscontinuityBeatPrecision { get; set; } = Constants.DefaultPrecision;

    /// <summary>
    /// 不支持缓动的处理选项。
    /// </summary>
    public class CuttingOptions
    {
        /// <summary>
        /// 非支持缓动切割精度，默认 64，值越大切割越精细，建议为 2 的倍数。
        /// </summary>
        public int UnsupportedEasingPrecision { get; set; } = Constants.DefaultPrecision;
    }
}
