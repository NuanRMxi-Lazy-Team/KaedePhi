namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;

/// <summary>
/// 提供 StellateRePhiEditExtended 噪域标记纹理的识别功能。
/// </summary>
public static class StellateRePhiEditExtendedTexture
{
    /// <summary>
    /// 判断纹理路径是否表示噪域标记。
    /// </summary>
    /// <param name="texture">待检查的纹理路径</param>
    public static bool IsBlockAreaTexture(string? texture)
    {
        var fileName = GetFileName(texture);
        return string.Equals(fileName, "isSubtract0.png", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "isSubtract1.png", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 判断纹理路径是否表示减法噪域。
    /// </summary>
    /// <param name="texture">待检查的纹理路径</param>
    public static bool IsSubtractTexture(string? texture) =>
        string.Equals(GetFileName(texture), "isSubtract1.png", StringComparison.OrdinalIgnoreCase);

    private static string? GetFileName(string? texture)
    {
        if (string.IsNullOrWhiteSpace(texture))
            return null;

        var normalized = texture.Replace('\\', '/');
        return normalized[(normalized.LastIndexOf('/') + 1)..];
    }
}
