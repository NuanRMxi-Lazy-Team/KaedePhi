using KaedePhi.Core.Common;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using KaedePhi.Tool.JudgeLines.KaedePhi;
using KaedePhi.Tool.Layer.KaedePhi;
using KpcEventLayer = KaedePhi.Core.KaedePhi.Events.EventLayer;
using KpcJudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;
using PhigrosEvent = KaedePhi.Core.Phigros.v3.Event;
using PhigrosJudgeLine = KaedePhi.Core.Phigros.v3.JudgeLine;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

/// <summary>
/// KPC 判定线到 PhigrosV3 判定线的构建器。
/// </summary>
public class PhigrosV3JudgeLineBuilder
{
    private const float PhigrosTimePerBeat = 32f;

    private readonly KpcToPhigrosV3ConvertOptions _options;
    private readonly PhigrosV3EventBuilder _phigrosV3EventBuilder;
    private readonly LayerProcessor _layerProcessor = new();
    private readonly float _globalBpm;
    private readonly Beat _chartEndBeat;
    private readonly float _chartEndTime;
    private readonly PhigrosV3TimeMapper? _timeMapper;
    private readonly Action<string>? _warnLogger;

    public PhigrosV3JudgeLineBuilder(
        KpcToPhigrosV3ConvertOptions options,
        float globalBpm,
        float chartEndTime,
        Action<string>? warnLogger
    )
        : this(
            options,
            globalBpm,
            new Beat(chartEndTime / PhigrosTimePerBeat),
            chartEndTime,
            null,
            warnLogger
        ) { }

    internal PhigrosV3JudgeLineBuilder(
        KpcToPhigrosV3ConvertOptions options,
        PhigrosV3TimeMapper timeMapper,
        Beat chartEndBeat,
        Action<string>? warnLogger
    )
        : this(options, PhigrosV3TimeMapper.TargetBpm, chartEndBeat, 0f, timeMapper, warnLogger) { }

    private PhigrosV3JudgeLineBuilder(
        KpcToPhigrosV3ConvertOptions options,
        float globalBpm,
        Beat chartEndBeat,
        float chartEndTime,
        PhigrosV3TimeMapper? timeMapper,
        Action<string>? warnLogger
    )
    {
        _options = options;
        _phigrosV3EventBuilder = new PhigrosV3EventBuilder(options, warnLogger, timeMapper);
        _globalBpm = globalBpm;
        _chartEndBeat = chartEndBeat;
        _chartEndTime = chartEndTime;
        _timeMapper = timeMapper;
        _warnLogger = warnLogger;
    }

