using KaedePhi.Tool.Converter.RePhiEdit.Model;

namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

public class JudgeLine
{
    /// <summary>
    /// 转换RePhiEdit的JudgeLine到KaedePhi的JudgeLine
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    public static Kpc.JudgeLine ConvertJudgeLine(Rpe.JudgeLine src) => new()
    {
        Name = src.Name,
        Texture = src.Texture,
        Anchor = (float[])src.Anchor.Clone(),
        Father = src.Father,
        IsCover = src.IsCover,
        ZOrder = src.ZOrder,
        AttachUi = src.AttachUi.HasValue ? (Kpc.AttachUi?)(int)src.AttachUi.Value : null,
        IsGif = src.IsGif,
        BpmFactor = src.BpmFactor,
        RotateWithFather = src.RotateWithFather,
        Notes = src.Notes.ConvertAll(Note.ConvertNote),
        EventLayers = src.EventLayers.ConvertAll(EventLayer.ConvertEventLayer),
        Extended = EventLayer.ConvertExtendLayer(src.Extended),
        PositionControls = src.PositionControls.ConvertAll(Control.ConvertXControl),
        AlphaControls = src.AlphaControls.ConvertAll(Control.ConvertAlphaControl),
        SizeControls = src.SizeControls.ConvertAll(Control.ConvertSizeControl),
        SkewControls = src.SkewControls.ConvertAll(Control.ConvertSkewControl),
        YControls = src.YControls.ConvertAll(Control.ConvertYControl)
    };

    public static Rpe.JudgeLine ConvertJudgeLine(Kpc.JudgeLine src, ConvertOption.CuttingOptions options) => new()
    {
        Name = src.Name,
        Texture = src.Texture,
        Anchor = (float[])src.Anchor.Clone(),
        Father = src.Father,
        IsCover = src.IsCover,
        ZOrder = src.ZOrder,
        AttachUi = src.AttachUi.HasValue ? (Rpe.AttachUi?)(int)src.AttachUi.Value : null,
        IsGif = src.IsGif,
        BpmFactor = src.BpmFactor,
        RotateWithFather = src.RotateWithFather,
        Notes = src.Notes.ConvertAll(Note.ConvertNote),
        EventLayers = src.EventLayers.ConvertAll(r => EventLayer.ConvertEventLayer(r, options)),
        Extended = EventLayer.ConvertExtendLayer(src.Extended, options),
        PositionControls = src.PositionControls.ConvertAll(Control.ConvertXControl),
        AlphaControls = src.AlphaControls.ConvertAll(Control.ConvertAlphaControl),
        SizeControls = src.SizeControls.ConvertAll(Control.ConvertSizeControl),
        SkewControls = src.SkewControls.ConvertAll(Control.ConvertSkewControl),
        YControls = src.YControls.ConvertAll(Control.ConvertYControl)
    };
}