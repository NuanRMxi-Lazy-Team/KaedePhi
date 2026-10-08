using System.Runtime.ExceptionServices;
using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using RpeChart = KaedePhi.Core.Formats.RePhiEdit.Model.Chart;
using RpeEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<float>;
using RpeEasing = KaedePhi.Core.Formats.RePhiEdit.Model.Easing;
using RpeEventLayer = KaedePhi.Core.Formats.RePhiEdit.Model.Events.EventLayer;
using RpeJudgeLine = KaedePhi.Core.Formats.RePhiEdit.Model.JudgeLine;
using IrEvent = KaedePhi.Core.Intermediate.Model.Events.Event<double>;
using RpeEasingConverter = KaedePhi.Tool.Converter.RePhiEdit.Utils.EasingConverter;
using RpeEventBuilder = KaedePhi.Tool.Converter.RePhiEdit.Utils.EventBuilder;
using RpeTransform = KaedePhi.Tool.Converter.RePhiEdit.Utils.Transform;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended;

internal sealed class StellateRePhiEditGeometry
{
    private const double AspectWidth = 3d;
    private const double AspectHeight = 2d;
    private const double TextureWidth = 900d;
    private const double TextureHeight = 900d;
    private const int SamplesPerBeat = 32;
    private const int ParallelSampleThreshold = 512;
    private const double SimplifyTolerance = 0.02d;
    private const double ValueEpsilon = 1e-10;
    private const double MinimumBaseSize = 1e-12d;
    private const double HardPointEpsilon = 2.384185791015625e-7;
    private const double CurveStartTimeEpsilon = 1e-10d;

    private readonly RpeChart _source;
    private readonly PhigrosV3TimeMapper _timeMapper;
    private readonly CancellationToken _ct;
    private readonly Beat _chartEndBeat;
    private readonly Dictionary<List<RpeEvent>, StellateCurveIndex> _curveIndexes = new();

    internal StellateRePhiEditGeometry(
        RpeChart source,
        PhigrosV3TimeMapper timeMapper,
        CancellationToken cancellationToken
    )
    {
        _source = source;
        _timeMapper = timeMapper;
        _ct = cancellationToken;
        _chartEndBeat = CalculateChartEndBeat(source);
    }

    private void PrepareCurveIndexes(int lineIndex, HashSet<int> visited, bool includeScale)
    {
        if (!visited.Add(lineIndex))
            return;

        var line = _source.JudgeLineList[lineIndex];
        if (line.EventLayers is not null)
        {
            foreach (var layer in line.EventLayers.OfType<RpeEventLayer>())
            {
                AddCurveIndex(layer.MoveXEvents);
                AddCurveIndex(layer.MoveYEvents);
                AddCurveIndex(layer.RotateEvents);
            }
        }

        if (includeScale)
        {
            AddCurveIndex(line.Extended?.ScaleXEvents);
            AddCurveIndex(line.Extended?.ScaleYEvents);
        }

        if (line.Father >= 0 && line.Father < _source.JudgeLineList.Count)
            PrepareCurveIndexes(line.Father, visited, includeScale: false);
    }

    private void AddCurveIndex(List<RpeEvent>? events)
    {
        if (events is { Count: > 0 } && !_curveIndexes.ContainsKey(events))
            _curveIndexes.Add(events, new StellateCurveIndex(events, _timeMapper));
    }

    private (
        Beat Beat,
        StellateBlockAreaGeometry Left,
        StellateBlockAreaGeometry Right,
        bool Hard
        )[] SampleGeometry(
            int lineIndex,
            SortedSet<Beat> sampleBeats,
            SortedSet<Beat> hardBeats
        )
    {
        var beats = sampleBeats.ToArray();
        var samples = new (
            Beat Beat,
            StellateBlockAreaGeometry Left,
            StellateBlockAreaGeometry Right,
            bool Hard
            )[beats.Length];

        void Sample(int index, HashSet<int> visiting)
        {
            _ct.ThrowIfCancellationRequested();
            var beat = beats[index];
            var musicTime = _timeMapper.ToMusicTime(beat);
            samples[index] = (
                beat,
                GetGeometry(lineIndex, musicTime, visiting, rightSide: false),
                GetGeometry(lineIndex, musicTime, visiting, rightSide: true),
                hardBeats.Contains(beat)
            );
        }

        if (beats.Length < ParallelSampleThreshold || Environment.ProcessorCount <= 1)
        {
            var visiting = new HashSet<int>();
            for (var index = 0; index < beats.Length; index++)
                Sample(index, visiting);

            return samples;
        }

        try
        {
            Parallel.For(
                0,
                beats.Length,
                new ParallelOptions
                {
                    CancellationToken = _ct,
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                },
                () => new HashSet<int>(),
                (index, _, visiting) =>
                {
                    Sample(index, visiting);
                    return visiting;
                },
                _ => { }
            );
        }
        catch (AggregateException exception)
        {
            var failures = exception.Flatten().InnerExceptions;
            if (failures.Count == 1)
                ExceptionDispatchInfo.Capture(failures[0]).Throw();

            throw;
        }

        return samples;
    }

