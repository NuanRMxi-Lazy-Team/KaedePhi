using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiEdit.Model;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

public class EventLayerConverter
{
    private readonly FrameEventInterpolator _frameEventInterpolator;

    public EventLayerConverter(PhiEditToKpcConvertOptions options)
    {
        _frameEventInterpolator = new FrameEventInterpolator(options);
    }
    /// <summary>
    /// 将 PE 判定线上的各通道帧/事件规范化为 KPC 事件层。
    /// </summary>
    public Kpc.EventLayer ConvertEventLayer(Pe.JudgeLine src, double horizonBeat) => new()
    {
        MoveXEvents = _frameEventInterpolator.BuildMoveAxisEvents(
            src.MoveFrames, src.MoveEvents, horizonBeat, point => point.X, Transform.TransformToKpcX),
        MoveYEvents = _frameEventInterpolator.BuildMoveAxisEvents(
            src.MoveFrames, src.MoveEvents, horizonBeat, point => point.Y, Transform.TransformToKpcY),
        RotateEvents = _frameEventInterpolator.BuildScalarEvents(
            src.RotateFrames, src.RotateEvents, horizonBeat, Transform.TransformToKpcAngle),
        AlphaEvents = _frameEventInterpolator.BuildScalarEvents(
            src.AlphaFrames, src.AlphaEvents, horizonBeat,
            value => Math.Clamp((int)Math.Round(value), 0, 255)),
        SpeedEvents = _frameEventInterpolator.BuildScalarEvents(
            src.SpeedFrames, [], horizonBeat, value => (float)(value / (14d / 9d)))
    };
}