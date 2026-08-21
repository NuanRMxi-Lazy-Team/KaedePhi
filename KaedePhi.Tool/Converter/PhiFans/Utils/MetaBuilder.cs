using KaedePhi.Core.PhiFans;

namespace KaedePhi.Tool.Converter.PhiFans.Utils;

internal static class MetaBuilder
{
    internal static Kpc.Meta ConvertToKpc(Info info, int offset) =>
        new()
        {
            Name = info.Name,
            Composer = info.Artist,
            Artist = info.Illustration,
            Level = info.Level,
            Author = info.Designer,
            Offset = offset,
        };

    internal static Info ConvertFromKpc(Kpc.Meta source) =>
        new()
        {
            Name = source.Name,
            Artist = source.Composer,
            Illustration = source.Artist,
            Level = source.Level,
            Designer = source.Author,
        };
}
