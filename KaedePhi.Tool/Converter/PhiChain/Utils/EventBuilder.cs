using KaedePhi.Core.Primitives;
using KaedePhi.Core.Formats.PhiChain.v6;
using KaedePhi.Tool.Converter.PhiChain.Model;
using KaedePhi.Tool.Event.Intermediate;
using PhichainEventType = KaedePhi.Core.Formats.PhiChain.v6.LineEventType;
using PhichainEventValueType = KaedePhi.Core.Formats.PhiChain.v6.LineEventValueType;

namespace KaedePhi.Tool.Converter.PhiChain.Utils;

/// <summary>
/// PhiChain 与 IR 事件之间的双向转换工具。
/// </summary>
public static class EventBuilder
{
    private const int MaximumEasingSegments = 1_000_000;

    private static readonly EventCutter<double> DoubleCutter = new();
    private static readonly EventCutter<int> IntCutter = new();
    private static readonly EventCutter<float> FloatCutter = new();

    /// <summary>
    /// 将 PhiChain 事件列表转换为 IR 事件层。
    /// </summary>
    /// <param name="events">PhiChain 事件列表</param>
    /// <returns>IR 事件层</returns>
    public static IrEvents.EventLayer ConvertEvents(List<LineEvent> events)
    {
        var layer = new IrEvents.EventLayer();

        foreach (var evt in events)
        {
            switch (evt.Type)
            {
                case PhichainEventType.X:
                    layer.MoveXEvents ??= [];
                    layer.MoveXEvents.Add(
                        ConvertEventToDoubleWithTransform(evt, Transform.TransformToIrX)
                    );
                    break;
                case PhichainEventType.Y:
                    layer.MoveYEvents ??= [];
                    layer.MoveYEvents.Add(
                        ConvertEventToDoubleWithTransform(evt, Transform.TransformToIrY)
                    );
                    break;
                case PhichainEventType.Rotation:
                    layer.RotateEvents ??= [];
                    layer.RotateEvents.Add(
                        ConvertEventToDoubleWithTransform(evt, Transform.TransformToIrAngle)
                    );
                    break;
                case PhichainEventType.Opacity:
                    layer.AlphaEvents ??= [];
                    layer.AlphaEvents.Add(ConvertEventToInt(evt));
                    break;
                case PhichainEventType.Speed:
                    layer.SpeedEvents ??= [];
                    layer.SpeedEvents.Add(ConvertEventToFloat(evt));
                    break;
            }
        }

        return layer;
    }

    /// <summary>
    /// 将 IR 事件层转换为 PhiChain 事件列表（使用默认选项）。
    /// </summary>
    /// <param name="layer">IR 事件层</param>
    /// <returns>PhiChain 事件列表</returns>
    public static List<LineEvent> ConvertEventLayer(IrEvents.EventLayer layer)
    {
        return ConvertEventLayer(layer, new IrToPhiChainConvertOptions());
    }

    /// <summary>
    /// 将 IR 事件层转换为 PhiChain 事件列表。
    /// </summary>
    /// <param name="layer">IR 事件层</param>
    /// <param name="options">转换选项</param>
    /// <returns>PhiChain 事件列表</returns>
    public static List<LineEvent> ConvertEventLayer(
        IrEvents.EventLayer layer,
        IrToPhiChainConvertOptions options
    )
    {
        if (options.EasingCutPrecision <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "EasingCutPrecision必须大于0。");

        var events = new List<LineEvent>();
        if (layer.MoveXEvents != null)
            events.AddRange(
                ConvertEventsWithTransform(
                    layer.MoveXEvents,
                    PhichainEventType.X,
                    Transform.TransformToPhiChainX,
                    options
                )
            );

        if (layer.MoveYEvents != null)
            events.AddRange(
                ConvertEventsWithTransform(
                    layer.MoveYEvents,
                    PhichainEventType.Y,
                    Transform.TransformToPhiChainY,
                    options
                )
            );

        if (layer.RotateEvents != null)
            events.AddRange(
                ConvertEventsWithTransform(
                    layer.RotateEvents,
                    PhichainEventType.Rotation,
                    v => (float)Transform.TransformToPhiChainAngle(v),
                    options
                )
            );

        if (layer.AlphaEvents != null)
            events.AddRange(
                ConvertIntEventsWithCutting(layer.AlphaEvents, PhichainEventType.Opacity, options)
            );

        if (layer.SpeedEvents != null)
            events.AddRange(
                ConvertFloatEventsWithCutting(layer.SpeedEvents, PhichainEventType.Speed, options)
            );

        return events;
    }

