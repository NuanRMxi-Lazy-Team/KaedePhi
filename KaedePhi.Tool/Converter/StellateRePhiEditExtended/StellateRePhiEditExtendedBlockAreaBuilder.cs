using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using KaedePhi.Tool.Converter.RePhiEdit.Utils;
using IrBlockArea = KaedePhi.Core.Intermediate.Model.BlockArea;
using IrEasing = KaedePhi.Core.Intermediate.Model.Easing;
using IrEvent = KaedePhi.Core.Intermediate.Model.Events.Event<double>;
using RpeEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<float>;
using RpeAlphaEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<int>;
using RpeEventBuilder = KaedePhi.Tool.Converter.RePhiEdit.Utils.EventBuilder;
using RpeEventLayer = KaedePhi.Core.Formats.RePhiEdit.Model.Events.EventLayer;
using RpeExtendLayer = KaedePhi.Core.Formats.RePhiEdit.Model.Events.ExtendLayer;
using RpeJudgeLine = KaedePhi.Core.Formats.RePhiEdit.Model.JudgeLine;
using RpeTransform = KaedePhi.Tool.Converter.RePhiEdit.Utils.Transform;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended;

internal static class StellateRePhiEditExtendedBlockAreaBuilder
{
    // 标记纹理按 900×900 像素映射到 1350×900 画布，IR 坐标跨度为 2。
    private const double MarkerTextureWidth = 900d;
    private const double MarkerTextureHeight = 900d;
    private const double RenderWidth = 1350d;
    private const double RenderHeight = 900d;
    private const double IrCoordinateSpan = 2d;
    private const double ValueEpsilon = 1e-10d;
    private const int MaximumAnchorSamples = 1_000_000;
    private static readonly Beat ZeroBeat = new(0);

    internal static RpeJudgeLine ConvertBlockArea(
        IrBlockArea source,
        int index,
        ConvertOption.CuttingOptions options,
        Action<string>? warnLogger,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCoordinates(source);
        ValidateLifecycle(source, index);

        var centerX = source.TopRightX / 2d + source.BottomLeftX / 2d;
        var centerY = source.TopRightY / 2d + source.BottomLeftY / 2d;
        var width = Math.Abs(source.TopRightX - source.BottomLeftX);
        var height = Math.Abs(source.TopRightY - source.BottomLeftY);
        if (!double.IsFinite(centerX) || !double.IsFinite(centerY))
            throw new FormatException($"IR 噪域 {index} 的中心坐标无法转换。");

        var baseScaleX = ToRpeFloat(
            width * RenderWidth / (IrCoordinateSpan * MarkerTextureWidth),
            index,
            "宽度"
        );
        var baseScaleY = ToRpeFloat(
            height * RenderHeight / (IrCoordinateSpan * MarkerTextureHeight),
            index,
            "高度"
        );

        var hasAnchorEvents =
            HasEvents(source.RotateAnchorXEvents)
            || HasEvents(source.RotateAnchorYEvents)
            || HasEvents(source.ScaleAnchorXEvents)
            || HasEvents(source.ScaleAnchorYEvents);
        var moveXEvents = source.MoveXEvents;
        var moveYEvents = source.MoveYEvents;
        var moveDefaultX = centerX;
        var moveDefaultY = centerY;
        if (hasAnchorEvents)
        {
            var bakedMotion = BakeAnchorMotion(
                source,
                index,
                options.UnsupportedEasingPrecision,
                cancellationToken
            );
            moveXEvents = bakedMotion.MoveXEvents;
            moveYEvents = bakedMotion.MoveYEvents;
            moveDefaultX = bakedMotion.CenterX;
            moveDefaultY = bakedMotion.CenterY;
            warnLogger?.Invoke(
                $"IR 噪域 {index} 的锚点变换已按每拍 {options.UnsupportedEasingPrecision} 段采样并烘焙到位置事件；锚点事件本身不会保留。"
            );
        }

        var eventLayer = new RpeEventLayer
        {
            MoveXEvents = ConvertEvents(
                moveXEvents,
                moveDefaultX,
                RpeTransform.TransformToRpeX,
                options,
                index,
                "X 轴移动",
                ensureBaseline: true,
                cancellationToken
            ),
            MoveYEvents = ConvertEvents(
                moveYEvents,
                moveDefaultY,
                RpeTransform.TransformToRpeY,
                options,
                index,
                "Y 轴移动",
                ensureBaseline: true,
                cancellationToken
            ),
            RotateEvents = ConvertEvents(
                source.RotateEvents,
                0d,
                RpeTransform.TransformToRpeAngle,
                options,
                index,
                "旋转",
                ensureBaseline: true,
                cancellationToken
            ),
            AlphaEvents = CreateAlphaEvents(source),
            SpeedEvents =
            [
                CreateMarker(source.AppearBeat, 1),
                CreateMarker(source.EnableBeat, 2),
                CreateMarker(source.DisableBeat, 3),
                CreateMarker(source.DisappearBeat, 4),
            ],
        };

        return new RpeJudgeLine
        {
            Name = $"BlockArea_{index + 1}",
            Texture = source.IsSubtract
                ? @"Pictures\isSubtract1.png"
                : @"Pictures\isSubtract0.png",
            Anchor = [0.5f, 0.5f],
            Father = -1,
            RotateWithFather = false,
            EventLayers = [eventLayer],
            Extended = new RpeExtendLayer
            {
                ScaleXEvents = ConvertEvents(
                    source.ScaleXEvents,
                    1d,
                    value => baseScaleX * value,
                    options,
                    index,
                    "X 轴缩放",
                    ensureBaseline: true,
                    cancellationToken
                ),
                ScaleYEvents = ConvertEvents(
                    source.ScaleYEvents,
                    1d,
                    value => baseScaleY * value,
                    options,
                    index,
                    "Y 轴缩放",
                    ensureBaseline: true,
                    cancellationToken
                ),
            },
        };
    }

