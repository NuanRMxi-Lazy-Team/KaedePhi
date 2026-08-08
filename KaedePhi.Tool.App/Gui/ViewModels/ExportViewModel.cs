using System.ComponentModel;
using System.Runtime.CompilerServices;
using KaedePhi.Tool.App.Config;
using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.App.Gui.ViewModels;

public sealed class ExportViewModel : INotifyPropertyChanged
{
    private ChartType _selectedFormat;
    private ChartType _sourceFormat;

    /// <summary>
    /// 判断源格式与目标格式是否不同
    /// </summary>
    private bool IsFormatChanged => _sourceFormat != _selectedFormat;

    public List<ChartType> AvailableFormats { get; } =
        new() { ChartType.RePhiEdit, ChartType.PhiEdit, ChartType.PhigrosV3, ChartType.PhiChain };

    public ChartType SourceFormat
    {
        get => _sourceFormat;
        set
        {
            _sourceFormat = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowConversionOptions));
            OnPropertyChanged(nameof(ShowPeOptions));
            OnPropertyChanged(nameof(ShowPhigrosOptions));
            OnPropertyChanged(nameof(ShowGenericOptions));
        }
    }

    public ChartType SelectedFormat
    {
        get => _selectedFormat;
        set
        {
            _selectedFormat = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsJsonFormat));
            OnPropertyChanged(nameof(ShowConversionOptions));
            OnPropertyChanged(nameof(ShowPeOptions));
            OnPropertyChanged(nameof(ShowPhigrosOptions));
            OnPropertyChanged(nameof(ShowRePhiEditOptions));
            OnPropertyChanged(nameof(ShowPhiChainOptions));
            OnPropertyChanged(nameof(ShowGenericOptions));
            if (!IsJsonFormat)
                IndentedOutput = false;
        }
    }

    /// <summary>
    /// 当前选择的导出格式是否为 JSON 格式（true 时才显示格式化输出选项）
    /// </summary>
    public bool IsJsonFormat =>
        _selectedFormat switch
        {
            ChartType.PhiEdit => false,
            _ => true,
        };

    /// <summary>
    /// 是否需要显示转换选项面板（目标格式存在可用选项时显示）
    /// </summary>
    public bool ShowConversionOptions =>
        _selectedFormat
            is ChartType.PhiEdit
                or ChartType.PhigrosV3
                or ChartType.RePhiEdit
                or ChartType.PhiChain;

    /// <summary>
    /// 是否显示 PhiEdit 专属转换选项（目标为 PhiEdit）
    /// </summary>
    public bool ShowPeOptions => ShowConversionOptions && _selectedFormat == ChartType.PhiEdit;

    /// <summary>
    /// 是否显示 PhigrosV3 专属转换选项（目标为 PhigrosV3）
    /// </summary>
    public bool ShowPhigrosOptions =>
        ShowConversionOptions && _selectedFormat == ChartType.PhigrosV3;

    /// <summary>
    /// 是否显示 RePhiEdit 专属转换选项（目标为 RePhiEdit）
    /// </summary>
    public bool ShowRePhiEditOptions =>
        ShowConversionOptions && _selectedFormat == ChartType.RePhiEdit;

    /// <summary>
    /// 是否显示 PhiChain 专属转换选项（目标为 PhiChain）
    /// </summary>
    public bool ShowPhiChainOptions =>
        ShowConversionOptions && _selectedFormat == ChartType.PhiChain;

    /// <summary>
    /// 是否显示通用转换选项（目标格式支持解绑/合并/线过滤）
    /// </summary>
    public bool ShowGenericOptions =>
        ShowConversionOptions && _selectedFormat is ChartType.PhiEdit or ChartType.PhigrosV3;

    public bool UseStream
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否格式化（缩进）JSON 输出，仅对 JSON 格式生效
    /// </summary>
    public bool IndentedOutput
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsExporting
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusText));
        }
    } = string.Empty;

    public bool HasStatusText => !string.IsNullOrEmpty(StatusText);

    #region PE 转换选项

    public double PeTrailingBeatPadding
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 1d / 64d;

    public double PeUnsupportedEasingPrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64d;

    public double PeMisalignedXyEventPrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64d;

    public double PeAlphaCutPrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64d;

    public double PeAlphaCutTolerance
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 0.1d;

    public double PeSpeedCutPrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64d;

    public double PeSpeedCutTolerance
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 0.1d;

    #endregion

    #region PhigrosV3 转换选项

    public float PhigrosDefaultBpm
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 120f;

    public double PhigrosEasingPrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64d;

    public double PhigrosMisalignedXyEventPrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64d;

    public double PhigrosAlphaCutPrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64d;

    public double PhigrosAlphaCutTolerance
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 0.1d;

    public double PhigrosSpeedCutPrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64d;

    #endregion

    #region 通用选项

    public double UnbindPrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64d;

    public double UnbindTolerance
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 0.1d;

    public bool UnbindClassicMode
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public double MultiLayerMergePrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64d;

    public double MultiLayerMergeTolerance
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 0.1d;

    public bool MultiLayerMergeClassicMode
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public bool UnbindCompress
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = true;

    public bool MultiLayerMergeCompress
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = true;

    public bool RemoveAttachUiLine
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public bool RemoveTextureLine
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public bool FilterFakeNotes
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public bool NegativeAlphaElevation
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public double NegativeAlphaStep
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 4.0d;

    #endregion

    #region RePhiEdit 转换选项

    /// <summary>
    /// RePhiEdit 非支持缓动切割精度
    /// </summary>
    public int RePhiEditUnsupportedEasingPrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64;

    #endregion

    #region PhiChain 转换选项

    /// <summary>
    /// PhiChain 是否对 rotateWithFather 为 false 的判定线进行父子线解绑
    /// </summary>
    public bool PhiChainUnbindNonRotatingChildren
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = true;

    /// <summary>
    /// PhiChain 父子线解绑精度
    /// </summary>
    public double PhiChainUnbindPrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64d;

    /// <summary>
    /// PhiChain 父子线解绑自适应采样容差
    /// </summary>
    public double PhiChainUnbindTolerance
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 0.1d;

    /// <summary>
    /// PhiChain 父子线解绑是否使用经典模式
    /// </summary>
    public bool PhiChainUnbindClassicMode
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// PhiChain 多层级合并精度
    /// </summary>
    public double PhiChainMultiLayerMergePrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64d;

    /// <summary>
    /// PhiChain 多层级合并自适应采样容差
    /// </summary>
    public double PhiChainMultiLayerMergeTolerance
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 0.1d;

    /// <summary>
    /// PhiChain 多层级合并是否使用经典模式
    /// </summary>
    public bool PhiChainMultiLayerMergeClassicMode
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// PhiChain 缓动截取切割精度
    /// </summary>
    public double PhiChainEasingCutPrecision
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 64d;

    /// <summary>
    /// PhiChain 缓动截取切割后是否压缩
    /// </summary>
    public bool PhiChainEasingCutCompress
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = true;

    /// <summary>
    /// PhiChain 缓动截取切割后压缩容差百分比
    /// </summary>
    public double PhiChainEasingCutTolerance
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = 0.1d;

    #endregion

    public event Action? RequestExport;
    public event Action? RequestReturnToImport;
    public event Action? RequestCancelExport;

    public void OnExportClicked() => RequestExport?.Invoke();

    public void OnReturnClicked() => RequestReturnToImport?.Invoke();

    public void OnCancelExportClicked() => RequestCancelExport?.Invoke();

    /// <summary>
    /// 从配置加载转换选项默认值
    /// </summary>
    public void ApplyConversionDefaults(ConvertDefaultsConfig config)
    {
        PeTrailingBeatPadding = config.PeTrailingBeatPadding;
        PeUnsupportedEasingPrecision = config.PeUnsupportedEasingPrecision;
        PeMisalignedXyEventPrecision = config.PeMisalignedXyEventPrecision;
        PeAlphaCutPrecision = config.PeAlphaCutPrecision;
        PeAlphaCutTolerance = config.PeAlphaCutTolerance;
        PeSpeedCutPrecision = config.PeSpeedCutPrecision;
        PeSpeedCutTolerance = config.PeSpeedCutTolerance;
        PhigrosDefaultBpm = config.PhigrosDefaultBpm;
        PhigrosEasingPrecision = config.PhigrosEasingPrecision;
        PhigrosMisalignedXyEventPrecision = config.PhigrosMisalignedXyEventPrecision;
        PhigrosAlphaCutPrecision = config.PhigrosAlphaCutPrecision;
        PhigrosAlphaCutTolerance = config.PhigrosAlphaCutTolerance;
        PhigrosSpeedCutPrecision = config.PhigrosSpeedCutPrecision;
        UnbindPrecision = config.UnbindPrecision;
        UnbindTolerance = config.UnbindTolerance;
        UnbindClassicMode = config.UnbindClassicMode;
        MultiLayerMergePrecision = config.MultiLayerMergePrecision;
        MultiLayerMergeTolerance = config.MultiLayerMergeTolerance;
        MultiLayerMergeClassicMode = config.MultiLayerMergeClassicMode;
        FilterFakeNotes = config.PhigrosFilterFakeNotes;
        NegativeAlphaElevation = config.PhigrosNegativeAlphaElevation;
        NegativeAlphaStep = config.PhigrosNegativeAlphaStep;

        // RePhiEdit 选项使用通用配置
        RePhiEditUnsupportedEasingPrecision = (int)config.UnbindPrecision;

        // PhiChain 选项使用通用配置
        PhiChainUnbindPrecision = config.UnbindPrecision;
        PhiChainUnbindTolerance = config.UnbindTolerance;
        PhiChainUnbindClassicMode = config.UnbindClassicMode;
        PhiChainMultiLayerMergePrecision = config.MultiLayerMergePrecision;
        PhiChainMultiLayerMergeTolerance = config.MultiLayerMergeTolerance;
        PhiChainMultiLayerMergeClassicMode = config.MultiLayerMergeClassicMode;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