    /// <summary>
    /// 将 KPC 判定线转换为 Phigros V3 判定线。
    /// </summary>
    /// <param name="src">待转换的 KPC 判定线</param>
    /// <param name="allLine">用于解除父子绑定的原始判定线列表</param>
    /// <returns>转换后的 Phigros V3 判定线；匹配过滤条件时返回空值</returns>
    public PhigrosJudgeLine? ConvertJudgeLine(KpcJudgeLine src, List<KpcJudgeLine> allLine)
    {
        WarnIfUnsupportedJudgeLineFields(src);
        WarnIfUnsupportedFloorPosition(src);

        if (
            (
                _options.LineFilter.RemoveTextureLine
                && !string.Equals(
                    src.Texture,
                    CoreConstants.DefaultTexture,
                    StringComparison.Ordinal
                )
            )
            || (_options.LineFilter.RemoveAttachUiLine && src.AttachUi.HasValue)
        )
            return null;

        var preprocessedSrc = src;
        if (preprocessedSrc.Father != -1)
        {
            if (_timeMapper is not null)
                ValidateFatherBpmFactors(src, allLine);
            Warn($"PhigrosV3 不支持 JudgeLine.Father（值={src.Father}），将自动解除父子绑定");
            var unbinder = new JudgeLineUnbinder();
            if (_options.FatherLineUnbind.ClassicMode)
            {
                preprocessedSrc = unbinder.FatherUnbind(
                    allLine.FindIndex(l => ReferenceEquals(l, src)),
                    allLine,
                    _options.FatherLineUnbind.Precision
                );
            }
            else
                preprocessedSrc = unbinder.FatherUnbindDynamic(
                    allLine.FindIndex(l => ReferenceEquals(l, src)),
                    allLine,
                    _options.FatherLineUnbind.Precision,
                    _options.FatherLineUnbind.Tolerance,
                    _options.FatherLineUnbind.MergeTolerance
                );

            if (_options.FatherLineUnbind.ClassicMode && _options.FatherLineUnbind.Compress)
            {
                foreach (var layer in preprocessedSrc.EventLayers)
                    _layerProcessor.LayerEventsCompress(layer, _options.FatherLineUnbind.Tolerance);
            }
        }

        var lineBpm = _timeMapper is null
            ? _globalBpm / preprocessedSrc.BpmFactor
            : PhigrosV3TimeMapper.TargetBpm;
        var chartEndTime = _timeMapper is null
            ? _chartEndTime
            : _timeMapper.ToEventTime(_chartEndBeat, preprocessedSrc.BpmFactor) + 1f;

        if (_options.NegativeAlpha.Enabled)
            ApplyNegativeAlphaElevation(preprocessedSrc);

        var primaryLayer = _phigrosV3EventBuilder.ResolvePrimaryLayer(preprocessedSrc.EventLayers);
        var speedEvents = primaryLayer.SpeedEvents;

        var (notesAbove, notesBelow) = PhigrosV3NoteBuilder.ConvertNotes(
            preprocessedSrc.Notes,
            speedEvents,
            _warnLogger,
            _options.NoteFilter.FilterFakeNotes,
            _timeMapper,
            preprocessedSrc.BpmFactor
        );

        var phigrosLine = new PhigrosJudgeLine
        {
            Bpm = lineBpm,
            NotesAbove = notesAbove,
            NotesBelow = notesBelow,
        };

        _phigrosV3EventBuilder.ConvertLineEvents(
            phigrosLine,
            primaryLayer,
            preprocessedSrc.BpmFactor
        );

        if (phigrosLine.JudgeLineDisappearEvents.Count == 0)
        {
            phigrosLine.JudgeLineDisappearEvents.Add(
                new PhigrosEvent
                {
                    StartTime = 0,
                    EndTime = chartEndTime,
                    Start = 0f,
                    End = 0f,
                }
            );
        }

        return phigrosLine;
    }

    private void WarnIfUnsupportedFloorPosition(KpcJudgeLine line)
    {
        if (line.Notes.Any(note => note.FloorPosition != 0f || note.EndFloorPosition != 0f))
            Warn("PhigrosV3 不支持 Note.FloorPosition 和 Note.EndFloorPosition，将丢弃该字段并输出 0。");

        if (
            line.EventLayers.Any(layer =>
                HasFloorPosition(layer.MoveXEvents)
                || HasFloorPosition(layer.MoveYEvents)
                || HasFloorPosition(layer.RotateEvents)
                || HasFloorPosition(layer.AlphaEvents)
                || HasFloorPosition(layer.SpeedEvents)
            )
        )
            Warn("PhigrosV3 不支持 Event.FloorPosition，将丢弃该字段并输出 0。");
    }

    private static void ValidateFatherBpmFactors(
        KpcJudgeLine src,
        List<KpcJudgeLine> allLines
    )
    {
        var lineIndex = allLines.FindIndex(line => ReferenceEquals(line, src));
        if (lineIndex < 0)
            throw new FormatException("待转换的判定线不属于当前谱面。");

        for (var fatherIndex = src.Father; fatherIndex >= 0; fatherIndex = allLines[fatherIndex].Father)
        {
            if (allLines[fatherIndex].BpmFactor != src.BpmFactor)
                throw new FormatException(
                    $"PhigrosV3 无法解除判定线 {lineIndex} 与父线 {fatherIndex} 的绑定：BPM 因子不一致。"
                );
        }
    }

    private static bool HasFloorPosition<T>(List<KpcEvents.Event<T>>? events)
        where T : notnull => events?.Any(evt => evt.FloorPosition != 0f) == true;

    #region 负不透明度段判定线抬高

