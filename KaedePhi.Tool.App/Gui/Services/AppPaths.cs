namespace KaedePhi.Tool.App.Gui.Services;

internal static class AppPaths
{
    private static readonly string AppDataRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KaedePhi"
    );

    /// <summary>
    /// 获取应用程序数据目录下的指定子目录路径，并确保该目录存在。
    /// </summary>
    /// <param name="segments">子目录路径段</param>
    /// <returns>子目录绝对路径</returns>
    public static string GetDirectory(params string[] segments)
    {
        var path = Path.Combine([.. segments.Prepend(AppDataRoot)]);
        Directory.CreateDirectory(path);
        return path;
    }
}
