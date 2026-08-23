using Avalonia.Controls;
using Avalonia.Interactivity;
using KaedePhi.Tool.App.Gui.ViewModels;

namespace KaedePhi.Tool.App.Gui.Views;

public partial class ProcessingPage : UserControl
{
    public ProcessingPage()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProcessingViewModel vm)
            vm.OnCancelClicked();
    }
}
