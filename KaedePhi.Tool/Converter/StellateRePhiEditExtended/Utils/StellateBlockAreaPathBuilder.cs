using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using KaedePhi.Tool.Converter.StellateRePhiEditExtended.Model;
using RpeEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<float>;
using IrBpmItem = KaedePhi.Core.Intermediate.Model.BpmItem;
using IrEvent = KaedePhi.Core.Intermediate.Model.Events.Event<double>;
using IrEasing = KaedePhi.Core.Intermediate.Model.Easing;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;

/// <summary>
/// 根据采样几何生成并简化噪域转换路径。
/// </summary>
public sealed class StellateBlockAreaPathBuilder
{
    private readonly PhigrosV3TimeMapper _timeMapper;
    private readonly CancellationToken _ct;

    /// <summary>
    /// 根据谱面的 BPM 列表创建噪域路径构建器。
    /// </summary>
    /// <param name="bpmList">用于将拍数转换为音乐时间的 BPM 列表</param>
    /// <param name="cancellationToken">用于取消路径生成的令牌</param>
    /// <remarks>对简化曲线而言，BPM 列表会影响拍数到音乐时间的映射。</remarks>
    public StellateBlockAreaPathBuilder(
        IReadOnlyList<IrBpmItem> bpmList,
        CancellationToken cancellationToken
    ) : this(new PhigrosV3TimeMapper(bpmList), cancellationToken) { }

    internal StellateBlockAreaPathBuilder(
        PhigrosV3TimeMapper timeMapper,
        CancellationToken cancellationToken
    )
    {
        _timeMapper = timeMapper;
        _ct = cancellationToken;
    }

    /// <summary>
    /// 将事件的开始拍和结束拍加入采样边界集合。
    /// </summary>
    /// <param name="events">待处理的曲线事件</param>
    /// <param name="beats">接收事件边界的拍数集合</param>
    public static void AddCurveBoundaries(List<RpeEvent>? events, SortedSet<Beat> beats)
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

    /// <summary>
    /// 为移动曲线补充采样切分点。
    /// </summary>
    /// <param name="sampleBeats">接收采样拍数的集合</param>
    /// <param name="moveXEvents">横向移动事件</param>
    /// <param name="moveYEvents">纵向移动事件</param>
    public void AddMovementCutPoints(
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

    /// <summary>
    /// 为旋转曲线补充采样切分点。
    /// </summary>
    /// <param name="sampleBeats">接收采样拍数的集合</param>
    /// <param name="rotateEvents">旋转事件</param>
    public void AddRotationCutPoints(
        SortedSet<Beat> sampleBeats,
        List<RpeEvent>? rotateEvents
    )
    {
        var times = CreateCurveBoundarySet(rotateEvents);
        AddCurveCutPoints(times, rotateEvents, splitSupportedIfInterrupted: false);
        sampleBeats.UnionWith(times);
    }

    /// <summary>
    /// 为缩放曲线补充采样切分点。
    /// </summary>
    /// <param name="sampleBeats">接收采样拍数的集合</param>
    /// <param name="scaleXEvents">横向缩放事件</param>
    /// <param name="scaleYEvents">纵向缩放事件</param>
    public void AddScaleCutPoints(
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
        var first = (long)Math.Floor((double)evt.StartBeat * StellateGeometryConstants.SamplesPerBeat) + 1L;
        var last = (long)Math.Ceiling((double)evt.EndBeat * StellateGeometryConstants.SamplesPerBeat) - 1L;
        for (var index = first; index <= last; index++)
        {
            _ct.ThrowIfCancellationRequested();
            var beat = new Beat((double)index / StellateGeometryConstants.SamplesPerBeat);
            if (evt.StartBeat < beat && beat < evt.EndBeat)
                times.Add(beat);
        }
    }

    private static bool HasInteriorBoundary(SortedSet<Beat> times, RpeEvent evt) =>
        HasInteriorBoundary(times, evt.StartBeat, evt.EndBeat);

    /// <summary>
    /// 判断拍数区间内是否存在其他采样边界。
    /// </summary>
    /// <param name="times">已收集的拍数边界</param>
    /// <param name="startBeat">区间起始拍</param>
    /// <param name="endBeat">区间结束拍</param>
    public static bool HasInteriorBoundary(SortedSet<Beat> times, Beat startBeat, Beat endBeat) =>
        startBeat < endBeat
        && times
            .GetViewBetween(startBeat, endBeat)
            .Any(beat => startBeat < beat && beat < endBeat);

    private static bool CanMapNativeEasing(RpeEvent evt, out int areaEaseType)
    {
        areaEaseType = -1;
        return !StellateRePhiEditEasing.IsBezierEvent(evt)
               && StellateDirectCurveConverter.IsFullEasingRange(evt)
               && StellateRePhiEditEasing.TryMapToBlockArea((int)evt.Easing, out areaEaseType);
    }

    /// <summary>
    /// 将均匀采样网格和结束拍加入采样集合。
    /// </summary>
    /// <param name="beats">接收采样拍数的集合</param>
    /// <param name="endBeat">采样结束拍</param>
    public void AddUniformGrid(SortedSet<Beat> beats, Beat endBeat)
    {
        var count = (long)Math.Floor(Math.Max(0d, endBeat) * StellateGeometryConstants.SamplesPerBeat + 1e-9);
        for (long index = 0; index <= count; index++)
        {
            _ct.ThrowIfCancellationRequested();
            beats.Add(new Beat((double)index / StellateGeometryConstants.SamplesPerBeat));
        }

        beats.Add(endBeat);
    }

    /// <summary>
    /// 从采样几何生成二维路径点。
    /// </summary>
    /// <param name="samples">按拍数排列的左右侧几何采样结果</param>
    /// <param name="selector">从几何中提取二维坐标的函数</param>
    /// <returns>按拍数排列的二维路径点</returns>
    public List<(Beat Beat, double X, double Y, bool Hard)> BuildVectorPath(
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
                || Math.Abs(left.X - right.X) > StellateGeometryConstants.ValueEpsilon
                || Math.Abs(left.Y - right.Y) > StellateGeometryConstants.ValueEpsilon
            )
                result.Add((sample.Beat, right.X, right.Y, index > 0 || sample.Hard));
        }

