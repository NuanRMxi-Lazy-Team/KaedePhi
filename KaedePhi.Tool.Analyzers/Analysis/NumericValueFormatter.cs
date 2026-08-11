using System.Globalization;

namespace KaedePhi.Tool.Analyzers.Analysis;

internal static class NumericValueFormatter
{
    public static string Format(double value) =>
        value == Math.Floor(value)
            ? value.ToString("F0", CultureInfo.InvariantCulture)
            : value.ToString("G", CultureInfo.InvariantCulture);
}
