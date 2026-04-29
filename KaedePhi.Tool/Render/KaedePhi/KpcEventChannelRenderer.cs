using KaedePhi.Core.Common;
using SkiaSharp;
using EventLayer = KaedePhi.Core.KaedePhi.EventLayer;

namespace KaedePhi.Tool.Render.KaedePhi;

/// <summary>
/// 将 NRC EventLayer 中各通道事件渲染为图片。
/// 布局：时间（拍）沿 Y 轴向上（beat 0 在底部），通道值沿各通道 X 轴。
/// 五个通道（MoveX / MoveY / Rotate / Alpha / Speed）横向排列。
/// </summary>
public static class KpcEventChannelRenderer
{
    private sealed class ChannelData
    {
        public string Name { get; init; } = "";
        public SKColor Color { get; init; }
        public double MinVal { get; init; }
        public double MaxVal { get; init; }
        public List<(double Start, double End, Func<double, double> GetValue)> Events { get; init; } = [];
    }

    private sealed class Segment
    {
        public double StartBeat { get; init; }
        public double EndBeat { get; init; }
        public double MinValue { get; init; }
        public double MaxValue { get; init; }
        public double RenderMin { get; init; }
        public double RenderMax { get; init; }
        public IReadOnlyList<(double Start, double End, Func<double, double> GetValue)> Events { get; init; }
            = Array.Empty<(double, double, Func<double, double>)>();
    }