    private static (
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
            var rotationAnchorValueX = EvaluateTrack(
                rotateAnchorX,
                beat,
                baseCenterX,
                rightSide
            );
            var rotationAnchorValueY = EvaluateTrack(
                rotateAnchorY,
                beat,
                baseCenterY,
                rightSide
            );
            var scaleValueX = EvaluateTrack(scaleX, beat, 1d, rightSide);
            var scaleValueY = EvaluateTrack(scaleY, beat, 1d, rightSide);
            var scaleAnchorValueX = EvaluateTrack(
                scaleAnchorX,
                beat,
                baseCenterX,
                rightSide
            );
            var scaleAnchorValueY = EvaluateTrack(
                scaleAnchorY,
                beat,
                baseCenterY,
                rightSide
            );

            // 先围绕缩放锚点缩放，再围绕旋转锚点旋转，最后叠加中心相对基础区域的移动量。
            var scaledCenterX =
                scaleAnchorValueX + (baseCenterX - scaleAnchorValueX) * scaleValueX;
            var scaledCenterY =
                scaleAnchorValueY + (baseCenterY - scaleAnchorValueY) * scaleValueY;
            var rotatedOffset = CoordinateGeometry.RotateIrOffset(
                scaledCenterX - rotationAnchorValueX,
                scaledCenterY - rotationAnchorValueY,
                rotation
            );
            var rotatedCenterX = rotationAnchorValueX + rotatedOffset.X;
            var rotatedCenterY = rotationAnchorValueY + rotatedOffset.Y;
            var centerX = rotatedCenterX + moveCenterX - baseCenterX;
            var centerY = rotatedCenterY + moveCenterY - baseCenterY;
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
            var left = beat == ZeroBeat ? right : EvaluateCenter(beat, rightSide: false);
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
        var beats = new SortedSet<Beat> { ZeroBeat };
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
            if (beats.Count > MaximumAnchorSamples)
                throw new FormatException(
                    $"IR 噪域 {blockAreaIndex} 的锚点采样点超过上限 {MaximumAnchorSamples}。"
                );
            currentBeat = nextBeat;
        }

        return [.. beats];

