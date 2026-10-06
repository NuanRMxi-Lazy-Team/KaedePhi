using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using IrBlockArea = KaedePhi.Core.Intermediate.Model.BlockArea;
using IrEasing = KaedePhi.Core.Intermediate.Model.Easing;
using IrEvent = KaedePhi.Core.Intermediate.Model.Events.Event<double>;
using PhigrosAreaEase = KaedePhi.Core.Formats.Phigros.v3.Model.AreaEase;
using PhigrosAreaEaseType = KaedePhi.Core.Formats.Phigros.v3.Model.AreaEaseType;
using PhigrosBlockArea = KaedePhi.Core.Formats.Phigros.v3.Model.BlockArea;
using PhigrosMoveEvent = KaedePhi.Core.Formats.Phigros.v3.Model.AreaMoveEvent;
using PhigrosPosition = KaedePhi.Core.Formats.Phigros.v3.Model.PositionUnit;
using PhigrosRotateEvent = KaedePhi.Core.Formats.Phigros.v3.Model.AreaRotateEvent;
using PhigrosScaleEvent = KaedePhi.Core.Formats.Phigros.v3.Model.AreaScaleEvent;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

/// <summary>
/// IR 噪域到 PhigrosV3 噪域的转换工具。
/// </summary>
internal static class PhigrosV3BlockAreaBuilder
{
    internal static List<PhigrosBlockArea> ConvertBlockAreas(
        IReadOnlyList<IrBlockArea> blockAreas,
        PhigrosV3TimeMapper timeMapper,
        double easingPrecision,
        Action<string>? warnLogger,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(blockAreas);
        ArgumentNullException.ThrowIfNull(timeMapper);
        if (!double.IsFinite(easingPrecision) || easingPrecision <= 0d)
            throw new ArgumentOutOfRangeException(nameof(easingPrecision));

        var result = new List<PhigrosBlockArea>(blockAreas.Count);
        for (var index = 0; index < blockAreas.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var blockArea = blockAreas[index];
            if (blockArea is null)
                throw new FormatException("IR 噪域列表不能包含 null。");
            result.Add(
                ConvertBlockArea(blockArea, timeMapper, easingPrecision, warnLogger, index)
            );
        }

        return result;
    }

    private static PhigrosBlockArea ConvertBlockArea(
        IrBlockArea source,
        PhigrosV3TimeMapper timeMapper,
        double easingPrecision,
        Action<string>? warnLogger,
        int index
    )
    {
        ValidateCoordinates(source);
        var centerX = (source.TopRightX + source.BottomLeftX) / 2d;
        var centerY = (source.TopRightY + source.BottomLeftY) / 2d;

        var moveFrames = BuildKeyframes(
            [source.MoveXEvents, source.MoveYEvents],
            [centerX, centerY],
            [[0], [1]],
            easingPrecision,
            warnLogger,
            index,
            "移动"
        );
        var rotateFrames = BuildKeyframes(
            [source.RotateEvents, source.RotateAnchorXEvents, source.RotateAnchorYEvents],
            [0d, centerX, centerY],
            [[0, 1, 2]],
            easingPrecision,
            warnLogger,
            index,
            "旋转"
        );
        var scaleFrames = BuildKeyframes(
            [
                source.ScaleXEvents,
                source.ScaleYEvents,
                source.ScaleAnchorXEvents,
                source.ScaleAnchorYEvents,
            ],
            [1d, 1d, centerX, centerY],
            [[0, 2], [1, 3]],
            easingPrecision,
            warnLogger,
            index,
            "缩放"
        );

        return new PhigrosBlockArea
        {
            TopRightPercentage = new PhigrosPosition
            {
                X = ToPhigrosX(source.TopRightX),
                Y = ToPhigrosY(source.TopRightY),
            },
            BottomLeftPercentage = new PhigrosPosition
            {
                X = ToPhigrosX(source.BottomLeftX),
                Y = ToPhigrosY(source.BottomLeftY),
            },
            AppearTime = ToPhigrosTime(source.AppearBeat, timeMapper),
            EnableTime = ToPhigrosTime(source.EnableBeat, timeMapper),
            DisableTime = ToPhigrosTime(source.DisableBeat, timeMapper),
            DisappearTime = ToPhigrosTime(source.DisappearBeat, timeMapper),
            IsSubtract = source.IsSubtract,
            MoveEvents =
            [
                .. moveFrames
                    .Select(frame => new PhigrosMoveEvent
                    {
                        Time = ToPhigrosTime(frame.Beat, timeMapper),
                        EndPosition = new PhigrosPosition
                        {
                            X = ToPhigrosX(frame.Values[0]),
                            Y = ToPhigrosY(frame.Values[1]),
                        },
                        EaseTypeX = new PhigrosAreaEase(frame.EasingTypes[0]),
                        EaseTypeY = new PhigrosAreaEase(frame.EasingTypes[1]),
                    })
            ],
            RotateEvents =
            [
                .. rotateFrames
                    .Select(frame => new PhigrosRotateEvent
                    {
                        Time = ToPhigrosTime(frame.Beat, timeMapper),
                        Rotation = ToPhigrosAngle(frame.Values[0]),
                        Anchor = new PhigrosPosition
                        {
                            X = ToPhigrosX(frame.Values[1]),
                            Y = ToPhigrosY(frame.Values[2]),
                        },
                        EaseType = new PhigrosAreaEase(frame.EasingTypes[0]),
                    })
            ],
            ScaleEvents =
            [
                .. scaleFrames
                    .Select(frame => new PhigrosScaleEvent
                    {
                        Time = ToPhigrosTime(frame.Beat, timeMapper),
                        Scale = new PhigrosPosition
                        {
                            X = ToPhigrosFloat(frame.Values[0]),
                            Y = ToPhigrosFloat(frame.Values[1]),
                        },
                        Anchor = new PhigrosPosition
                        {
                            X = ToPhigrosX(frame.Values[2]),
                            Y = ToPhigrosY(frame.Values[3]),
                        },
                        EaseTypeX = new PhigrosAreaEase(frame.EasingTypes[0]),
                        EaseTypeY = new PhigrosAreaEase(frame.EasingTypes[1]),
                    })
            ],
        };
    }