    /// <summary>
    /// 对判定线中不透明度为负值的时间段，将判定线 Y 坐标抬高至屏幕外。
    /// 若判定线有旋转角度，则进行类父线解绑操作，保证抬高方向为屏幕上方（而非直接对 Y 轴做加法）。
    /// </summary>
    private void ApplyNegativeAlphaElevation(KpcJudgeLine line)
    {
        if (line.EventLayers is not { Count: > 0 })
            return;

        var layer = line.EventLayers[0];
        if (layer.AlphaEvents is not { Count: > 0 })
            return;

        if (layer.MoveXEvents is { Count: > 1 })
            layer.MoveXEvents = layer.MoveXEvents.OrderBy(e => e.StartBeat).ToList();
        if (layer.MoveYEvents is { Count: > 1 })
            layer.MoveYEvents = layer.MoveYEvents.OrderBy(e => e.StartBeat).ToList();

        var negativeSegments = CollectNegativeAlphaSegments(layer.AlphaEvents);
        if (negativeSegments.Count == 0)
            return;

        var angle = GetRepresentativeAngle(layer.RotateEvents);
        var elevationKpcY = ComputeElevationKpcY(angle);
        var renderProfile = _options.NegativeAlpha.RenderProfile;
        var fillLength = new Beat(1d / _options.MultiLayerMerge.Precision);

        Warn(
            $"判定线存在 {negativeSegments.Count} 个负不透明度段，将抬高判定线至屏幕外（角度={angle:F1}°, ΔKPC_Y={elevationKpcY:F4}）"
        );

        foreach (var (segStart, segEnd) in negativeSegments)
        {
            var currentY = GetCurrentValueAtBeat(layer.MoveYEvents, segStart);
            var screenPos = GetScreenPosition(layer, segStart);

            // 持续抬高直到判定线超出屏幕
            var elevatedY = currentY;
            var attempts = 0;
            while (IsOnScreen(screenPos.X, elevatedY, angle, renderProfile) && attempts < 100)
            {
                elevatedY += elevationKpcY;
                attempts++;
            }

            var deltaY = elevatedY - currentY;
            if (Math.Abs(deltaY) < 1e-9)
                continue;

            ApplyYOffsetToLayer(layer, segStart, segEnd, deltaY, fillLength);
        }
    }

    /// <summary>
    /// 收集所有不透明度为负值的精确时间段。
    /// 遵循事件列表的空隙保持规则：若负 Alpha 事件结束后无后续事件将其回正，
    /// 空隙中 Alpha 仍保持负值，直到遇到回正事件或谱面结束。
    /// 跨零点事件会在零点处拆分，仅返回负值区间。
    /// </summary>
    private List<(Beat Start, Beat End)> CollectNegativeAlphaSegments(
        List<KpcEvents.Event<int>> alphaEvents
    )
    {
        if (alphaEvents.Count == 0)
            return [];

        var sorted = alphaEvents.OrderBy(e => (double)e.StartBeat).ToList();
        var segments = new List<(Beat, Beat)>();

        var lastEndValue = 0;
        Beat lastEndBeat = new(0);
        Beat? segStart = null;

        foreach (var ev in sorted)
        {
            // 处理事件之前的空隙：用上一个事件的 EndValue 填充
            if (ev.StartBeat > lastEndBeat)
            {
                if (lastEndValue < 0)
                {
                    // 空隙中 Alpha 保持负值
                    segStart ??= lastEndBeat;
                }
                else if (segStart.HasValue)
                {
                    // 空隙中 Alpha 非负 → 结束之前的负段
                    segments.Add((segStart.Value, lastEndBeat));
                    segStart = null;
                }
            }

            // 处理当前事件本身
            var evNegStart = ev.StartValue < 0;
            var evNegEnd = ev.EndValue < 0;

            switch (evNegStart)
            {
                case true when evNegEnd:
                    // 全负段
                    segStart ??= ev.StartBeat;
                    break;
                case false when !evNegEnd:
                {
                    // 全非负
                    if (segStart.HasValue)
                    {
                        segments.Add((segStart.Value, ev.StartBeat));
                        segStart = null;
                    }

                    break;
                }
                case true when !evNegEnd:
                {
                    // 负→正：在零点处拆分
                    var zeroBeat = FindZeroCrossingBeat(ev);
                    segStart ??= ev.StartBeat;
                    segments.Add((segStart.Value, zeroBeat));
                    segStart = null;
                    break;
                }
                default:
                {
                    // 正→负：在零点处拆分
                    var zeroBeat = FindZeroCrossingBeat(ev);
                    if (segStart.HasValue)
                    {
                        segments.Add((segStart.Value, zeroBeat));
                    }

                    segStart = zeroBeat;
                    break;
                }
            }

            lastEndValue = ev.EndValue;
            lastEndBeat = ev.EndBeat;
        }

        // 处理最后一个事件之后的空隙：若 EndValue 为负，保持到谱面结束
        if (segStart.HasValue)
        {
            if (lastEndValue < 0)
                segments.Add((segStart.Value, _chartEndBeat));
            else
                segments.Add((segStart.Value, lastEndBeat));
        }

        return segments;
    }

