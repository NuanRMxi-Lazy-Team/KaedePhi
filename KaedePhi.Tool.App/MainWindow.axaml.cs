using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using KaedePhi.Tool.App.Gui.ViewModels;

namespace KaedePhi.Tool.App;

public partial class MainWindow : Window
{
    private MainViewModel? _backgroundViewModel;
    private int _backgroundSavePressCount;
    private long _lastBackgroundSavePress;
    private bool _isBackgroundSaveKeyDown;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(
            KeyDownEvent,
            OnBackgroundShortcutKeyDown,
            RoutingStrategies.Tunnel,
            handledEventsToo: true
        );
        AddHandler(
            KeyUpEvent,
            OnBackgroundShortcutKeyUp,
            RoutingStrategies.Tunnel,
            handledEventsToo: true
        );
        Deactivated += (_, _) =>
        {
            _backgroundSavePressCount = 0;
            _isBackgroundSaveKeyDown = false;
        };
#if !Release
        // 版本号的前景色由 XAML 中的 DynamicResource 按当前主题解析。
        var ver =
            Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? "unknown";
        VersionLabel.Text = $"v{ver}";
        VersionLabel.Opacity = 0.85;
#else
        // Release 构建显示程序集版本号，并使用控件默认前景色。
        VersionLabel.ClearValue(TextBlock.ForegroundProperty);
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionLabel.Text =
            version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v?";
#endif
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _backgroundViewModel = DataContext as MainViewModel;
        if (_backgroundViewModel == null)
            return;

        _backgroundViewModel.BackgroundTransitionRequested += AnimateBackgroundRevealAsync;
        _backgroundViewModel.StartBackgroundRefresh();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_backgroundViewModel != null)
        {
            _backgroundViewModel.StopBackgroundRefresh();
            _backgroundViewModel.BackgroundTransitionRequested -= AnimateBackgroundRevealAsync;
            _backgroundViewModel = null;
        }

        base.OnClosed(e);
    }

    private void OnImportNavigationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is Gui.ViewModels.MainViewModel vm)
            vm.OnImportNavigationClicked();
    }

    private void OnToolsNavigationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is Gui.ViewModels.MainViewModel vm)
            vm.OnToolsNavigationClicked();
    }

    private void OnExportNavigationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is Gui.ViewModels.MainViewModel vm)
            vm.OnExportNavigationClicked();
    }

    private void OnSettingsNavigationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is Gui.ViewModels.MainViewModel vm)
            vm.OnSettingsNavigationClicked();
    }

    private void OnSidebarToggleClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is Gui.ViewModels.MainViewModel vm)
            vm.ToggleSidebar();
    }

    private async void OnBackgroundRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.RefreshBackgroundOnceAsync();
    }

    private async Task AnimateBackgroundRevealAsync(CancellationToken cancellationToken)
    {
        if (!IsVisible || BackgroundCanvas.Bounds.Width <= 0 || BackgroundCanvas.Bounds.Height <= 0)
            return;

        var origin =
            BackgroundRefreshButton.TranslatePoint(
                new Point(
                    BackgroundRefreshButton.Bounds.Width / 2,
                    BackgroundRefreshButton.Bounds.Height / 2
                ),
                BackgroundCanvas
            ) ?? new Point(BackgroundCanvas.Bounds.Width / 2, BackgroundCanvas.Bounds.Height / 2);
        var circle = new EllipseGeometry
        {
            Center = origin,
            RadiusX = 0,
            RadiusY = 0,
        };
        BackgroundRevealLayer.Clip = circle;
        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        var started = Stopwatch.GetTimestamp();
        EventHandler tick = (_, _) =>
        {
            var progress = Math.Clamp(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds / 650,
                0,
                1
            );
            var width = BackgroundCanvas.Bounds.Width;
            var height = BackgroundCanvas.Bounds.Height;
            var farthestX = Math.Max(Math.Abs(origin.X), Math.Abs(width - origin.X));
            var farthestY = Math.Max(Math.Abs(origin.Y), Math.Abs(height - origin.Y));
            var radius =
                Math.Sqrt(farthestX * farthestX + farthestY * farthestY)
                * (1 - Math.Pow(1 - progress, 3));
            circle.RadiusX = radius;
            circle.RadiusY = radius;
            if (progress >= 1)
                completion.TrySetResult();
        };
        timer.Tick += tick;
        using var registration = cancellationToken.Register(() =>
            completion.TrySetCanceled(cancellationToken)
        );
        try
        {
            timer.Start();
            await completion.Task;
        }
        finally
        {
            timer.Stop();
            timer.Tick -= tick;
            BackgroundRevealLayer.Clip = null;
        }
    }

    private async void OnBackgroundShortcutKeyDown(object? sender, KeyEventArgs e)
    {
        var focused = FocusManager?.GetFocusedElement() as Visual;
        if (
            e.Key != Key.P
            || (e.KeyModifiers & ~KeyModifiers.Shift) != KeyModifiers.None
            || IsTextInput(focused)
            || IsTextInput(e.Source as Visual)
        )
        {
            _backgroundSavePressCount = 0;
            return;
        }

        e.Handled = true;
        if (_isBackgroundSaveKeyDown)
            return;

        _isBackgroundSaveKeyDown = true;
        var now = Stopwatch.GetTimestamp();
        if (Stopwatch.GetElapsedTime(_lastBackgroundSavePress, now) > TimeSpan.FromSeconds(2))
            _backgroundSavePressCount = 0;
        _lastBackgroundSavePress = now;
        if (++_backgroundSavePressCount < 5)
            return;

        _backgroundSavePressCount = 0;
        if (DataContext is MainViewModel vm)
            await vm.SaveBackgroundAsync();
    }

    private void OnBackgroundShortcutKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.P)
            _isBackgroundSaveKeyDown = false;
    }

    private static bool IsTextInput(Visual? visual) =>
        visual is TextBox or ComboBox or NumericUpDown
        || visual
            ?.GetVisualAncestors()
            .Any(parent => parent is TextBox or ComboBox or NumericUpDown) == true;
}
