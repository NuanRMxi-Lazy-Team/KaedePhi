using Avalonia.Controls;
using Avalonia.Platform.Storage;
using KaedePhi.Tool.App.Gui.Services;
using KaedePhi.Tool.App.Gui.ViewModels;
using KaedePhi.Tool.App.Gui.Views;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.PhiChain.Model;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Converter.PhiFans.Model;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using Serilog;
using static KaedePhi.Tool.Localization.GuiLocalizationString;

namespace KaedePhi.Tool.App.Gui.Controllers;

internal sealed class ExportController
{
    private readonly GuiNavigationService _navigation;
    private readonly GuiChartService _chart;
    private readonly LogService _logService;
    private readonly ILogger _log;
    private readonly Window _window;

    private CancellationTokenSource? _cts;

    public ExportController(
        GuiNavigationService navigation,
        GuiChartService chart,
        LogService logService,
        Window window,
        Action returnToImport
    )
    {
        _navigation = navigation;
        _chart = chart;
        _logService = logService;
        _log = logService.ForContext<ExportController>();
        _window = window;

        _navigation.Export.RequestExport += OnExportExecute;
        _navigation.Export.RequestReturnToImport += returnToImport;
        _navigation.Export.RequestCancelExport += OnCancelExport;
    }

    public void Cancel()
    {
        if (_cts is { IsCancellationRequested: false })
            _cts.Cancel();
    }

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
        var exportVm = _navigation.Export;
        if (exportVm.IsExporting)
            return;

        try
        {
            var topLevel = TopLevel.GetTopLevel(_window);
            if (topLevel == null)
            {
                exportVm.StatusText = status_cannot_access_picker;
                return;
            }

            var targetFormat = exportVm.SelectedFormat;
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
                if (string.IsNullOrEmpty(outputPath))
                    return;
                // 用户已选择文件，此时再显示导出动画
                exportVm.IsExporting = true;
                exportVm.StatusText = status_exporting;
                _cts = new CancellationTokenSource();
                var cts = _cts;
                var ct = cts.Token;

                // 若 OS 未自动附加扩展名，则手动补全
                var expectedExt = $".{ext}";
                if (!outputPath.EndsWith(expectedExt, StringComparison.OrdinalIgnoreCase))
                    outputPath += expectedExt;
                if (
                    _chart.SourceFilePath is not null
                    && string.Equals(
                        Path.GetFullPath(_chart.SourceFilePath),
                        Path.GetFullPath(outputPath),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    exportVm.StatusText = export_same_file_error;
                    return;
                }

                // 在后台线程执行耗时的导出操作，避免阻塞 UI
                var exportOptions = BuildExportOptions(targetFormat, exportVm);
                var useStream = exportVm.UseStream;
                var indented = exportVm.IndentedOutput;

                await Task.Run(
                    () => _chart.ExportChartAsync(
                        targetFormat,
                        outputPath,
                        useStream,
                        indented,
                        exportOptions,
                        ct
                    ),
                    ct
                );

                exportVm.StatusText = string.Format(status_exported_to, outputPath);
                _log.Information(log_export_done);
                MessageDialog.ShowSuccess(
                    _window,
                    export_success_title,
                    string.Format(status_exported_to, outputPath)
                );
            }
            else
            {
                exportVm.StatusText = status_export_cancelled;
                _log.Information(log_export_cancelled);
            }
        }
        catch (OperationCanceledException)
        {
            exportVm.StatusText = status_export_cancelled;
            _log.Information(log_export_cancelled);
        }
        catch (Exception ex)
        {
            _log.Error(ex, log_export_failed);
            exportVm.StatusText = string.Format(
                status_error_with_log,
                ex.Message,
                _logService.CurrentLogFile
            );
            MessageDialog.ShowError(
                _window,
                export_error_title,
                string.Format(status_error_with_log, ex.Message, _logService.CurrentLogFile)
            );
        }
        finally
        {
            exportVm.IsExporting = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnCancelExport()
    {
        if (_cts is not { IsCancellationRequested: false })
            return;
        _cts.Cancel();
        _navigation.Export.StatusText = status_export_cancelled;
        _log.Information(log_export_cancelled);
    }

    private static object? BuildExportOptions(ChartType targetFormat, ExportViewModel vm)
    {
        return targetFormat switch
        {
            ChartType.PhiEdit => BuildPhiEditOptions(vm),
            ChartType.PhigrosV3 => BuildPhigrosV3Options(vm),
            ChartType.PhiChain => BuildPhiChainOptions(vm),
            ChartType.RePhiEdit => BuildRePhiEditOptions(vm),
            ChartType.PhiFans => BuildPhiFansOptions(vm),
            _ => null,
        };
    }

    private static KpcToPhigrosV3ConvertOptions BuildPhigrosV3Options(ExportViewModel vm) =>
        new()
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
            },
            Speed = new KpcToPhigrosV3ConvertOptions.SpeedOptions
            {
                CutPrecision = vm.PhigrosSpeedCutPrecision,
            },
            FatherLineUnbind = new KpcToPhigrosV3ConvertOptions.FatherLineUnbindOptions
            {
                Precision = vm.UnbindPrecision,
                Tolerance = vm.UnbindTolerance,
                MergeTolerance = vm.UnbindMergeTolerance,
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

    private static KpcToPhiEditConvertOptions BuildPhiEditOptions(ExportViewModel vm) =>
        new()
        {
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
            },
            FatherLineUnbind = new KpcToPhiEditConvertOptions.FatherLineUnbindOptions
            {
                Precision = vm.UnbindPrecision,
                Tolerance = vm.UnbindTolerance,
                MergeTolerance = vm.UnbindMergeTolerance,
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

    private static KpcToPhiChainConvertOptions BuildPhiChainOptions(ExportViewModel vm) =>
        new()
        {
            UnbindNonRotatingChildren = vm.PhiChainUnbindNonRotatingChildren,
            UnbindPrecision = vm.PhiChainUnbindPrecision,
            UnbindTolerance = vm.PhiChainUnbindTolerance,
            UnbindMergeTolerance = vm.PhiChainUnbindMergeTolerance,
            UnbindClassicMode = vm.PhiChainUnbindClassicMode,
            MultiLayerMergePrecision = vm.PhiChainMultiLayerMergePrecision,
            MultiLayerMergeTolerance = vm.PhiChainMultiLayerMergeTolerance,
            MultiLayerMergeClassicMode = vm.PhiChainMultiLayerMergeClassicMode,
            EasingCutPrecision = vm.PhiChainEasingCutPrecision,
        };

    private static ConvertOption BuildRePhiEditOptions(ExportViewModel vm) =>
        new()
        {
            Cutting = new ConvertOption.CuttingOptions
            {
                UnsupportedEasingPrecision = vm.RePhiEditUnsupportedEasingPrecision,
            },
        };

    private static KpcToPhiFansConvertOptions BuildPhiFansOptions(ExportViewModel vm) =>
        new()
        {
            Cutting = new KpcToPhiFansConvertOptions.CuttingOptions
            {
                UnsupportedEasingPrecision = vm.PhiFansUnsupportedEasingPrecision,
            },
            DiscontinuityBeatPrecision = vm.PhiFansDiscontinuityBeatPrecision,
            MultiLayerMerge = new KpcToPhiFansConvertOptions.MultiLayerMergeOptions
            {
                Precision = vm.MultiLayerMergePrecision,
                Tolerance = vm.MultiLayerMergeTolerance,
                ClassicMode = vm.MultiLayerMergeClassicMode,
                Compress = vm.MultiLayerMergeCompress,
            },
        };
}