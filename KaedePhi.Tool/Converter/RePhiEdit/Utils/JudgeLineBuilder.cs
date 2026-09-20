using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Converter.RePhiEdit.Model;

namespace KaedePhi.Tool.Converter.RePhiEdit.Utils;

public static class JudgeLineBuilder
{
    /// <summary>
    /// 转换RePhiEdit的JudgeLine到KaedePhi的JudgeLine
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    public static Ir.JudgeLine ConvertJudgeLine(Rpe.JudgeLine src) =>
        new()
        {
            Name = src.Name,
            Texture = src.Texture,
            Anchor = (float[])src.Anchor.Clone(),
            Father = src.Father,
            IsCover = src.IsCover,
            ZOrder = src.ZOrder,
            AttachUi = src.AttachUi.HasValue ? (AttachUi?)(int)src.AttachUi.Value : null,
            IsGif = src.IsGif,
            BpmFactor = src.BpmFactor,
            RotateWithFather = src.RotateWithFather,
            Notes = src.Notes is not null ? src.Notes.ConvertAll(NoteBuilder.ConvertNote) : [],
            EventLayers = src.EventLayers.ConvertAll(EventLayerBuilder.ConvertEventLayer),
            Extended =
                EventLayerBuilder.ConvertExtendLayer(src.Extended) ?? new IrEvents.ExtendLayer(),
            PositionControls = src.PositionControls.ConvertAll(ControlBuilder.ConvertXControl),
            AlphaControls = src.AlphaControls.ConvertAll(ControlBuilder.ConvertAlphaControl),
            SizeControls = src.SizeControls.ConvertAll(ControlBuilder.ConvertSizeControl),
            SkewControls = src.SkewControls.ConvertAll(ControlBuilder.ConvertSkewControl),
            YControls = src.YControls.ConvertAll(ControlBuilder.ConvertYControl),
        };

    public static Rpe.JudgeLine ConvertJudgeLine(
        Ir.JudgeLine src,
        ConvertOption.CuttingOptions options
    ) =>
        new()
        {
            Name = src.Name,
            Texture = src.Texture,
            Anchor = (float[])src.Anchor.Clone(),
            Father = src.Father,
            IsCover = src.IsCover,
            ZOrder = src.ZOrder,
            AttachUi = src.AttachUi.HasValue ? (AttachUi?)(int)src.AttachUi.Value : null,
            IsGif = src.IsGif,
            BpmFactor = src.BpmFactor,
            RotateWithFather = src.RotateWithFather,
            Notes = src.Notes.ConvertAll(NoteBuilder.ConvertNote),
            EventLayers = src.EventLayers.ConvertAll(r =>
                EventLayerBuilder.ConvertEventLayer(r, options)
            ),
            Extended =
                EventLayerBuilder.ConvertExtendLayer(src.Extended, options)
                ?? new RpeEvents.ExtendLayer(),
            PositionControls = src.PositionControls.ConvertAll(ControlBuilder.ConvertXControl),
            AlphaControls = src.AlphaControls.ConvertAll(ControlBuilder.ConvertAlphaControl),
            SizeControls = src.SizeControls.ConvertAll(ControlBuilder.ConvertSizeControl),
            SkewControls = src.SkewControls.ConvertAll(ControlBuilder.ConvertSkewControl),
            YControls = src.YControls.ConvertAll(ControlBuilder.ConvertYControl),
        };
}