    internal List<Ir.BlockArea> ConvertLine(int lineIndex)
    {
        _ct.ThrowIfCancellationRequested();
        var source = _source.JudgeLineList[lineIndex];
        var markers = GetMarkers(source);
        var lifeCycles = BuildLifeCycles(markers);
        if (lifeCycles.Count == 0)
            return [];

        PrepareCurveIndexes(lineIndex, [], includeScale: true);
        var endBeat = _chartEndBeat;
        foreach (var marker in markers.Where(marker => marker.Beat > endBeat))
            endBeat = marker.Beat;

        var hardBeats = new SortedSet<Beat> { new(0) };
        CollectPoseBreaks(lineIndex, hardBeats, []);
        AddCurveBoundaries(source.Extended?.ScaleXEvents, hardBeats);
        AddCurveBoundaries(source.Extended?.ScaleYEvents, hardBeats);
        var scaleHardBeats = new SortedSet<Beat> { new(0) };
        AddCurveBoundaries(source.Extended?.ScaleXEvents, scaleHardBeats);
        AddCurveBoundaries(source.Extended?.ScaleYEvents, scaleHardBeats);
        var rotateHardBeats = new SortedSet<Beat> { new(0) };
        CollectRotationBreaks(lineIndex, rotateHardBeats, []);

        var moveXSourceEvents = GetSingleCurve(source, static layer => layer.MoveXEvents);
        var moveYSourceEvents = GetSingleCurve(source, static layer => layer.MoveYEvents);
        var rotateSourceEvents = GetDirectRotationCurve(source, out var hasMultipleRotationCurves);
        var rotateDirectSourceEvents = RemoveRedundantConstantEvents(rotateSourceEvents);
        var scaleXSourceEvents = source.Extended?.ScaleXEvents;
        var scaleYSourceEvents = source.Extended?.ScaleYEvents;
        var isDirectMoveShape =
            source.Father < 0
            && IsCenteredAnchor(source)
            && NonEmptyLayerCount(source, static layer => layer.MoveXEvents) <= 1
            && NonEmptyLayerCount(source, static layer => layer.MoveYEvents) <= 1;
        var canUseDirectMove =
            isDirectMoveShape
            && CanConvertDirectCurve(moveXSourceEvents)
            && CanConvertDirectCurve(moveYSourceEvents)
            && !HasInteriorCurveBoundary(moveXSourceEvents, moveYSourceEvents);
        var isDirectRotateShape =
            !(source.Father >= 0 && source.RotateWithFather)
            && !hasMultipleRotationCurves;
        var canUseDirectRotate =
            isDirectRotateShape
            && CanConvertDirectCurve(rotateDirectSourceEvents)
            && !HasInteriorCurveBoundary(rotateDirectSourceEvents);
        var canUseDirectScale =
            CanConvertDirectScale(scaleXSourceEvents)
            && CanConvertDirectScale(scaleYSourceEvents)
            && !HasInteriorCurveBoundary(scaleXSourceEvents, scaleYSourceEvents);

        var sampleBeats = new SortedSet<Beat>(hardBeats);
        if (isDirectMoveShape && !canUseDirectMove)
            AddMovementCutPoints(sampleBeats, moveXSourceEvents, moveYSourceEvents);
        if (isDirectRotateShape && !canUseDirectRotate)
            AddRotationCutPoints(sampleBeats, rotateSourceEvents);
        AddScaleCutPoints(sampleBeats, scaleXSourceEvents, scaleYSourceEvents);
        AddUniformGrid(sampleBeats, endBeat);
        var samples = SampleGeometry(lineIndex, sampleBeats, hardBeats).ToList();

        var baseBeat = new Beat(0);
        var baseGeometry = GetGeometry(
            lineIndex,
            _timeMapper.ToMusicTime(baseBeat),
            new HashSet<int>(),
            rightSide: true
        );
        var baseWidth = baseGeometry.Width > MinimumBaseSize
            ? baseGeometry.Width
            : TextureWidth / 1350d;
        var baseHeight = baseGeometry.Height > MinimumBaseSize
            ? baseGeometry.Height
            : TextureHeight / 900d;
        var baseCenterX = ToIrCoordinate(baseGeometry.CenterX);
        var baseCenterY = ToIrCoordinate(baseGeometry.CenterY);

        var movePath = SimplifyVector(
            BuildVectorPath(samples, geometry => (geometry.CenterX, geometry.CenterY))
        ).ConvertAll(point =>
            (point.Beat, ToIrCoordinate(point.X), ToIrCoordinate(point.Y), point.Hard)
        );
        var scaleSamples = samples.ConvertAll(sample =>
            sample with { Hard = scaleHardBeats.Contains(sample.Beat) }
        );
        var rotateSamples = samples.ConvertAll(sample =>
            sample with { Hard = rotateHardBeats.Contains(sample.Beat) }
        );
        var scalePath = SimplifyVector(
            BuildVectorPath(
                scaleSamples,
                geometry => (geometry.Width / baseWidth, geometry.Height / baseHeight)
            )
        );
        var rotatePath = SimplifyScalar(
            BuildScalarPath(rotateSamples, geometry => geometry.Rotation)
        );
        var moveXEvents = CreateVectorEvents(movePath, baseCenterX, baseCenterY, xAxis: true);
        var moveYEvents = CreateVectorEvents(movePath, baseCenterX, baseCenterY, xAxis: false);
        var scaleXEvents = CreateVectorEvents(scalePath, 1d, 1d, xAxis: true);
        var scaleYEvents = CreateVectorEvents(scalePath, 1d, 1d, xAxis: false);
        var rotateEvents = CreateScalarEvents(rotatePath, 0d);

        if (canUseDirectMove)
        {
            if (
                !TryConvertDirectCurve(
                    moveXSourceEvents,
                    RpeTransform.TransformToIrX,
                    baseCenterX,
                    out var directMoveX
                )
            )
                throw new InvalidOperationException("无法转换已验证的噪域 X 轴移动事件。");
            if (
                !TryConvertDirectCurve(
                    moveYSourceEvents,
                    RpeTransform.TransformToIrY,
                    baseCenterY,
                    out var directMoveY
                )
            )
                throw new InvalidOperationException("无法转换已验证的噪域 Y 轴移动事件。");

            moveXEvents = directMoveX;
            moveYEvents = directMoveY;
        }

        if (canUseDirectRotate)
        {
            if (
                !TryConvertDirectCurve(
                    rotateDirectSourceEvents,
                    RpeTransform.TransformToIrAngle,
                    0d,
                    out var directRotate
                )
            )
                throw new InvalidOperationException("无法转换已验证的噪域旋转事件。");
            rotateEvents = directRotate;
        }

        if (canUseDirectScale)
        {
            if (
                !TryConvertDirectScale(
                    scaleXSourceEvents,
                    baseBeat: new Beat(0),
                    canonicalBaseSize: TextureWidth / 1350d,
                    out var directScaleX
                )
            )
                throw new InvalidOperationException("无法转换已验证的噪域 X 轴缩放事件。");
            if (
                !TryConvertDirectScale(
                    scaleYSourceEvents,
                    baseBeat: new Beat(0),
                    canonicalBaseSize: TextureHeight
                                       * AspectWidth
                                       / (1350d * AspectHeight),
                    out var directScaleY
                )
            )
                throw new InvalidOperationException("无法转换已验证的噪域 Y 轴缩放事件。");

            scaleXEvents = directScaleX;
            scaleYEvents = directScaleY;
        }

        var geometryTemplate = new Ir.BlockArea
        {
            TopRightX = ToIrCoordinate(baseGeometry.CenterX + baseWidth / 2d),
            TopRightY = ToIrCoordinate(baseGeometry.CenterY + baseHeight / 2d),
            BottomLeftX = ToIrCoordinate(baseGeometry.CenterX - baseWidth / 2d),
            BottomLeftY = ToIrCoordinate(baseGeometry.CenterY - baseHeight / 2d),
            IsSubtract = StellateRePhiEditExtendedTexture.IsSubtractTexture(source.Texture),
            MoveXEvents = moveXEvents,
            MoveYEvents = moveYEvents,
            ScaleXEvents = scaleXEvents,
            ScaleYEvents = scaleYEvents,
            RotateEvents = rotateEvents,
        };

        var result = new List<Ir.BlockArea>(lifeCycles.Count);
        foreach (var lifeCycle in lifeCycles)
        {
            _ct.ThrowIfCancellationRequested();
            var area = geometryTemplate.Clone();
            area.AppearBeat = lifeCycle.AppearBeat;
            area.EnableBeat = lifeCycle.EnableBeat;
            area.DisableBeat = lifeCycle.DisableBeat;
            area.DisappearBeat = lifeCycle.DisappearBeat;
            result.Add(area);
        }

        return result;
    }

