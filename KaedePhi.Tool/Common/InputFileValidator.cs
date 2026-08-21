namespace KaedePhi.Tool.Common;

/// <summary>
/// 校验待读取的谱面文件。
/// </summary>
public static class InputFileValidator
{
    private const long BytesPerMegabyte = 1024L * 1024;
    private const long MaximumInputMegabytes = 4096L;

    /// <summary>
    /// 允许读取的最大谱面文件大小。
    /// </summary>
    public const long MaximumInputBytes = MaximumInputMegabytes * BytesPerMegabyte;

    /// <summary>
    /// 校验输入文件路径、存在性和文件大小。
    /// </summary>
    /// <param name="path">输入文件路径。</param>
    /// <returns>无返回值。</returns>
    public static void Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("输入文件路径不能为空。", nameof(path));

        var info = new FileInfo(path);
        if (!info.Exists)
            throw new FileNotFoundException("输入谱面文件不存在。", path);
        if (info.Length > MaximumInputBytes)
            throw new IOException($"输入谱面文件超过 {MaximumInputMegabytes} MB 大小限制。");
    }
}
