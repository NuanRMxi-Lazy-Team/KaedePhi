using SkiaSharp;

namespace KaedePhi.Tool.KaedePhi.Render;

/// <summary>
/// NRC谱面事件通道渲染配置
/// </summary>
public class RenderOptions
{
    /// <summary>每拍对应的像素高度</summary>
    public float PixelsPerBeat { get; set; } = 100f;

    /// <summary>每个通道的宽度（像素）</summary>
    public int ChannelWidth { get; set; } = 150;

    /// <summary>每个事件的采样点数（越高曲线越平滑）</summary>
    public int SamplesPerEvent { get; set; } = 64;

    /// <summary>
    /// 每拍的细分格线数。
    /// 1 = 只绘制节拍线（每拍一条）；
    /// 4 = 每拍绘制四条（四分音符位置）；
    /// 默认 2（半拍）。
    /// </summary>
    public int BeatSubdivisions { get; set; } = 2;

    /// <summary>曲线描边宽度</summary>
    public float StrokeWidth { get; set; } = 2f;

    /// <summary>左侧留白宽度（用于绘制拍号标注）</summary>
    public int LeftMargin { get; set; } = 64;

    /// <summary>顶部留白高度（用于绘制通道标题）</summary>
    public int HeaderHeight { get; set; } = 44;

    /// <summary>底部留白</summary>
    public int BottomPadding { get; set; } = 24;

    /// <summary>通道之间的间距</summary>
    public int ChannelPadding { get; set; } = 4;

    /// <summary>背景颜色</summary>
    public SKColor BackgroundColor { get; set; } = new(22, 22, 30);

    /// <summary>整拍格线颜色</summary>
    public SKColor BeatGridColor { get; set; } = new(65, 65, 80);

    /// <summary>半拍格线颜色（较暗）</summary>
    public SKColor SubBeatGridColor { get; set; } = new(40, 40, 52);

    /// <summary>通道背景颜色</summary>
    public SKColor ChannelBackgroundColor { get; set; } = new(32, 32, 46);

    /// <summary>通道中心参考线颜色</summary>
    public SKColor CenterLineColor { get; set; } = new(80, 80, 100);

    /// <summary>事件背景块颜色（半透明）</summary>
    public SKColor EventBlockColor { get; set; } = new(255, 255, 255, 18);

    /// <summary>事件间连接线颜色</summary>
    public SKColor ConnectionColor { get; set; } = new(160, 160, 160, 140);

    /// <summary>标注文字颜色</summary>
    public SKColor TextColor { get; set; } = new(180, 180, 190);

    // ── 通道曲线颜色 ──
    /// <summary>MoveX通道颜色</summary>
    public SKColor MoveXColor { get; set; } = new(90, 180, 255);

    /// <summary>MoveY通道颜色</summary>
    public SKColor MoveYColor { get; set; } = new(80, 230, 160);

    /// <summary>Rotate通道颜色</summary>
    public SKColor RotateColor { get; set; } = new(255, 200, 80);

    /// <summary>Alpha通道颜色</summary>
    public SKColor AlphaColor { get; set; } = new(200, 100, 255);

    /// <summary>Speed通道颜色</summary>
    public SKColor SpeedColor { get; set; } = new(255, 110, 110);
}

