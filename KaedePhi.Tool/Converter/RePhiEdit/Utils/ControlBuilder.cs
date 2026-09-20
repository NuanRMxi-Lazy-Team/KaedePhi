namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

/// <summary>
/// RPE 与 IR 控制点之间的双向转换工具。
/// </summary>
public static class ControlBuilder
{
    public static IrControls.XControl ConvertXControl(RpeControls.XControl src) =>
        new()
        {
            Easing = EasingConverter.ConvertEasing(src.Easing),
            X = src.X,
            Pos = src.Pos,
        };

    public static RpeControls.XControl ConvertXControl(IrControls.XControl src) =>
        new()
        {
            Easing = EasingConverter.ConvertEasing(src.Easing),
            X = src.X,
            Pos = src.Pos,
        };

    public static IrControls.AlphaControl ConvertAlphaControl(RpeControls.AlphaControl src) =>
        new()
        {
            Easing = EasingConverter.ConvertEasing(src.Easing),
            X = src.X,
            Alpha = src.Alpha,
        };

    public static RpeControls.AlphaControl ConvertAlphaControl(IrControls.AlphaControl src) =>
        new()
        {
            Easing = EasingConverter.ConvertEasing(src.Easing),
            X = src.X,
            Alpha = src.Alpha,
        };

    public static IrControls.SizeControl ConvertSizeControl(RpeControls.SizeControl src) =>
        new()
        {
            Easing = EasingConverter.ConvertEasing(src.Easing),
            X = src.X,
            Size = src.Size,
        };

    public static RpeControls.SizeControl ConvertSizeControl(IrControls.SizeControl src) =>
        new()
        {
            Easing = EasingConverter.ConvertEasing(src.Easing),
            X = src.X,
            Size = src.Size,
        };

    public static IrControls.SkewControl ConvertSkewControl(RpeControls.SkewControl src) =>
        new()
        {
            Easing = EasingConverter.ConvertEasing(src.Easing),
            X = src.X,
            Skew = src.Skew,
        };

    public static RpeControls.SkewControl ConvertSkewControl(IrControls.SkewControl src) =>
        new()
        {
            Easing = EasingConverter.ConvertEasing(src.Easing),
            X = src.X,
            Skew = src.Skew,
        };

    public static IrControls.YControl ConvertYControl(RpeControls.YControl src) =>
        new()
        {
            Easing = EasingConverter.ConvertEasing(src.Easing),
            X = src.X,
            Y = src.Y,
        };

    public static RpeControls.YControl ConvertYControl(IrControls.YControl src) =>
        new()
        {
            Easing = EasingConverter.ConvertEasing(src.Easing),
            X = src.X,
            Y = src.Y,
        };
}
