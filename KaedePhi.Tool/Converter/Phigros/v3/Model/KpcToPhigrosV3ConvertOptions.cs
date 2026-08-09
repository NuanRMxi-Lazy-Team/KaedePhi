using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Converter.Phigros.v3.Model;

public class KpcToPhigrosV3ConvertOptions
{
    public const double DefaultPrecision = Constants.DefaultPrecision;
    public const double DefaultTolerancePercent = Constants.DefaultTolerancePercent;
    public const float DefaultGlobalBpm = 120f;

    /// <summary>
    /// 当谱面 BPM 列表为空时使用的默认全局 BPM。
    /// </summary>
    public float DefaultBpm { get; set; } = DefaultGlobalBpm;

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
    /// 音符过滤相关配置
    /// </summary>
    public NoteFilterOptions NoteFilter { get; set; } = new();

    /// <summary>
    /// 负不透明度段判定线抬高相关配置
    /// </summary>
    public NegativeAlphaOptions NegativeAlpha { get; set; } = new();

    public class CuttingOptions
    {
        /// <summary>
        /// 非支持缓动切割精度
        /// </summary>
        public double EasingPrecision { get; set; } = DefaultPrecision;

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
        /// 自适应模式下父子线事件通道合并容差百分比，默认 0.1%。
        /// </summary>
        public double MergeTolerance { get; set; } = DefaultTolerancePercent;

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

    public class NoteFilterOptions
    {
        /// <summary>
        /// 是否过滤假音符。为 true 时直接删除 IsFake=true 的音符，为 false 时视为真音符保留。
        /// </summary>
        public bool FilterFakeNotes { get; set; }
    }

    public class NegativeAlphaOptions
    {
        public const double DefaultElevationStep = 4.0;

        /// <summary>
        /// 是否启用负不透明度段判定线抬高。
        /// 当判定线不透明度为负值时，将判定线抬高至屏幕外。
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 每次抬高的 KPC 坐标系 Y 偏移量（默认 4.0，约等于两个屏幕高度）。
        /// </summary>
        public double ElevationStep { get; set; } = DefaultElevationStep;

        /// <summary>
        /// 抬高操作使用的渲染坐标系配置。
        /// 默认使用标准 675×450 编辑器坐标。
        /// </summary>
        public CoordinateProfile RenderProfile { get; set; } =
            CoordinateProfile.DefaultRenderProfile;
    }
}
