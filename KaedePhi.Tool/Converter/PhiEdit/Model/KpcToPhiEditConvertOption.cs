using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter.PhiEdit.Model;

public class KpcToPhiEditConvertOptions
{
    public const double DefaultPrecision = Constants.DefaultPrecision;
    public const double DefaultTolerancePercent = Constants.DefaultTolerancePercent;

    /// <summary>
    /// 事件切割相关配置
    /// </summary>
    public CuttingOptions Cutting { get; set; } = new();

    /// <summary>
    /// Alpha 事件相关配置
    /// </summary>
    public AlphaOptions Alpha { get; set; } = new();

    /// <summary>
    /// 速度事件相关配置
    /// </summary>
    public SpeedOptions Speed { get; set; } = new();

    /// <summary>
    /// 父子线解绑相关配置
    /// </summary>
    public FatherLineUnbindOptions FatherLineUnbind { get; set; } = new();

    /// <summary>
    /// 多层级合并相关配置
    /// </summary>
    public MultiLayerMergeOptions MultiLayerMerge { get; set; } = new();

    /// <summary>
    /// 判定线过滤相关配置
    /// </summary>
    public LineFilterOptions LineFilter { get; set; } = new();

    /// <summary>
    /// PE 速度帧值到 KPC 速度事件值的转换比率。
    /// PE 速度值 = KPC 速度值 * SpeedConversionRatio。
    /// </summary>
    public double SpeedConversionRatio { get; set; } = 14d / 9d;

    /// <summary>
    /// 尾部拍填充量（拍），用于确保事件覆盖到判定线时间范围末端。
    /// </summary>
    public double TrailingBeatPadding { get; set; } = 1d / 64d;

    public class CuttingOptions
    {
        /// <summary>
        /// 非支持缓动切割精度
        /// </summary>
        public double UnsupportedEasingPrecision { get; set; } = DefaultPrecision;

        /// <summary>
        /// 非对齐 XY 事件切割精度
        /// </summary>
        public double MisalignedXyEventPrecision { get; set; } = DefaultPrecision;
    }

    public class AlphaOptions
    {
        /// <summary>
        /// 带有缓动效果的 Alpha 事件切割精度
        /// </summary>
        public double CutPrecision { get; set; } = DefaultPrecision;

        /// <summary>
        /// 带有缓动效果的 Alpha 事件切割后是否压缩
        /// </summary>
        public bool CutCompress { get; set; } = true;

        /// <summary>
        /// 带有缓动效果的 Alpha 事件切割后压缩容差百分比
        /// </summary>
        public double CutTolerance { get; set; } = DefaultTolerancePercent;
    }

    public class SpeedOptions
    {
        /// <summary>
        /// 速度事件切割精度
        /// </summary>
        public double CutPrecision { get; set; } = DefaultPrecision;

        /// <summary>
        /// 速度事件切割后是否压缩
        /// </summary>
        public bool CutCompress { get; set; } = true;

        /// <summary>
        /// 速度事件切割后压缩拟合容差百分比
        /// </summary>
        public double CutTolerance { get; set; } = DefaultTolerancePercent;
    }

    public class FatherLineUnbindOptions
    {
        private bool _classicMode;
        private bool _compress = true;

        /// <summary>
        /// 遇到父子线时的解绑采样精度（每拍采样数）
        /// </summary>
        public double Precision { get; set; } = DefaultPrecision;

        /// <summary>
        /// 遇到父子线时是否使用经典等间隔采样模式。
        /// 自适应模式会根据容差直接生成压缩后的结果。
        /// </summary>
        public bool ClassicMode
        {
            get => _classicMode;
            set
            {
                _classicMode = value;
                if (!_classicMode && !_compress)
                {
                    _compress = true;
                }
            }
        }

        /// <summary>
        /// 父子线解绑相对原始运动范围的几何容差百分比。
        /// 自适应模式用于决定切段，经典模式用于压缩等间隔采样结果。
        /// </summary>
        public double Tolerance { get; set; } = 1d;

        /// <summary>
        /// 在经典模式下是否对等间隔采样结果进行压缩。
        /// 自适应模式已经完成压缩，不会执行额外压缩。
        /// </summary>
        public bool Compress
        {
            get => _compress;
            set
            {
                _compress = value;
                if (!_compress)
                {
                    _classicMode = true;
                }
            }
        }
    }

    public class MultiLayerMergeOptions
    {
        private bool _classicMode;
        private bool _compress = true;

        /// <summary>
        /// 遇到多层级时的合并精度
        /// </summary>
        public double Precision { get; set; } = DefaultPrecision;

        /// <summary>
        /// 遇到多层级时合并后压缩拟合容差百分比
        /// </summary>
        public double Tolerance { get; set; } = 0.1d;

        /// <summary>
        /// 遇到多层级时是否使用经典模式。
        /// 当 Compress 为 false 时，该值会被强制为 true。
        /// </summary>
        public bool ClassicMode
        {
            get => _classicMode;
            set
            {
                _classicMode = value;
                if (!_classicMode && !_compress)
                {
                    _compress = true;
                }
            }
        }

        /// <summary>
        /// 在启用经典模式的情况下，是否对合并层级后的事件列表进行压缩
        /// 当该值为 false 时，ClassicMode 会被强制为 true。
        /// </summary>
        public bool Compress
        {
            get => _compress;
            set
            {
                _compress = value;
                if (!_compress)
                {
                    _classicMode = true;
                }
            }
        }
    }

    public class LineFilterOptions
    {
        /// <summary>
        /// 是否删除带有绑定 UI 的判定线
        /// </summary>
        public bool RemoveAttachUiLine { get; set; }

        /// <summary>
        /// 是否移除带有自定义材质的判定线
        /// </summary>
        public bool RemoveTextureLine { get; set; }
    }
}