    /// <summary>
    /// 转换 double 事件列表，对使用缓动截取的事件进行切割。
    /// </summary>
    private static List<LineEvent> ConvertEventsWithTransform(
        List<IrEvents.Event<double>> events,
        PhichainEventType eventType,
        Func<double, float> transform,
        IrToPhiChainConvertOptions options
    )
    {
        var result = new List<LineEvent>();
        foreach (var evt in events)
        {
            if (NeedsCutting(evt))
            {
                var cutEvents = DoubleCutter.CutEventToLinear(
                    evt,
                    1.0 / options.EasingCutPrecision
                );
                result.AddRange(
                    cutEvents.Select(e => ConvertEventWithTransform(e, eventType, transform))
                );
            }
            else
            {
                result.Add(ConvertEventWithTransform(evt, eventType, transform));
            }
        }

        return result;
    }

    /// <summary>
    /// 转换 int 事件列表，对使用缓动截取的事件进行切割。
    /// </summary>
    private static List<LineEvent> ConvertIntEventsWithCutting(
        List<IrEvents.Event<int>> events,
        PhichainEventType eventType,
        IrToPhiChainConvertOptions options
    )
    {
        var result = new List<LineEvent>();
        foreach (var evt in events)
        {
            if (NeedsCutting(evt))
            {
                var cutEvents = IntCutter.CutEventToLinear(evt, 1.0 / options.EasingCutPrecision);
                result.AddRange(cutEvents.Select(e => ConvertEvent(e, eventType)));
            }
            else
            {
                result.Add(ConvertEvent(evt, eventType));
            }
        }

        return result;
    }

    /// <summary>
    /// 转换 float 事件列表，对使用缓动截取的事件进行切割。
    /// </summary>
    private static List<LineEvent> ConvertFloatEventsWithCutting(
        List<IrEvents.Event<float>> events,
        PhichainEventType eventType,
        IrToPhiChainConvertOptions options
    )
    {
        var result = new List<LineEvent>();
        foreach (var evt in events)
        {
            if (NeedsCutting(evt))
            {
                var cutEvents = FloatCutter.CutEventToLinear(evt, 1.0 / options.EasingCutPrecision);
                result.AddRange(cutEvents.Select(e => ConvertEvent(e, eventType)));
            }
            else
            {
                result.Add(ConvertEvent(evt, eventType));
            }
        }

        return result;
    }

    /// <summary>
    /// 检查事件是否需要切割（使用了非默认的缓动截取）。
    /// </summary>
    private static bool NeedsCutting<T>(IrEvents.Event<T> evt)
        where T : notnull
    {
        return Math.Abs(evt.EasingLeft) > 0.0001f || Math.Abs(evt.EasingRight - 1.0f) > 0.0001f;
    }

    /// <summary>
    /// 将 PhiChain 事件转换为 IR double 事件，带坐标变换。
    /// </summary>
    private static IrEvents.Event<double> ConvertEventToDoubleWithTransform(
        LineEvent src,
        Func<float, double> transform
    )
    {
        var irEvent = new IrEvents.Event<double>
        {
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
        };

        if (src.Value.Type == PhichainEventValueType.Transition)
        {
            irEvent.StartValue = transform(src.Value.Start);
            irEvent.EndValue = transform(src.Value.End);

            if (src.Value.Easing.EasingType == EasingKind.Custom)
            {
                irEvent.IsBezier = true;
                irEvent.BezierPoints =
                [
                    src.Value.Easing.X1,
                    src.Value.Easing.Y1,
                    src.Value.Easing.X2,
                    src.Value.Easing.Y2,
                ];
            }
            else
            {
                irEvent.Easing = EasingConverter.ConvertEasing(src.Value.Easing);
            }
        }
        else
        {
            irEvent.StartValue = transform(src.Value.Value);
            irEvent.EndValue = transform(src.Value.Value);
        }

        return irEvent;
    }

    /// <summary>
    /// 将 PhiChain 事件转换为 IR int 事件（透明度）。
    /// </summary>
    private static IrEvents.Event<int> ConvertEventToInt(LineEvent src)
    {
        var irEvent = new IrEvents.Event<int>
        {
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
        };

        if (src.Value.Type == PhichainEventValueType.Transition)
        {
            // PhiChain 透明度范围 0-255，与 IR 一致
            irEvent.StartValue = (int)src.Value.Start;
            irEvent.EndValue = (int)src.Value.End;

            if (src.Value.Easing.EasingType == EasingKind.Custom)
            {
                irEvent.IsBezier = true;
                irEvent.BezierPoints =
                [
                    src.Value.Easing.X1,
                    src.Value.Easing.Y1,
                    src.Value.Easing.X2,
                    src.Value.Easing.Y2,
                ];
            }
            else
            {
                irEvent.Easing = EasingConverter.ConvertEasing(src.Value.Easing);
            }
        }
        else
        {
            irEvent.StartValue = (int)src.Value.Value;
            irEvent.EndValue = (int)src.Value.Value;
        }

        return irEvent;
    }