    /// <summary>
    /// 根据事件实际插值计算 alpha 事件穿越零点的拍位置。
    /// </summary>
    private static Beat FindZeroCrossingBeat(KpcEvents.Event<int> ev)
    {
        var startBeat = (double)ev.StartBeat;
        var endBeat = (double)ev.EndBeat;
        if (endBeat - startBeat < 1e-9)
            return ev.StartBeat;

        var startValue = ev.GetValueAtBeat(ev.StartBeat);
        var endValue = ev.GetValueAtBeat(ev.EndBeat);
        if (startValue == 0)
            return ev.StartBeat;
        if (endValue == 0)
            return ev.EndBeat;

        var startsNegative = startValue < 0;
        var low = startBeat;
        var high = endBeat;
        for (var i = 0; i < 64; i++)
        {
            var middle = (low + high) / 2d;
            var middleBeat = new Beat(middle);
            var middleValue = ev.GetValueAtBeat(middleBeat);
            if (middleValue == 0)
                return middleBeat;

            if ((middleValue < 0) == startsNegative)
                low = middle;
            else
                high = middle;
        }

        return new Beat((low + high) / 2d);
    }

    /// <summary>
    /// 获取旋转事件中绝对值最大的角度（最坏情况估计）。
    /// </summary>
    private static double GetRepresentativeAngle(List<KpcEvents.Event<double>>? rotateEvents)
    {
        if (rotateEvents is not { Count: > 0 })
            return 0;

        var maxAbs = 0d;
        foreach (var ev in rotateEvents)
        {
            var absStart = Math.Abs(ev.StartValue);
            var absEnd = Math.Abs(ev.EndValue);
            if (absStart > maxAbs)
                maxAbs = absStart;
            if (absEnd > maxAbs)
                maxAbs = absEnd;
        }

        return maxAbs;
    }

    /// <summary>
    /// 计算抬高操作所需的 KPC Y 偏移量。
    /// 无旋转时直接使用配置的抬高步长；有旋转时根据角度缩放，保证屏幕 Y 方向位移一致。
    /// </summary>
    private double ComputeElevationKpcY(double angleDegrees)
    {
        var step = _options.NegativeAlpha.ElevationStep;
        if (Math.Abs(angleDegrees) < Constants.FloatEpsilon)
            return step;

        // 有旋转时，抬高方向在判定线局部坐标系的 +Y 方向；
        // 屏幕 Y 位移 = ΔKPC_Y × cos(θ)，因此 ΔKPC_Y = step / cos(θ)
        var rad = angleDegrees * (Math.PI / 180d);
        var cos = Math.Abs(Math.Cos(rad));
        if (cos < 1e-6)
            return step * 10; // 接近 90° 时用大步长快速推离屏幕

        return step / cos;
    }

    /// <summary>
    /// 获取指定拍点上判定线的屏幕坐标。
    /// </summary>
    private static (double X, double Y) GetScreenPosition(KpcEventLayer layer, Beat beat)
    {
        var x = layer.MoveXEvents is { Count: > 0 }
            ? GetCurrentValueAtBeat(layer.MoveXEvents, beat)
            : 0;
        var y = layer.MoveYEvents is { Count: > 0 }
            ? GetCurrentValueAtBeat(layer.MoveYEvents, beat)
            : 0;
        return (x, y);
    }

