using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiChain.Model;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using KaedePhi.Tool.App.Gui.Services;
using KaedePhi.Tool.App.Gui.ViewModels;
using KaedePhi.Tool.App.Gui.Views;
using Serilog;
using static KaedePhi.Tool.Localization.GuiLocalizationString;

namespace KaedePhi.Tool.App.Gui;

internal sealed class AppController
{
    private readonly MainViewModel _main;
    private readonly GuiChartService _chart;
    private readonly ILogger _log;
    private readonly LogService _logService;
    private readonly ConfigService _config;
    private readonly Window _window;

    private readonly ImportViewModel _importVm;
    private readonly ImportOptionsViewModel _importOptionsVm;
    private readonly ToolViewModel _toolVm;
    private readonly ExportViewModel _exportVm;
    private readonly ProcessingViewModel _processingVm;
    private readonly SettingsViewModel _settingsVm;

    private CancellationTokenSource? _cts;
    private bool _isFileProcessing;
    private string? _pendingFilePath;
    private bool _pendingUseStream;

    public AppController(
        MainViewModel main,
        GuiChartService chart,
        LogService logService,
        ConfigService config,
        Window window
    )
    {
        _main = main;
        _chart = chart;
        _logService = logService;
        _log = logService.ForContext<AppController>();
        _config = config;
        _window = window;

        _importVm = new ImportViewModel();
        _importOptionsVm = new ImportOptionsViewModel();
        _toolVm = new ToolViewModel();
        _exportVm = new ExportViewModel();
        _processingVm = new ProcessingViewModel();
        _settingsVm = new SettingsViewModel(config);

        WireEvents();
    }

    public void Initialize()
    {
        NavigateToImport();
    }

    private void WireEvents()
    {
        _importVm.FileSelected += OnFileSelected;
        _importOptionsVm.RequestConfirm += OnImportOptionsConfirm;
        _importOptionsVm.RequestCancel += OnReturnToImport;
        _importOptionsVm.RequestCancelImport += OnCancelImport;
        _toolVm.RequestRun += OnToolRun;
        _toolVm.RequestExport += OnToolExport;
        _toolVm.RequestSettings += NavigateToSettings;
        _toolVm.RequestReturnToImport += OnReturnToImport;
        _toolVm.PropertyChanged += OnToolVmPropertyChanged;
        _exportVm.RequestExport += OnExportExecute;
        _exportVm.RequestReturnToImport += OnReturnToImport;
        _exportVm.RequestCancelExport += OnCancelExport;
        _processingVm.RequestReturnToTools += NavigateToTool;
        _processingVm.RequestReturnToImport += OnReturnToImport;
        _processingVm.RequestGoToExport += NavigateToExport;
        _settingsVm.RequestReturnToTools += OnReturnFromSettings;
    }

