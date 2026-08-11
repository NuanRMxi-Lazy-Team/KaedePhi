#nullable enable

using System.Globalization;
using System.Resources;

namespace KaedePhi.Tool.Analyzers;

internal static class Resource
{
    private static readonly ResourceManager Manager =
        new("KaedePhi.Tool.Analyzers.Resource", typeof(Resource).Assembly);

    internal static ResourceManager ResourceManager => Manager;

    internal static CultureInfo? Culture { get; set; }

    internal static string KPTI0001Title => GetString(nameof(KPTI0001Title));

    internal static string KPTI0001MessageFormat => GetString(nameof(KPTI0001MessageFormat));

    internal static string KPTI0001Description => GetString(nameof(KPTI0001Description));

    internal static string KPTI0002Title => GetString(nameof(KPTI0002Title));

    internal static string KPTI0002MessageFormat => GetString(nameof(KPTI0002MessageFormat));

    internal static string KPTI0002Description => GetString(nameof(KPTI0002Description));

    internal static string KPTI0001CodeFixTitle => GetString(nameof(KPTI0001CodeFixTitle));

    internal static string KPTE0001Title => GetString(nameof(KPTE0001Title));

    internal static string KPTE0001MessageFormat => GetString(nameof(KPTE0001MessageFormat));

    internal static string KPTE0001Description => GetString(nameof(KPTE0001Description));

    internal static string KPTE0001CodeFixTitle => GetString(nameof(KPTE0001CodeFixTitle));

    internal static string KPTI0003Title => GetString(nameof(KPTI0003Title));

    internal static string KPTI0003MessageFormat => GetString(nameof(KPTI0003MessageFormat));

    internal static string KPTI0003Description => GetString(nameof(KPTI0003Description));

    internal static string KPTI0003CodeFixTitle => GetString(nameof(KPTI0003CodeFixTitle));

    internal static string KPTR0001Title => GetString(nameof(KPTR0001Title));

    internal static string KPTR0001MessageFormat => GetString(nameof(KPTR0001MessageFormat));

    internal static string KPTR0001Description => GetString(nameof(KPTR0001Description));

    internal static string KPTR0001CodeFixTitle => GetString(nameof(KPTR0001CodeFixTitle));

    private static string GetString(string name) => Manager.GetString(name, Culture) ?? name;
}
