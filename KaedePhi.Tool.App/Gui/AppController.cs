using Avalonia.Controls;
using KaedePhi.Tool.App.Config;
using KaedePhi.Tool.App.Gui.Controllers;
using KaedePhi.Tool.App.Gui.Services;
using KaedePhi.Tool.App.Gui.ViewModels;

namespace KaedePhi.Tool.App.Gui;

internal sealed class AppController
{
    private readonly GuiNavigationService _navigation;
    private readonly ImportController _importController;
    private readonly ToolController _toolController;
    private readonly ExportController _exportController;

    public AppController(
        MainViewModel main,
        GuiChartService chart,
        LogService logService,
        AppConfigService config,
        Window window
    )
    {
        _navigation = new GuiNavigationService(main, chart, logService, config);
        _importController = new ImportController(_navigation, chart, logService, window);
        _toolController = new ToolController(
            _navigation,
            chart,
            logService,
            config,
            window,
            _importController.ReturnToImport
        );
        _exportController = new ExportController(
            _navigation,
            chart,
            logService,
            window,
            _importController.ReturnToImport
        );

        window.Closing += OnWindowClosing;
    }

    public void Initialize()
    {
        _navigation.ShowImport();
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        _importController.Cancel();
        _toolController.Cancel();
        _exportController.Cancel();
    }
}
