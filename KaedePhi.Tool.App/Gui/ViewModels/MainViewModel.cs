using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using KaedePhi.Tool.App.Gui.Models;
using KaedePhi.Tool.App.Gui.Services;
using KaedePhi.Tool.Localization;
using static KaedePhi.Tool.Localization.GuiLocalizationString;

namespace KaedePhi.Tool.App.Gui.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly BackgroundSceneService _backgroundScene;
    private CancellationTokenSource? _backgroundRefreshCancellation = new();
    private Task? _automaticBackgroundRefresh;
    private BackgroundImageResource? _currentBackground;
    private bool _backgroundStopped;
    private bool _isSavingBackground;
    private bool _hasChart;

    public MainViewModel()
        : this(new BackgroundSceneService()) { }

    internal MainViewModel(BackgroundSceneService backgroundScene)
    {
        _backgroundScene = backgroundScene;
    }

    public bool IsSidebarExpanded
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SidebarWidth));
            OnPropertyChanged(nameof(SidebarToggleIcon));
            OnPropertyChanged(nameof(SidebarToggleDescription));
        }
    } = true;

    public double SidebarWidth => IsSidebarExpanded ? 236 : 72;
    public string SidebarToggleIcon => IsSidebarExpanded ? "\uf053" : "\uf054";

    public string SidebarToggleDescription =>
        IsSidebarExpanded ? GuiUiText.ui_sidebar_collapse : GuiUiText.ui_sidebar_expand;

    public Bitmap? BackgroundImage
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasBackgroundImage));
        }
    }

    public bool HasBackgroundImage => BackgroundImage != null;

    public Bitmap? PendingBackgroundImage
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPendingBackgroundImage));
        }
    }

    public bool HasPendingBackgroundImage => PendingBackgroundImage != null;

    public bool IsRefreshingBackground
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public string BackgroundMessage
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasBackgroundMessage));
        }
    } = string.Empty;

    public bool HasBackgroundMessage => !string.IsNullOrEmpty(BackgroundMessage);

    internal event Func<CancellationToken, Task>? BackgroundTransitionRequested;

    public object? CurrentPage
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public string CurrentPageKey
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsImportActive));
            OnPropertyChanged(nameof(IsToolActive));
            OnPropertyChanged(nameof(IsExportActive));
            OnPropertyChanged(nameof(IsSettingsActive));
        }
    } = "import";

    public string CurrentPageTitle
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = import_button;

    public string CurrentPageDescription
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = app_subtitle;

    public int CurrentStep
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 1;

    public string CurrentFileName
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChart));
            OnPropertyChanged(nameof(CanOpenTools));
            OnPropertyChanged(nameof(CanOpenExport));
        }
    } = string.Empty;

    public string CurrentFormat
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public bool HasChart => _hasChart;
    public bool CanOpenTools => HasChart;
    public bool CanOpenExport => HasChart;

    public bool IsImportActive => CurrentPageKey == "import" || CurrentPageKey == "options";
    public bool IsToolActive => CurrentPageKey is "tool" or "processing";
    public bool IsExportActive => CurrentPageKey == "export";
    public bool IsSettingsActive => CurrentPageKey == "settings";

    public event Action? RequestImport;
    public event Action? RequestTools;
    public event Action? RequestExport;
    public event Action? RequestSettings;

    public void SetShellState(
        string pageKey,
        string pageTitle,
        string pageDescription,
        int step,
        string? fileName = null,
        string? format = null,
        bool? hasChart = null
    )
    {
        CurrentPageKey = pageKey;
        CurrentPageTitle = pageTitle;
        CurrentPageDescription = pageDescription;
        CurrentStep = step;

        if (fileName != null)
            CurrentFileName = fileName;
        if (format != null)
            CurrentFormat = format;
        if (hasChart.HasValue)
        {
            _hasChart = hasChart.Value;
            OnPropertyChanged(nameof(HasChart));
            OnPropertyChanged(nameof(CanOpenTools));
            OnPropertyChanged(nameof(CanOpenExport));
        }
    }

    public void ClearChartContext()
    {
        _hasChart = false;
        CurrentFileName = string.Empty;
        CurrentFormat = string.Empty;
        OnPropertyChanged(nameof(HasChart));
        OnPropertyChanged(nameof(CanOpenTools));
        OnPropertyChanged(nameof(CanOpenExport));
    }

    public void OnImportNavigationClicked() => RequestImport?.Invoke();

    public void OnToolsNavigationClicked() => RequestTools?.Invoke();

    public void OnExportNavigationClicked() => RequestExport?.Invoke();

    public void OnSettingsNavigationClicked() => RequestSettings?.Invoke();

    public void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

    public void StartBackgroundRefresh()
    {
        if (_backgroundStopped || _automaticBackgroundRefresh != null)
            return;

        _automaticBackgroundRefresh = RefreshBackgroundAutomaticallyAsync(
            _backgroundRefreshCancellation!.Token
        );
    }

    public void StopBackgroundRefresh()
    {
        if (_backgroundStopped)
            return;

        _backgroundStopped = true;
        _backgroundRefreshCancellation?.Cancel();
        _backgroundRefreshCancellation?.Dispose();
        _backgroundRefreshCancellation = null;
        PendingBackgroundImage = null;
        BackgroundImage = null;
        _currentBackground?.Dispose();
        _currentBackground = null;
    }

    public async Task<bool> RefreshBackgroundOnceAsync()
    {
        if (_backgroundStopped || IsRefreshingBackground)
            return false;

        var cancellationToken = _backgroundRefreshCancellation!.Token;
        IsRefreshingBackground = true;
        BackgroundMessage = GuiUiText.ui_background_refreshing;
        BackgroundImageResource? incoming = null;
        try
        {
            incoming = await _backgroundScene.FetchResourceAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (incoming == null)
            {
                BackgroundMessage = GuiUiText.ui_background_refresh_failed;
                return false;
            }

            PendingBackgroundImage = incoming.Image;
            if (BackgroundTransitionRequested is { } transition)
                await transition(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var previous = _currentBackground;
            _currentBackground = incoming;
            incoming = null;
            BackgroundImage = _currentBackground.Image;
            PendingBackgroundImage = null;
            previous?.Dispose();
            BackgroundMessage = string.Empty;
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception)
        {
            BackgroundMessage = GuiUiText.ui_background_refresh_failed;
            return false;
        }
        finally
        {
            PendingBackgroundImage = null;
            incoming?.Dispose();
            IsRefreshingBackground = false;
        }
    }

    public async Task<string?> SaveBackgroundAsync(string? directory = null)
    {
        if (_backgroundStopped || _isSavingBackground)
            return null;
        if (_currentBackground == null)
        {
            BackgroundMessage = GuiUiText.ui_background_unavailable;
            return null;
        }

        var cancellationToken = _backgroundRefreshCancellation!.Token;
        // 保存原图数据的快照，允许刷新完成后释放旧位图而不影响正在写入的文件。
        var data = _currentBackground.ImageData;
        var extension = _currentBackground.Extension;
        _isSavingBackground = true;
        try
        {
            var path = await BackgroundSceneService.SaveAsync(
                data,
                extension,
                cancellationToken,
                directory
            );
            BackgroundMessage = string.Format(GuiUiText.ui_background_saved, path);
            return path;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception)
        {
            BackgroundMessage = GuiUiText.ui_background_save_failed;
            return null;
        }
        finally
        {
            _isSavingBackground = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private async Task RefreshBackgroundAutomaticallyAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await RefreshBackgroundOnceAsync();

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(30), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