    private bool TryConvertDirectScale(
        List<RpeEvent>? events,
        Beat baseBeat,
        double canonicalBaseSize,
        out List<IrEvent>? converted
    )
    {
        converted = null;
        if (events is not { Count: > 0 })
            return true;
        if (!CanConvertDirectScale(events))
            return false;

        var baseScale = FiniteOr(
            CurveValue(
                events,
                _timeMapper.ToMusicTime(baseBeat),
                rightSide: true,
                1d
            ),
            1d
        );
        var divisor = Math.Abs(baseScale) * canonicalBaseSize > MinimumBaseSize
            ? Math.Abs(baseScale)
            : 1d;
        return TryConvertDirectCurve(
            events,
            value => Math.Abs(value) / divisor,
            1d,
            out converted
        );
    }

    private static bool TryConvertDirectCurve(
        List<RpeEvent>? events,
        Func<float, double> valueTransform,
        double initialValue,
        out List<IrEvent>? converted
    )
    {
        converted = null;
        if (events is not { Count: > 0 })
            return true;
        if (!CanConvertDirectCurve(events))
            return false;

        var result = events
            .Select((evt, index) => (Event: evt, Index: index))
            .OrderBy(item => item.Event.StartBeat)
            .ThenBy(item => item.Event.EndBeat)
            .ThenBy(item => item.Index)
            .Select(item =>
            {
                var evt = item.Event;
                if (evt.BezierPoints is null)
                {
                    evt = new RpeEvent
                    {
                        IsBezier = false,
                        BezierPoints = [],
                        Easing = evt.Easing,
                        EasingLeft = evt.EasingLeft,
                        EasingRight = evt.EasingRight,
                        StartValue = evt.StartValue,
                        EndValue = evt.EndValue,
                        StartBeat = new Beat((int[])evt.StartBeat),
                        EndBeat = new Beat((int[])evt.EndBeat),
                        Font = evt.Font,
                    };
                }

                var convertedEvent = RpeEventBuilder.ConvertFloatToDoubleEvent(
                    evt,
                    valueTransform
                );
                convertedEvent.IsBezier = false;
                convertedEvent.EasingLeft = 0f;
                convertedEvent.EasingRight = 1f;
                convertedEvent.Easing = new Ir.Easing(
                    RpeEasingConverter.MapToIr(
                        new RpeEasing(StellateRePhiEditEasing.NormalizeType((int)evt.Easing))
                    )
                );
                return convertedEvent;
            })
            .ToList();
        if (
            result.Any(evt =>
                Math.Abs(evt.StartValue - initialValue) > ValueEpsilon
                || Math.Abs(evt.EndValue - initialValue) > ValueEpsilon
            )
        )
            converted = result;
        return true;
    }

    private static bool CanPreserveNativeEasings(IEnumerable<RpeEvent> events) =>
        events.All(evt =>
            !StellateRePhiEditEasing.IsBezierEvent(evt)
            && IsFullEasingRange(evt)
            && StellateRePhiEditEasing.TryMapToBlockArea((int)evt.Easing, out _)
        );

    private static bool IsFullEasingRange(RpeEvent evt) =>
        Math.Abs(evt.EasingLeft) <= ValueEpsilon
        && Math.Abs(evt.EasingRight - 1f) <= ValueEpsilon;

    private static bool CanConvertDirectCurve(List<RpeEvent>? events)
    {
        if (events is not { Count: > 0 })
            return true;
        if (!CanPreserveNativeEasings(events))
            return false;

        var ordered = events
            .Select((evt, index) => (Event: evt, Index: index))
            .OrderBy(item => item.Event.StartBeat)
            .ThenBy(item => item.Event.EndBeat)
            .ThenBy(item => item.Index)
            .ToList();
        if (ordered[0].Event.StartBeat > new Beat(0))
            return false;

        for (var index = 0; index < ordered.Count; index++)
        {
            var current = ordered[index].Event;
            if (current.StartBeat > current.EndBeat)
                return false;
            if (index > 0 && current.StartBeat < ordered[index - 1].Event.EndBeat)
                return false;
        }

        return true;
    }