        void AddBoundary(Beat beat)
        {
            if (
                beat > ZeroBeat
                && beats.Add(beat)
                && beats.Count > MaximumAnchorSamples
            )
                throw new FormatException(
                    $"IR 噪域 {blockAreaIndex} 的锚点采样点超过上限 {MaximumAnchorSamples}。"
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
                && Math.Abs(leftValue - rightValue) > ValueEpsilon
            )
                result.Add(
                    CreateEvent(sample.Beat, sample.Beat, leftValue, rightValue)
                );

            if (index + 1 >= samples.Count)
                continue;

            var next = samples[index + 1];
            var endValue = xAxis ? next.LeftX : next.LeftY;
            if (
                next.Beat > sample.Beat
                && Math.Abs(rightValue - endValue) > ValueEpsilon
            )
                result.Add(
                    CreateEvent(sample.Beat, next.Beat, rightValue, endValue)
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
            ValidateEventFields(evt, blockAreaIndex, trackName);
            ordered.Add((evt, index));
        }

        return ordered
            .OrderBy(item => item.Event.StartBeat)
            .ThenBy(item => item.Index)
            .Select(item => item.Event)
            .ToList();
    }

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

    private static List<RpeEvent>? ConvertEvents(
        List<IrEvent>? source,
        double defaultValue,
        Func<double, double> valueTransformer,
        ConvertOption.CuttingOptions options,
        int blockAreaIndex,
        string trackName,
        bool ensureBaseline,
        CancellationToken cancellationToken
    )
    {
        if (source is not { Count: > 0 })
        {
            if (!ensureBaseline)
                return null;

            ValidateEncodedValue(
                valueTransformer(defaultValue),
                blockAreaIndex,
                trackName
            );
            source =
            [
                CreateEvent(ZeroBeat, ZeroBeat, defaultValue, defaultValue),
            ];
        }

        var ordered = new List<(IrEvent Event, int Index)>(source.Count);
        for (var index = 0; index < source.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var evt = source[index];
            if (evt is null)
                throw new FormatException(
                    $"IR 噪域 {blockAreaIndex} 的{trackName}事件列表不能包含 null。"
                );
            ValidateEvent(evt, valueTransformer, blockAreaIndex, trackName);
            ordered.Add((evt, index));
        }

        var events = ordered
            .OrderBy(item => item.Event.StartBeat)
            .ThenBy(item => item.Event.EndBeat)
            .ThenBy(item => item.Index)
            .Select(item => item.Event)
            .ToList();

        if (ensureBaseline && events[0].StartBeat > ZeroBeat)
        {
            var seed = CreateEvent(
                ZeroBeat,
                events[0].StartBeat,
                defaultValue,
                defaultValue
            );
            ValidateEvent(seed, valueTransformer, blockAreaIndex, trackName);
            events.Insert(0, seed);
        }

        var converted = new List<RpeEvent>();
        foreach (var evt in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            converted.AddRange(
                RpeEventBuilder.ConvertDoubleEventExpanding(
                    evt,
                    options,
                    valueTransformer
                )
            );
        }

        return converted;
    }

    private static List<RpeAlphaEvent> CreateAlphaEvents(IrBlockArea source)
    {
        var beats = new SortedSet<Beat> { ZeroBeat };
        AddBoundary(source.AppearBeat);
        AddBoundary(source.EnableBeat);
        AddBoundary(source.DisableBeat);
        AddBoundary(source.DisappearBeat);

        var result = new List<RpeAlphaEvent>(beats.Count);
        var previousAlpha = -1;
        foreach (var beat in beats)
        {
            var alpha = GetAlphaAtBeat(source, beat);
            if (beat == ZeroBeat || alpha != previousAlpha)
                result.Add(CreateAlphaEvent(beat, alpha));
            previousAlpha = alpha;
        }

        return result;

        void AddBoundary(Beat beat)
        {
            if (beat > ZeroBeat)
                beats.Add(beat);
        }
    }

    private static int GetAlphaAtBeat(IrBlockArea source, Beat beat)
    {
        // 阶段边界使用严格小于判断，确保准备激活与激活重合时直接采用激活透明度。
        if (beat < source.AppearBeat)
            return 0;
        if (beat < source.EnableBeat)
            return 128;
        if (beat < source.DisableBeat)
            return 255;
        return 0;
    }

