using KaedePhi.Core.Primitives;
using KaedePhi.Core.Formats.PhiFans.Model;

namespace KaedePhi.Tool.Converter.PhiFans.Utils;

internal static class BpmBuilder
{
    internal static Ir.BpmItem ConvertToIr(Bpm source) =>
        new() { Bpm = source.BeatPerMinute, StartBeat = new Beat((int[])source.StartBeat) };

    internal static Bpm ConvertFromIr(Ir.BpmItem source) =>
        new() { BeatPerMinute = source.Bpm, StartBeat = new Beat((int[])source.StartBeat) };
}
