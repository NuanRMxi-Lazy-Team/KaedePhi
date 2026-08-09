using KaedePhi.Core.KaedePhi.Events;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Event.KaedePhi;
using KaedePhi.Tool.JudgeLines.KaedePhi;
using KaedePhi.Tool.Layer.KaedePhi;
using KaedePhi.Tool.Render.KaedePhi;
using Chart = KaedePhi.Core.KaedePhi.Chart;

namespace KaedePhi.Tool.App.Shared;

public static class ChartProcessor
{
    public static void UnbindFather(
        Chart chart,
        double precision,
        double tolerance,
        bool classic,
        bool disableCompress = false,
        Action<string>? info = null,
        Action<string>? warning = null,
        Action<string>? error = null,
        Action<string>? debug = null,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        ChartProcessingValidator.ValidatePrecision(precision);
        ChartProcessingValidator.ValidateTolerance(tolerance);
        ChartProcessingValidator.ValidateJudgeLineHierarchy(chart.JudgeLineList);

        var unbinder = new JudgeLineUnbinder();
        if (info != null || warning != null || error != null || debug != null)
            unbinder.SubscribeLog(info, warning, error, debug);
        var compressor = new LayerProcessor();
        if (info != null || warning != null || error != null || debug != null)
            compressor.SubscribeLog(info, warning, error, debug);

        var linesToProcess = new List<int>();
        for (var i = 0; i < chart.JudgeLineList.Count; i++)
        {
            if (chart.JudgeLineList[i].Father != -1)
                linesToProcess.Add(i);
        }

        var totalLines = linesToProcess.Count;
        for (var idx = 0; idx < totalLines; idx++)
        {
            ct.ThrowIfCancellationRequested();
            var i = linesToProcess[idx];
            var capturedIdx = idx;
            var lineProgress = progress is null
                ? null
                : new Progress<ToolProgress>(p =>
                {
                    var overall = (double)capturedIdx / totalLines;
                    progress.Report(new ToolProgress(p.Percentage, overall, p.Detail));
                });
            var unboundLine = classic
                ? unbinder.FatherUnbind(i, chart.JudgeLineList, precision, lineProgress, ct)
                : unbinder.FatherUnbind(
                    i,
                    chart.JudgeLineList,
                    precision,
                    tolerance,
                    lineProgress,
                    ct
                );
            if (classic && !disableCompress)
            {
                foreach (var layer in unboundLine.EventLayers)
                    compressor.LayerEventsCompress(layer, tolerance, lineProgress);
            }
            chart.JudgeLineList[i] = unboundLine;
        }

        progress?.Report(new ToolProgress(1.0, 1.0));
    }

    public static void LayerMerge(
        Chart chart,
        double precision,
        double tolerance,
        bool classic,
        bool disableCompress,
        Action<string>? info = null,
        Action<string>? warning = null,
        Action<string>? error = null,
        Action<string>? debug = null,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        ChartProcessingValidator.ValidatePrecision(precision);
        ChartProcessingValidator.ValidateTolerance(tolerance);

        var processor = new LayerProcessor();
        if (info != null || warning != null || error != null || debug != null)
            processor.SubscribeLog(info, warning, error, debug);

        var totalLines = chart.JudgeLineList.Count;
        for (var li = 0; li < totalLines; li++)
        {
            ct.ThrowIfCancellationRequested();
            var line = chart.JudgeLineList[li];
            if (line.EventLayers is not { Count: > 1 })
            {
                progress?.Report(new ToolProgress(1.0, (double)(li + 1) / totalLines));
                continue;
            }

            var capturedLi = li;
            var lineProgress = progress is null
                ? null
                : new Progress<ToolProgress>(p =>
                {
                    var overall = (double)capturedLi / totalLines;
                    progress.Report(new ToolProgress(p.Percentage, overall, p.Detail));
                });
            var merged = classic
                ? processor.LayerMerge(line.EventLayers, precision, lineProgress)
                : processor.LayerMergePlus(line.EventLayers, precision, tolerance, lineProgress);
            if (!disableCompress)
                processor.LayerEventsCompress(merged, tolerance, lineProgress);
            line.EventLayers.Clear();
            line.EventLayers.Add(merged);
        }

        progress?.Report(new ToolProgress(1.0, 1.0));
    }

