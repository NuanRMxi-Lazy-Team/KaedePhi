using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.App.Cli.Model;

/// <summary>
/// 格式转换命令默认配置
/// </summary>
public class ConvertConfig
{
    /// <summary>
    /// 转换目标格式
    /// </summary>
    public ChartType TargetType { get; set; } = ChartType.RePhiEdit;

    /// <summary>
    /// 是否美化格式化输出 JSON（仅在输出为文件时生效）
    /// </summary>
    public bool FormatOutput { get; set; } = false;

    /// <summary>
    /// 是否流式输出到文件（大文件推荐）
    /// </summary>
    public bool StreamOutput { get; set; } = false;

    /// <summary>
    /// 是否为干运行（仅计算不写入）
    /// </summary>
    public bool DryRun { get; set; } = false;

    #region PhiEdit 转换选项

    /// <summary>
    /// PE 尾部拍填充量（拍）
    /// </summary>
    public double PeTrailingBeatPadding { get; set; } = 1d / 64d;

    #endregion

    #region KPC -> PhiEdit 转换选项

    /// <summary>
    /// 非支持缓动切割精度
    /// </summary>
    public double PeUnsupportedEasingPrecision { get; set; } = 64d;

    /// <summary>
    /// 非对齐 XY 事件切割精度
    /// </summary>
    public double PeMisalignedXyEventPrecision { get; set; } = 64d;

    /// <summary>
    /// Alpha 事件切割精度
    /// </summary>
    public double PeAlphaCutPrecision { get; set; } = 64d;

    /// <summary>
    /// Alpha 事件切割后是否压缩
    /// </summary>
    public bool PeAlphaCutCompress { get; set; } = true;

    /// <summary>
    /// Alpha 事件切割后压缩容差百分比
    /// </summary>
    public double PeAlphaCutTolerance { get; set; } = 0.1d;

    /// <summary>
    /// 速度事件切割精度
    /// </summary>
    public double PeSpeedCutPrecision { get; set; } = 64d;

    /// <summary>
    /// 速度事件切割后是否压缩
    /// </summary>
    public bool PeSpeedCutCompress { get; set; } = true;

    /// <summary>
    /// 速度事件切割后压缩容差百分比
    /// </summary>
    public double PeSpeedCutTolerance { get; set; } = 0.1d;

    #endregion

    #region KPC -> PhigrosV3 转换选项

    /// <summary>
    /// PhigrosV3 默认 BPM（当谱面 BPM 列表为空时使用）
    /// </summary>
    public float PhigrosDefaultBpm { get; set; } = 120f;

    /// <summary>
    /// PhigrosV3 非支持缓动切割精度
    /// </summary>
    public double PhigrosEasingPrecision { get; set; } = 64d;

    /// <summary>
    /// PhigrosV3 非对齐 XY 事件切割精度
    /// </summary>
    public double PhigrosMisalignedXyEventPrecision { get; set; } = 64d;

    /// <summary>
    /// PhigrosV3 Alpha 事件切割精度
    /// </summary>
    public double PhigrosAlphaCutPrecision { get; set; } = 64d;

    /// <summary>
    /// PhigrosV3 Alpha 事件切割后是否压缩
    /// </summary>
    public bool PhigrosAlphaCutCompress { get; set; } = true;

    /// <summary>
    /// PhigrosV3 Alpha 事件切割后压缩容差百分比
    /// </summary>
    public double PhigrosAlphaCutTolerance { get; set; } = 0.1d;

    /// <summary>
    /// PhigrosV3 速度事件切割精度
    /// </summary>
    public double PhigrosSpeedCutPrecision { get; set; } = 64d;

    /// <summary>
    /// 是否过滤假音符（直接删除 IsFake=true 的音符）
    /// </summary>
    public bool PhigrosFilterFakeNotes { get; set; } = false;

    /// <summary>
    /// 是否启用负不透明度段判定线抬高
    /// </summary>
    public bool PhigrosNegativeAlphaElevation { get; set; } = false;

    /// <summary>
    /// 负不透明度段每次抬高的 KPC 坐标系 Y 偏移量
    /// </summary>
    public double PhigrosNegativeAlphaStep { get; set; } = 4.0;

    #endregion

    #region 通用父子线解绑选项

    /// <summary>
    /// 父子线解绑精度
    /// </summary>
    public double UnbindPrecision { get; set; } = 64d;

    /// <summary>
    /// 父子线解绑容差百分比
    /// </summary>
    public double UnbindTolerance { get; set; } = 0.1d;

    /// <summary>
    /// 父子线解绑是否使用经典模式
    /// </summary>
    public bool UnbindClassicMode { get; set; } = false;

    #endregion

    #region 通用多层级合并选项

    /// <summary>
    /// 多层级合并精度
    /// </summary>
    public double MultiLayerMergePrecision { get; set; } = 64d;

    /// <summary>
    /// 多层级合并容差百分比
    /// </summary>
    public double MultiLayerMergeTolerance { get; set; } = 0.1d;

    /// <summary>
    /// 多层级合并是否使用经典模式
    /// </summary>
    public bool MultiLayerMergeClassicMode { get; set; } = false;

    #endregion
}
