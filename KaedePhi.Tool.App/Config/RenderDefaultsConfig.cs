namespace KaedePhi.Tool.App.Config;

/// <summary>
/// 事件通道渲染工具的默认参数。
/// </summary>
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

    /// <summary>默认输出目录（CLI 使用，为空时使用输入文件所在目录）。</summary>
    public string OutputDir { get; set; } = "";
}