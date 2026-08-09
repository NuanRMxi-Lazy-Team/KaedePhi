using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.App.Config;

/// <summary>
/// 格式转换的默认参数。
/// </summary>
public sealed class ConvertDefaultsConfig
{
    /// <summary>转换目标格式。</summary>
    public ChartType TargetType { get; set; } = ChartType.RePhiEdit;

    /// <summary>是否美化格式化输出 JSON（CLI 使用）。</summary>
    public bool FormatOutput { get; set; }

    /// <summary>是否流式输出到文件（CLI 使用）。</summary>
    public bool StreamOutput { get; set; }

    /// <summary>是否为干运行（仅计算不写入，CLI 使用）。</summary>
    public bool DryRun { get; set; }

    // PhiEdit 转换选项
    public double PeTrailingBeatPadding { get; set; } = 1d / 64d;
    public double PeUnsupportedEasingPrecision { get; set; } = 64d;
    public double PeMisalignedXyEventPrecision { get; set; } = 64d;
    public double PeAlphaCutPrecision { get; set; } = 64d;
    public bool PeAlphaCutCompress { get; set; } = true;
    public double PeAlphaCutTolerance { get; set; } = 0.1d;
    public double PeSpeedCutPrecision { get; set; } = 64d;
    public bool PeSpeedCutCompress { get; set; } = true;
    public double PeSpeedCutTolerance { get; set; } = 0.1d;

    // PhigrosV3 转换选项
    public float PhigrosDefaultBpm { get; set; } = 120f;
    public double PhigrosEasingPrecision { get; set; } = 64d;
    public double PhigrosMisalignedXyEventPrecision { get; set; } = 64d;
    public double PhigrosAlphaCutPrecision { get; set; } = 64d;
    public bool PhigrosAlphaCutCompress { get; set; } = true;
    public double PhigrosAlphaCutTolerance { get; set; } = 0.1d;
    public double PhigrosSpeedCutPrecision { get; set; } = 64d;
    public bool PhigrosFilterFakeNotes { get; set; }
    public bool PhigrosNegativeAlphaElevation { get; set; }
    public double PhigrosNegativeAlphaStep { get; set; } = 4.0d;

    // PhiFans 转换选项
    public int PhiFansUnsupportedEasingPrecision { get; set; } = 64;
    public int PhiFansDiscontinuityBeatPrecision { get; set; } = 64;

    // 通用选项
    public double UnbindPrecision { get; set; } = 64d;
    public double UnbindTolerance { get; set; } = 1d;
    public double UnbindMergeTolerance { get; set; } = 0.1d;
    public bool UnbindClassicMode { get; set; }
    public double MultiLayerMergePrecision { get; set; } = 64d;
    public double MultiLayerMergeTolerance { get; set; } = 0.2d;
    public bool MultiLayerMergeClassicMode { get; set; }
}