    /// <summary>
    /// 判断 KPC 坐标点在考虑旋转后是否仍在屏幕内。
    /// </summary>
    private static bool IsOnScreen(
        double kpcX,
        double kpcY,
        double angleDegrees,
        CoordinateProfile renderProfile
    )
    {
        var (absoluteKpcX, absoluteKpcY) = CoordinateGeometry.GetKpcAbsolutePos(
            0,
            0,
            angleDegrees,
            kpcX,
            kpcY,
            renderProfile
        );
        var renderX = CoordinateGeometry.ToTargetX(absoluteKpcX, renderProfile);
        var renderY = CoordinateGeometry.ToTargetY(absoluteKpcY, renderProfile);
        return renderX >= renderProfile.MinX
            && renderX <= renderProfile.MaxX
            && renderY >= renderProfile.MinY
            && renderY <= renderProfile.MaxY;
    }

    /// <summary>
    /// 在指定拍点获取事件列表的当前值（二分查找）。
    /// </summary>
    private static double GetCurrentValueAtBeat(List<KpcEvents.Event<double>>? events, Beat beat)
    {
        if (events is not { Count: > 0 })
            return 0;
        int lo = 0,
            hi = events.Count - 1,
            idx = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (events[mid].StartBeat <= beat)
            {
                idx = mid;
                lo = mid + 1;
            }
            else
                hi = mid - 1;
        }

