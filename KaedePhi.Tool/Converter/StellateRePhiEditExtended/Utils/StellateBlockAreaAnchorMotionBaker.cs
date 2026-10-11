using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using IrBlockArea = KaedePhi.Core.Intermediate.Model.BlockArea;
using IrEvent = KaedePhi.Core.Intermediate.Model.Events.Event<double>;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;

/// <summary>
/// 将 IR 噪域的锚点变换烘焙为位置事件。
/// </summary>
public static class StellateBlockAreaAnchorMotionBaker
{
    /// <summary>
    /// 根据锚点、缩放和旋转事件生成烘焙后的位置轨道。
    /// </summary>
    /// <param name="source">源 IR 噪域</param>
    /// <param name="blockAreaIndex">噪域索引</param>
    /// <param name="samplingPrecision">每拍使用的采样段数</param>
    /// <param name="cancellationToken">用于取消处理的令牌</param>
    public static (
        double CenterX,
        double CenterY,
        List<IrEvent> MoveXEvents,
        List<IrEvent> MoveYEvents
    ) BakeAnchorMotion(
        IrBlockArea source,
        int blockAreaIndex,
        int samplingPrecision,
        CancellationToken cancellationToken
    )
    {
        var baseCenterX = source.TopRightX / 2d + source.BottomLeftX / 2d;
        var baseCenterY = source.TopRightY / 2d + source.BottomLeftY / 2d;
        var moveX = PrepareSamplingEvents(
            source.MoveXEvents,
            blockAreaIndex,
            "X 轴移动",
            cancellationToken
        );
        var moveY = PrepareSamplingEvents(
            source.MoveYEvents,
            blockAreaIndex,
            "Y 轴移动",
            cancellationToken
        );
        var rotate = PrepareSamplingEvents(
            source.RotateEvents,
            blockAreaIndex,
            "旋转",
            cancellationToken
        );
        var rotateAnchorX = PrepareSamplingEvents(
            source.RotateAnchorXEvents,
            blockAreaIndex,
            "X 轴旋转锚点",
            cancellationToken
        );
        var rotateAnchorY = PrepareSamplingEvents(
            source.RotateAnchorYEvents,
            blockAreaIndex,
            "Y 轴旋转锚点",
            cancellationToken
        );
        var scaleX = PrepareSamplingEvents(
            source.ScaleXEvents,
            blockAreaIndex,
            "X 轴缩放",
            cancellationToken
        );
        var scaleY = PrepareSamplingEvents(
            source.ScaleYEvents,
            blockAreaIndex,
            "Y 轴缩放",
            cancellationToken
        );
        var scaleAnchorX = PrepareSamplingEvents(
            source.ScaleAnchorXEvents,
            blockAreaIndex,
            "X 轴缩放锚点",
            cancellationToken
        );
        var scaleAnchorY = PrepareSamplingEvents(
            source.ScaleAnchorYEvents,
            blockAreaIndex,
            "Y 轴缩放锚点",
            cancellationToken
        );
        var tracks = new[]
        {
            moveX,
            moveY,
            rotate,
            rotateAnchorX,
            rotateAnchorY,
            scaleX,
            scaleY,
            scaleAnchorX,
            scaleAnchorY,
        };
        var sampleBeats = CreateAnchorSampleBeats(
            tracks,
            samplingPrecision,
            blockAreaIndex,
            cancellationToken
        );

        (double X, double Y) EvaluateCenter(Beat beat, bool rightSide)
        {
            var moveCenterX = EvaluateTrack(moveX, beat, baseCenterX, rightSide);
            var moveCenterY = EvaluateTrack(moveY, beat, baseCenterY, rightSide);
            var rotation = EvaluateTrack(rotate, beat, 0d, rightSide);
            var rotationAnchorValueX = EvaluateAnchor(
                rotateAnchorX,
                beat,
                baseCenterX,
                rightSide
            );
            var rotationAnchorValueY = EvaluateAnchor(
                rotateAnchorY,
                beat,
                baseCenterY,
                rightSide
            );
            var scaleValueX = EvaluateTrack(scaleX, beat, 1d, rightSide);
            var scaleValueY = EvaluateTrack(scaleY, beat, 1d, rightSide);
            var scaleAnchorValueX = EvaluateAnchor(
                scaleAnchorX,
                beat,
                baseCenterX,
                rightSide
            );
            var scaleAnchorValueY = EvaluateAnchor(
                scaleAnchorY,
                beat,
                baseCenterY,
                rightSide
            );

            // 锚点为绝对坐标：先变换原始中心，再叠加移动中心相对原始中心的偏移。
            var scaledCenterX =
                scaleAnchorValueX + (baseCenterX - scaleAnchorValueX) * scaleValueX;
            var scaledCenterY =
                scaleAnchorValueY + (baseCenterY - scaleAnchorValueY) * scaleValueY;
            var rotatedOffset = CoordinateGeometry.RotateIrOffset(
                scaledCenterX - rotationAnchorValueX,
                scaledCenterY - rotationAnchorValueY,
                rotation
            );
            var centerX = rotationAnchorValueX + rotatedOffset.X + moveCenterX - baseCenterX;
            var centerY = rotationAnchorValueY + rotatedOffset.Y + moveCenterY - baseCenterY;
            if (!double.IsFinite(centerX) || !double.IsFinite(centerY))
                throw new FormatException($"IR 噪域 {blockAreaIndex} 的锚点变换后位置不是有限数值。");

            return (centerX, centerY);
        }

        var samples = new List<(
            Beat Beat,
            double LeftX,
            double LeftY,
            double RightX,
            double RightY
        )>(sampleBeats.Count);
        foreach (var beat in sampleBeats)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var right = EvaluateCenter(beat, rightSide: true);
            var left = beat == StellateBlockAreaConstants.ZeroBeat ? right : EvaluateCenter(beat, rightSide: false);
            samples.Add((beat, left.X, left.Y, right.X, right.Y));
        }