    private void OnToolVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ToolViewModel.SelectedTool) && _toolVm.SelectedTool != null)
            _toolVm.ApplyConfigDefaults(_config.Config);
    }

    private void NavigateToImport()
    {
        _chart.Clear();
        _importVm.UseStream = false;
        _main.CurrentPage = _importVm;
        _log.Information(log_navigate_import);
    }

    private void NavigateToTool()
    {
        _toolVm.StatusText = string.Empty;
        _main.CurrentPage = _toolVm;
    }

    private void NavigateToExport()
    {
        // 直接使用导入时已检测的格式，避免重复加载导致死锁
        _exportVm.SourceFormat = _toolVm.SourceChartType;
        _exportVm.SelectedFormat = ChartType.RePhiEdit;
        _exportVm.UseStream = false;
        _exportVm.IndentedOutput = false;
        _exportVm.StatusText = string.Empty;
        _exportVm.IsExporting = false;
        _exportVm.ApplyConversionDefaults(_config.Config.Convert);
        _main.CurrentPage = _exportVm;
    }

    private void NavigateToSettings()
    {
        _settingsVm.StatusText = string.Empty;
        _main.CurrentPage = _settingsVm;
    }

    private void OnReturnFromSettings()
    {
        _toolVm.ApplyConfigDefaults(_config.Config);
        NavigateToTool();
    }

    private void NavigateToProcessing()
    {
        _processingVm.Progress = 0;
        _processingVm.CurrentStep = string.Empty;
        _processingVm.StatusMessage = string.Empty;
        _processingVm.IsCompleted = false;
        _processingVm.HasError = false;
        _processingVm.ErrorMessage = string.Empty;
        _processingVm.LogFilePath = string.Empty;
        _main.CurrentPage = _processingVm;
    }

    private void OnReturnToImport()
    {
        _pendingFilePath = null;
        NavigateToImport();
    }

    private async void OnFileSelected(string filePath, bool useStream)
    {
        if (_isFileProcessing)
            return;
        _isFileProcessing = true;
        _importVm.IsLoading = true;

        try
        {
            // 先检测格式
            var detectedType = _chart.DetectChartType(filePath, useStream);

            // 检查是否需要显示导入选项
            if (detectedType is ChartType.PhiEdit or ChartType.PhiChain)
            {
                // 保存待处理的文件信息
                _pendingFilePath = filePath;
                _pendingUseStream = useStream;

                // 导航到导入选项页面
                _importOptionsVm.DetectedFormat = detectedType;
                _importOptionsVm.FileName = Path.GetFileName(filePath);
                _main.CurrentPage = _importOptionsVm;
            }
            else
            {
                // 不需要选项，直接加载
                await LoadChartWithOptions(filePath, useStream, detectedType, null);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, log_load_failed);
            MessageDialog.ShowError(_window, load_error_title, ex.Message);
        }
        finally
        {
            _isFileProcessing = false;
            _importVm.IsLoading = false;
        }
    }

    private async void OnImportOptionsConfirm()
    {
        if (_pendingFilePath == null || _isFileProcessing)
            return;

        _isFileProcessing = true;
        _importOptionsVm.IsLoading = true;
        _cts = new CancellationTokenSource();

        try
        {
            var detectedType = _importOptionsVm.DetectedFormat;
            var importOptions = BuildImportOptions(detectedType);
            await LoadChartWithOptions(
                _pendingFilePath,
                _pendingUseStream,
                detectedType,
                importOptions,
                _cts.Token
            );
        }
        catch (OperationCanceledException)
        {
            _log.Information("Import cancelled by user");
            NavigateToImport();
        }
        catch (Exception ex)
        {
            _log.Error(ex, log_load_failed);
            MessageDialog.ShowError(_window, load_error_title, ex.Message);
            NavigateToImport();
        }
        finally
        {
            _isFileProcessing = false;
            _importOptionsVm.IsLoading = false;
            _pendingFilePath = null;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnCancelImport()
    {
        if (_cts is { IsCancellationRequested: false })
        {
            _cts.Cancel();
        }
    }

    private async Task LoadChartWithOptions(
        string filePath,
        bool useStream,
        ChartType detectedType,
        object? importOptions,
        CancellationToken ct = default
    )
    {
        await _chart.LoadChartAsync(filePath, useStream, ct, importOptions);

        _toolVm.CurrentFileName = Path.GetFileName(filePath);
        _toolVm.DetectedFormat = detectedType.ToString();
        _toolVm.SourceChartType = detectedType;
        _toolVm.SelectedTool = null;
        _toolVm.StatusText = string.Empty;

        NavigateToTool();
    }

    /// <summary>
    /// 根据检测到的格式构建导入选项
    /// </summary>
    private object? BuildImportOptions(ChartType detectedType)
    {
        return detectedType switch
        {
            ChartType.PhiEdit => new PhiEditToKpcConvertOptions
            {
                FrameDurationBeat = _importOptionsVm.PeFrameDurationBeat,
                SpeedConversionRatio = _importOptionsVm.PeSpeedConversionRatio,
                TrailingBeatPadding = _importOptionsVm.PeTrailingBeatPadding,
            },
            ChartType.PhiChain => new PhiChainToKpcConvertOptions
            {
                UnsupportedEasingPrecision = _importOptionsVm.PhiChainUnsupportedEasingPrecision,
            },
            _ => null,
        };
    }

    private async void OnToolRun()
    {
        if (_toolVm.SelectedTool == null || _toolVm.IsProcessing)
            return;
        if (_chart.CurrentChart == null)
            return;

        _toolVm.IsProcessing = true;
        _toolVm.StatusText = status_processing;
        _cts = new CancellationTokenSource();

        try
        {
            var toolId = _toolVm.SelectedTool.ToolId;
            NavigateToProcessing();

            // 直接使用内存中的 KPC 图表，无需加载和转换
            var kpcChart = _chart.CurrentChart;

            // 运行工具
            _processingVm.SetStep(0, string.Format(log_running_tool, toolId));
            var toolProgress = new Progress<ToolProgress>(p =>
            {
                var overall = p.OverallPercentage >= 0 ? p.OverallPercentage : p.Percentage;
                _processingVm.SetToolProgress(p.Percentage, overall, p.Detail);
            });
            await Task.Run(
                () =>
                {
                    switch (toolId)
                    {
                        case "unbind":
                            _chart.RunFatherUnbind(
                                kpcChart,
                                _toolVm.Precision,
                                _toolVm.Tolerance,
                                _toolVm.ClassicMode,
                                _toolVm.DisableCompress,
                                toolProgress
                            );
                            break;
                        case "layermerge":
                            _chart.RunLayerMerge(
                                kpcChart,
                                _toolVm.Precision,
                                _toolVm.Tolerance,
                                _toolVm.ClassicMode,
                                _toolVm.DisableCompress,
                                toolProgress
                            );
                            break;
                        case "cut":
                            _chart.RunCutEvent(
                                kpcChart,
                                _toolVm.Precision,
                                _toolVm.Tolerance,
                                _toolVm.DisableCompress,
                                toolProgress
                            );
                            break;
                        case "fit":
                            _chart.RunFitEvent(kpcChart, _toolVm.Tolerance, toolProgress);
                            break;
                        case "render":
                            var renderPaths = _chart.RunRender(
                                kpcChart,
                                _toolVm.PixelsPerBeat,
                                _toolVm.ChannelWidth,
                                _toolVm.SamplesPerEvent,
                                _toolVm.BeatSubdivisions,
                                toolProgress
                            );
                            foreach (var p in renderPaths)
                                _log.Information(log_tool_render_output, p);
                            break;
                    }
                },
                _cts.Token
            );

            _log.Information(log_tool_completed, toolId);

            // 返回工具页面并显示成功对话框
            NavigateToTool();
            MessageDialog.ShowSuccess(
                _window,
                tool_success_title,
                string.Format(log_tool_completed, toolId)
            );
        }
        catch (Exception ex)
        {
            _log.Error(ex, log_tool_failed);

            // 返回工具页面并显示失败对话框
            NavigateToTool();
            MessageDialog.ShowError(_window, tool_error_title, ex.Message);
        }
        finally
        {
            _toolVm.IsProcessing = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnToolExport()
    {
        NavigateToExport();
    }

    /// <summary>
    /// 根据导出格式返回 (扩展名不含点, 文件类型描述) 元组
    /// </summary>
    private static (string Extension, string TypeLabel) GetFormatFileInfo(ChartType format) =>
        format switch
        {
            ChartType.PhiEdit => ("pec", file_type_pe_chart),
            ChartType.RePhiEdit => ("json", file_type_rpe_json),
            ChartType.PhigrosV3 => ("json", file_type_phigros_json),
            ChartType.PhiFans => ("json", file_type_phifans_json),
            ChartType.PhiChain => ("json", file_type_phichain_json),
            _ => ("json", file_type_json),
        };

    private async void OnExportExecute()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(_window);
            if (topLevel == null)
            {
                _exportVm.StatusText = status_cannot_access_picker;
                return;
            }

            var targetFormat = _exportVm.SelectedFormat;
            var formatName = targetFormat.ToString();
            var (ext, typeLabel) = GetFormatFileInfo(targetFormat);

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = export_title,
                    SuggestedFileName = $"export_{formatName}.{ext}",
                    DefaultExtension = ext,
                    FileTypeChoices =
                    [
                        new FilePickerFileType(typeLabel) { Patterns = [$"*.{ext}"] },
                    ],
                }
            );

            if (file != null)
            {
                var outputPath = file.TryGetLocalPath();
                if (!string.IsNullOrEmpty(outputPath))
                {
                    // 用户已选择文件，此时再显示导出动画
                    _exportVm.IsExporting = true;
                    _exportVm.StatusText = status_exporting;
                    _cts = new CancellationTokenSource();

                    // 若 OS 未自动附加扩展名，则手动补全
                    var expectedExt = $".{ext}";
                    if (!outputPath.EndsWith(expectedExt, StringComparison.OrdinalIgnoreCase))
                        outputPath += expectedExt;

                    // 在后台线程执行耗时的导出操作，避免阻塞 UI
                    var exportOptions = BuildExportOptions(targetFormat);
                    var useStream = _exportVm.UseStream;
                    var indented = _exportVm.IndentedOutput;

                    await Task.Run(
                        async () =>
                        {
                            await _chart.ExportChartAsync(
                                targetFormat,
                                outputPath,
                                useStream,
                                indented,
                                exportOptions,
                                _cts.Token
                            );
                        },
                        _cts.Token
                    );

                    _exportVm.StatusText = string.Format(status_exported_to, outputPath);
                    _log.Information(log_export_done);
                    MessageDialog.ShowSuccess(
                        _window,
                        export_success_title,
                        string.Format(status_exported_to, outputPath)
                    );
                }
            }
            else
            {
                _exportVm.StatusText = status_export_cancelled;
                _log.Information(log_export_cancelled);
            }
        }
        catch (OperationCanceledException)
        {
            _exportVm.StatusText = status_export_cancelled;
            _log.Information(log_export_cancelled);
        }
        catch (Exception ex)
        {
            _log.Error(ex, log_export_failed);
            _exportVm.StatusText = string.Format(
                status_error_with_log,
                ex.Message,
                _logService.CurrentLogFile
            );
            MessageDialog.ShowError(_window, export_error_title, ex.Message);
        }
        finally
        {
            _exportVm.IsExporting = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnCancelExport()
    {
        if (_cts is { IsCancellationRequested: false })
        {
            _cts.Cancel();
            _exportVm.StatusText = status_export_cancelled;
            _log.Information(log_export_cancelled);
        }
    }

    private static KpcToPhigrosV3ConvertOptions BuildPhigrosOptions(ExportViewModel vm)
    {
        return new KpcToPhigrosV3ConvertOptions
        {
            DefaultBpm = vm.PhigrosDefaultBpm,
            Cutting = new KpcToPhigrosV3ConvertOptions.CuttingOptions
            {
                EasingPrecision = vm.PhigrosEasingPrecision,
                MisalignedXyEventPrecision = vm.PhigrosMisalignedXyEventPrecision,
            },
            Alpha = new KpcToPhigrosV3ConvertOptions.AlphaOptions
            {
                CutPrecision = vm.PhigrosAlphaCutPrecision,
                CutTolerance = vm.PhigrosAlphaCutTolerance,
            },
            Speed = new KpcToPhigrosV3ConvertOptions.SpeedOptions
            {
                CutPrecision = vm.PhigrosSpeedCutPrecision,
            },
            FatherLineUnbind = new KpcToPhigrosV3ConvertOptions.FatherLineUnbindOptions
            {
                Precision = vm.UnbindPrecision,
                Tolerance = vm.UnbindTolerance,
                ClassicMode = vm.UnbindClassicMode,
                Compress = vm.UnbindCompress,
            },
            MultiLayerMerge = new KpcToPhigrosV3ConvertOptions.MultiLayerMergeOptions
            {
                Precision = vm.MultiLayerMergePrecision,
                Tolerance = vm.MultiLayerMergeTolerance,
                ClassicMode = vm.MultiLayerMergeClassicMode,
                Compress = vm.MultiLayerMergeCompress,
            },
            LineFilter = new KpcToPhigrosV3ConvertOptions.LineFilterOptions
            {
                RemoveAttachUiLine = vm.RemoveAttachUiLine,
                RemoveTextureLine = vm.RemoveTextureLine,
            },
            NoteFilter = new KpcToPhigrosV3ConvertOptions.NoteFilterOptions
            {
                FilterFakeNotes = vm.FilterFakeNotes,
            },
            NegativeAlpha = new KpcToPhigrosV3ConvertOptions.NegativeAlphaOptions
            {
                Enabled = vm.NegativeAlphaElevation,
                ElevationStep = vm.NegativeAlphaStep,
            },
        };
    }

    private static KpcToPhiEditConvertOptions BuildPhiEditOptions(ExportViewModel vm)
    {
        return new KpcToPhiEditConvertOptions
        {
            SpeedConversionRatio = vm.PeSpeedConversionRatio,
            TrailingBeatPadding = vm.PeTrailingBeatPadding,
            Cutting = new KpcToPhiEditConvertOptions.CuttingOptions
            {
                UnsupportedEasingPrecision = vm.PeUnsupportedEasingPrecision,
                MisalignedXyEventPrecision = vm.PeMisalignedXyEventPrecision,
            },
            Alpha = new KpcToPhiEditConvertOptions.AlphaOptions
            {
                CutPrecision = vm.PeAlphaCutPrecision,
                CutTolerance = vm.PeAlphaCutTolerance,
            },
            Speed = new KpcToPhiEditConvertOptions.SpeedOptions
            {
                CutPrecision = vm.PeSpeedCutPrecision,
                CutTolerance = vm.PeSpeedCutTolerance,
            },
            FatherLineUnbind = new KpcToPhiEditConvertOptions.FatherLineUnbindOptions
            {
                Precision = vm.UnbindPrecision,
                Tolerance = vm.UnbindTolerance,
                ClassicMode = vm.UnbindClassicMode,
                Compress = vm.UnbindCompress,
            },
            MultiLayerMerge = new KpcToPhiEditConvertOptions.MultiLayerMergeOptions
            {
                Precision = vm.MultiLayerMergePrecision,
                Tolerance = vm.MultiLayerMergeTolerance,
                ClassicMode = vm.MultiLayerMergeClassicMode,
                Compress = vm.MultiLayerMergeCompress,
            },
            LineFilter = new KpcToPhiEditConvertOptions.LineFilterOptions
            {
                RemoveAttachUiLine = vm.RemoveAttachUiLine,
                RemoveTextureLine = vm.RemoveTextureLine,
            },
        };
    }

    private static KpcToPhiChainConvertOptions BuildPhiChainOptions(ExportViewModel vm)
    {
        return new KpcToPhiChainConvertOptions
        {
            UnbindNonRotatingChildren = vm.PhiChainUnbindNonRotatingChildren,
            UnbindPrecision = vm.PhiChainUnbindPrecision,
            UnbindTolerance = vm.PhiChainUnbindTolerance,
            UnbindClassicMode = vm.PhiChainUnbindClassicMode,
            MultiLayerMergePrecision = vm.PhiChainMultiLayerMergePrecision,
            MultiLayerMergeTolerance = vm.PhiChainMultiLayerMergeTolerance,
            MultiLayerMergeClassicMode = vm.PhiChainMultiLayerMergeClassicMode,
            EasingCutPrecision = vm.PhiChainEasingCutPrecision,
            EasingCutCompress = vm.PhiChainEasingCutCompress,
            EasingCutTolerance = vm.PhiChainEasingCutTolerance,
        };
    }

    private static ConvertOption BuildRePhiEditOptions(ExportViewModel vm)
    {
        return new ConvertOption
        {
            Cutting = new ConvertOption.CuttingOptions
            {
                UnsupportedEasingPrecision = vm.RePhiEditUnsupportedEasingPrecision,
            },
        };
    }

    /// <summary>
    /// 根据目标格式构建导出选项
    /// </summary>
    private object? BuildExportOptions(ChartType targetFormat)
    {
        return targetFormat switch
        {
            ChartType.PhiEdit => BuildPhiEditOptions(_exportVm),
            ChartType.PhigrosV3 => BuildPhigrosOptions(_exportVm),
            ChartType.PhiChain => BuildPhiChainOptions(_exportVm),
            ChartType.RePhiEdit => BuildRePhiEditOptions(_exportVm),
            _ => null,
        };
    }
}
