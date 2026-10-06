using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KaedePhi.Tool.App.Gui.ViewModels;

public sealed class ImportProgressViewModel : INotifyPropertyChanged
{
    private bool _isIndeterminate = true;
    private double _value;
    private string _percentageText = string.Empty;

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        private set
        {
            if (_isIndeterminate == value)
                return;

            _isIndeterminate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDeterminate));
        }
    }

    public bool IsDeterminate => !IsIndeterminate;

    public double Value
    {
        get => _value;
        private set
        {
            _value = value;
            OnPropertyChanged();
        }
    }

    public string PercentageText
    {
        get => _percentageText;
        private set
        {
            _percentageText = value;
            OnPropertyChanged();
        }
    }

    public void SetProgress(double? progress)
    {
        if (progress is null)
        {
            IsIndeterminate = true;
            PercentageText = string.Empty;
            return;
        }

        Value = Math.Clamp(progress.Value, 0.0, 1.0) * 100;
        PercentageText = $"{Math.Floor(Value):0}%";
        IsIndeterminate = false;
    }

    public void Reset() => SetProgress(null);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
