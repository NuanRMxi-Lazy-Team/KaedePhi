using System.Runtime.ExceptionServices;
using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using KaedePhi.Tool.Converter.StellateRePhiEditExtended.Model;
using RpeChart = KaedePhi.Core.Formats.RePhiEdit.Model.Chart;
using RpeEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<float>;
using RpeEventLayer = KaedePhi.Core.Formats.RePhiEdit.Model.Events.EventLayer;
using RpeJudgeLine = KaedePhi.Core.Formats.RePhiEdit.Model.JudgeLine;
using IrBpmItem = KaedePhi.Core.Intermediate.Model.BpmItem;
using IrEvent = KaedePhi.Core.Intermediate.Model.Events.Event<double>;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;

/// <summary>
/// 对判定线变换曲线进行采样并计算噪域几何。
/// </summary>
public sealed class StellateGeometrySampler
{
    private readonly RpeChart _source;
    private readonly PhigrosV3TimeMapper _timeMapper;
    private readonly CancellationToken _ct;
    private readonly Dictionary<List<RpeEvent>, StellateCurveIndex> _curveIndexes = new();

    /// <summary>
    /// 根据源谱面和 BPM 列表创建几何采样器。
    /// </summary>
    /// <param name="source">包含判定线和变换曲线的 RePhiEdit 谱面</param>
    /// <param name="bpmList">用于将拍数转换为音乐时间的 BPM 列表</param>
    /// <param name="cancellationToken">用于取消几何采样的令牌</param>
    public StellateGeometrySampler(
        RpeChart source,
        IReadOnlyList<IrBpmItem> bpmList,
        CancellationToken cancellationToken
    ) : this(source, new PhigrosV3TimeMapper(bpmList), cancellationToken) { }

    internal StellateGeometrySampler(
        RpeChart source,
        PhigrosV3TimeMapper timeMapper,
        CancellationToken cancellationToken
    )
    {
        _source = source;
        _timeMapper = timeMapper;
        _ct = cancellationToken;
    }

    /// <summary>
    /// 获取指定判定线在目标拍数处的几何状态。
    /// </summary>
    /// <param name="lineIndex">判定线索引</param>
    /// <param name="beat">目标拍数</param>
    /// <param name="rightSide">取拍数边界右侧状态时为 <see langword="true"/></param>
    /// <returns>目标拍数处的几何状态</returns>
    /// <remarks>调用前需先对该判定线调用 <see cref="PrepareLine"/>。</remarks>
    public StellateBlockAreaGeometry GetGeometryAtBeat(int lineIndex, Beat beat, bool rightSide) =>
        GetGeometry(
            lineIndex,
            _timeMapper.ToMusicTime(beat),
            new HashSet<int>(),
            new StellateCurveValueCache(_curveIndexes.Count),
            rightSide
        );

