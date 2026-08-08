namespace KaedePhi.Tool.App.Gui.Services;

internal static class AppPaths
{
    private static readonly string AppDataRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KaedePhi"
    );

    public static string GetDirectory(params string[] segments)
    {
        var path = Path.Combine(segments.Prepend(AppDataRoot).ToArray());
        Directory.CreateDirectory(path);
        return path;
    }
}
