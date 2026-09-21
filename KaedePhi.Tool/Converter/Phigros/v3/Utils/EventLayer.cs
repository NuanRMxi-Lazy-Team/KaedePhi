using KaedePhi.Tool.Event.Intermediate;
using PhigrosJudgeLine = KaedePhi.Core.Formats.Phigros.v3.Model.JudgeLine;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

/// <summary>
/// PhigrosV3 判定线到 IR 事件层的构建器。
/// </summary>
public static class EventLayerBuilder
{
    public static IrEvents.EventLayer ConvertEventLayer(PhigrosJudgeLine src, double horizonBeat)
    {
        var result = new IrEvents.EventLayer();
        var eventListCompress = new EventCompressor<double>();

        var moveX = EventBuilder.ConvertMoveAxisEvents(
            src.JudgeLineMoveEvents,
            horizonBeat,
            e => e.Start,
            e => e.End,
            Transform.ToIrX
        );
        if (moveX != null)
            result.MoveXEvents = eventListCompress.EventListCompressSqrt(moveX, 0d);

        var moveY = EventBuilder.ConvertMoveAxisEvents(
            src.JudgeLineMoveEvents,
            horizonBeat,
            e => e.Start2,
            e => e.End2,
            Transform.ToIrY
        );
        if (moveY != null)
            result.MoveYEvents = eventListCompress.EventListCompressSqrt(moveY, 0d);

        result.RotateEvents = EventBuilder.ConvertEvents(
            src.JudgeLineRotateEvents,
            horizonBeat,
            Transform.ToIrAngle
        );

        result.AlphaEvents = EventBuilder.ConvertEvents(
            src.JudgeLineDisappearEvents,
            horizonBeat,
            v => (int)Math.Clamp(Math.Round(v * 255), 0, 255)
        );

        result.SpeedEvents = EventBuilder.ConvertSpeedEvents(src.SpeedEvents, horizonBeat);

        result.Anticipation();
        return result;
    }
}
