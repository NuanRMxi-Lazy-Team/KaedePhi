using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using KaedePhi.Tool.Converter.StellateRePhiEditExtended.Model;
using KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;
using RpeChart = KaedePhi.Core.Formats.RePhiEdit.Model.Chart;
using RpeEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<float>;
using RpeEventLayer = KaedePhi.Core.Formats.RePhiEdit.Model.Events.EventLayer;
using RpeJudgeLine = KaedePhi.Core.Formats.RePhiEdit.Model.JudgeLine;
using IrBpmItem = KaedePhi.Core.Intermediate.Model.BpmItem;
using RpeTransform = KaedePhi.Tool.Converter.RePhiEdit.Utils.Transform;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;

/// <summary>
/// 将 RePhiEdit 标记判定线转换为 IR 噪域几何。
/// </summary>
public sealed class StellateRePhiEditGeometry
{
    private readonly RpeChart _source;
    private readonly CancellationToken _ct;
    private readonly Beat _chartEndBeat;
    private readonly StellateGeometrySampler _sampler;
    private readonly StellateBlockAreaPathBuilder _pathBuilder;

    /// <summary>
    /// 根据源谱面和 BPM 列表创建几何转换器。
    /// </summary>
    /// <param name="source">包含噪域标记判定线的 RePhiEdit 谱面</param>
    /// <param name="bpmList">用于将拍数转换为音乐时间的 BPM 列表</param>
    /// <param name="cancellationToken">用于取消几何转换的令牌</param>
    public StellateRePhiEditGeometry(
        RpeChart source,
        IReadOnlyList<IrBpmItem> bpmList,
        CancellationToken cancellationToken
    ) : this(source, new PhigrosV3TimeMapper(bpmList), cancellationToken) { }

    internal StellateRePhiEditGeometry(
        RpeChart source,
        PhigrosV3TimeMapper timeMapper,
        CancellationToken cancellationToken
    )
    {
        _source = source;
        _ct = cancellationToken;
        _chartEndBeat = StellateBlockAreaLifecycle.CalculateChartEndBeat(source);
        _sampler = new StellateGeometrySampler(source, timeMapper, cancellationToken);
        _pathBuilder = new StellateBlockAreaPathBuilder(timeMapper, cancellationToken);
    }

    /// <summary>
    /// 为指定判定线及其父级准备曲线索引。
    /// </summary>
    /// <param name="lineIndex">判定线索引</param>
    public void PrepareLine(int lineIndex) => _sampler.PrepareLine(lineIndex);

