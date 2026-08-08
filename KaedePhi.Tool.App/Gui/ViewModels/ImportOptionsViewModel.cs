using System.ComponentModel;
using System.Runtime.CompilerServices;
using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.App.Gui.ViewModels;

public sealed class ImportOptionsViewModel : INotifyPropertyChanged
{
    private ChartType _detectedFormat;
    private string _fileName = string.Empty;
    private bool _isLoading;

    /// <summary>
    /// 检测到的源文件格式
    /// </summary>
    public ChartType DetectedFormat
    {
        get => _detectedFormat;
        set
        {
            _detectedFormat = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowPeOptions));
            OnPropertyChanged(nameof(ShowPhiChainOptions));
            OnPropertyChanged(nameof(FormatName));
        }
    }

    /// <summary>
    /// 源文件名
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 格式名称（用于显示）
    /// </summary>
    public string FormatName => _detectedFormat.ToString();

    /// <summary>
    /// 是否显示 PhiEdit 导入选项
    /// </summary>
    public bool ShowPeOptions => _detectedFormat == ChartType.PhiEdit;

    /// <summary>
    /// 是否显示 PhiChain 导入选项
    /// </summary>
    public bool ShowPhiChainOptions => _detectedFormat == ChartType.PhiChain;

    #region PhiEdit 导入选项

    /// <summary>
    /// PE 帧转事件后持续拍长度（1 / x 拍的分母）
    /// </summary>
    public int PeFrameDurationBeat
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64;

    /// <summary>
    /// PE 尾部拍填充量（1 / x 拍的分母）
    /// </summary>
    public int PeTrailingBeatPadding
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64;

    #endregion

    #region PhiChain 导入选项

    /// <summary>
    /// PhiChain 不支持的缓动切段精度
    /// </summary>
    public int PhiChainUnsupportedEasingPrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64;

    #endregion

    public event Action? RequestConfirm;
    public event Action? RequestCancel;
    public event Action? RequestCancelImport;

    public void OnConfirmClicked() => RequestConfirm?.Invoke();

    public void OnCancelClicked() => RequestCancel?.Invoke();

    public void OnCancelImportClicked() => RequestCancelImport?.Invoke();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