        return result;
    }

    /// <summary>
    /// 从采样几何生成一维路径点。
    /// </summary>
    /// <param name="samples">按拍数排列的左右侧几何采样结果</param>
    /// <param name="selector">从几何中提取数值的函数</param>
    /// <returns>按拍数排列的一维路径点</returns>
    public List<(Beat Beat, double Value, bool Hard)> BuildScalarPath(
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
            if (index == 0 || Math.Abs(left - right) > StellateGeometryConstants.ValueEpsilon)
                result.Add((sample.Beat, right, index > 0 || sample.Hard));
        }

        return result;
    }

    /// <summary>
    /// 按音乐时间简化二维路径，同时保留硬边界点。
    /// </summary>
    /// <param name="points">待简化的二维路径点</param>
    /// <returns>简化后的二维路径点</returns>
    public List<(Beat Beat, double X, double Y, bool Hard)> SimplifyVector(
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
                    deviationX / StellateGeometryConstants.SimplifyTolerance,
                    deviationY / StellateGeometryConstants.SimplifyTolerance
                );
                if (points[index].Hard)
                    deviation = Math.Max(
                        deviation,
                        Math.Max(
                            deviationX / StellateGeometryConstants.HardPointEpsilon,
                            deviationY / StellateGeometryConstants.HardPointEpsilon
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

    /// <summary>
    /// 按音乐时间简化一维路径，同时保留硬边界点。
    /// </summary>
    /// <param name="points">待简化的一维路径点</param>
    /// <returns>简化后的一维路径点</returns>
    public List<(Beat Beat, double Value, bool Hard)> SimplifyScalar(
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
                var normalizedDeviation = deviation / StellateGeometryConstants.SimplifyTolerance;
                if (points[index].Hard)
                    normalizedDeviation = Math.Max(
                        normalizedDeviation,
                        deviation / StellateGeometryConstants.HardPointEpsilon
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

    /// <summary>
    /// 将二维路径转换为 IR 数值事件。
    /// </summary>
    /// <param name="points">按拍数排列的二维路径点</param>
    /// <param name="initialX">横向初始值</param>
    /// <param name="initialY">纵向初始值</param>
    /// <param name="xAxis">为 <see langword="true"/> 时生成横向事件，否则生成纵向事件</param>
    public static List<IrEvent>? CreateVectorEvents(
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
            if (Math.Abs(nextValue - previousValue) > StellateGeometryConstants.ValueEpsilon)
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

    /// <summary>
    /// 将一维路径转换为 IR 数值事件。
    /// </summary>
    /// <param name="points">按拍数排列的一维路径点</param>
    /// <param name="initialValue">路径初始值</param>
    public static List<IrEvent>? CreateScalarEvents(
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
            if (Math.Abs(point.Value - previousValue) > StellateGeometryConstants.ValueEpsilon)
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

}
