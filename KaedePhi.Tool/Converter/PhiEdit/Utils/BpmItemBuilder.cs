using KaedePhi.Core.Primitives;

namespace KaedePhi.Tool.Converter.PhiEdit.Utils;

/// <summary>
/// PE 与 IR BPM 项之间的双向转换工具。
/// </summary>
public static class BpmItemBuilder
{
    public static Ir.BpmItem ConvertBpmItem(Pe.BpmItem src) =>
        new() { Bpm = src.Bpm, StartBeat = new Beat(src.StartBeat) };

    public static Pe.BpmItem ConvertBpmItem(Ir.BpmItem src) =>
        new() { Bpm = src.Bpm, StartBeat = (float)(double)src.StartBeat };
}
