using KaedePhi.Core.Primitives;
using KaedePhi.Core.Formats.PhiChain.v6;

namespace KaedePhi.Tool.Converter.PhiChain.Utils;

/// <summary>
/// PhiChain 与 IR BPM 之间的双向转换工具。
/// </summary>
public static class BpmBuilder
{
    /// <summary>
    /// 将 PhiChain BPM 点转换为 IR BPM 项。
    /// </summary>
    /// <param name="src">PhiChain BPM 点</param>
    /// <returns>IR BPM 项</returns>
    public static Ir.BpmItem ConvertBpmPoint(BpmPoint src) =>
        new() { Bpm = src.Bpm, StartBeat = new Beat((int[])src.Beat) };

    /// <summary>
    /// 将 IR BPM 项转换为 PhiChain BPM 点。
    /// </summary>
    /// <param name="src">IR BPM 项</param>
    /// <returns>PhiChain BPM 点</returns>
    public static BpmPoint ConvertBpmItem(Ir.BpmItem src) =>
        new() { Bpm = src.Bpm, Beat = new Beat((int[])src.StartBeat) };
}
