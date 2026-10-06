namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended;

internal static class StellateRePhiEditExtendedTexture
{
    internal static bool IsBlockAreaTexture(string? texture)
    {
        var fileName = GetFileName(texture);
        return string.Equals(fileName, "isSubtract0.png", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "isSubtract1.png", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsSubtractTexture(string? texture) =>
        string.Equals(GetFileName(texture), "isSubtract1.png", StringComparison.OrdinalIgnoreCase);

    private static string? GetFileName(string? texture)
    {
        if (string.IsNullOrWhiteSpace(texture))
            return null;

        var normalized = texture.Replace('\\', '/');
        return normalized[(normalized.LastIndexOf('/') + 1)..];
    }
}
