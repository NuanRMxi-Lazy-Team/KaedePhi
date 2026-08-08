using System.Reflection;
using Avalonia.Controls;

namespace KaedePhi.Tool.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if !Release
        // 版本号的前景色由 XAML 中的 DynamicResource 按主题解析（暗色黄色、亮色深橙）
        var ver =
            Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? "unknown";
        VersionLabel.Text = $"v{ver}";
        VersionLabel.Opacity = 0.85;
#else
        // Release 构建不套用主题化前景色，恢复控件默认前景色
        VersionLabel.ClearValue(TextBlock.ForegroundProperty);
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionLabel.Text =
            version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v?";
#endif
    }
}