    /// <summary>
    /// 获取曲线在目标拍数处的值。
    /// </summary>
    /// <param name="events">待计算的曲线事件</param>
    /// <param name="beat">目标拍数</param>
    /// <param name="rightSide">取拍数边界右侧状态时为 <see langword="true"/></param>
    /// <param name="defaultValue">曲线没有事件时使用的默认值</param>
    /// <returns>目标拍数处的曲线值</returns>
    /// <remarks>调用前需先对包含该曲线的判定线调用 <see cref="PrepareLine"/>。</remarks>
    public double EvaluateCurveAtBeat(
        List<RpeEvent>? events,
        Beat beat,
        bool rightSide,
        double defaultValue
    ) => CurveValue(events, _timeMapper.ToMusicTime(beat), rightSide, defaultValue);

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
            _curveIndexes.Add(
                events,
                new StellateCurveIndex(events, _timeMapper, _curveIndexes.Count)
            );
    }

    /// <summary>
    /// 为指定判定线及其父级准备曲线索引。
    /// </summary>
    /// <param name="lineIndex">判定线索引</param>
    public void PrepareLine(int lineIndex) =>
        PrepareCurveIndexes(lineIndex, [], includeScale: true);

    /// <summary>
    /// 在指定拍数集合上采样判定线几何。
    /// </summary>
    /// <param name="lineIndex">判定线索引</param>
    /// <param name="sampleBeats">需要采样的拍数集合</param>
    /// <param name="hardBeats">必须保留为硬边界的拍数集合</param>
    /// <param name="maxDegreeOfParallelism">采样使用的最大并行度</param>
    /// <returns>包含拍数、左右侧几何和硬边界标记的采样结果</returns>
    /// <remarks>调用前需先对该判定线调用 <see cref="PrepareLine"/>。</remarks>
    public (
        Beat Beat,
        StellateBlockAreaGeometry Left,
        StellateBlockAreaGeometry Right,
        bool Hard
        )[] SampleGeometry(
            int lineIndex,
            SortedSet<Beat> sampleBeats,
            SortedSet<Beat> hardBeats,
            int maxDegreeOfParallelism
        )
    {
        var beats = sampleBeats.ToArray();
        var samples = new (
            Beat Beat,
            StellateBlockAreaGeometry Left,
            StellateBlockAreaGeometry Right,
            bool Hard
            )[beats.Length];

        void Sample(
            int index,
            HashSet<int> visiting,
            StellateCurveValueCache curveValues
        )
        {
            _ct.ThrowIfCancellationRequested();
            curveValues.BeginSample();
            var beat = beats[index];
            var musicTime = _timeMapper.ToMusicTime(beat);
            samples[index] = (
                beat,
                GetGeometry(lineIndex, musicTime, visiting, curveValues, rightSide: false),
                GetGeometry(lineIndex, musicTime, visiting, curveValues, rightSide: true),
                hardBeats.Contains(beat)
            );
        }

        if (
            beats.Length < StellateGeometryConstants.ParallelSampleThreshold
            || maxDegreeOfParallelism <= 1
            || Environment.ProcessorCount <= 1
        )
        {
            var visiting = new HashSet<int>();
            var curveValues = new StellateCurveValueCache(_curveIndexes.Count);
            for (var index = 0; index < beats.Length; index++)
                Sample(index, visiting, curveValues);

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
                    MaxDegreeOfParallelism = maxDegreeOfParallelism,
                },
                () => (
                    Visiting: new HashSet<int>(),
                    CurveValues: new StellateCurveValueCache(_curveIndexes.Count)
                ),
                (index, _, state) =>
                {
                    Sample(index, state.Visiting, state.CurveValues);
                    return state;
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

    private StellateBlockAreaGeometry GetGeometry(
        int lineIndex,
        double musicTime,
        HashSet<int> visiting,
        StellateCurveValueCache curveValues,
        bool rightSide
    )
    {
        var pose = GetPose(lineIndex, musicTime, rightSide, visiting, curveValues);
        var line = _source.JudgeLineList[lineIndex];
        var scaleX = StellateGeometryConstants.FiniteOr(
            CurveValue(line.Extended?.ScaleXEvents, musicTime, rightSide, 1d, curveValues),
            1d
        );
        var scaleY = StellateGeometryConstants.FiniteOr(
            CurveValue(line.Extended?.ScaleYEvents, musicTime, rightSide, 1d, curveValues),
            1d
        );

        var imageWidth = StellateGeometryConstants.TextureWidth * scaleX * StellateGeometryConstants.AspectWidth / 1350d;
        var imageHeight = StellateGeometryConstants.TextureHeight * scaleY * StellateGeometryConstants.AspectWidth / 1350d;
        var anchorX = line.Anchor is { Length: >= 2 } ? line.Anchor[0] : 0.5d;
        var anchorY = line.Anchor is { Length: >= 2 } ? line.Anchor[1] : 0.5d;
        var lineScreenX = StellateGeometryConstants.AspectWidth * 0.5d + pose.X * StellateGeometryConstants.AspectWidth / 1350d;
        var lineScreenY = StellateGeometryConstants.AspectHeight * 0.5d - pose.Y * StellateGeometryConstants.AspectHeight / 900d;
        var deltaX = (0.5d - anchorX) * imageWidth;
        var deltaY = (anchorY - 0.5d) * imageHeight;
        var radians = pose.Rotation * Math.PI / 180d;
        var shiftX = deltaX * Math.Cos(radians) - deltaY * Math.Sin(radians);
        var shiftY = deltaX * Math.Sin(radians) + deltaY * Math.Cos(radians);

        return new StellateBlockAreaGeometry(
            (lineScreenX + shiftX) / StellateGeometryConstants.AspectWidth,
            1d - (lineScreenY + shiftY) / StellateGeometryConstants.AspectHeight,
            Math.Abs(imageWidth / StellateGeometryConstants.AspectWidth),
            Math.Abs(imageHeight / StellateGeometryConstants.AspectHeight),
            -pose.Rotation
        );
    }

    private (double X, double Y, double Rotation) GetPose(
        int lineIndex,
        double musicTime,
        bool rightSide,
        HashSet<int> visiting,
        StellateCurveValueCache curveValues
    )
    {
        if (!visiting.Add(lineIndex))
            return (0d, 0d, 0d);

        try
        {
            var line = _source.JudgeLineList[lineIndex];
            var x = StellateGeometryConstants.FiniteOr(
                SumLayerCurve(line, musicTime, rightSide, static layer => layer.MoveXEvents, curveValues),
                0d
            );
            var y = StellateGeometryConstants.FiniteOr(
                SumLayerCurve(line, musicTime, rightSide, static layer => layer.MoveYEvents, curveValues),
                0d
            );
            var rotation = StellateGeometryConstants.FiniteOr(
                SumLayerCurve(line, musicTime, rightSide, static layer => layer.RotateEvents, curveValues),
                0d
            );

            if (line.Father < 0 || line.Father >= _source.JudgeLineList.Count)
                return (x, y, rotation);
            var father = GetPose(line.Father, musicTime, rightSide, visiting, curveValues);
            x += father.X;
            y += father.Y;
            var parentPhysicalX = father.X * StellateGeometryConstants.AspectWidth / 1350d;
            var parentPhysicalY = father.Y * StellateGeometryConstants.AspectHeight / 900d;
            var childPhysicalX = x * StellateGeometryConstants.AspectWidth / 1350d;
            var childPhysicalY = y * StellateGeometryConstants.AspectHeight / 900d;
            (childPhysicalX, childPhysicalY) = RotatePoint(
                childPhysicalX,
                childPhysicalY,
                parentPhysicalX,
                parentPhysicalY,
                -father.Rotation * Math.PI / 180d
            );
            x = childPhysicalX * 1350d / StellateGeometryConstants.AspectWidth;
            y = childPhysicalY * 900d / StellateGeometryConstants.AspectHeight;
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
        Func<RpeEventLayer, List<RpeEvent>?> selector,
        StellateCurveValueCache? curveValues = null
    )
    {
        var total = 0d;
        foreach (var eventLayer in line.EventLayers)
        {
            if (eventLayer is RpeEventLayer layer)
                total += CurveValue(selector(layer), musicTime, rightSide, 0d, curveValues);
        }

        return total;
    }

    private double CurveValue(
        List<RpeEvent>? events,
        double musicTime,
        bool rightSide,
        double defaultValue,
        StellateCurveValueCache? curveValues = null
    )
    {
        if (events is not { Count: > 0 })
            return defaultValue;

        if (!_curveIndexes.TryGetValue(events, out var curveIndex))
            throw new InvalidOperationException("扩展曲线索引未初始化。");

        if (curveValues is not null && curveValues.TryGetValue(curveIndex, out var cachedValue))
            return cachedValue;

        var (dominantIndex, previousIndex) = curveIndex.FindDominant(
            musicTime,
            StellateGeometryConstants.CurveStartTimeEpsilon
        );
        if (dominantIndex < 0)
        {
            var value = EventValueAtMusicTime(
                curveIndex,
                curveIndex.FirstIndex,
                musicTime,
                allowExtrapolation: true
            );
            curveValues?.SetValue(curveIndex, value);
            return value;
        }

        var dominant = curveIndex.GetEvent(dominantIndex);
        var dominantStartTime = curveIndex.GetStartTime(dominantIndex);
        if (!(Math.Abs(musicTime - dominantStartTime) <= StellateGeometryConstants.CurveStartTimeEpsilon))
        {
            var value = EventValueAtMusicTime(
                curveIndex,
                dominantIndex,
                musicTime,
                allowExtrapolation: false
            );
            curveValues?.SetValue(curveIndex, value);
            return value;
        }
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

    /// <summary>
    /// 收集判定线及其父级的位置和旋转事件边界。
    /// </summary>
    /// <param name="lineIndex">判定线索引</param>
    /// <param name="beats">接收事件边界的拍数集合</param>
    /// <param name="visited">记录已处理判定线的集合</param>
    public void CollectPoseBreaks(int lineIndex, SortedSet<Beat> beats, HashSet<int> visited)
    {
        while (true)
        {
            if (!visited.Add(lineIndex)) return;
            var line = _source.JudgeLineList[lineIndex];
            foreach (var layer in line.EventLayers.OfType<RpeEventLayer>())
            {
                StellateBlockAreaPathBuilder.AddCurveBoundaries(layer.MoveXEvents, beats);
                StellateBlockAreaPathBuilder.AddCurveBoundaries(layer.MoveYEvents, beats);
                StellateBlockAreaPathBuilder.AddCurveBoundaries(layer.RotateEvents, beats);
            }

            if (line.Father >= 0 && line.Father < _source.JudgeLineList.Count)
            {
                lineIndex = line.Father;
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// 收集影响判定线旋转的事件边界。
    /// </summary>
    /// <param name="lineIndex">判定线索引</param>
    /// <param name="beats">接收事件边界的拍数集合</param>
    /// <param name="visited">记录已处理判定线的集合</param>
    public void CollectRotationBreaks(int lineIndex, SortedSet<Beat> beats, HashSet<int> visited)
    {
        while (true)
        {
            if (!visited.Add(lineIndex)) return;

            var line = _source.JudgeLineList[lineIndex];
            foreach (var layer in line.EventLayers.OfType<RpeEventLayer>())
            {
                StellateBlockAreaPathBuilder.AddCurveBoundaries(layer.RotateEvents, beats);
            }

            if (line.RotateWithFather && line.Father >= 0 && line.Father < _source.JudgeLineList.Count)
            {
                lineIndex = line.Father;
                continue;
            }

            break;
        }
    }

}
