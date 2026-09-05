using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.JudgeLines.KaedePhi;
using KaedePhi.Tool.Layer.KaedePhi;
using ExtendLayer = KaedePhi.Core.KaedePhi.Events.ExtendLayer;
using KpcJudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

/// <summary>
/// KPC 判定线到 PE 判定线的构建器。
/// </summary>
public class PhiEditJudgeLineBuilder
{
    private readonly KpcToPhiEditConvertOptions _options;
    private readonly LineEventBuilder _eventBuilder;
    private readonly LayerProcessor _layerProcessor = new();
    private readonly Action<string>? _warnLogger;

    public PhiEditJudgeLineBuilder(KpcToPhiEditConvertOptions options, Action<string>? warnLogger)
    {
        _options = options;
        _eventBuilder = new LineEventBuilder(options, warnLogger);
        _warnLogger = warnLogger;
    }

    /// <summary>
    /// 转换单条判定线，并在转换前记录 PE 不支持字段的告警。
    /// </summary>
    /// <param name="src">待转换的 KPC 判定线</param>
    /// <param name="allLine">用于解除父子绑定的原始判定线列表</param>
    /// <returns>转换后的 PE 判定线；匹配过滤条件时返回空值</returns>
    public Pe.JudgeLine? ConvertJudgeLine(KpcJudgeLine src, List<KpcJudgeLine> allLine)
    {
        WarnIfUnsupportedJudgeLineFields(src);

        if (
            (
                _options.LineFilter.RemoveTextureLine
                && !string.Equals(
                    src.Texture,
                    CoreConstants.DefaultTexture,
                    StringComparison.Ordinal
                )
            ) || (_options.LineFilter.RemoveAttachUiLine && src.AttachUi.HasValue)
        )
            return null;

        var trueSrc = src;
        var pe = new Pe.JudgeLine
        {
            NoteList = trueSrc.Notes.ConvertAll(n => NoteBuilder.ConvertNote(n, _warnLogger)),
        };

        if (trueSrc.Father != -1)
        {
            var unbinder = new JudgeLineUnbinder();
            if (_options.FatherLineUnbind.ClassicMode)
            {
                trueSrc = unbinder.FatherUnbind(
                    allLine.FindIndex(l => ReferenceEquals(l, src)),
                    allLine,
                    _options.FatherLineUnbind.Precision
                );
            }
            else
                trueSrc = unbinder.FatherUnbindDynamic(
                    allLine.FindIndex(l => ReferenceEquals(l, src)),
                    allLine,
                    _options.FatherLineUnbind.Precision,
                    _options.FatherLineUnbind.Tolerance,
                    _options.FatherLineUnbind.MergeTolerance
                );

            if (_options.FatherLineUnbind is { ClassicMode: true, Compress: true })
            {
                foreach (var layer in trueSrc.EventLayers)
                    _layerProcessor.LayerEventsCompress(layer, _options.FatherLineUnbind.Tolerance);
            }
        }

        _eventBuilder.ConvertLineEvents(pe, trueSrc.EventLayers);
        if (pe.AlphaEvents.Count == 0 && pe.AlphaFrames.Count == 0)
            pe.AlphaFrames.Add(new Pe.Frame());

        return pe;
    }

    /// <summary>
    /// 检查判定线中 PE 不支持字段是否出现非默认值，并逐项告警。
    /// </summary>
    private void WarnIfUnsupportedJudgeLineFields(KpcJudgeLine src)
    {
        var textureRemoveHint = _options.LineFilter.RemoveTextureLine
            ? "，判定线将被自动移除。"
            : "。";
        var attachUiRemoveHint = _options.LineFilter.RemoveAttachUiLine
            ? "，判定线将被自动移除。"
            : "。";

        if (!string.Equals(src.Texture, CoreConstants.DefaultTexture, StringComparison.Ordinal))
            Warn($"PE 不支持 JudgeLine.Texture（值='{src.Texture}'），{textureRemoveHint}");
        if (!IsDefaultAnchor(src.Anchor))
            Warn($"PE 不支持 JudgeLine.Anchor（值='[{string.Join(", ", src.Anchor)}]'）");
        if (src.Father != -1)
            Warn($"PE 不支持 JudgeLine.Father（值={src.Father}），将自动解除父子绑定");
        if (!src.IsCover)
            Warn($"PE 不支持 JudgeLine.IsCover（值={src.IsCover}）");
        if (src.ZOrder != 0)
            Warn($"PE 不支持 JudgeLine.ZOrder（值={src.ZOrder}）");
        if (src.AttachUi.HasValue)
            Warn(
                $"PE 不支持 JudgeLine.AttachUi（值={(int)src.AttachUi.Value}）{attachUiRemoveHint}"
            );
        if (src.IsGif)
            Warn($"PE 不支持 JudgeLine.IsGif（值={src.IsGif}）");
        if (Math.Abs(src.BpmFactor - 1f) > Constants.FloatEpsilon)
            Warn($"PE 不支持 JudgeLine.BpmFactor（值={src.BpmFactor}）");

        WarnIfUnsupportedControlFields(src);
    }

    /// <summary>
    /// 检查判定线中 PE 不支持的控件字段，并逐项告警。
    /// </summary>
    private void WarnIfUnsupportedControlFields(KpcJudgeLine src)
    {
        if (HasNonDefaultExtendLayer(src.Extended))
            Warn("PE 不支持 JudgeLine.Extended（包含非默认数据）");
        if (!ControlDefaultChecker.IsDefaultControls(src.PositionControls))
            Warn("PE 不支持 JudgeLine.PositionControls（包含非默认数据）");
        if (!ControlDefaultChecker.IsDefaultControls(src.AlphaControls))
            Warn("PE 不支持 JudgeLine.AlphaControls（包含非默认数据）");
        if (!ControlDefaultChecker.IsDefaultControls(src.SizeControls))
            Warn("PE 不支持 JudgeLine.SizeControls（包含非默认数据）");
        if (!ControlDefaultChecker.IsDefaultControls(src.SkewControls))
            Warn("PE 不支持 JudgeLine.SkewControls（包含非默认数据）");
        if (!ControlDefaultChecker.IsDefaultControls(src.YControls))
            Warn("PE 不支持 JudgeLine.YControls（包含非默认数据）");
    }

    private static bool HasNonDefaultExtendLayer(ExtendLayer? layer) =>
        layer != null
        && (
            layer.ColorEvents?.Count > 0
            || layer.ScaleXEvents?.Count > 0
            || layer.ScaleYEvents?.Count > 0
            || layer.TextEvents?.Count > 0
            || layer.PaintEvents?.Count > 0
            || layer.GifEvents?.Count > 0
        );

    private static bool IsDefaultAnchor(float[]? anchor) =>
        anchor is { Length: 2 }
        && Math.Abs(anchor[0] - 0.5f) <= Constants.FloatEpsilon
        && Math.Abs(anchor[1] - 0.5f) <= Constants.FloatEpsilon;

    private void Warn(string message) => _warnLogger?.Invoke(message);
}
