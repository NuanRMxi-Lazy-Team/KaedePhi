using KaedePhi.Core.Common;
using KaedePhi.Core.PhiFans;

namespace KaedePhi.Tool.Converter.PhiFans.Utils;

internal static class BpmBuilder
{
    internal static Kpc.BpmItem ConvertToKpc(Bpm source) =>
        new() { Bpm = source.BeatPerMinute, StartBeat = new Beat((int[])source.StartBeat) };

    internal static Bpm ConvertFromKpc(Kpc.BpmItem source) =>
        new() { BeatPerMinute = source.Bpm, StartBeat = new Beat((int[])source.StartBeat) };
}
