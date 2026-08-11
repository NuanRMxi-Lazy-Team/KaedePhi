using System.Globalization;

namespace KaedePhi.Tool.Analyzers.Analysis;

/// <summary>
/// 将诊断中出现的数值格式化为适合展示的文本。
/// </summary>
internal static class NumericValueFormatter
{
    /// <summary>
    /// 将数值格式化为字符串，整数值省略小数位。
    /// </summary>
    /// <param name="value">待格式化的数值</param>
    /// <returns>格式化后的文本</returns>
    public static string Format(double value) =>
        // 整数使用定点格式去掉小数位，小数使用最短表示
        value == Math.Floor(value)
            ? value.ToString("F0", CultureInfo.InvariantCulture)
            : value.ToString("G", CultureInfo.InvariantCulture);
}
