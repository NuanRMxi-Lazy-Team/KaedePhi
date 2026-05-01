using PhigrosJudgeLine = KaedePhi.Core.Phigros.v3.JudgeLine;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

public static class EventLayer
{
    public static Kpc.EventLayer ConvertEventLayer(PhigrosJudgeLine src, double horizonBeat)
    {
        var result = new Kpc.EventLayer();
        var eventListCompress = new Tool.Event.KaedePhi.EventCompressor<double>(); 

        var moveX = Event.ConvertMoveAxisEvents(
            src.JudgeLineMoveEvents, horizonBeat, e => e.Start, e => e.End);
        if (moveX != null)
            result.MoveXEvents = eventListCompress.EventListCompressSqrt(moveX, 0d);

        var moveY = Event.ConvertMoveAxisEvents(
            src.JudgeLineMoveEvents, horizonBeat, e => e.Start2, e => e.End2);
        if (moveY != null)
            result.MoveYEvents = eventListCompress.EventListCompressSqrt(moveY, 0d);

        result.RotateEvents = Event.ConvertEvents(
            src.JudgeLineRotateEvents, horizonBeat,
            Transform.ToKpcAngle);

        result.AlphaEvents = Event.ConvertEvents(
            src.JudgeLineDisappearEvents, horizonBeat,
            v => (int)Math.Clamp(Math.Round(v * 255), 0, 255));

        result.SpeedEvents = Event.ConvertSpeedEvents(src.SpeedEvents, horizonBeat);

        result.Anticipation();
        return result;
    }
}