    private static bool CanConvertDirectScale(List<RpeEvent>? events) =>
        CanConvertDirectCurve(events)
        && (
            events is not { Count: > 0 }
            || !events.Any(evt =>
                evt.StartBeat < evt.EndBeat && evt.StartValue * evt.EndValue < 0f
            )
        );

    private static List<RpeEvent>? RemoveRedundantConstantEvents(List<RpeEvent>? events)
    {
        if (events is not { Count: > 1 })
            return events;

        var ordered = events
            .Select((evt, index) => (Event: evt, Index: index))
            .OrderBy(item => item.Event.StartBeat)
            .ThenBy(item => item.Event.EndBeat)
            .ThenBy(item => item.Index)
            .ToList();
        var result = new List<RpeEvent>(ordered.Count);
        for (var index = 0; index < ordered.Count; index++)
        {
            var current = ordered[index].Event;
            var isRedundantConstant =
                Math.Abs(current.StartValue - current.EndValue) <= ValueEpsilon
                && ordered.Skip(index + 1).Any(item =>
                    item.Event.StartBeat == current.StartBeat
                    && item.Event.EndBeat >= current.EndBeat
                    && Math.Abs(item.Event.StartValue - current.StartValue) <= ValueEpsilon
                    && Math.Abs(item.Event.EndValue - current.EndValue) <= ValueEpsilon
                );
            if (!isRedundantConstant)
                result.Add(current);
        }

        return result;
    }

    private static bool HasInteriorCurveBoundary(params List<RpeEvent>?[] curves)
    {
        var events = curves
            .Where(curve => curve is { Count: > 0 })
            .SelectMany(curve => curve!)
            .ToList();
        if (events.Count == 0)
            return false;

        var boundaries = new SortedSet<Beat> { new(0) };
        foreach (var evt in events)
        {
            boundaries.Add(evt.StartBeat);
            boundaries.Add(evt.EndBeat);
        }

        return events.Any(evt =>
            evt.StartBeat < evt.EndBeat
            && HasInteriorBoundary(boundaries, evt.StartBeat, evt.EndBeat)
        );
    }

    private static bool IsCenteredAnchor(RpeJudgeLine line) =>
        line.Anchor is { Length: >= 2 }
        && Math.Abs(line.Anchor[0] - 0.5f) <= ValueEpsilon
        && Math.Abs(line.Anchor[1] - 0.5f) <= ValueEpsilon;

    private static int NonEmptyLayerCount(
        RpeJudgeLine line,
        Func<RpeEventLayer, List<RpeEvent>?> selector
    ) => line.EventLayers.Count(layer => layer is not null && selector(layer) is { Count: > 0 });

    private static List<RpeEvent>? GetSingleCurve(
        RpeJudgeLine line,
        Func<RpeEventLayer, List<RpeEvent>?> selector
    ) => line.EventLayers.Where(layer => layer is not null)
        .Select(selector)
        .FirstOrDefault(events => events is { Count: > 0 });

    private static List<RpeEvent>? GetDirectRotationCurve(
        RpeJudgeLine line,
        out bool hasMultipleRotationCurves
    )
    {
        hasMultipleRotationCurves = false;
        List<RpeEvent>? result = null;
        foreach (var events in line.EventLayers.Select(layer => layer?.RotateEvents))
        {
            if (events is not { Count: > 0 } || IsNeutralRotationCurve(events))
                continue;
            if (result is not null)
            {
                hasMultipleRotationCurves = true;
                return null;
            }

            result = events;
        }

        return result;
    }

    private static bool IsNeutralRotationCurve(IEnumerable<RpeEvent> events) =>
        events.All(evt =>
            Math.Abs(evt.StartValue) <= ValueEpsilon && Math.Abs(evt.EndValue) <= ValueEpsilon
        );

    private List<(int Code, Beat Beat, int Order)> GetMarkers(RpeJudgeLine line)
    {
        var result = new List<(int Code, Beat Beat, int Order)>();
        var order = 0;
        foreach (var layer in line.EventLayers)
        {
            _ct.ThrowIfCancellationRequested();
            if (layer?.SpeedEvents is not { Count: > 0 } events)
                continue;
            foreach (var evt in events)
            {
                if (!float.IsFinite(evt.StartValue))
                    continue;
                var code = (int)Math.Round(evt.StartValue);
                if (code is >= 1 and <= 4 && Math.Abs(evt.StartValue - code) <= 1e-6)
                    result.Add((code, evt.StartBeat, order++));
            }
        }

        return
        [
            .. result
                .OrderBy(marker => marker.Beat)
                .ThenBy(marker => marker.Order),
        ];
    }

    private List<(Beat AppearBeat, Beat EnableBeat, Beat DisableBeat, Beat DisappearBeat)> BuildLifeCycles(
        List<(int Code, Beat Beat, int Order)> markers
    )
    {
        if (markers.Count == 0)
            return [];

        var groups = new List<List<(int Code, Beat Beat, int Order)>> { new() };
        foreach (var marker in markers)
        {
            var currentGroup = groups[^1];
            if (currentGroup.Count > 0 && marker.Code <= currentGroup[^1].Code)
                groups.Add([]);
            groups[^1].Add(marker);
        }

        var result = new List<(Beat, Beat, Beat, Beat)>(groups.Count);
        for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            _ct.ThrowIfCancellationRequested();
            var group = groups[groupIndex];
            var groupEndBeat = groupIndex + 1 < groups.Count
                ? groups[groupIndex + 1][0].Beat
                : _chartEndBeat;
            if (groupEndBeat < group[^1].Beat)
                groupEndBeat = group[^1].Beat;

            var phaseBeats = new Beat[4];
            for (var phase = 1; phase <= 4; phase++)
            {
                var markerIndex = 0;
                while (markerIndex < group.Count && group[markerIndex].Code < phase)
                    markerIndex++;
                phaseBeats[phase - 1] = markerIndex < group.Count
                    ? group[markerIndex].Beat
                    : groupEndBeat;
            }

            result.Add((phaseBeats[0], phaseBeats[1], phaseBeats[2], phaseBeats[3]));
        }