    private static List<(Beat Beat, double[] Values, PhigrosAreaEaseType[] EasingTypes)> BuildKeyframes(
        List<IrEvent>?[] sourceTracks,
        double[] initialValues,
        int[][] linkedTracks,
        double easingPrecision,
        Action<string>? warnLogger,
        int blockAreaIndex,
        string groupName
    )
    {
        var tracks = sourceTracks
            .Select(events => SortAndValidateEvents(events, easingPrecision))
            .ToArray();
        if (tracks.All(track => track.Count == 0))
            return [];

        var useNativeEasings = CanUseNativeEasings(tracks, linkedTracks);
        var boundaries = new SortedSet<Beat>();
        foreach (var track in tracks)
        {
            foreach (var evt in track)
            {
                boundaries.Add(evt.StartBeat);
                boundaries.Add(evt.EndBeat);
            }
        }

        if (!useNativeEasings)
        {
            warnLogger?.Invoke(
                $"PhigrosV3 噪域 {blockAreaIndex} 的{groupName}事件无法直接表示，将按配置精度近似为线性事件。"
            );
            AddSampleBeats(boundaries, easingPrecision);
        }

        var result = new List<(Beat, double[], PhigrosAreaEaseType[])>(boundaries.Count);
        foreach (var beat in boundaries)
        {
            var beforeValues = new double[tracks.Length];
            var values = new double[tracks.Length];
            for (var trackIndex = 0; trackIndex < tracks.Length; trackIndex++)
            {
                beforeValues[trackIndex] = GetValueBeforeBeat(
                    tracks[trackIndex],
                    beat,
                    initialValues[trackIndex]
                );
                values[trackIndex] = GetValueAtBeat(
                    tracks[trackIndex],
                    beat,
                    initialValues[trackIndex]
                );
            }

            var stepGroups = new bool[linkedTracks.Length];
            for (var groupIndex = 0; groupIndex < linkedTracks.Length; groupIndex++)
                stepGroups[groupIndex] = linkedTracks[groupIndex].Any(trackIndex =>
                    !ValuesEqual(beforeValues[trackIndex], values[trackIndex])
                );

            if (stepGroups.Any(isStep => isStep))
            {
                var beforeEasingTypes = new PhigrosAreaEaseType[linkedTracks.Length];
                for (var groupIndex = 0; groupIndex < linkedTracks.Length; groupIndex++)
                {
                    beforeEasingTypes[groupIndex] = useNativeEasings
                        ? GetNativeEaseBeforeBeat(tracks, linkedTracks[groupIndex], beat)
                        : PhigrosAreaEaseType.Linear;
                }

                if (beat != new Beat(0) || !IsDefaultFrame(beforeValues, initialValues))
                    result.Add((beat, beforeValues, beforeEasingTypes));

                result.Add(
                    (
                        beat,
                        values,
                        Enumerable
                            .Repeat(PhigrosAreaEaseType.One, linkedTracks.Length)
                            .ToArray()
                    )
                );
                continue;
            }

            var easingTypes = new PhigrosAreaEaseType[linkedTracks.Length];
            for (var groupIndex = 0; groupIndex < linkedTracks.Length; groupIndex++)
            {
                if (useNativeEasings)
                {
                    easingTypes[groupIndex] = GetNativeEaseBeforeBeat(
                        tracks,
                        linkedTracks[groupIndex],
                        beat
                    );
                }
                else
                {
                    easingTypes[groupIndex] = PhigrosAreaEaseType.Linear;
                }
            }

            if (beat == new Beat(0) && IsDefaultFrame(values, initialValues))
                continue;

            result.Add((beat, values, easingTypes));
        }

        return result;
    }

