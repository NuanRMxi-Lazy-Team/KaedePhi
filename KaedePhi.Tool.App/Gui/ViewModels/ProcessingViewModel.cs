using System.ComponentModel;
using System.Runtime.CompilerServices;
using static KaedePhi.Tool.Localization.GuiLocalizationString;

namespace KaedePhi.Tool.App.Gui.ViewModels;

public sealed class ProcessingViewModel : INotifyPropertyChanged
{
    private double _progress;
    private double _toolProgressValue;
    private string _currentStep = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isCompleted;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private string _logFilePath = string.Empty;

    public List<string> Steps { get; } = new() { step_running_tool };

    public double Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            OnPropertyChanged();
        }
    }

    public double ToolProgressValue
    {
        get => _toolProgressValue;
        set
        {
            _toolProgressValue = value;
            OnPropertyChanged();
        }
    }

    public string CurrentStep
    {
        get => _currentStep;
        set
        {
            _currentStep = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            _isCompleted = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowSuccessIcon));
        }
    }

    public bool HasError
    {
        get => _hasError;
        set
        {
            _hasError = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowSuccessIcon));
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public string LogFilePath
    {
        get => _logFilePath;
        set
        {
            _logFilePath = value;
            OnPropertyChanged();
        }
    }

    public bool ShowSuccessIcon => IsCompleted && !HasError;

    public event Action? RequestReturnToTools;
    public event Action? RequestReturnToImport;
    public event Action? RequestGoToExport;
    public event Action? RequestCancel;

    public void OnReturnToToolsClicked() => RequestReturnToTools?.Invoke();

    public void OnReturnToImportClicked() => RequestReturnToImport?.Invoke();

    public void OnGoToExportClicked() => RequestGoToExport?.Invoke();

    public void OnCancelClicked() => RequestCancel?.Invoke();

    public void SetStep(int index, string detail = "")
    {
        if (index < 0 || index >= Steps.Count)
            return;
        CurrentStep = Steps[index];
        StatusMessage = string.IsNullOrEmpty(detail) ? Steps[index] : detail;
        Progress = (double)(index + 1) / Steps.Count * 100;
        if (index != 0)
            ToolProgressValue = 0;
    }

    public void SetToolProgress(double toolProgress, double overallProgress, string? detail = null)
    {
        ToolProgressValue = Math.Clamp(toolProgress, 0, 1) * 100;
        if (overallProgress >= 0)
        {
            const int toolStepIndex = 0;
            var stepStart = (double)toolStepIndex / Steps.Count * 100;
            var stepEnd = (double)(toolStepIndex + 1) / Steps.Count * 100;
            Progress = stepStart + Math.Clamp(overallProgress, 0, 1) * (stepEnd - stepStart);
        }
        if (!string.IsNullOrEmpty(detail))
            StatusMessage = detail;
    }

    public void SetCompleted(string message)
    {
        IsCompleted = true;
        HasError = false;
        StatusMessage = message;
        Progress = 100;
    }

    public void SetError(string message, string logPath)
    {
        IsCompleted = true;
        HasError = true;
        ErrorMessage = message;
        LogFilePath = logPath;
        StatusMessage = processing_error;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