    public static void CutEvent(
        Chart chart,
        double precision,
        double tolerance,
        bool disableCompress,
        Action<string>? info = null,
        Action<string>? warning = null,
        Action<string>? error = null,
        Action<string>? debug = null,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        ChartProcessingValidator.ValidatePrecision(precision);
        ChartProcessingValidator.ValidateTolerance(tolerance);

        var processor = new LayerProcessor();
        if (info != null || warning != null || error != null || debug != null)
            processor.SubscribeLog(info, warning, error, debug);

        var totalLines = chart.JudgeLineList.Count;
        for (var li = 0; li < totalLines; li++)
        {
            ct.ThrowIfCancellationRequested();
            var line = chart.JudgeLineList[li];
            if (line.EventLayers is not { Count: > 0 })
            {
                progress?.Report(new ToolProgress(1.0, (double)(li + 1) / totalLines));
                continue;
            }

            var capturedLi = li;
            var lineProgress = progress is null
                ? null
                : new Progress<ToolProgress>(p =>
                {
                    var overall = (double)capturedLi / totalLines;
                    progress.Report(new ToolProgress(p.Percentage, overall, p.Detail));
                });
            line.EventLayers = processor.CutLayerEvents(line.EventLayers, precision, lineProgress);
            if (disableCompress)
                continue;
            foreach (var layer in line.EventLayers)
                processor.LayerEventsCompress(layer, tolerance, lineProgress);
        }

        progress?.Report(new ToolProgress(1.0, 1.0));
    }

    public static void FitEvent(
        Chart chart,
        double tolerance,
        Action<string>? info = null,
        Action<string>? warning = null,
        Action<string>? error = null,
        Action<string>? debug = null,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        ChartProcessingValidator.ValidateTolerance(tolerance);

        var doubleFit = new EventFit<double>();
        var intFit = new EventFit<int>();
        var floatFit = new EventFit<float>();
        if (info != null || warning != null || error != null || debug != null)
        {
            doubleFit.SubscribeLog(info, warning, error, debug);
            intFit.SubscribeLog(info, warning, error, debug);
            floatFit.SubscribeLog(info, warning, error, debug);
        }

        var totalLines = chart.JudgeLineList.Count;
        for (var li = 0; li < totalLines; li++)
        {
            ct.ThrowIfCancellationRequested();
            var line = chart.JudgeLineList[li];
            if (line.EventLayers is not { Count: > 0 })
            {
                progress?.Report(new ToolProgress(1.0, (double)(li + 1) / totalLines));
                continue;
            }

            var totalLayers = line.EventLayers.Count;
            for (var ei = 0; ei < totalLayers; ei++)
            {
                ct.ThrowIfCancellationRequested();
                var capturedLi = li;
                var capturedEi = ei;
                var layerProgress = progress is null
                    ? null
                    : new Progress<ToolProgress>(p =>
                    {
                        var overall = (capturedLi + (double)capturedEi / totalLayers) / totalLines;
                        progress.Report(new ToolProgress(p.Percentage, overall, p.Detail));
                    });
                FitLayer(
                    line.EventLayers[ei],
                    doubleFit,
                    intFit,
                    floatFit,
                    tolerance,
                    layerProgress
                );
            }
        }

        progress?.Report(new ToolProgress(1.0, 1.0));
    }

    private static void FitLayer(
        EventLayer layer,
        EventFit<double> doubleFit,
        EventFit<int> intFit,
        EventFit<float> floatFit,
        double tolerance,
        IProgress<ToolProgress>? progress
    )
    {
        if (layer.MoveXEvents is { Count: > 0 })
            layer.MoveXEvents = doubleFit.FitEvents(layer.MoveXEvents, tolerance);
        if (layer.MoveYEvents is { Count: > 0 })
            layer.MoveYEvents = doubleFit.FitEvents(layer.MoveYEvents, tolerance);
        if (layer.RotateEvents is { Count: > 0 })
            layer.RotateEvents = doubleFit.FitEvents(layer.RotateEvents, tolerance);
        if (layer.AlphaEvents is { Count: > 0 })
            layer.AlphaEvents = intFit.FitEvents(layer.AlphaEvents, tolerance);
        if (layer.SpeedEvents is { Count: > 0 })
            layer.SpeedEvents = floatFit.FitEvents(layer.SpeedEvents, tolerance);
        progress?.Report(new ToolProgress(1.0));
    }

    public static IReadOnlyList<string> Render(
        Chart chart,
        string outputDir,
        KpcRenderOptions options,
        int? lineIndex = null,
        int? layerIndex = null,
        Action<string>? info = null,
        Action<string>? warning = null,
        Action<string>? error = null,
        Action<string>? debug = null,
        IProgress<ToolProgress>? progress = null,
        CancellationToken ct = default
    )
    {
        ChartProcessingValidator.ValidateRender(chart, options, lineIndex, layerIndex);
        var exporter = new KpcChartRenderExporter();
        if (info != null || warning != null || error != null || debug != null)
            exporter.SubscribeLog(info, warning, error, debug);
        return exporter.ExportChart(chart, outputDir, options, lineIndex, layerIndex, progress, ct);
    }
}