        return (
            samples[0].RightX,
            samples[0].RightY,
            CreatePositionEvents(samples, xAxis: true, cancellationToken),
            CreatePositionEvents(samples, xAxis: false, cancellationToken)
        );
    }

    private static List<Beat> CreateAnchorSampleBeats(
        IEnumerable<List<IrEvent>> tracks,
        int samplingPrecision,
        int blockAreaIndex,
        CancellationToken cancellationToken
    )
    {
        var beats = new SortedSet<Beat> { StellateBlockAreaConstants.ZeroBeat };
        foreach (var events in tracks)
        {
            foreach (var evt in events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddBoundary(evt.StartBeat);
                AddBoundary(evt.EndBeat);
            }
        }

        var endBeat = (double)beats.Max;
        if (!double.IsFinite(endBeat))
            throw new FormatException($"IR 噪域 {blockAreaIndex} 的锚点事件时间无法转换。");

        var step = 1d / samplingPrecision;
        var currentBeat = 0d;
        while (currentBeat < endBeat)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nextBeat = Math.Min(currentBeat + step, endBeat);
            if (!double.IsFinite(nextBeat) || nextBeat <= currentBeat)
                throw new FormatException($"IR 噪域 {blockAreaIndex} 的锚点采样步长无法推进。");
            beats.Add(new Beat(nextBeat));
            if (beats.Count > StellateBlockAreaConstants.MaximumAnchorSamples)
                throw new FormatException(
                    $"IR 噪域 {blockAreaIndex} 的锚点采样点超过上限 {StellateBlockAreaConstants.MaximumAnchorSamples}。"
                );
            currentBeat = nextBeat;
        }

        return [.. beats];

        void AddBoundary(Beat beat)
        {
            if (
                beat > StellateBlockAreaConstants.ZeroBeat
                && beats.Add(beat)
                && beats.Count > StellateBlockAreaConstants.MaximumAnchorSamples
            )
                throw new FormatException(
                    $"IR 噪域 {blockAreaIndex} 的锚点采样点超过上限 {StellateBlockAreaConstants.MaximumAnchorSamples}。"
                );
        }
    }

    private static List<IrEvent> CreatePositionEvents(
        IReadOnlyList<(
            Beat Beat,
            double LeftX,
            double LeftY,
            double RightX,
            double RightY
        )> samples,
        bool xAxis,
        CancellationToken cancellationToken
    )
    {
        var result = new List<IrEvent>();
        for (var index = 0; index < samples.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sample = samples[index];
            var leftValue = xAxis ? sample.LeftX : sample.LeftY;
            var rightValue = xAxis ? sample.RightX : sample.RightY;
            if (
                index > 0
                && Math.Abs(leftValue - rightValue) > StellateBlockAreaConstants.ValueEpsilon
            )
                result.Add(
                    StellateBlockAreaEventConverter.CreateEvent(sample.Beat, sample.Beat, leftValue, rightValue)
                );

            if (index + 1 >= samples.Count)
                continue;

            var next = samples[index + 1];
            var endValue = xAxis ? next.LeftX : next.LeftY;
            if (
                next.Beat > sample.Beat
                && Math.Abs(rightValue - endValue) > StellateBlockAreaConstants.ValueEpsilon
            )
                result.Add(
                    StellateBlockAreaEventConverter.CreateEvent(sample.Beat, next.Beat, rightValue, endValue)
                );
        }

        return result;
    }

    private static List<IrEvent> PrepareSamplingEvents(
        List<IrEvent>? source,
        int blockAreaIndex,
        string trackName,
        CancellationToken cancellationToken
    )
    {
        if (source is not { Count: > 0 })
            return [];

        var ordered = new List<(IrEvent Event, int Index)>(source.Count);
        for (var index = 0; index < source.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var evt = source[index];
            if (evt is null)
                throw new FormatException(
                    $"IR 噪域 {blockAreaIndex} 的{trackName}事件列表不能包含 null。"
                );
            StellateBlockAreaEventConverter.ValidateEventFields(evt, blockAreaIndex, trackName);
            ordered.Add((evt, index));
        }

        return ordered
            .OrderBy(item => item.Event.StartBeat)
            .ThenBy(item => item.Index)
            .Select(item => item.Event)
            .ToList();
    }

    private static double EvaluateAnchor(
        IReadOnlyList<IrEvent> events,
        Beat beat,
        double baseValue,
        bool includeStartAtBeat
    ) =>
        events.Count == 0
            ? baseValue
            : EvaluateTrack(events, beat, baseValue, includeStartAtBeat);

    private static double EvaluateTrack(
        IReadOnlyList<IrEvent> events,
        Beat beat,
        double defaultValue,
        bool includeStartAtBeat
    )
    {
        var low = 0;
        var high = events.Count - 1;
        var dominantIndex = -1;
        while (low <= high)
        {
            var middle = low + (high - low) / 2;
            var startsBeforeBeat = includeStartAtBeat
                ? events[middle].StartBeat <= beat
                : events[middle].StartBeat < beat;
            if (startsBeforeBeat)
            {
                dominantIndex = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        if (dominantIndex < 0)
            return defaultValue;

        var dominant = events[dominantIndex];
        return beat <= dominant.EndBeat
            ? dominant.GetValueAtBeatAsDouble(beat)
            : dominant.EndValue;
    }

}
