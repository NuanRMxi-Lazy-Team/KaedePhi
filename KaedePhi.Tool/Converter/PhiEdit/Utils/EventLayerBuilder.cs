using KaedePhi.Tool.Converter.PhiEdit.Model;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

/// <summary>
/// PE 判定线事件层到 KPC 事件层的构建器。
/// </summary>
public class EventLayerBuilder
{
    private readonly PhiEditFrameEventBuilder _phiEditFrameEvent;

    public EventLayerBuilder(PhiEditToKpcConvertOptions options)
    {
        _phiEditFrameEvent = new PhiEditFrameEventBuilder(options);
    }

    /// <summary>
    /// 将 PE 判定线上的各通道帧/事件规范化为 KPC 事件层。
    /// </summary>
    public KpcEvents.EventLayer ConvertEventLayer(Pe.JudgeLine src, double horizonBeat) =>
        new()
        {
            MoveXEvents = _phiEditFrameEvent.BuildMoveAxisEvents(
                src.MoveFrames,
                src.MoveEvents,
                horizonBeat,
                point => point.X,
                Transform.TransformToKpcX
            ),
            MoveYEvents = _phiEditFrameEvent.BuildMoveAxisEvents(
                src.MoveFrames,
                src.MoveEvents,
                horizonBeat,
                point => point.Y,
                Transform.TransformToKpcY
            ),
            RotateEvents = _phiEditFrameEvent.BuildScalarEvents(
                src.RotateFrames,
                src.RotateEvents,
                horizonBeat,
                Transform.TransformToKpcAngle
            ),
            AlphaEvents = _phiEditFrameEvent.BuildScalarEvents(
                src.AlphaFrames,
                src.AlphaEvents,
                horizonBeat,
                value => (int)Math.Round(value)
            ),
            SpeedEvents = _phiEditFrameEvent.BuildScalarEvents(
                src.SpeedFrames,
                [],
                horizonBeat,
                Transform.TransformToKpcSpeed
            ),
        };
}
