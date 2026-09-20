using KaedePhi.Core.Formats.PhiFans;

namespace KaedePhi.Tool.Converter.PhiFans.Utils;

internal static class MetaBuilder
{
    internal static Ir.Meta ConvertToIr(Info info, int offset) =>
        new()
        {
            Name = info.Name,
            Composer = info.Artist,
            Artist = info.Illustration,
            Level = info.Level,
            Author = info.Designer,
            Offset = offset,
        };

    internal static Info ConvertFromIr(Ir.Meta source) =>
        new()
        {
            Name = source.Name,
            Artist = source.Composer,
            Illustration = source.Artist,
            Level = source.Level,
            Designer = source.Author,
        };
}
