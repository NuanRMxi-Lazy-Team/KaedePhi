using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter.PhiFans.Model;

/// <summary>
/// KPC 转 PhiFans 的输出选项。
/// </summary>
public class KpcToPhiFansConvertOptions
{
    /// <summary>
    /// 多事件层合并选项。
    /// </summary>
    public MultiLayerMergeOptions MultiLayerMerge { get; set; } = new();

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

    /// <summary>
    /// 多事件层合并选项。
    /// </summary>
    public class MultiLayerMergeOptions
    {
        private bool _classicMode;
        private bool _compress = true;

        /// <summary>
        /// 多事件层合并采样精度。
        /// </summary>
        public double Precision { get; set; } = Constants.DefaultPrecision;

        /// <summary>
        /// 多事件层合并拟合容差百分比。
        /// </summary>
        public double Tolerance { get; set; } = Constants.DefaultTolerancePercent;

        /// <summary>
        /// 是否使用经典合并模式；关闭压缩时会强制启用经典模式。
        /// </summary>
        public bool ClassicMode
        {
            get => _classicMode;
            set
            {
                _classicMode = value;
                if (!_classicMode && !_compress)
                    _compress = true;
            }
        }

        /// <summary>
        /// 经典合并模式下是否压缩事件；关闭时会强制启用经典模式。
        /// </summary>
        public bool Compress
        {
            get => _compress;
            set
            {
                _compress = value;
                if (!_compress)
                    _classicMode = true;
            }
        }
    }
}
