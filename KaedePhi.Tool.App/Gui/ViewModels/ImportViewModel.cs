using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KaedePhi.Tool.App.Gui.ViewModels;

public sealed class ImportViewModel : INotifyPropertyChanged
{
    private bool _useStream;
    private bool _isLoading;

    public bool UseStream
    {
        get => _useStream;
        set
        {
            _useStream = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public event Action<string, bool>? FileSelected;

    public void OnFileSelected(string filePath)
    {
        FileSelected?.Invoke(filePath, UseStream);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