        return result;
    }

    private StellateBlockAreaGeometry GetGeometry(
        int lineIndex,
        double musicTime,
        HashSet<int> visiting,
        bool rightSide
    )
    {
        var pose = GetPose(lineIndex, musicTime, rightSide, visiting);
        var line = _source.JudgeLineList[lineIndex];
        var scaleX = FiniteOr(
            CurveValue(line.Extended?.ScaleXEvents, musicTime, rightSide, 1d),
            1d
        );
        var scaleY = FiniteOr(
            CurveValue(line.Extended?.ScaleYEvents, musicTime, rightSide, 1d),
            1d
        );

        var imageWidth = TextureWidth * scaleX * AspectWidth / 1350d;
        var imageHeight = TextureHeight * scaleY * AspectWidth / 1350d;
        var anchorX = line.Anchor is { Length: >= 2 } ? line.Anchor[0] : 0.5d;
        var anchorY = line.Anchor is { Length: >= 2 } ? line.Anchor[1] : 0.5d;
        var lineScreenX = AspectWidth * 0.5d + pose.X * AspectWidth / 1350d;
        var lineScreenY = AspectHeight * 0.5d - pose.Y * AspectHeight / 900d;
        var deltaX = (0.5d - anchorX) * imageWidth;
        var deltaY = (anchorY - 0.5d) * imageHeight;
        var radians = pose.Rotation * Math.PI / 180d;
        var shiftX = deltaX * Math.Cos(radians) - deltaY * Math.Sin(radians);
        var shiftY = deltaX * Math.Sin(radians) + deltaY * Math.Cos(radians);

        return new StellateBlockAreaGeometry(
            (lineScreenX + shiftX) / AspectWidth,
            1d - (lineScreenY + shiftY) / AspectHeight,
            Math.Abs(imageWidth / AspectWidth),
            Math.Abs(imageHeight / AspectHeight),
            -pose.Rotation
        );
    }

    private (double X, double Y, double Rotation) GetPose(
        int lineIndex,
        double musicTime,
        bool rightSide,
        HashSet<int> visiting
    )
    {
        if (!visiting.Add(lineIndex))
            return (0d, 0d, 0d);

        try
        {
            var line = _source.JudgeLineList[lineIndex];
            var x = FiniteOr(
                SumLayerCurve(line, musicTime, rightSide, static layer => layer.MoveXEvents),
                0d
            );
            var y = FiniteOr(
                SumLayerCurve(line, musicTime, rightSide, static layer => layer.MoveYEvents),
                0d
            );
            var rotation = FiniteOr(
                SumLayerCurve(line, musicTime, rightSide, static layer => layer.RotateEvents),
                0d
            );

            if (line.Father < 0 || line.Father >= _source.JudgeLineList.Count) 
                return (x, y, rotation);
            var father = GetPose(line.Father, musicTime, rightSide, visiting);
            x += father.X;
            y += father.Y;
            var parentPhysicalX = father.X * AspectWidth / 1350d;
            var parentPhysicalY = father.Y * AspectHeight / 900d;
            var childPhysicalX = x * AspectWidth / 1350d;
            var childPhysicalY = y * AspectHeight / 900d;
            (childPhysicalX, childPhysicalY) = RotatePoint(
                childPhysicalX,
                childPhysicalY,
                parentPhysicalX,
                parentPhysicalY,
                -father.Rotation * Math.PI / 180d
            );
            x = childPhysicalX * 1350d / AspectWidth;
            y = childPhysicalY * 900d / AspectHeight;
            if (line.RotateWithFather)
                rotation += father.Rotation;

            return (x, y, rotation);
        }
        finally
        {
            visiting.Remove(lineIndex);
        }
    }

    private static (double X, double Y) RotatePoint(
        double x,
        double y,
        double centerX,
        double centerY,
        double radians
    )
    {
        var offsetX = x - centerX;
        var offsetY = y - centerY;
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);
        return (
            centerX + offsetX * cosine - offsetY * sine,
            centerY + offsetX * sine + offsetY * cosine
        );
    }

    private double SumLayerCurve(
        RpeJudgeLine line,
        double musicTime,
        bool rightSide,
        Func<RpeEventLayer, List<RpeEvent>?> selector
    )
    {
        return line.EventLayers.OfType<RpeEventLayer>()
            .Sum(layer => CurveValue(selector(layer), musicTime, rightSide, 0d));
    }

    private double CurveValue(
        List<RpeEvent>? events,
        double musicTime,
        bool rightSide,
        double defaultValue
    )
    {
        if (events is not { Count: > 0 })
            return defaultValue;

        if (!_curveIndexes.TryGetValue(events, out var curveIndex))
            throw new InvalidOperationException("扩展曲线索引未初始化。");

        var (dominantIndex, previousIndex) = curveIndex.FindDominant(
            musicTime,
            CurveStartTimeEpsilon
        );
        if (dominantIndex < 0)
            return EventValueAtMusicTime(
                curveIndex,
                curveIndex.FirstIndex,
                musicTime,
                allowExtrapolation: true
            );

        var dominant = curveIndex.GetEvent(dominantIndex);
        var dominantStartTime = curveIndex.GetStartTime(dominantIndex);
        if (!(Math.Abs(musicTime - dominantStartTime) <= CurveStartTimeEpsilon))
            return EventValueAtMusicTime(
                curveIndex,
                dominantIndex,
                musicTime,
                allowExtrapolation: false
            );
        if (rightSide)
            return EventValueAtStart(curveIndex, dominantIndex);
        return previousIndex < 0
            ? EventValueAtStart(curveIndex, dominantIndex)
            : EventValueAtMusicTime(
                curveIndex,
                previousIndex,
                musicTime,
                allowExtrapolation: false
            );
    }

    private static double EventValueAtStart(StellateCurveIndex curveIndex, int eventIndex)
    {
        var curveEvent = curveIndex.GetEvent(eventIndex);
        return curveIndex.GetEndTime(eventIndex) <= curveIndex.GetStartTime(eventIndex)
            ? curveEvent.EndValue
            : curveEvent.StartValue;
    }

    private static double EventValueAtMusicTime(
        StellateCurveIndex curveIndex,
        int eventIndex,
        double time,
        bool allowExtrapolation
    )
    {
        var evt = curveIndex.GetEvent(eventIndex);
        var startTime = curveIndex.GetStartTime(eventIndex);
        var endTime = curveIndex.GetEndTime(eventIndex);
        if (endTime <= startTime)
            return evt.EndValue;
        if (!allowExtrapolation && time <= startTime)
            return evt.StartValue;
        if (time >= endTime)
            return evt.EndValue;
        var progress = (time - startTime) / (endTime - startTime);
        var easedProgress = StellateRePhiEditEasing.Evaluate(evt, progress, !allowExtrapolation);
        return evt.StartValue + (evt.EndValue - evt.StartValue) * easedProgress;
    }

    private void CollectPoseBreaks(int lineIndex, SortedSet<Beat> beats, HashSet<int> visited)
    {
        while (true)
        {
            if (!visited.Add(lineIndex)) return;
            var line = _source.JudgeLineList[lineIndex];
            foreach (var layer in line.EventLayers.OfType<RpeEventLayer>())
            {
                AddCurveBoundaries(layer.MoveXEvents, beats);
                AddCurveBoundaries(layer.MoveYEvents, beats);
                AddCurveBoundaries(layer.RotateEvents, beats);
            }

            if (line.Father >= 0 && line.Father < _source.JudgeLineList.Count)
            {
                lineIndex = line.Father;
                continue;
            }

            break;
        }
    }

    private void CollectRotationBreaks(int lineIndex, SortedSet<Beat> beats, HashSet<int> visited)
    {
        while (true)
        {
            if (!visited.Add(lineIndex)) return;

            var line = _source.JudgeLineList[lineIndex];
            foreach (var layer in line.EventLayers.OfType<RpeEventLayer>())
            {
                AddCurveBoundaries(layer.RotateEvents, beats);
            }

            if (line.RotateWithFather && line.Father >= 0 && line.Father < _source.JudgeLineList.Count)
            {
                lineIndex = line.Father;
                continue;
            }

            break;
        }
    }

    private static void AddCurveBoundaries(List<RpeEvent>? events, SortedSet<Beat> beats)
    {
        if (events is null)
            return;
        foreach (var evt in events)
        {
            beats.Add(evt.StartBeat);
            beats.Add(evt.EndBeat);
        }
    }

    private static SortedSet<Beat> CreateCurveBoundarySet(params List<RpeEvent>?[] curves)
    {
        var times = new SortedSet<Beat> { new(0) };
        foreach (var curve in curves)
            AddCurveBoundaries(curve, times);
        return times;
    }

    private void AddMovementCutPoints(
        SortedSet<Beat> sampleBeats,
        List<RpeEvent>? moveXEvents,
        List<RpeEvent>? moveYEvents
    )
    {
        var curves = new[] { moveXEvents, moveYEvents };
        var times = CreateCurveBoundarySet(curves);
        for (var pass = 0; pass < 2; pass++)
        {
            AddCurveCutPoints(times, moveXEvents, splitSupportedIfInterrupted: true);
            AddCurveCutPoints(times, moveYEvents, splitSupportedIfInterrupted: true);
        }

        sampleBeats.UnionWith(times);
    }

    private void AddRotationCutPoints(
        SortedSet<Beat> sampleBeats,
        List<RpeEvent>? rotateEvents
    )
    {
        var times = CreateCurveBoundarySet(rotateEvents);
        AddCurveCutPoints(times, rotateEvents, splitSupportedIfInterrupted: false);
        sampleBeats.UnionWith(times);
    }

    private void AddScaleCutPoints(
        SortedSet<Beat> sampleBeats,
        List<RpeEvent>? scaleXEvents,
        List<RpeEvent>? scaleYEvents
    )
    {
        var times = CreateCurveBoundarySet(scaleXEvents, scaleYEvents);
        for (var pass = 0; pass < 2; pass++)
        {
            AddCurveCutPoints(times, scaleXEvents, splitSupportedIfInterrupted: true);
            AddScaleZeroCrossingCutPoints(times, scaleXEvents);
            AddCurveCutPoints(times, scaleYEvents, splitSupportedIfInterrupted: true);
            AddScaleZeroCrossingCutPoints(times, scaleYEvents);
        }

        sampleBeats.UnionWith(times);
    }

    private void AddCurveCutPoints(
        SortedSet<Beat> times,
        List<RpeEvent>? events,
        bool splitSupportedIfInterrupted
    )
    {
        if (events is not { Count: > 0 })
            return;

        foreach (var evt in events)
        {
            _ct.ThrowIfCancellationRequested();
            if (evt.StartBeat >= evt.EndBeat)
                continue;

            var isMapped = CanMapNativeEasing(evt, out var areaEaseType);
            var hasInteriorBoundary = HasInteriorBoundary(times, evt);
            if (!isMapped || (splitSupportedIfInterrupted && areaEaseType != 0 && hasInteriorBoundary))
                AddUniformCutPoints(times, evt);
        }
    }

    private void AddScaleZeroCrossingCutPoints(
        SortedSet<Beat> times,
        List<RpeEvent>? events
    )
    {
        if (events is not { Count: > 0 })
            return;

        foreach (var evt in events)
        {
            _ct.ThrowIfCancellationRequested();
            if (
                evt.StartBeat < evt.EndBeat
                && (double)evt.StartValue * evt.EndValue < 0d
            )
                AddUniformCutPoints(times, evt);
        }
    }

    private void AddUniformCutPoints(SortedSet<Beat> times, RpeEvent evt)
    {
        var first = (long)Math.Floor((double)evt.StartBeat * SamplesPerBeat) + 1L;
        var last = (long)Math.Ceiling((double)evt.EndBeat * SamplesPerBeat) - 1L;
        for (var index = first; index <= last; index++)
        {
            _ct.ThrowIfCancellationRequested();
            var beat = new Beat((double)index / SamplesPerBeat);
            if (evt.StartBeat < beat && beat < evt.EndBeat)
                times.Add(beat);
        }
    }

    private static bool HasInteriorBoundary(SortedSet<Beat> times, RpeEvent evt) =>
        HasInteriorBoundary(times, evt.StartBeat, evt.EndBeat);

    private static bool HasInteriorBoundary(SortedSet<Beat> times, Beat startBeat, Beat endBeat) =>
        startBeat < endBeat
        && times
            .GetViewBetween(startBeat, endBeat)
            .Any(beat => startBeat < beat && beat < endBeat);

    private static bool CanMapNativeEasing(RpeEvent evt, out int areaEaseType)
    {
        areaEaseType = -1;
        return !StellateRePhiEditEasing.IsBezierEvent(evt)
               && IsFullEasingRange(evt)
               && StellateRePhiEditEasing.TryMapToBlockArea((int)evt.Easing, out areaEaseType);
    }

    private void AddUniformGrid(SortedSet<Beat> beats, Beat endBeat)
    {
        var count = (long)Math.Floor(Math.Max(0d, endBeat) * SamplesPerBeat + 1e-9);
        for (long index = 0; index <= count; index++)
        {
            _ct.ThrowIfCancellationRequested();
            beats.Add(new Beat((double)index / SamplesPerBeat));
        }

        beats.Add(endBeat);
    }

    private List<(Beat Beat, double X, double Y, bool Hard)> BuildVectorPath(
        List<(
            Beat Beat,
            StellateBlockAreaGeometry Left,
            StellateBlockAreaGeometry Right,
            bool Hard
            )> samples,
        Func<StellateBlockAreaGeometry, (double X, double Y)> selector
    )
    {
        var result = new List<(Beat, double, double, bool)>();
        for (var index = 0; index < samples.Count; index++)
        {
            _ct.ThrowIfCancellationRequested();
            var sample = samples[index];
            var left = selector(sample.Left);
            var right = selector(sample.Right);
            if (index > 0)
                result.Add((sample.Beat, left.X, left.Y, sample.Hard));
            if (
                index == 0
                || Math.Abs(left.X - right.X) > ValueEpsilon
                || Math.Abs(left.Y - right.Y) > ValueEpsilon
            )
                result.Add((sample.Beat, right.X, right.Y, index > 0 || sample.Hard));
        }

        return result;
    }

    private List<(Beat Beat, double Value, bool Hard)> BuildScalarPath(
        List<(
            Beat Beat,
            StellateBlockAreaGeometry Left,
            StellateBlockAreaGeometry Right,
            bool Hard
            )> samples,
        Func<StellateBlockAreaGeometry, double> selector
    )
    {
        var result = new List<(Beat, double, bool)>();
        for (var index = 0; index < samples.Count; index++)
        {
            _ct.ThrowIfCancellationRequested();
            var sample = samples[index];
            var left = selector(sample.Left);
            var right = selector(sample.Right);
            if (index > 0)
                result.Add((sample.Beat, left, sample.Hard));
            if (index == 0 || Math.Abs(left - right) > ValueEpsilon)
                result.Add((sample.Beat, right, index > 0 || sample.Hard));
        }

        return result;
    }

    private List<(Beat Beat, double X, double Y, bool Hard)> SimplifyVector(
        List<(Beat Beat, double X, double Y, bool Hard)> points
    )
    {
        if (points.Count <= 2)
            return points;

        var times = points.ConvertAll(point => _timeMapper.ToMusicTime(point.Beat));
        var keep = new bool[points.Count];
        keep[0] = true;
        keep[^1] = true;
        for (var index = 1; index < points.Count; index++)
        {
            if (!(times[index] <= times[index - 1] + 1e-12)) continue;
            keep[index - 1] = true;
            keep[index] = true;
        }

        var anchors = Enumerable.Range(0, keep.Length).Where(index => keep[index]).ToArray();
        var ranges = new Stack<(int Start, int End)>();
        for (var index = 0; index < anchors.Length - 1; index++)
            ranges.Push((anchors[index], anchors[index + 1]));

        while (ranges.TryPop(out var range))
        {
            var startIndex = range.Start;
            var endIndex = range.End;
            if (
                endIndex <= startIndex + 1
                || times[endIndex] <= times[startIndex] + 1e-12
            )
                continue;

            var largestDeviation = 1d;
            var splitIndex = -1;
            for (var index = startIndex + 1; index < endIndex; index++)
            {
                var progress =
                    (times[index] - times[startIndex]) / (times[endIndex] - times[startIndex]);
                var expectedX =
                    points[startIndex].X
                    + (points[endIndex].X - points[startIndex].X) * progress;
                var expectedY =
                    points[startIndex].Y
                    + (points[endIndex].Y - points[startIndex].Y) * progress;
                var deviationX = RelativeDeviation(
                    points[index].X,
                    expectedX,
                    points[startIndex].X,
                    points[endIndex].X
                );
                var deviationY = RelativeDeviation(
                    points[index].Y,
                    expectedY,
                    points[startIndex].Y,
                    points[endIndex].Y
                );
                var deviation = Math.Max(
                    deviationX / SimplifyTolerance,
                    deviationY / SimplifyTolerance
                );
                if (points[index].Hard)
                    deviation = Math.Max(
                        deviation,
                        Math.Max(
                            deviationX / HardPointEpsilon,
                            deviationY / HardPointEpsilon
                        )
                    );
                if (!(deviation > largestDeviation)) continue;
                largestDeviation = deviation;
                splitIndex = index;
            }

            if (splitIndex < 0)
                continue;
            keep[splitIndex] = true;
            ranges.Push((startIndex, splitIndex));
            ranges.Push((splitIndex, endIndex));
        }

        return points.Where((_, index) => keep[index]).ToList();
    }

    private List<(Beat Beat, double Value, bool Hard)> SimplifyScalar(
        List<(Beat Beat, double Value, bool Hard)> points
    )
    {
        if (points.Count <= 2)
            return points;

        var times = points.ConvertAll(point => _timeMapper.ToMusicTime(point.Beat));
        var keep = new bool[points.Count];
        keep[0] = true;
        keep[^1] = true;
        for (var index = 1; index < points.Count; index++)
        {
            if (!(times[index] <= times[index - 1] + 1e-12)) continue;
            keep[index - 1] = true;
            keep[index] = true;
        }

        var anchors = Enumerable.Range(0, keep.Length).Where(index => keep[index]).ToArray();
        var ranges = new Stack<(int Start, int End)>();
        for (var index = 0; index < anchors.Length - 1; index++)
            ranges.Push((anchors[index], anchors[index + 1]));

        while (ranges.TryPop(out var range))
        {
            var startIndex = range.Start;
            var endIndex = range.End;
            if (
                endIndex <= startIndex + 1
                || times[endIndex] <= times[startIndex] + 1e-12
            )
                continue;

            var largestDeviation = 1d;
            var splitIndex = -1;
            for (var index = startIndex + 1; index < endIndex; index++)
            {
                var progress =
                    (times[index] - times[startIndex]) / (times[endIndex] - times[startIndex]);
                var expected =
                    points[startIndex].Value
                    + (points[endIndex].Value - points[startIndex].Value) * progress;
                var deviation = RelativeDeviation(
                    points[index].Value,
                    expected,
                    points[startIndex].Value,
                    points[endIndex].Value
                );
                var normalizedDeviation = deviation / SimplifyTolerance;
                if (points[index].Hard)
                    normalizedDeviation = Math.Max(
                        normalizedDeviation,
                        deviation / HardPointEpsilon
                    );
                if (normalizedDeviation > largestDeviation)
                {
                    largestDeviation = normalizedDeviation;
                    splitIndex = index;
                }
            }

            if (splitIndex < 0)
                continue;
            keep[splitIndex] = true;
            ranges.Push((startIndex, splitIndex));
            ranges.Push((splitIndex, endIndex));
        }

        return points.Where((_, index) => keep[index]).ToList();
    }

    private static List<IrEvent>? CreateVectorEvents(
        List<(Beat Beat, double X, double Y, bool Hard)> points,
        double initialX,
        double initialY,
        bool xAxis
    )
    {
        var events = new List<IrEvent>();
        var previousBeat = new Beat(0);
        var previousX = initialX;
        var previousY = initialY;

        foreach (var point in points)
        {
            if (point.Beat < previousBeat)
                continue;
            var nextValue = xAxis ? point.X : point.Y;
            var previousValue = xAxis ? previousX : previousY;
            if (Math.Abs(nextValue - previousValue) > ValueEpsilon)
            {
                events.Add(
                    CreateEvent(
                        previousBeat,
                        point.Beat,
                        previousValue,
                        nextValue
                    )
                );
            }

            previousBeat = point.Beat;
            if (xAxis)
                previousX = point.X;
            else
                previousY = point.Y;
        }

        return events.Count > 0 ? events : null;
    }

    private static List<IrEvent>? CreateScalarEvents(
        List<(Beat Beat, double Value, bool Hard)> points,
        double initialValue
    )
    {
        var events = new List<IrEvent>();
        var previousBeat = new Beat(0);
        var previousValue = initialValue;
        foreach (var point in points)
        {
            if (point.Beat < previousBeat)
                continue;
            if (Math.Abs(point.Value - previousValue) > ValueEpsilon)
                events.Add(CreateEvent(previousBeat, point.Beat, previousValue, point.Value));
            previousBeat = point.Beat;
            previousValue = point.Value;
        }

        return events.Count > 0 ? events : null;
    }

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
            Easing = new Ir.Easing(1),
        };

    private static double RelativeDeviation(double actual, double expected, double start, double end)
    {
        var span = end - start;
        return Math.Abs(span) <= 1e-12
            ? Math.Abs(actual - expected)
            : Math.Abs((actual - expected) / span);
    }

    private static double ToIrCoordinate(double percentage) => percentage * 2d - 1d;

    private static double FiniteOr(double value, double fallback) =>
        double.IsFinite(value) ? value : fallback;

    private static Beat CalculateChartEndBeat(RpeChart source)
    {
        var endBeat = new Beat(0);
        foreach (var line in source.JudgeLineList)
        {
            foreach (var layer in line.EventLayers.OfType<RpeEventLayer>())
            {
                UpdateEndBeat(layer.MoveXEvents, ref endBeat);
                UpdateEndBeat(layer.MoveYEvents, ref endBeat);
                UpdateEndBeat(layer.RotateEvents, ref endBeat);
                UpdateEndBeat(layer.AlphaEvents, ref endBeat);
                UpdateEndBeat(layer.SpeedEvents, ref endBeat);
            }

            if (line.Extended is { } extended)
            {
                UpdateEndBeat(extended.ColorEvents, ref endBeat);
                UpdateEndBeat(extended.ScaleXEvents, ref endBeat);
                UpdateEndBeat(extended.ScaleYEvents, ref endBeat);
                UpdateEndBeat(extended.TextEvents, ref endBeat);
                UpdateEndBeat(extended.PaintEvents, ref endBeat);
                UpdateEndBeat(extended.GifEvents, ref endBeat);
                UpdateEndBeat(extended.InclineEvents, ref endBeat);
            }

            foreach (var note in line.Notes ?? [])
            {
                if (note.StartBeat > endBeat)
                    endBeat = note.StartBeat;
                if (note.EndBeat > endBeat)
                    endBeat = note.EndBeat;
            }
        }

        return endBeat;
    }

    private static void UpdateEndBeat<T>(
        List<RpeEvents.Event<T>>? events,
        ref Beat endBeat
    )
        where T : notnull
    {
        if (events is null)
            return;
        foreach (var evt in events)
        {
            if (evt.StartBeat > endBeat)
                endBeat = evt.StartBeat;
            if (evt.EndBeat > endBeat)
                endBeat = evt.EndBeat;
        }
    }
}