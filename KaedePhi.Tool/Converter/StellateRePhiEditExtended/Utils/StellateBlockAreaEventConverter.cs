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

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;

/// <summary>
/// 提供 IR 噪域事件与 RePhiEdit 事件之间的转换和验证功能。
/// </summary>
public static class StellateBlockAreaEventConverter
{
    /// <summary>
    /// 转换噪域事件，并按需补齐零拍基线事件。
    /// </summary>
    /// <param name="source">源 IR 事件</param>
    /// <param name="defaultValue">没有源事件时使用的默认值</param>
    /// <param name="valueTransformer">将 IR 事件值转换为目标值的函数</param>
    /// <param name="options">事件切分选项</param>
    /// <param name="blockAreaIndex">所属噪域的索引</param>
    /// <param name="trackName">事件轨道名称</param>
    /// <param name="ensureBaseline">是否保证从零拍开始具有基线事件</param>
    /// <param name="cancellationToken">用于取消转换的令牌</param>
    public static List<RpeEvent>? ConvertEvents(
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
                CreateEvent(StellateBlockAreaConstants.ZeroBeat, StellateBlockAreaConstants.ZeroBeat, defaultValue, defaultValue),
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

        if (ensureBaseline && events[0].StartBeat > StellateBlockAreaConstants.ZeroBeat)
        {
            var seed = CreateEvent(
                StellateBlockAreaConstants.ZeroBeat,
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

    /// <summary>
    /// 根据噪域显示阶段生成透明度事件。
    /// </summary>
    /// <param name="source">包含显示阶段的 IR 噪域</param>
    public static List<RpeAlphaEvent> CreateAlphaEvents(IrBlockArea source)
    {
        var beats = new SortedSet<Beat> { StellateBlockAreaConstants.ZeroBeat };
        AddBoundary(source.AppearBeat);
        AddBoundary(source.EnableBeat);
        AddBoundary(source.DisableBeat);
        AddBoundary(source.DisappearBeat);

        var result = new List<RpeAlphaEvent>(beats.Count);
        var previousAlpha = -1;
        foreach (var beat in beats)
        {
            var alpha = GetAlphaAtBeat(source, beat);
            if (beat == StellateBlockAreaConstants.ZeroBeat || alpha != previousAlpha)
                result.Add(CreateAlphaEvent(beat, alpha));
            previousAlpha = alpha;
        }

        return result;

        void AddBoundary(Beat beat)
        {
            if (beat > StellateBlockAreaConstants.ZeroBeat)
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

    /// <summary>
    /// 验证 IR 事件的时间和值字段。
    /// </summary>
    /// <param name="evt">待验证的 IR 事件</param>
    /// <param name="blockAreaIndex">所属噪域的索引</param>
    /// <param name="trackName">事件轨道名称</param>
    public static void ValidateEventFields(
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

    /// <summary>
    /// 将有限数值转换为 RePhiEdit 可编码的单精度数值。
    /// </summary>
    /// <param name="value">待转换的数值</param>
    /// <param name="blockAreaIndex">所属噪域的索引</param>
    /// <param name="valueName">数值名称</param>
    public static float ToRpeFloat(double value, int blockAreaIndex, string valueName)
    {
        return double.IsFinite(value) && value is >= -float.MaxValue and <= float.MaxValue
            ? (float)value
            : throw new FormatException(
                $"IR 噪域 {blockAreaIndex} 的{valueName}超出 RePhiEdit 可编码范围。"
            );
    }

    /// <summary>
    /// 创建表示指定显示阶段的 RePhiEdit 标记事件。
    /// </summary>
    /// <param name="beat">标记所在拍数</param>
    /// <param name="phase">显示阶段编号</param>
    public static RpeEvent CreateMarker(Beat beat, int phase)
    {
        return new RpeEvent
        {
            StartBeat = beat,
            EndBeat = beat,
            StartValue = phase,
            EndValue = phase,
        };
    }

    private static RpeAlphaEvent CreateAlphaEvent(Beat beat, int alpha)
    {
        return new RpeAlphaEvent
        {
            StartBeat = beat,
            EndBeat = beat,
            StartValue = alpha,
            EndValue = alpha,
        };
    }

    /// <summary>
    /// 创建 IR 数值事件。
    /// </summary>
    /// <param name="startBeat">事件开始拍</param>
    /// <param name="endBeat">事件结束拍</param>
    /// <param name="startValue">事件起始值</param>
    /// <param name="endValue">事件结束值</param>
    public static IrEvent CreateEvent(
        Beat startBeat,
        Beat endBeat,
        double startValue,
        double endValue
    )
    {
        return new IrEvent
        {
            StartBeat = startBeat,
            EndBeat = endBeat,
            StartValue = startValue,
            EndValue = endValue,
            Easing = new IrEasing(1),
        };
    }

    /// <summary>
    /// 判断事件列表是否包含至少一个事件。
    /// </summary>
    /// <param name="events">待检查的事件列表</param>
    public static bool HasEvents(List<IrEvent>? events)
    {
        return events is { Count: > 0 };
    }
}
