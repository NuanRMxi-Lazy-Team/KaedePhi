namespace KaedePhi.Tool.App.Gui.Models;

public sealed class ToolOption
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string IconGlyph { get; init; }
    public required string ToolId { get; init; }
    public bool HasPrecision { get; init; }
    public bool HasTolerance { get; init; }
    public bool HasMergeTolerance { get; init; }
    public bool HasClassicMode { get; init; }
    public bool HasDisableCompress { get; init; }
    public bool HasRenderOptions { get; init; }
    public bool HasFitOptions { get; init; }
    public double DefaultPrecision { get; init; } = 64;
    public double DefaultTolerance { get; init; } = 0.1;
    public double DefaultMergeTolerance { get; init; } = 0.1;
}