    private static void ValidateCoordinates(IrBlockArea source)
    {
        if (
            !double.IsFinite(source.TopRightX)
            || !double.IsFinite(source.TopRightY)
            || !double.IsFinite(source.BottomLeftX)
            || !double.IsFinite(source.BottomLeftY)
        )
            throw new FormatException("IR 噪域坐标必须是有限数值。");

        if (
            !double.IsFinite(Math.Abs(source.TopRightX - source.BottomLeftX))
            || !double.IsFinite(Math.Abs(source.TopRightY - source.BottomLeftY))
        )
            throw new FormatException("IR 噪域尺寸超出 RePhiEdit 可编码范围。");
    }

    private static void ValidateLifecycle(IrBlockArea source, int index)
    {
        if (
            source.AppearBeat > source.EnableBeat
            || source.EnableBeat > source.DisableBeat
            || source.DisableBeat > source.DisappearBeat
        )
            throw new FormatException(
                $"IR 噪域 {index} 的显示阶段顺序无效，无法转换为 StellateRePhiEditExtended。"
            );
    }

    private static void ValidateEvent(
        IrEvent evt,
        Func<double, double> valueTransformer,
        int blockAreaIndex,
        string trackName
    )
    {
        ValidateEventFields(evt, blockAreaIndex, trackName);
        ValidateEncodedValue(
            valueTransformer(evt.StartValue),
            blockAreaIndex,
            trackName
        );
        ValidateEncodedValue(
            valueTransformer(evt.EndValue),
            blockAreaIndex,
            trackName
        );
    }

    private static void ValidateEventFields(
        IrEvent evt,
        int blockAreaIndex,
        string trackName
    )
    {
        if (evt.StartBeat > evt.EndBeat)
            throw new FormatException(
                $"IR 噪域 {blockAreaIndex} 的{trackName}事件结束拍早于开始拍。"
            );
        if (!double.IsFinite(evt.StartValue) || !double.IsFinite(evt.EndValue))
            throw new FormatException(
                $"IR 噪域 {blockAreaIndex} 的{trackName}事件值必须是有限数值。"
            );
        if (!float.IsFinite(evt.EasingLeft) || !float.IsFinite(evt.EasingRight))
            throw new FormatException(
                $"IR 噪域 {blockAreaIndex} 的{trackName}缓动范围必须是有限数值。"
            );
        if (evt.Easing is null)
            throw new FormatException(
                $"IR 噪域 {blockAreaIndex} 的{trackName}缓动类型不能为空。"
            );
        if (
            evt.IsBezier
            && (
                evt.BezierPoints is not { Length: 4 }
                || evt.BezierPoints.Any(point => !float.IsFinite(point))
            )
        )
            throw new FormatException(
                $"IR 噪域 {blockAreaIndex} 的{trackName}贝塞尔控制点无效。"
            );
    }

    private static void ValidateEncodedValue(double value, int blockAreaIndex, string trackName)
    {
        if (!double.IsFinite(value) || value is < -float.MaxValue or > float.MaxValue)
            throw new FormatException(
                $"IR 噪域 {blockAreaIndex} 的{trackName}事件值超出 RePhiEdit 可编码范围。"
            );
    }

    private static float ToRpeFloat(double value, int blockAreaIndex, string valueName) =>
        double.IsFinite(value) && value is >= -float.MaxValue and <= float.MaxValue
            ? (float)value
            : throw new FormatException(
                $"IR 噪域 {blockAreaIndex} 的{valueName}超出 RePhiEdit 可编码范围。"
            );

    private static RpeEvent CreateMarker(Beat beat, int phase) =>
        new()
        {
            StartBeat = beat,
            EndBeat = beat,
            StartValue = phase,
            EndValue = phase,
        };

    private static RpeAlphaEvent CreateAlphaEvent(Beat beat, int alpha) =>
        new()
        {
            StartBeat = beat,
            EndBeat = beat,
            StartValue = alpha,
            EndValue = alpha,
        };

    private static IrEvent CreateEvent(
        Beat startBeat,
        Beat endBeat,
        double startValue,
        double endValue
    ) =>
        new()
        {
            StartBeat = startBeat,
            EndBeat = endBeat,
            StartValue = startValue,
            EndValue = endValue,
            Easing = new IrEasing(1),
        };

    private static bool HasEvents(List<IrEvent>? events) => events is { Count: > 0 };
}
