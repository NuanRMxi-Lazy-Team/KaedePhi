using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using KaedePhi.Tool.App.Gui.ViewModels;
using static KaedePhi.Tool.Localization.GuiLocalizationString;

namespace KaedePhi.Tool.App.Gui.Views;

public partial class ToolPage : UserControl
{
    public ToolPage()
    {
        InitializeComponent();
    }

    private void OnRunClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ToolViewModel vm)
            vm.OnRunClicked();
    }

    private void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ToolViewModel vm)
            vm.OnExportClicked();
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ToolViewModel vm)
            vm.OnSettingsClicked();
    }

    private async void OnBrowseOutputDirClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ToolViewModel vm)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        var dirs = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = render_output_dir_picker_title,
                AllowMultiple = false,
            }
        );

        if (dirs.Count > 0)
        {
            var path = dirs[0].TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
                vm.RenderOutputDir = path;
        }
    }

    private void OnReturnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ToolViewModel vm)
            vm.OnReturnToImportClicked();
    }
}