    private static List<IrEvent> SortAndValidateEvents(
        List<IrEvent>? events,
        double easingPrecision
    )
    {
        if (events is not { Count: > 0 })
            return [];
        if (events.Any(evt => evt is null))
            throw new FormatException("IR 噪域事件列表不能包含 null。");

        foreach (var evt in events)
        {
            if (evt.EndBeat < evt.StartBeat)
                throw new FormatException("IR 噪域事件的结束拍不能早于开始拍。");
            if (!double.IsFinite(evt.StartValue) || !double.IsFinite(evt.EndValue))
                throw new FormatException("IR 噪域事件值必须是有限数值。");
            if (!float.IsFinite(evt.EasingLeft) || !float.IsFinite(evt.EasingRight))
                throw new FormatException("IR 噪域缓动范围必须是有限数值。");
        }

        var result = events
            .Select((evt, index) => (Event: evt, Index: index))
            .OrderBy(item => item.Event.StartBeat)
            .ThenBy(item => item.Index)
            .Select(item => item.Event.Clone())
            .ToList();
        FixDiscontinuityGaps(result, easingPrecision);
        return result;
    }

    private static void FixDiscontinuityGaps(List<IrEvent> events, double precision)
    {
        if (events.Count < 2)
            return;

        var padding = new Beat(1d / precision);
        if (padding == new Beat(0))
            return;

        for (var index = 1; index < events.Count; index++)
        {
            var previous = events[index - 1];
            var current = events[index];
            if (
                previous.StartBeat >= previous.EndBeat
                || current.StartBeat == current.EndBeat
                || previous.EndBeat != current.StartBeat
                || ValuesEqual(previous.EndValue, current.StartValue)
            )
                continue;

            // 相邻区间终值与起值不连续时错开新起点，保留前一段的终点关键帧。
            var shiftedStartBeat = current.StartBeat + padding;
            if (
                shiftedStartBeat >= current.EndBeat
                || (index + 1 < events.Count && shiftedStartBeat >= events[index + 1].StartBeat)
            )
                continue;

            current.StartBeat = shiftedStartBeat;
        }
    }

    private static bool CanUseNativeEasings(List<IrEvent>[] tracks, int[][] linkedTracks)
    {
        var boundaries = new SortedSet<Beat>();
        foreach (var track in tracks)
        {
            foreach (var evt in track)
            {
                boundaries.Add(evt.StartBeat);
                boundaries.Add(evt.EndBeat);
                if (evt.StartBeat == evt.EndBeat || ValuesEqual(evt.StartValue, evt.EndValue))
                    continue;
                if (!TryToAreaEase(evt.Easing, out _))
                    return false;
                if (
                    evt.IsBezier
                    || evt.EasingLeft != 0f
                    || Math.Abs(evt.EasingRight - 1f) > Constants.FloatEpsilon
                )
                    return false;
            }
        }

        if ((from track in tracks from evt in track where !ValuesEqual(evt.StartValue, evt.EndValue) select evt)
            .Any(evt => boundaries.Any(beat => beat > evt.StartBeat && beat < evt.EndBeat)))
        {
            return false;
        }

        var orderedBoundaries = boundaries.ToList();
        for (var boundaryIndex = 1; boundaryIndex < orderedBoundaries.Count; boundaryIndex++)
        {
            var startBeat = orderedBoundaries[boundaryIndex - 1];
            var endBeat = orderedBoundaries[boundaryIndex];
            foreach (var linkedGroup in linkedTracks)
            {
                var groupEasing = -1;
                foreach (var trackIndex in linkedGroup)
                {
                    var evt = tracks[trackIndex].FirstOrDefault(candidate =>
                        candidate.StartBeat == startBeat
                        && candidate.EndBeat == endBeat
                        && !ValuesEqual(candidate.StartValue, candidate.EndValue)
                    );
                    if (evt is null)
                        continue;
                    if (!TryToAreaEase(evt.Easing, out var areaEaseType))
                        return false;
                    if (groupEasing != -1 && groupEasing != (int)areaEaseType)
                        return false;
                    groupEasing = (int)areaEaseType;
                }
            }
        }

        return true;
    }

