namespace KaedePhi.Tool.App.Gui.Models;

public sealed class GuiAppConfig
{
    public int MaxLogFiles { get; set; } = 5;
    public ToolDefaultsConfig Unbind { get; set; } =
        new()
        {
            Precision = 64,
            Tolerance = 5,
            ClassicMode = false,
            DisableCompress = false,
        };
    public ToolDefaultsConfig LayerMerge { get; set; } =
        new()
        {
            Precision = 64,
            Tolerance = 0.2,
            ClassicMode = false,
            DisableCompress = false,
        };
    public ToolDefaultsConfig Cut { get; set; } =
        new()
        {
            Precision = 64,
            Tolerance = 0.1,
            DisableCompress = false,
        };
    public FitDefaultsConfig Fit { get; set; } = new();
    public RenderDefaultsConfig Render { get; set; } = new();
    public ConvertDefaultsConfig Convert { get; set; } = new();
}

public sealed class ToolDefaultsConfig
{
    public double Precision { get; set; } = 64;
    public double Tolerance { get; set; } = 0.1;
    public bool ClassicMode { get; set; }
    public bool DisableCompress { get; set; }
}

public sealed class FitDefaultsConfig
{
    /// <summary>
    /// 容差百分比，取值范围 [0, 100]。默认值 0.1（即 0.1%）。
    /// </summary>
    public double Tolerance { get; set; } = 0.1;
}

public sealed class RenderDefaultsConfig
{
    public int PixelsPerBeat { get; set; } = 100;
    public int ChannelWidth { get; set; } = 150;
    public int SamplesPerEvent { get; set; } = 64;
    public int BeatSubdivisions { get; set; } = 2;
    public double RangePaddingRatio { get; set; } = 0.10;
    public int RangeSamplesPerEvent { get; set; } = 16;
    public double SegmentGroupTolerance { get; set; } = 1e-6;
    public double MinValueRangeHalf { get; set; } = 0.1;
    public double MinValueRangeHalfRatio { get; set; } = 0.15;
}

public sealed class ConvertDefaultsConfig
{
    // PhiEdit 转换选项
    public double PeTrailingBeatPadding { get; set; } = 1d / 64d;
    public double PeUnsupportedEasingPrecision { get; set; } = 64d;
    public double PeMisalignedXyEventPrecision { get; set; } = 64d;
    public double PeAlphaCutPrecision { get; set; } = 64d;
    public double PeAlphaCutTolerance { get; set; } = 0.1d;
    public double PeSpeedCutPrecision { get; set; } = 64d;
    public double PeSpeedCutTolerance { get; set; } = 0.1d;

    // PhigrosV3 转换选项
    public float PhigrosDefaultBpm { get; set; } = 120f;
    public double PhigrosEasingPrecision { get; set; } = 64d;
    public double PhigrosMisalignedXyEventPrecision { get; set; } = 64d;
    public double PhigrosAlphaCutPrecision { get; set; } = 64d;
    public double PhigrosAlphaCutTolerance { get; set; } = 0.1d;
    public double PhigrosSpeedCutPrecision { get; set; } = 64d;
    public bool PhigrosFilterFakeNotes { get; set; }
    public bool PhigrosNegativeAlphaElevation { get; set; }
    public double PhigrosNegativeAlphaStep { get; set; } = 4.0d;

    // 通用选项
    public double UnbindPrecision { get; set; } = 64d;
    public double UnbindTolerance { get; set; } = 5d;
    public bool UnbindClassicMode { get; set; }
    public double MultiLayerMergePrecision { get; set; } = 64d;
    public double MultiLayerMergeTolerance { get; set; } = 0.2d;
    public bool MultiLayerMergeClassicMode { get; set; }
}