    /// <summary>
    /// 将 PhiChain 事件转换为 IR float 事件（速度）。
    /// </summary>
    private static IrEvents.Event<float> ConvertEventToFloat(LineEvent src)
    {
        var irEvent = new IrEvents.Event<float>
        {
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
        };

        if (src.Value.Type == PhichainEventValueType.Transition)
        {
            irEvent.StartValue = src.Value.Start;
            irEvent.EndValue = src.Value.End;

            if (src.Value.Easing.EasingType == EasingKind.Custom)
            {
                irEvent.IsBezier = true;
                irEvent.BezierPoints =
                [
                    src.Value.Easing.X1,
                    src.Value.Easing.Y1,
                    src.Value.Easing.X2,
                    src.Value.Easing.Y2,
                ];
            }
            else
            {
                irEvent.Easing = EasingConverter.ConvertEasing(src.Value.Easing);
            }
        }
        else
        {
            irEvent.StartValue = src.Value.Value;
            irEvent.EndValue = src.Value.Value;
        }

        return irEvent;
    }

    /// <summary>
    /// 将 IR double 事件转换为 PhiChain 事件，带坐标变换。
    /// </summary>
    private static LineEvent ConvertEventWithTransform(
        IrEvents.Event<double> src,
        PhichainEventType eventType,
        Func<double, float> transform
    )
    {
        var lineEvent = new LineEvent
        {
            Type = eventType,
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
        };

        if (src.IsBezier)
        {
            lineEvent.Value = LineEventValue.Transition(
                transform(src.StartValue),
                transform(src.EndValue),
                new Easing
                {
                    EasingType = EasingKind.Custom,
                    X1 = src.BezierPoints[0],
                    Y1 = src.BezierPoints[1],
                    X2 = src.BezierPoints[2],
                    Y2 = src.BezierPoints[3],
                }
            );
        }
        else if (Math.Abs(src.StartValue - src.EndValue) < 0.0001)
        {
            lineEvent.Value = LineEventValue.Constant(transform(src.StartValue));
        }
        else
        {
            lineEvent.Value = LineEventValue.Transition(
                transform(src.StartValue),
                transform(src.EndValue),
                EasingConverter.ConvertEasing(src.Easing, false)
            );
        }

        return lineEvent;
    }

    /// <summary>
    /// 将 IR int 事件转换为 PhiChain 事件。
    /// </summary>
    private static LineEvent ConvertEvent(IrEvents.Event<int> src, PhichainEventType eventType)
    {
        var lineEvent = new LineEvent
        {
            Type = eventType,
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
        };

        if (src.IsBezier)
        {
            lineEvent.Value = LineEventValue.Transition(
                src.StartValue,
                src.EndValue,
                new Easing
                {
                    EasingType = EasingKind.Custom,
                    X1 = src.BezierPoints[0],
                    Y1 = src.BezierPoints[1],
                    X2 = src.BezierPoints[2],
                    Y2 = src.BezierPoints[3],
                }
            );
        }
        else if (src.StartValue == src.EndValue)
        {
            lineEvent.Value = LineEventValue.Constant(src.StartValue);
        }
        else
        {
            lineEvent.Value = LineEventValue.Transition(
                src.StartValue,
                src.EndValue,
                EasingConverter.ConvertEasing(src.Easing, false)
            );
        }

        return lineEvent;
    }

    /// <summary>
    /// 将 IR float 事件转换为 PhiChain 事件。
    /// </summary>
    private static LineEvent ConvertEvent(IrEvents.Event<float> src, PhichainEventType eventType)
    {
        var lineEvent = new LineEvent
        {
            Type = eventType,
            StartBeat = new Beat((int[])src.StartBeat),
            EndBeat = new Beat((int[])src.EndBeat),
        };

        if (src.IsBezier)
        {
            lineEvent.Value = LineEventValue.Transition(
                src.StartValue,
                src.EndValue,
                new Easing
                {
                    EasingType = EasingKind.Custom,
                    X1 = src.BezierPoints[0],
                    Y1 = src.BezierPoints[1],
                    X2 = src.BezierPoints[2],
                    Y2 = src.BezierPoints[3],
                }
            );
        }
        else if (Math.Abs(src.StartValue - src.EndValue) < 0.0001f)
        {
            lineEvent.Value = LineEventValue.Constant(src.StartValue);
        }
        else
        {
            lineEvent.Value = LineEventValue.Transition(
                src.StartValue,
                src.EndValue,
                EasingConverter.ConvertEasing(src.Easing, false)
            );
        }

        return lineEvent;
    }