    /// <summary>
    /// 将指定判定线转换为零个或多个 IR 噪域。
    /// </summary>
    /// <param name="lineIndex">源判定线索引</param>
    /// <param name="maxDegreeOfParallelism">采样使用的最大并行度，零表示使用处理器数量</param>
    /// <returns>转换后的 IR 噪域列表</returns>
    public List<Ir.BlockArea> ConvertLine(int lineIndex, int maxDegreeOfParallelism = 0)
    {
        _ct.ThrowIfCancellationRequested();
        var source = _source.JudgeLineList[lineIndex];
        var markers = StellateBlockAreaLifecycle.GetMarkers(source, _ct);
        var lifeCycles = StellateBlockAreaLifecycle.BuildLifeCycles(markers, _chartEndBeat, _ct);
        if (lifeCycles.Count == 0)
            return [];

        _sampler.PrepareLine(lineIndex);
        var endBeat = _chartEndBeat;
        foreach (var marker in markers.Where(marker => marker.Beat > endBeat))
            endBeat = marker.Beat;

        var hardBeats = new SortedSet<Beat> { new(0) };
        _sampler.CollectPoseBreaks(lineIndex, hardBeats, []);
        StellateBlockAreaPathBuilder.AddCurveBoundaries(source.Extended?.ScaleXEvents, hardBeats);
        StellateBlockAreaPathBuilder.AddCurveBoundaries(source.Extended?.ScaleYEvents, hardBeats);
        var scaleHardBeats = new SortedSet<Beat> { new(0) };
        StellateBlockAreaPathBuilder.AddCurveBoundaries(source.Extended?.ScaleXEvents, scaleHardBeats);
        StellateBlockAreaPathBuilder.AddCurveBoundaries(source.Extended?.ScaleYEvents, scaleHardBeats);
        var rotateHardBeats = new SortedSet<Beat> { new(0) };
        _sampler.CollectRotationBreaks(lineIndex, rotateHardBeats, []);

        var moveXSourceEvents = StellateDirectCurveConverter.GetSingleCurve(source, static layer => layer.MoveXEvents);
        var moveYSourceEvents = StellateDirectCurveConverter.GetSingleCurve(source, static layer => layer.MoveYEvents);
        var rotateSourceEvents = StellateDirectCurveConverter.GetDirectRotationCurve(source, out var hasMultipleRotationCurves);
        var rotateDirectSourceEvents = StellateDirectCurveConverter.RemoveRedundantConstantEvents(rotateSourceEvents);
        var scaleXSourceEvents = source.Extended?.ScaleXEvents;
        var scaleYSourceEvents = source.Extended?.ScaleYEvents;
        var isDirectMoveShape =
            source.Father < 0
            && StellateDirectCurveConverter.IsCenteredAnchor(source)
            && StellateDirectCurveConverter.NonEmptyLayerCount(source, static layer => layer.MoveXEvents) <= 1
            && StellateDirectCurveConverter.NonEmptyLayerCount(source, static layer => layer.MoveYEvents) <= 1;
        var canUseDirectMove =
            isDirectMoveShape
            && StellateDirectCurveConverter.CanConvertDirectCurve(moveXSourceEvents)
            && StellateDirectCurveConverter.CanConvertDirectCurve(moveYSourceEvents)
            && !StellateDirectCurveConverter.HasInteriorCurveBoundary(moveXSourceEvents, moveYSourceEvents);
        var isDirectRotateShape =
            !(source.Father >= 0 && source.RotateWithFather)
            && !hasMultipleRotationCurves;
        var canUseDirectRotate =
            isDirectRotateShape
            && StellateDirectCurveConverter.CanConvertDirectCurve(rotateDirectSourceEvents)
            && !StellateDirectCurveConverter.HasInteriorCurveBoundary(rotateDirectSourceEvents);
        var canUseDirectScale =
            StellateDirectCurveConverter.CanConvertDirectScale(scaleXSourceEvents)
            && StellateDirectCurveConverter.CanConvertDirectScale(scaleYSourceEvents)
            && !StellateDirectCurveConverter.HasInteriorCurveBoundary(scaleXSourceEvents, scaleYSourceEvents);

        var sampleBeats = new SortedSet<Beat>(hardBeats);
        if (isDirectMoveShape && !canUseDirectMove)
            _pathBuilder.AddMovementCutPoints(sampleBeats, moveXSourceEvents, moveYSourceEvents);
        if (isDirectRotateShape && !canUseDirectRotate)
            _pathBuilder.AddRotationCutPoints(sampleBeats, rotateSourceEvents);
        _pathBuilder.AddScaleCutPoints(sampleBeats, scaleXSourceEvents, scaleYSourceEvents);
        _pathBuilder.AddUniformGrid(sampleBeats, endBeat);
        var sampleParallelism = maxDegreeOfParallelism > 0
            ? maxDegreeOfParallelism
            : Environment.ProcessorCount;
        var samples = _sampler.SampleGeometry(lineIndex, sampleBeats, hardBeats, sampleParallelism).ToList();

        var baseBeat = new Beat(0);
        var baseGeometry = _sampler.GetGeometryAtBeat(lineIndex, baseBeat, rightSide: true);
        var baseWidth = baseGeometry.Width > StellateGeometryConstants.MinimumBaseSize
            ? baseGeometry.Width
            : StellateGeometryConstants.TextureWidth / 1350d;
        var baseHeight = baseGeometry.Height > StellateGeometryConstants.MinimumBaseSize
            ? baseGeometry.Height
            : StellateGeometryConstants.TextureHeight / 900d;
        var baseCenterX = StellateGeometryConstants.ToIrCoordinate(baseGeometry.CenterX);
        var baseCenterY = StellateGeometryConstants.ToIrCoordinate(baseGeometry.CenterY);

        var movePath = _pathBuilder.SimplifyVector(
            _pathBuilder.BuildVectorPath(samples, geometry => (geometry.CenterX, geometry.CenterY))
        ).ConvertAll(point =>
            (point.Beat, StellateGeometryConstants.ToIrCoordinate(point.X), StellateGeometryConstants.ToIrCoordinate(point.Y), point.Hard)
        );
        var scaleSamples = samples.ConvertAll(sample =>
            sample with { Hard = scaleHardBeats.Contains(sample.Beat) }
        );
        var rotateSamples = samples.ConvertAll(sample =>
            sample with { Hard = rotateHardBeats.Contains(sample.Beat) }
        );
        var scalePath = _pathBuilder.SimplifyVector(
            _pathBuilder.BuildVectorPath(
                scaleSamples,
                geometry => (geometry.Width / baseWidth, geometry.Height / baseHeight)
            )
        );
        var rotatePath = _pathBuilder.SimplifyScalar(
            _pathBuilder.BuildScalarPath(rotateSamples, geometry => geometry.Rotation)
        );
        var moveXEvents = StellateBlockAreaPathBuilder.CreateVectorEvents(movePath, baseCenterX, baseCenterY, xAxis: true);
        var moveYEvents = StellateBlockAreaPathBuilder.CreateVectorEvents(movePath, baseCenterX, baseCenterY, xAxis: false);
        var scaleXEvents = StellateBlockAreaPathBuilder.CreateVectorEvents(scalePath, 1d, 1d, xAxis: true);
        var scaleYEvents = StellateBlockAreaPathBuilder.CreateVectorEvents(scalePath, 1d, 1d, xAxis: false);
        var rotateEvents = StellateBlockAreaPathBuilder.CreateScalarEvents(rotatePath, 0d);

        if (canUseDirectMove)
        {
            if (
                !StellateDirectCurveConverter.TryConvertDirectCurve(
                    moveXSourceEvents,
                    RpeTransform.TransformToIrX,
                    baseCenterX,
                    out var directMoveX
                )
            )
                throw new InvalidOperationException("无法转换已验证的噪域 X 轴移动事件。");
            if (
                !StellateDirectCurveConverter.TryConvertDirectCurve(
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
                !StellateDirectCurveConverter.TryConvertDirectCurve(
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
                !StellateDirectCurveConverter.TryConvertDirectScale(
                    scaleXSourceEvents,
                    baseBeat: new Beat(0),
                    canonicalBaseSize: StellateGeometryConstants.TextureWidth / 1350d,
                    geometrySampler: _sampler,
                    out var directScaleX
                )
            )
                throw new InvalidOperationException("无法转换已验证的噪域 X 轴缩放事件。");
            if (
                !StellateDirectCurveConverter.TryConvertDirectScale(
                    scaleYSourceEvents,
                    baseBeat: new Beat(0),
                    canonicalBaseSize: StellateGeometryConstants.TextureHeight
                                       * StellateGeometryConstants.AspectWidth
                                       / (1350d * StellateGeometryConstants.AspectHeight),
                    geometrySampler: _sampler,
                    out var directScaleY
                )
            )
                throw new InvalidOperationException("无法转换已验证的噪域 Y 轴缩放事件。");

            scaleXEvents = directScaleX;
            scaleYEvents = directScaleY;
        }

        var geometryTemplate = new Ir.BlockArea
        {
            TopRightX = StellateGeometryConstants.ToIrCoordinate(baseGeometry.CenterX + baseWidth / 2d),
            TopRightY = StellateGeometryConstants.ToIrCoordinate(baseGeometry.CenterY + baseHeight / 2d),
            BottomLeftX = StellateGeometryConstants.ToIrCoordinate(baseGeometry.CenterX - baseWidth / 2d),
            BottomLeftY = StellateGeometryConstants.ToIrCoordinate(baseGeometry.CenterY - baseHeight / 2d),
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

}
