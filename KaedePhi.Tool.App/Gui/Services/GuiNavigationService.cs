using KaedePhi.Tool.App.Config;
using KaedePhi.Tool.App.Gui.ViewModels;
using KaedePhi.Tool.Common;
using Serilog;
using static KaedePhi.Tool.Localization.GuiLocalizationString;

namespace KaedePhi.Tool.App.Gui.Services;

internal sealed class GuiNavigationService
{
    private readonly MainViewModel _main;
    private readonly GuiChartService _chart;
    private readonly AppConfigService _config;
    private readonly ILogger _log;

    public ImportViewModel Import { get; } = new();
    public ImportOptionsViewModel ImportOptions { get; } = new();
    public ToolViewModel Tool { get; } = new();
    public ExportViewModel Export { get; } = new();
    public ProcessingViewModel Processing { get; } = new();
    public SettingsViewModel Settings { get; }

    public GuiNavigationService(
        MainViewModel main,
        GuiChartService chart,
        LogService logService,
        AppConfigService config
    )
    {
        _main = main;
        _chart = chart;
        _config = config;
        _log = logService.ForContext<GuiNavigationService>();
        Settings = new SettingsViewModel(config);
        Settings.RequestReturnToTools += ReturnFromSettings;
    }

    public void ShowImport()
    {
        _chart.Clear();
        Import.UseStream = false;
        _main.CurrentPage = Import;
        _log.Information(log_navigate_import);
    }

    public void ShowImportOptions(ChartType detectedType, string fileName)
    {
        ImportOptions.DetectedFormat = detectedType;
        ImportOptions.FileName = fileName;
        _main.CurrentPage = ImportOptions;
    }

    public void ShowTool()
    {
        Tool.StatusText = string.Empty;
        _main.CurrentPage = Tool;
    }

    public void ShowToolForChart(string filePath, ChartType detectedType)
    {
        Tool.CurrentFileName = Path.GetFileName(filePath);
        Tool.DetectedFormat = detectedType.ToString();
        Tool.SourceChartType = detectedType;
        Tool.RenderOutputDir = Path.Combine(
            Path.GetDirectoryName(filePath) ?? string.Empty,
            "RenderLayer"
        );
        Tool.SelectedTool = null;
        ShowTool();
    }

    public void ShowExport()
    {
        Export.SourceFormat = Tool.SourceChartType;
        Export.SelectedFormat = ChartType.RePhiEdit;
        Export.UseStream = false;
        Export.IndentedOutput = false;
        Export.StatusText = string.Empty;
        Export.IsExporting = false;
        Export.ApplyConversionDefaults(_config.Config.Convert);
        _main.CurrentPage = Export;
    }

    public void ShowProcessing()
    {
        Processing.Progress = 0;
        Processing.CurrentStep = string.Empty;
        Processing.StatusMessage = string.Empty;
        Processing.IsCompleted = false;
        Processing.HasError = false;
        Processing.ErrorMessage = string.Empty;
        Processing.LogFilePath = string.Empty;
        _main.CurrentPage = Processing;
    }

    public void ShowSettings()
    {
        Settings.StatusText = string.Empty;
        _main.CurrentPage = Settings;
    }

    public void ReturnFromSettings()
    {
        Tool.ApplyConfigDefaults(_config.Config);
        ShowTool();
    }
}
