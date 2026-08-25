using System.ComponentModel;
using Avalonia.Controls;
using KaedePhi.Tool.App.Config;
using KaedePhi.Tool.App.Gui.Services;
using KaedePhi.Tool.App.Gui.ViewModels;
using KaedePhi.Tool.App.Gui.Views;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Render.KaedePhi;
using Serilog;
using static KaedePhi.Tool.Localization.GuiLocalizationString;

namespace KaedePhi.Tool.App.Gui.Controllers;

internal sealed class ToolController
{
    private readonly GuiNavigationService _navigation;
    private readonly GuiChartService _chart;
    private readonly AppConfigService _config;
    private readonly LogService _logService;
    private readonly ILogger _log;
    private readonly Window _window;

    private CancellationTokenSource? _cts;

    public ToolController(
        GuiNavigationService navigation,
        GuiChartService chart,
        LogService logService,
        AppConfigService config,
        Window window,
        Action returnToImport
    )
    {
        _navigation = navigation;
        _chart = chart;
        _config = config;
        _logService = logService;
        _log = logService.ForContext<ToolController>();
        _window = window;

        _navigation.Tool.RequestRun += OnToolRun;
        _navigation.Tool.RequestExport += _navigation.ShowExport;
        _navigation.Tool.RequestSettings += _navigation.ShowSettings;
        _navigation.Tool.RequestReturnToImport += returnToImport;
        _navigation.Tool.PropertyChanged += OnToolVmPropertyChanged;
        _navigation.Processing.RequestReturnToTools += _navigation.ShowTool;
        _navigation.Processing.RequestReturnToImport += returnToImport;
        _navigation.Processing.RequestGoToExport += _navigation.ShowExport;
        _navigation.Processing.RequestCancel += OnCancelProcessing;
    }

    public void Cancel()
    {
        if (_cts is { IsCancellationRequested: false })
            _cts.Cancel();
    }

    private void OnToolVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            sender is ToolViewModel { SelectedTool: not null } vm
            && e.PropertyName == nameof(ToolViewModel.SelectedTool)
        )
            vm.ApplyConfigDefaults(_config.Config);
    }

    private async void OnToolRun()
    {
        var toolVm = _navigation.Tool;
        var processingVm = _navigation.Processing;
        if (toolVm.SelectedTool == null || toolVm.IsProcessing)
            return;
        if (_chart.CurrentChart is not { } currentChart)
            return;

        toolVm.IsProcessing = true;
        toolVm.StatusText = status_processing;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            var toolId = toolVm.SelectedTool.ToolId;
            _navigation.ShowProcessing();

            // 渲染为只读操作直接读取当前谱面；其余变更型工具在克隆副本上处理，
            // 保证取消或异常时当前谱面不会被半处理污染
            var isRender = toolId == "render";
            var kpcChart = isRender ? currentChart : currentChart.Clone();

            // 运行工具
            processingVm.SetStep(0, string.Format(log_running_tool, toolId));
            var toolProgress = new Progress<ToolProgress>(p =>
            {
                var overall = p.OverallPercentage >= 0 ? p.OverallPercentage : p.Percentage;
                processingVm.SetToolProgress(p.Percentage, overall, p.Detail);
            });
            await Task.Run(
                () => RunTool(toolId, kpcChart, toolProgress, ct),
                ct
            );

            // 校验并提交：变更型工具完成后校验副本，通过后才替换当前谱面
            if (!isRender)
            {
                KpcChartValidator.ValidateJudgeLineHierarchy(kpcChart.JudgeLineList);
                _chart.CommitChart(kpcChart);
            }

            _log.Information(log_tool_completed, toolId);

            // 返回工具页面并显示成功对话框
            _navigation.ShowTool();
            MessageDialog.ShowSuccess(
                _window,
                tool_success_title,
                string.Format(log_tool_completed, toolId)
            );
        }
        catch (OperationCanceledException)
        {
            _log.Information(status_export_cancelled);
            _navigation.ShowTool();
        }
        catch (Exception ex)
        {
            _log.Error(ex, log_tool_failed);

            // 返回工具页面并显示失败对话框
            _navigation.ShowTool();
            MessageDialog.ShowError(
                _window,
                tool_error_title,
                string.Format(status_error_with_log, ex.Message, _logService.CurrentLogFile)
            );
        }
        finally
        {
            toolVm.IsProcessing = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void RunTool(
        string toolId,
        KaedePhi.Core.KaedePhi.Chart chart,
        IProgress<ToolProgress> progress,
        CancellationToken ct
    )
    {
        var toolVm = _navigation.Tool;
        switch (toolId)
        {
            case "unbind":
                _chart.RunFatherUnbind(
                    chart,
                    toolVm.Precision,
                    toolVm.Tolerance,
                    toolVm.ClassicMode,
                    toolVm.DisableCompress,
                    toolVm.MergeTolerance,
                    progress,
                    ct
                );
                break;
            case "layermerge":
                _chart.RunLayerMerge(
                    chart,
                    toolVm.Precision,
                    toolVm.Tolerance,
                    toolVm.ClassicMode,
                    toolVm.DisableCompress,
                    progress,
                    ct
                );
                break;
            case "cut":
                _chart.RunCutEvent(
                    chart,
                    toolVm.Precision,
                    toolVm.Tolerance,
                    toolVm.DisableCompress,
                    progress,
                    ct
                );
                break;
            case "fit":
                _chart.RunFitEvent(chart, toolVm.Tolerance, progress, ct);
                break;
            case "render":
                var renderPaths = _chart.RunRender(
                    chart,
                    toolVm.RenderOutputDir,
                    new KpcRenderOptions
                    {
                        PixelsPerBeat = toolVm.PixelsPerBeat,
                        ChannelWidth = toolVm.ChannelWidth,
                        SamplesPerEvent = toolVm.SamplesPerEvent,
                        BeatSubdivisions = toolVm.BeatSubdivisions,
                        RangePaddingRatio = _config.Config.Render.RangePaddingRatio,
                        RangeSamplesPerEvent = _config.Config.Render.RangeSamplesPerEvent,
                        SegmentGroupTolerance = _config.Config.Render.SegmentGroupTolerance,
                        MinValueRangeHalf = _config.Config.Render.MinValueRangeHalf,
                        MinValueRangeHalfRatio = _config.Config.Render.MinValueRangeHalfRatio,
                    },
                    progress,
                    ct
                );
                foreach (var path in renderPaths)
                    _log.Information(log_tool_render_output, path);
                break;
        }
    }

    private void OnCancelProcessing()
    {
        if (_cts is not { IsCancellationRequested: false })
            return;
        _cts.Cancel();
        _log.Information(status_export_cancelled);
    }
}
