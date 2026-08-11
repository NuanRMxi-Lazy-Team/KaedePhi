namespace KaedePhi.Core.Analyzers.Analysis;

/// <summary>
/// 单个格式的缓动编号有效范围。
/// </summary>
internal sealed class EasingFormatRange
{
    /// <summary>
    /// 缓动类型的完全限定名，用于在编译时识别格式。
    /// </summary>
    public string TypeFullName { get; }

    /// <summary>
    /// 格式的显示名称，用于诊断消息。
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 缓动编号下限（含）。
    /// </summary>
    public int Min { get; }

    /// <summary>
    /// 缓动编号上限（含）。
    /// </summary>
    public int Max { get; }

    /// <summary>
    /// 创建格式范围描述。
    /// </summary>
    /// <param name="typeFullName">缓动类型的完全限定名</param>
    /// <param name="displayName">格式的显示名称</param>
    /// <param name="min">缓动编号下限</param>
    /// <param name="max">缓动编号上限</param>
    public EasingFormatRange(string typeFullName, string displayName, int min, int max)
    {
        TypeFullName = typeFullName;
        DisplayName = displayName;
        Min = min;
        Max = max;
    }
}
