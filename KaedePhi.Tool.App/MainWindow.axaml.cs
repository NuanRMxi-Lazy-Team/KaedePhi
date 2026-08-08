using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace KaedePhi.Tool.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if !Release
        // 版本号使用主题化前景色：暗色主题黄色、明亮主题深橙，保证两种主题下均可读
        var ver =
            Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? "unknown";
        VersionLabel.Text = $"v{ver}";
        if (
            Application.Current != null
            && Application.Current.TryGetResource("VersionLabelForeground", out var value)
            && value is IBrush brush
        )
            VersionLabel.Foreground = brush;
        VersionLabel.Opacity = 0.85;
#else
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionLabel.Text =
            version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v?";
#endif
    }
}