    /// <summary>渲染单个 EventLayer 的所有通道，返回 SKBitmap。</summary>
    /// <param name="layer">待渲染的事件层。</param>
    /// <param name="opts">渲染配置。</param>
    /// <returns>渲染后的位图。</returns>
    public static SKBitmap RenderEventLayer(EventLayer layer, KpcRenderOptions opts)
    {
        var channels = BuildChannels(layer, opts);
        var totalBeats = ComputeTotalBeats(layer);
        if (totalBeats <= 0) totalBeats = 4;

        const int numCh = 5;
        var totalWidth = opts.LeftMargin + numCh * opts.ChannelWidth + (numCh - 1) * opts.ChannelPadding + 8;
        var totalHeight = opts.HeaderHeight + (int)Math.Ceiling(totalBeats * opts.PixelsPerBeat) + opts.BottomPadding;

        var bitmap = new SKBitmap(totalWidth, totalHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(opts.BackgroundColor);

        // Pass 1：所有通道背景 + 标题
        for (var ci = 0; ci < channels.Count; ci++)
        {
            var chX = opts.LeftMargin + ci * (opts.ChannelWidth + opts.ChannelPadding);
            DrawChannelBackground(canvas, chX, totalHeight, opts);
            DrawChannelHeader(canvas, chX, channels[ci], opts, totalHeight);
        }

        // Pass 2：格线
        DrawBeatGrid(canvas, totalBeats, totalWidth, totalHeight, opts);

        // Pass 3：各通道事件块 + 曲线 + 边界标注
        for (var ci = 0; ci < channels.Count; ci++)
        {
            var chX = opts.LeftMargin + ci * (opts.ChannelWidth + opts.ChannelPadding);
            DrawChannelContent(canvas, chX, channels[ci], totalHeight, opts);
        }

        // Pass 4：左侧拍号文字
        DrawBeatLabels(canvas, totalBeats, totalHeight, opts);

        return bitmap;
    }

    private static List<ChannelData> BuildChannels(EventLayer layer, KpcRenderOptions opts)
    {
        return
        [
            BuildChannel("MoveX", opts.MoveXColor, layer.MoveXEvents, (e, b) => Convert.ToDouble(e.GetValueAtBeat(b))),
            BuildChannel("MoveY", opts.MoveYColor, layer.MoveYEvents, (e, b) => Convert.ToDouble(e.GetValueAtBeat(b))),
            BuildChannel("Rotate", opts.RotateColor, layer.RotateEvents, (e, b) => Convert.ToDouble(e.GetValueAtBeat(b))),
            BuildChannel("Alpha", opts.AlphaColor, layer.AlphaEvents, (e, b) => Convert.ToDouble(e.GetValueAtBeat(b))),
            BuildChannel("Speed", opts.SpeedColor, layer.SpeedEvents, (e, b) => Convert.ToDouble(e.GetValueAtBeat(b))),
        ];
    }

    private static ChannelData BuildChannel<T>(
        string name, SKColor color,
        List<Kpc.Event<T>>? events,
        Func<Kpc.Event<T>, Beat, double> getValue)
    {
        var (minVal, maxVal) = SampleEventRange(events, getValue);
        var list = new List<(double, double, Func<double, double>)>();
        if (events != null)
        {
            foreach (var evt in events)
            {
                var captured = evt;
                list.Add(((double)evt.StartBeat, (double)evt.EndBeat,
                    bd => getValue(captured, new Beat(bd))));
            }
        }

        return new ChannelData { Name = name, Color = color, MinVal = minVal, MaxVal = maxVal, Events = list };
    }

    private static (double min, double max) SampleEventRange<T>(
        List<Kpc.Event<T>>? events,
        Func<Kpc.Event<T>, Beat, double> getValue,
        double paddingRatio = 0.10,
        int samplesPerEvent = 16)
    {
        if (events == null || events.Count == 0) return (-1.0, 1.0);

        double mn = double.MaxValue, mx = double.MinValue;

        foreach (var evt in events)
        {
            double sb = evt.StartBeat, eb = evt.EndBeat;
            if (eb <= sb) continue;

            for (var i = 0; i <= samplesPerEvent; i++)
            {
                var t = (double)i / samplesPerEvent;
                var val = getValue(evt, new Beat(sb + t * (eb - sb)));
                if (val < mn) mn = val;
                if (val > mx) mx = val;
            }
        }

        if (mn > mx) return (-1.0, 1.0);

        if (mx - mn < 1e-9)
        {
            double c = (mn + mx) / 2.0, half = Math.Max(Math.Abs(c) * 0.15, 0.1);
            return (c - half, c + half);
        }

        var pad = (mx - mn) * paddingRatio;
        return (mn - pad, mx + pad);
    }

    private static double ComputeTotalBeats(EventLayer layer)
    {
        double max = 0;
        Scan(layer.MoveXEvents);
        Scan(layer.MoveYEvents);
        Scan(layer.RotateEvents);
        Scan(layer.AlphaEvents);
        Scan(layer.SpeedEvents);
        return max;

        void Scan<T>(List<Kpc.Event<T>>? list)
        {
            if (list == null) return;
            var localMax = list.Select(e => (double)e.EndBeat).DefaultIfEmpty(0).Max();
            if (localMax > max) max = localMax;
        }
    }

    private static float BeatToY(double beat, KpcRenderOptions opts, int totalHeight) =>
        totalHeight - opts.HeaderHeight - (float)(beat * opts.PixelsPerBeat);

    private static float ValueToX(double value, int channelX, double minVal, double maxVal, int channelWidth)
    {
        if (maxVal <= minVal) return channelX + channelWidth / 2f;
        return channelX + (float)(Math.Clamp((value - minVal) / (maxVal - minVal), 0.0, 1.0) * channelWidth);
    }

    private static List<Segment> GroupSegments(
        List<(double Start, double End, Func<double, double> GetValue)> events,
        double tolerance = 1e-6)
    {
        var result = new List<Segment>();
        if (events.Count == 0) return result;

        var group = new List<(double Start, double End, Func<double, double> GetValue)> { events[0] };

        for (var i = 1; i < events.Count; i++)
        {
            if (Math.Abs(events[i - 1].End - events[i].Start) < tolerance)
                group.Add(events[i]);
            else
            {
                result.Add(BuildSegment(group));
                group = [events[i]];
            }
        }

        result.Add(BuildSegment(group));
        return result;
    }

    private static Segment BuildSegment(List<(double Start, double End, Func<double, double> GetValue)> group)
    {
        const double paddingRatio = 0.10;
        const int samplesPerEvent = 16;

        double epMn = double.MaxValue, epMx = double.MinValue;
        double smMn = double.MaxValue, smMx = double.MinValue;

        foreach (var (s, e, get) in group)
        {
            double v1 = get(s), v2 = get(e);
            if (v1 < epMn) epMn = v1;
            if (v1 > epMx) epMx = v1;
            if (v2 < epMn) epMn = v2;
            if (v2 > epMx) epMx = v2;

            if (e <= s) continue;
            for (var i = 0; i <= samplesPerEvent; i++)
            {
                var val = get(s + (double)i / samplesPerEvent * (e - s));
                if (val < smMn) smMn = val;
                if (val > smMx) smMx = val;
            }
        }

        if (epMn > epMx) epMn = epMx = 0;
        if (smMn > smMx) { smMn = epMn; smMx = epMx; }

        var smRange = smMx - smMn;
        double renderMin, renderMax;
        if (smRange < 1e-9)
        {
            double c = (smMn + smMx) / 2.0, half = Math.Max(Math.Abs(c) * 0.15, 0.1);
            renderMin = c - half;
            renderMax = c + half;
        }
        else
        {
            var pad = smRange * paddingRatio;
            renderMin = smMn - pad;
            renderMax = smMx + pad;
        }

        return new Segment
        {
            StartBeat = group[0].Start, EndBeat = group[^1].End,
            MinValue = epMn, MaxValue = epMx,
            RenderMin = renderMin, RenderMax = renderMax,
            Events = group
        };
    }

    private static void DrawBeatGrid(SKCanvas canvas, double totalBeats, int totalWidth, int totalHeight,
        KpcRenderOptions opts)
    {
        var subdivisions = Math.Max(1, opts.BeatSubdivisions);
        var step = 1.0 / subdivisions;

        using var beatPaint = new SKPaint();
        beatPaint.Color = opts.BeatGridColor;
        beatPaint.StrokeWidth = 1f;
        beatPaint.PathEffect = SKPathEffect.CreateDash([8f, 5f], 0);
        using var subPaint = new SKPaint();
        subPaint.Color = opts.SubBeatGridColor;
        subPaint.StrokeWidth = 1f;
        subPaint.PathEffect = SKPathEffect.CreateDash([4f, 6f], 0);

        for (double b = 0; b <= totalBeats + step * 0.01; b += step)
        {
            var y = BeatToY(b, opts, totalHeight);
            var isWholeBeat = Math.Abs(b - Math.Round(b)) < step * 0.1;
            if (y > totalHeight - opts.HeaderHeight) continue;
            canvas.DrawLine(0, y, totalWidth, y, isWholeBeat ? beatPaint : subPaint);
        }
    }

    private static void DrawBeatLabels(SKCanvas canvas, double totalBeats, int totalHeight, KpcRenderOptions opts)
    {
        using var font = new SKFont();
        font.Size = 11;
        using var paint = new SKPaint();
        paint.Color = opts.TextColor;
        paint.IsAntialias = true;

        for (var b = 0; b <= (int)Math.Ceiling(totalBeats); b++)
        {
            var y = BeatToY(b, opts, totalHeight);
            canvas.DrawText(b.ToString(), 4, y - 3, font, paint);
        }
    }

    private static void DrawChannelBackground(SKCanvas canvas, int channelX, int totalHeight,
        KpcRenderOptions opts)
    {
        using var bgPaint = new SKPaint();
        bgPaint.Color = opts.ChannelBackgroundColor;
        canvas.DrawRect(SKRect.Create(channelX, 0, opts.ChannelWidth, totalHeight - opts.HeaderHeight), bgPaint);
    }

    private static void DrawChannelHeader(SKCanvas canvas, int channelX, ChannelData ch, KpcRenderOptions opts,
        int totalHeight)
    {
        var footerTop = totalHeight - opts.HeaderHeight;

        using var hdrBg = new SKPaint();
        hdrBg.Color = new SKColor(ch.Color.Red, ch.Color.Green, ch.Color.Blue, 60);
        canvas.DrawRect(SKRect.Create(channelX, footerTop, opts.ChannelWidth, opts.HeaderHeight), hdrBg);

        using var font = new SKFont();
        font.Size = 12;
        using var txtPaint = new SKPaint();
        txtPaint.Color = ch.Color;
        txtPaint.IsAntialias = true;
        canvas.DrawText(ch.Name, channelX + 6, footerTop + opts.HeaderHeight / 2f + 5, font, txtPaint);

        using var sf = new SKFont();
        sf.Size = 9;
        using var sp = new SKPaint();
        sp.Color = new SKColor(ch.Color.Red, ch.Color.Green, ch.Color.Blue, 170);
        sp.IsAntialias = true;
        var rt = $"[{ch.MinVal:G4}, {ch.MaxVal:G4}]";
        canvas.DrawText(rt, channelX + opts.ChannelWidth - sf.MeasureText(rt) - 4,
            footerTop + opts.HeaderHeight - 5, sf, sp);
    }

    private static void DrawChannelContent(SKCanvas canvas, int channelX, ChannelData ch, int totalHeight,
        KpcRenderOptions opts)
    {
        if (ch.Events.Count == 0) return;

        var segments = GroupSegments(ch.Events);

        using var blockPaint = new SKPaint();
        blockPaint.Color = opts.EventBlockColor;
        using var curvePaint = new SKPaint();
        curvePaint.Color = ch.Color;
        curvePaint.StrokeWidth = opts.StrokeWidth;
        curvePaint.IsAntialias = true;
        curvePaint.IsStroke = true;
        curvePaint.StrokeCap = SKStrokeCap.Round;
        curvePaint.StrokeJoin = SKStrokeJoin.Round;

        foreach (var seg in segments)
        {
            foreach (var (start, end, getValue) in seg.Events)
            {
                if (end <= start) continue;

                var yTop = BeatToY(end, opts, totalHeight);
                var yBot = BeatToY(start, opts, totalHeight);
                var blockH = yBot - yTop;

                canvas.DrawRect(SKRect.Create(channelX, yTop, opts.ChannelWidth, blockH), blockPaint);

                canvas.Save();
                canvas.ClipRect(SKRect.Create(channelX, yTop, opts.ChannelWidth, blockH));
                canvas.DrawPath(
                    BuildCurvePath(start, end, getValue, channelX, seg.RenderMin, seg.RenderMax, opts, totalHeight),
                    curvePaint);
                canvas.Restore();
            }
        }

        DrawSegmentBoundaryLabels(canvas, channelX, segments, totalHeight, opts);
    }

    private static SKPath BuildCurvePath(
        double startBeat, double endBeat,
        Func<double, double> getValue,
        int channelX, double minVal, double maxVal,
        KpcRenderOptions opts, int totalHeight)
    {
        var path = new SKPath();
        var samples = Math.Max(opts.SamplesPerEvent, 2);

        for (var s = 0; s < samples; s++)
        {
            var t = (double)s / (samples - 1);
            var beat = startBeat + t * (endBeat - startBeat);
            var value = getValue(beat);

            var x = ValueToX(value, channelX, minVal, maxVal, opts.ChannelWidth);
            var y = BeatToY(beat, opts, totalHeight);

            if (s == 0) path.MoveTo(x, y);
            else path.LineTo(x, y);
        }

        return path;
    }

    private static void DrawSegmentBoundaryLabels(SKCanvas canvas, int channelX, List<Segment> segments,
        int totalHeight, KpcRenderOptions opts)
    {
        using var font = new SKFont();
        font.Size = 11;
        font.GetFontMetrics(out var metrics);

        var vOffset = -(metrics.Ascent + metrics.Descent) / 2f;
        var halfH = (-metrics.Ascent + metrics.Descent) / 2f + 1f;

        using var bgPaint = new SKPaint();
        bgPaint.Color = new SKColor(22, 22, 30, 210);
        using var labelPaint = new SKPaint();
        labelPaint.Color = opts.CenterLineColor;
        labelPaint.IsAntialias = true;

        foreach (var seg in segments)
        {
            var yTop = BeatToY(seg.EndBeat, opts, totalHeight);
            var yBot = BeatToY(seg.StartBeat, opts, totalHeight);

            var minLbl = FormatBoundaryValue(seg.MinValue);
            var maxLbl = FormatBoundaryValue(seg.MaxValue);
            var minLw = font.MeasureText(minLbl);
            var maxLw = font.MeasureText(maxLbl);

            var xMin = ValueToX(seg.MinValue, channelX, seg.RenderMin, seg.RenderMax, opts.ChannelWidth);
            var xMax = ValueToX(seg.MaxValue, channelX, seg.RenderMin, seg.RenderMax, opts.ChannelWidth);

            var lxMin = Math.Clamp(xMin, channelX + 2f, channelX + opts.ChannelWidth - minLw - 2f);
            var lxMax = Math.Clamp(xMax - maxLw, channelX + 2f, channelX + opts.ChannelWidth - maxLw - 2f);

            DrawBoundaryLabel(canvas, minLbl, lxMin, yTop, halfH, vOffset, minLw, font, bgPaint, labelPaint);
            DrawBoundaryLabel(canvas, maxLbl, lxMax, yTop, halfH, vOffset, maxLw, font, bgPaint, labelPaint);

            if (yBot - yTop > halfH * 4)
            {
                DrawBoundaryLabel(canvas, minLbl, lxMin, yBot, halfH, vOffset, minLw, font, bgPaint, labelPaint);
                DrawBoundaryLabel(canvas, maxLbl, lxMax, yBot, halfH, vOffset, maxLw, font, bgPaint, labelPaint);
            }
        }
    }

    private static void DrawBoundaryLabel(SKCanvas canvas, string text, float lx, float lineY,
        float halfH, float vOffset, float lw, SKFont font, SKPaint bgPaint, SKPaint textPaint)
    {
        canvas.DrawRect(SKRect.Create(lx - 1, lineY - halfH, lw + 2, halfH * 2), bgPaint);
        canvas.DrawText(text, lx, lineY + vOffset, font, textPaint);
    }

    private static string FormatBoundaryValue(double value) =>
        Math.Abs(value) < 1000 ? $"{value:F2}" : $"{value:G4}";
}
