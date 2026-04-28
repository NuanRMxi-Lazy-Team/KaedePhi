namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

public static class Control
{
    #region RpeToKpc

    public static Kpc.XControl ConvertXControl(Rpe.XControl src) =>
        new() { Easing = Easing.ConvertEasing(src.Easing), X = src.X, Pos = src.Pos };

    public static Kpc.AlphaControl ConvertAlphaControl(Rpe.AlphaControl src) => new()
        { Easing = Easing.ConvertEasing(src.Easing), X = src.X, Alpha = src.Alpha };

    public static Kpc.SizeControl ConvertSizeControl(Rpe.SizeControl src) =>
        new() { Easing = Easing.ConvertEasing(src.Easing), X = src.X, Size = src.Size };

    public static Kpc.SkewControl ConvertSkewControl(Rpe.SkewControl src) =>
        new() { Easing = Easing.ConvertEasing(src.Easing), X = src.X, Skew = src.Skew };

    public static Kpc.YControl ConvertYControl(Rpe.YControl src) =>
        new() { Easing = Easing.ConvertEasing(src.Easing), X = src.X, Y = src.Y };

    #endregion

    #region KpcToRpe

    public static Rpe.XControl ConvertXControl(Kpc.XControl src) =>
        new() { Easing = Easing.ConvertEasing(src.Easing), X = src.X, Pos = src.Pos };

    public static Rpe.AlphaControl ConvertAlphaControl(Kpc.AlphaControl src) => new()
        { Easing = Easing.ConvertEasing(src.Easing), X = src.X, Alpha = src.Alpha };

    public static Rpe.SizeControl ConvertSizeControl(Kpc.SizeControl src) =>
        new() { Easing = Easing.ConvertEasing(src.Easing), X = src.X, Size = src.Size };

    public static Rpe.SkewControl ConvertSkewControl(Kpc.SkewControl src) =>
        new() { Easing = Easing.ConvertEasing(src.Easing), X = src.X, Skew = src.Skew };

    public static Rpe.YControl ConvertYControl(Kpc.YControl src) =>
        new() { Easing = Easing.ConvertEasing(src.Easing), X = src.X, Y = src.Y };

    #endregion
}