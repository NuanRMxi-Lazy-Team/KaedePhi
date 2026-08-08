using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using KaedePhi.Tool.App.Config;
using KaedePhi.Tool.App.Gui;
using KaedePhi.Tool.App.Gui.Services;
using KaedePhi.Tool.App.Gui.ViewModels;
using Serilog;
using static KaedePhi.Tool.Localization.GuiLocalizationString;

namespace KaedePhi.Tool.App;

public partial class App : Application
{
    internal static AppConfigService ConfigService { get; } = AppConfigService.Instance;
    internal static LogService LogService { get; } = new(ConfigService.Config.MaxLogFiles);

    internal static GuiChartService ChartService { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            LogService.StartSession();

            ChartService = new GuiChartService(LogService);

            Log.ForContext<App>().Information(log_app_starting);

            var mainVm = new MainViewModel();
            var mainWindow = new MainWindow { DataContext = mainVm };

            var controller = new AppController(
                mainVm,
                ChartService,
                LogService,
                ConfigService,
                mainWindow
            );
            controller.Initialize();

            desktop.MainWindow = mainWindow;
            desktop.Exit += (_, _) =>
            {
                Log.ForContext<App>().Information(log_shutdown);
                ChartService.Clear();
                LogService.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