    private static void AddSampleBeats(SortedSet<Beat> boundaries, double precision)
    {
        if (boundaries.Count < 2)
            return;

        var start = (double)boundaries.Min;
        var end = (double)boundaries.Max;
        var step = 1d / precision;
        var current = start;
        while (current < end)
        {
            var next = Math.Min(current + step, end);
            if (next <= current)
                throw new InvalidOperationException("噪域事件切割步长无法推进。");
            boundaries.Add(new Beat(next));
            current = next;
        }
    }

    private static double GetValueAtBeat(List<IrEvent> events, Beat beat, double defaultValue)
    {
        IrEvent? dominant = null;
        foreach (var evt in events.TakeWhile(evt => evt.StartBeat <= beat))
        {
            dominant = evt;
        }

        if (dominant is null)
            return defaultValue;
        return beat <= dominant.EndBeat ? dominant.GetValueAtBeatAsDouble(beat) : dominant.EndValue;
    }

    private static PhigrosAreaEaseType GetNativeEaseBeforeBeat(
        List<IrEvent>[] tracks,
        int[] linkedTracks,
        Beat beat
    )
    {
        var easingType = PhigrosAreaEaseType.Linear;
        foreach (var trackIndex in linkedTracks)
        {
            var evt = tracks[trackIndex].FirstOrDefault(candidate =>
                candidate.StartBeat < candidate.EndBeat
                && candidate.EndBeat == beat
                && !ValuesEqual(candidate.StartValue, candidate.EndValue)
            );
            if (evt is not null && TryToAreaEase(evt.Easing, out var mapped))
                easingType = mapped;
        }

        return easingType;
    }

    private static double GetValueBeforeBeat(
        List<IrEvent> events,
        Beat beat,
        double defaultValue
    )
    {
        IrEvent? dominant = null;
        foreach (var evt in events.TakeWhile(evt => evt.StartBeat < beat))
        {
            dominant = evt;
        }

        if (dominant is null)
            return defaultValue;
        return beat <= dominant.EndBeat ? dominant.GetValueAtBeatAsDouble(beat) : dominant.EndValue;
    }

    private static bool IsDefaultFrame(double[] values, double[] initialValues)
    {
        return !values.Where((t, index) => !ValuesEqual(t, initialValues[index])).Any();
    }

    private static bool ValuesEqual(double left, double right) =>
        Math.Abs(left - right) <= Constants.FloatEpsilon;

    private static bool TryToAreaEase(
        IrEasing easing,
        out PhigrosAreaEaseType areaEaseType
    )
    {
        areaEaseType = (int)easing switch
        {
            1 => PhigrosAreaEaseType.Linear,
            5 => PhigrosAreaEaseType.EaseInQuad,
            6 => PhigrosAreaEaseType.EaseOutQuad,
            7 => PhigrosAreaEaseType.EaseInOutQuad,
            8 => PhigrosAreaEaseType.EaseInCubic,
            9 => PhigrosAreaEaseType.EaseOutCubic,
            10 => PhigrosAreaEaseType.EaseInOutCubic,
            11 => PhigrosAreaEaseType.EaseInQuart,
            12 => PhigrosAreaEaseType.EaseOutQuart,
            13 => PhigrosAreaEaseType.EaseInOutQuart,
            14 => PhigrosAreaEaseType.EaseInQuint,
            15 => PhigrosAreaEaseType.EaseOutQuint,
            16 => PhigrosAreaEaseType.EaseInOutQuint,
            _ => default,
        };
        return (int)easing is 1 or >= 5 and <= 16;
    }

    private static void ValidateCoordinates(IrBlockArea blockArea)
    {
        if (
            !double.IsFinite(blockArea.TopRightX)
            || !double.IsFinite(blockArea.TopRightY)
            || !double.IsFinite(blockArea.BottomLeftX)
            || !double.IsFinite(blockArea.BottomLeftY)
        )
            throw new FormatException("IR 噪域坐标必须是有限数值。");
    }

    private static float ToPhigrosX(double value) =>
        ToPhigrosFloat(Transform.ToPhigrosV3X(value));

    private static float ToPhigrosY(double value) =>
        ToPhigrosFloat(Transform.ToPhigrosV3Y(value));

    private static float ToPhigrosAngle(double value) =>
        ToPhigrosFloat(Transform.ToPhigrosV3Angle(value));

    private static float ToPhigrosTime(Beat beat, PhigrosV3TimeMapper timeMapper)
    {
        var time = timeMapper.ToMusicTime(beat);
        if (!double.IsFinite(time) || time is < float.MinValue or > float.MaxValue)
            throw new FormatException("IR 噪域时间超出 PhigrosV3 可编码范围。");
        return (float)time;
    }

    private static float ToPhigrosFloat(double value) =>
        double.IsFinite(value) && value is >= float.MinValue and <= float.MaxValue
            ? (float)value
            : throw new FormatException("IR 噪域值超出 PhigrosV3 可编码范围。");
}