    /// <summary>
    /// 将不支持的缓动事件切段为线性事件。
    /// </summary>
    /// <param name="src">源事件</param>
    /// <param name="precision">每拍细分数量</param>
    /// <returns>切段后的事件列表</returns>
    public static List<LineEvent> SliceUnsupportedEasing(
        LineEvent src,
        int precision,
        CancellationToken ct = default
    )
    {
        if (src.Value.Type != PhichainEventValueType.Transition)
            return [src];

        var events = new List<LineEvent>();
        var startBeat = new Beat((int[])src.StartBeat);
        var endBeat = new Beat((int[])src.EndBeat);

        var startBeatVal = (double)startBeat;
        var endBeatVal = (double)endBeat;
        var totalBeats = endBeatVal - startBeatVal;
        if (totalBeats <= 0)
            return [src];

        // 用 long 计算切段数，避免 int 溢出；超过上限时显式失败
        var segmentCount = (long)Math.Ceiling(totalBeats * precision);
        if (segmentCount > MaximumEasingSegments)
            throw new FormatException(
                $"PhiChain 缓动切段数量 {segmentCount} 超过安全上限 {MaximumEasingSegments}。"
            );
        var segments = (int)segmentCount;
        if (segments <= 0)
            segments = 1;

        // 对原始缓动函数进行采样
        var easing = src.Value.Easing;
        var valueStart = src.Value.Start;
        var valueEnd = src.Value.End;

        for (var i = 0; i < segments; i++)
        {
            ct.ThrowIfCancellationRequested();
            var t1 = (double)i / segments;
            var t2 = (double)(i + 1) / segments;

            // 在原始缓动上采样
            var easedT1 = ApplyPhichainEasing(t1, easing);
            var easedT2 = ApplyPhichainEasing(t2, easing);

            var value1 = valueStart + (valueEnd - valueStart) * (float)easedT1;
            var value2 = valueStart + (valueEnd - valueStart) * (float)easedT2;

            var segStartBeat = new Beat(startBeatVal + t1 * totalBeats);
            var segEndBeat = new Beat(startBeatVal + t2 * totalBeats);

            events.Add(
                new LineEvent
                {
                    Type = src.Type,
                    StartBeat = segStartBeat,
                    EndBeat = segEndBeat,
                    Value = LineEventValue.Transition(value1, value2, Easing.Linear),
                }
            );
        }

        return events;
    }

    /// <summary>
    /// 应用 PhiChain 缓动函数采样，与 NoteBuilder.ApplyCurve 逻辑一致。
    /// </summary>
    private static double ApplyPhichainEasing(double t, Easing easing)
    {
        return easing.EasingType switch
        {
            EasingKind.Linear => t,
            EasingKind.Steps => easing.Count > 0 ? Math.Round(t * easing.Count) / easing.Count : t,
            EasingKind.Elastic => ApplyElasticCurve(t, easing.Omega),
            EasingKind.Custom => ApplyBezierCurve(t, easing.X1, easing.Y1, easing.X2, easing.Y2),
            _ => ApplyStandardEasingCurve(t, easing),
        };
    }

    private static double ApplyElasticCurve(double t, float omega)
    {
        if (Math.Abs(omega) <= Common.Constants.FloatEpsilon)
            return t;
        return 1.0
            - Math.Pow(1.0 - t, 2) * (2.0 * Math.Sin(omega * t) / omega + Math.Cos(omega * t));
    }

    private static double ApplyBezierCurve(double t, float x1, float y1, float x2, float y2)
    {
        var cx = 3.0 * x1;
        var bx = 3.0 * (x2 - x1) - cx;
        var ax = 1.0 - cx - bx;
        var cy = 3.0 * y1;
        var by = 3.0 * (y2 - y1) - cy;
        var ay = 1.0 - cy - by;

        var guess = t;
        for (var i = 0; i < 8; i++)
        {
            var currentX = ((ax * guess + bx) * guess + cx) * guess;
            var currentSlope = (3.0 * ax * guess + 2.0 * bx) * guess + cx;
            if (Math.Abs(currentSlope) < 1e-7)
                break;
            guess -= (currentX - t) / currentSlope;
        }

        return ((ay * guess + by) * guess + cy) * guess;
    }

    private static double ApplyStandardEasingCurve(double t, Easing easing)
    {
        try
        {
            var easingNumber = EasingConverter.ConvertToIrEasingNumber(easing);
            var irEasing = new Ir.Easing(easingNumber);
            return irEasing.Interpolate(0f, 1f, 0.0, 1.0, t);
        }
        catch (EasingConverter.EasingNotSupportedException)
        {
            return t;
        }
    }
}