        if (idx < 0)
            return 0;
        var ev = events[idx];
        return beat <= ev.EndBeat ? ev.GetValueAtBeat(beat) : ev.EndValue;
    }

    /// <summary>
    /// 将 Y 偏移量应用到事件层的 MoveYEvents 中指定时间范围内。
    /// 在段边界处精确拆分事件，仅对段内事件施加偏移，段外事件完全不变。
    /// 若段内无 MoveY 事件覆盖，则填充单个常量偏移事件。
    /// </summary>
    private static void ApplyYOffsetToLayer(
        KpcEventLayer layer,
        Beat segStart,
        Beat segEnd,
        double deltaY,
        Beat fillLength
    )
    {
        if (segStart >= segEnd)
            return;

        if (layer.MoveYEvents is not { Count: > 0 })
        {
            var events = new List<KpcEvents.Event<double>>
            {
                new()
                {
                    StartBeat = segStart,
                    EndBeat = segEnd,
                    StartValue = deltaY,
                    EndValue = deltaY,
                },
            };
            // 回正事件
            var resetEnd = segEnd + fillLength;
            events.Add(
                new KpcEvents.Event<double>
                {
                    StartBeat = segEnd,
                    EndBeat = resetEnd,
                    StartValue = 0,
                    EndValue = 0,
                }
            );
            layer.MoveYEvents = events;
            return;
        }

        var sourceEvents = layer.MoveYEvents.OrderBy(e => e.StartBeat).ToList();
        var result = new List<KpcEvents.Event<double>>();
        foreach (var ev in sourceEvents)
        {
            // 事件与段无重叠 → 原样保留
            if (ev.EndBeat <= segStart || ev.StartBeat >= segEnd)
            {
                result.Add(ev);
                continue;
            }

            // 段前部分（ev.StartBeat ~ segStart）
            if (ev.StartBeat < segStart)
            {
                result.Add(
                    new KpcEvents.Event<double>
                    {
                        StartBeat = ev.StartBeat,
                        EndBeat = segStart,
                        StartValue = ev.StartValue,
                        EndValue = ev.GetValueAtBeat(segStart),
                        Easing = ev.Easing,
                        EasingLeft = ev.EasingLeft,
                        EasingRight = ev.EasingRight,
                        IsBezier = ev.IsBezier,
                        BezierPoints = ev.BezierPoints,
                    }
                );
            }

            // 段内部分（max(ev.Start, segStart) ~ min(ev.End, segEnd)）→ 施加偏移
            var innerStart = ev.StartBeat > segStart ? ev.StartBeat : segStart;
            var innerEnd = ev.EndBeat < segEnd ? ev.EndBeat : segEnd;
            if (innerStart < innerEnd)
            {
                result.Add(
                    new KpcEvents.Event<double>
                    {
                        StartBeat = innerStart,
                        EndBeat = innerEnd,
                        StartValue = ev.GetValueAtBeat(innerStart) + deltaY,
                        EndValue = ev.GetValueAtBeat(innerEnd) + deltaY,
                        Easing = ev.Easing,
                        EasingLeft = ev.EasingLeft,
                        EasingRight = ev.EasingRight,
                        IsBezier = ev.IsBezier,
                        BezierPoints = ev.BezierPoints,
                    }
                );
            }

            // 段后部分（segEnd ~ ev.EndBeat）
            if (ev.EndBeat > segEnd)
            {
                result.Add(
                    new KpcEvents.Event<double>
                    {
                        StartBeat = segEnd,
                        EndBeat = ev.EndBeat,
                        StartValue = ev.GetValueAtBeat(segEnd),
                        EndValue = ev.EndValue,
                        Easing = ev.Easing,
                        EasingLeft = ev.EasingLeft,
                        EasingRight = ev.EasingRight,
                        IsBezier = ev.IsBezier,
                        BezierPoints = ev.BezierPoints,
                    }
                );
            }
        }

        // 段内无事件覆盖的空隙填充保持原始 Y 值的偏移事件
        FillGapsInRange(result, sourceEvents, segStart, segEnd, deltaY);

        // 段结束后添加回正事件：使 Y 值恢复到未偏移时的原始值
        // 仅当 segEnd 处没有已有事件覆盖时才添加，避免重叠
        var restoreEnd = segEnd + fillLength;
        var hasEventAtSegEnd = result.Any(e => e.StartBeat <= segEnd && e.EndBeat > segEnd);
        if (!hasEventAtSegEnd)
        {
            var originalYAtEnd = GetCurrentValueAtBeat(sourceEvents, segEnd);
            result.Add(
                new KpcEvents.Event<double>
                {
                    StartBeat = segEnd,
                    EndBeat = restoreEnd,
                    StartValue = originalYAtEnd,
                    EndValue = originalYAtEnd,
                }
            );
        }

        result = result.OrderBy(e => e.StartBeat).ToList();
        layer.MoveYEvents = result;
    }

    /// <summary>
    /// 在指定范围内检测空隙，并填充叠加原始 Y 值后的常量偏移事件。
    /// </summary>
    private static void FillGapsInRange(
        List<KpcEvents.Event<double>> events,
        List<KpcEvents.Event<double>> sourceEvents,
        Beat segStart,
        Beat segEnd,
        double deltaY
    )
    {
        // 收集段内已有事件覆盖的区间
        var covered = events
            .Where(e => e.StartBeat < segEnd && e.EndBeat > segStart)
            .Select(e =>
                (
                    Start: e.StartBeat > segStart ? e.StartBeat : segStart,
                    End: e.EndBeat < segEnd ? e.EndBeat : segEnd
                )
            )
            .OrderBy(r => r.Start)
            .ToList();

        // 空隙中也要叠加原始 Y 值，避免无事件时从零点突然跳变。
        var cursor = segStart;
        foreach (var (s, e) in covered)
        {
            if (e <= cursor)
                continue;

            if (s > cursor)
            {
                var baseValue = GetCurrentValueAtBeat(sourceEvents, cursor) + deltaY;
                events.Add(
                    new KpcEvents.Event<double>
                    {
                        StartBeat = cursor,
                        EndBeat = s,
                        StartValue = baseValue,
                        EndValue = baseValue,
                    }
                );
            }

            if (e > cursor)
                cursor = e > segEnd ? segEnd : e;
            if (cursor >= segEnd)
                break;
        }

        if (cursor < segEnd)
        {
            var baseValue = GetCurrentValueAtBeat(sourceEvents, cursor) + deltaY;
            events.Add(
                new KpcEvents.Event<double>
                {
                    StartBeat = cursor,
                    EndBeat = segEnd,
                    StartValue = baseValue,
                    EndValue = baseValue,
                }
            );
        }

        var sorted = events.OrderBy(e => e.StartBeat).ToList();
        events.Clear();
        events.AddRange(sorted);
    }

    #endregion

    private void WarnIfUnsupportedJudgeLineFields(KpcJudgeLine src)
    {
        var textureRemoveHint = _options.LineFilter.RemoveTextureLine ? "判定线将被自动移除。" : "";
        var attachUiRemoveHint = _options.LineFilter.RemoveAttachUiLine
            ? "，判定线将被自动移除。"
            : "";

        if (!string.Equals(src.Name, "Untitled", StringComparison.Ordinal))
            Warn($"PhigrosV3 不支持 JudgeLine.Name（值='{src.Name}'）");
        if (!string.Equals(src.Texture, CoreConstants.DefaultTexture, StringComparison.Ordinal))
            Warn($"PhigrosV3 不支持 JudgeLine.Texture（值='{src.Texture}'），{textureRemoveHint}");
        if (!IsDefaultAnchor(src.Anchor))
            Warn($"PhigrosV3 不支持 JudgeLine.Anchor（值='[{string.Join(", ", src.Anchor)}]'）");
        if (src.Father != -1)
            Warn($"PhigrosV3 不支持 JudgeLine.Father（值={src.Father}），将自动解除父子绑定");
        if (!src.IsCover)
            Warn($"PhigrosV3 不支持 JudgeLine.IsCover（值={src.IsCover}）");
        if (src.ZOrder != 0)
            Warn($"PhigrosV3 不支持 JudgeLine.ZOrder（值={src.ZOrder}）");
        if (src.AttachUi.HasValue)
            Warn(
                $"PhigrosV3 不支持 JudgeLine.AttachUi（值={(int)src.AttachUi.Value}）{attachUiRemoveHint}"
            );
        if (src.IsGif)
            Warn($"PhigrosV3 不支持 JudgeLine.IsGif（值={src.IsGif}）");
        if (src.RotateWithFather)
            Warn($"PhigrosV3 不支持 JudgeLine.RotateWithFather（值={src.RotateWithFather}）");

        WarnIfUnsupportedControlFields(src);
    }

    /// <summary>
    /// 检查判定线中 PhigrosV3 不支持的控件字段，并逐项告警。
    /// </summary>
    private void WarnIfUnsupportedControlFields(KpcJudgeLine src)
    {
        if (HasNonDefaultExtendLayer(src.Extended))
            Warn("PhigrosV3 不支持 JudgeLine.Extended（包含非默认数据）");
        if (!ControlDefaultChecker.IsDefaultControls(src.PositionControls))
            Warn("PhigrosV3 不支持 JudgeLine.PositionControls（包含非默认数据）");
        if (!ControlDefaultChecker.IsDefaultControls(src.AlphaControls))
            Warn("PhigrosV3 不支持 JudgeLine.AlphaControls（包含非默认数据）");
        if (!ControlDefaultChecker.IsDefaultControls(src.SizeControls))
            Warn("PhigrosV3 不支持 JudgeLine.SizeControls（包含非默认数据）");
        if (!ControlDefaultChecker.IsDefaultControls(src.SkewControls))
            Warn("PhigrosV3 不支持 JudgeLine.SkewControls（包含非默认数据）");
        if (!ControlDefaultChecker.IsDefaultControls(src.YControls))
            Warn("PhigrosV3 不支持 JudgeLine.YControls（包含非默认数据）");
    }

    private static bool HasNonDefaultExtendLayer(KpcEvents.ExtendLayer? layer) =>
        layer != null
        && (
            (layer.ColorEvents?.Count ?? 0) > 0
            || (layer.ScaleXEvents?.Count ?? 0) > 0
            || (layer.ScaleYEvents?.Count ?? 0) > 0
            || (layer.TextEvents?.Count ?? 0) > 0
            || (layer.PaintEvents?.Count ?? 0) > 0
            || (layer.GifEvents?.Count ?? 0) > 0
        );

    private static bool IsDefaultAnchor(float[]? anchor) =>
        anchor is { Length: 2 }
        && Math.Abs(anchor[0] - 0.5f) <= Constants.FloatEpsilon
        && Math.Abs(anchor[1] - 0.5f) <= Constants.FloatEpsilon;

    private void Warn(string message) => _warnLogger?.Invoke(message);
}
