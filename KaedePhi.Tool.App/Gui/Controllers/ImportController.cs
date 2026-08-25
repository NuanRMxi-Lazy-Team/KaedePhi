using Avalonia.Controls;
using KaedePhi.Tool.App.Gui.Services;
using KaedePhi.Tool.App.Gui.Views;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiChain.Model;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using Serilog;
using static KaedePhi.Tool.Localization.GuiLocalizationString;

namespace KaedePhi.Tool.App.Gui.Controllers;

internal sealed class ImportController
{
    private readonly GuiNavigationService _navigation;
    private readonly GuiChartService _chart;
    private readonly LogService _logService;
    private readonly ILogger _log;
    private readonly Window _window;

    private CancellationTokenSource? _cts;
    private bool _isFileProcessing;
    private string? _pendingFilePath;
    private bool _pendingUseStream;

    public ImportController(
        GuiNavigationService navigation,
        GuiChartService chart,
        LogService logService,
        Window window
    )
    {
        _navigation = navigation;
        _chart = chart;
        _logService = logService;
        _log = logService.ForContext<ImportController>();
        _window = window;

        _navigation.Import.FileSelected += OnFileSelected;
        _navigation.Import.RequestCancelLoading += OnCancelImport;
        _navigation.ImportOptions.RequestConfirm += OnImportOptionsConfirm;
        _navigation.ImportOptions.RequestCancel += ReturnToImport;
        _navigation.ImportOptions.RequestCancelImport += OnCancelImport;
    }

    public void Cancel()
    {
        if (_cts is { IsCancellationRequested: false })
            _cts.Cancel();
    }

    public void ReturnToImport()
    {
        if (_isFileProcessing)
            return;
        _pendingFilePath = null;
        _navigation.ShowImport();
    }

    private async void OnFileSelected(string filePath, bool useStream)
    {
        if (_isFileProcessing)
            return;
        _isFileProcessing = true;
        _navigation.Import.IsLoading = true;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            await Task.Yield();

            // 先检测格式
            var detectedType = await Task.Run(
                () => _chart.DetectChartTypeAsync(filePath, useStream, ct),
                ct
            );

            // 检查是否需要显示导入选项
            if (detectedType is ChartType.PhiEdit or ChartType.PhiChain)
            {
                // 保存待处理的文件信息
                _pendingFilePath = filePath;
                _pendingUseStream = useStream;
                _navigation.ShowImportOptions(detectedType, Path.GetFileName(filePath));
            }
            else
            {
                // 不需要选项，直接加载
                await LoadChartWithOptions(filePath, useStream, detectedType, null, ct);
            }
        }
        catch (OperationCanceledException)
        {
            _log.Information(status_export_cancelled);
            _navigation.ShowImport();
        }
        catch (Exception ex)
        {
            _log.Error(ex, log_load_failed);
            MessageDialog.ShowError(
                _window,
                load_error_title,
                string.Format(status_error_with_log, ex.Message, _logService.CurrentLogFile)
            );
        }
        finally
        {
            _isFileProcessing = false;
            _navigation.Import.IsLoading = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async void OnImportOptionsConfirm()
    {
        if (_pendingFilePath == null || _isFileProcessing)
            return;

        _isFileProcessing = true;
        _navigation.ImportOptions.IsLoading = true;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            await Task.Yield();

            var detectedType = _navigation.ImportOptions.DetectedFormat;
            var importOptions = BuildImportOptions(detectedType);
            await LoadChartWithOptions(
                _pendingFilePath,
                _pendingUseStream,
                detectedType,
                importOptions,
                ct
            );
        }
        catch (OperationCanceledException)
        {
            _log.Information(status_export_cancelled);
            _navigation.ShowImport();
        }
        catch (Exception ex)
        {
            _log.Error(ex, log_load_failed);
            MessageDialog.ShowError(
                _window,
                load_error_title,
                string.Format(status_error_with_log, ex.Message, _logService.CurrentLogFile)
            );
            _navigation.ShowImport();
        }
        finally
        {
            _isFileProcessing = false;
            _navigation.ImportOptions.IsLoading = false;
            _pendingFilePath = null;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnCancelImport()
    {
        Cancel();
    }

    private async Task LoadChartWithOptions(
        string filePath,
        bool useStream,
        ChartType detectedType,
        object? importOptions,
        CancellationToken ct
    )
    {
        await Task.Run(() => _chart.LoadChartAsync(filePath, useStream, ct, importOptions), ct);
        ct.ThrowIfCancellationRequested();
        _navigation.ShowToolForChart(filePath, detectedType);
    }

    private object? BuildImportOptions(ChartType detectedType)
    {
        var vm = _navigation.ImportOptions;
        return detectedType switch
        {
            ChartType.PhiEdit => new PhiEditToKpcConvertOptions
            {
                FrameDurationBeat = 1d / vm.PeFrameDurationBeat,
                TrailingBeatPadding = 1d / vm.PeTrailingBeatPadding,
            },
            ChartType.PhiChain => new PhiChainToKpcConvertOptions
            {
                UnsupportedEasingPrecision = vm.PhiChainUnsupportedEasingPrecision,
            },
            _ => null,
        };
    }
}
