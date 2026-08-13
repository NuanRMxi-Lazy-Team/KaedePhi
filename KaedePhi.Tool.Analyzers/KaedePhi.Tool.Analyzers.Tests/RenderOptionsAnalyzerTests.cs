using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    KaedePhi.Tool.Analyzers.RenderOptionsAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace KaedePhi.Tool.Analyzers.Tests;

public sealed class RenderOptionsAnalyzerTests
{
    [Fact]
    public async Task ZeroPixelsPerBeatAssignment_ReportsError()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var options = new KaedePhi.Tool.Render.KaedePhi.KpcRenderOptions();
                    options.PixelsPerBeat = {|#0:0f|};
                }
            }

            namespace KaedePhi.Tool.Render.KaedePhi
            {
                public class KpcRenderOptions
                {
                    public float PixelsPerBeat { get; set; }
                    public int ChannelWidth { get; set; }
                    public int SamplesPerEvent { get; set; }
                    public int BeatSubdivisions { get; set; }
                    public double SegmentGroupTolerance { get; set; }
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(RenderOptionsAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("PixelsPerBeat", "0", "(0, 10000]");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ExcessiveChannelWidthAssignment_ReportsError()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var options = new KaedePhi.Tool.Render.KaedePhi.KpcRenderOptions();
                    options.ChannelWidth = {|#0:20000|};
                }
            }

            namespace KaedePhi.Tool.Render.KaedePhi
            {
                public class KpcRenderOptions
                {
                    public float PixelsPerBeat { get; set; }
                    public int ChannelWidth { get; set; }
                    public int SamplesPerEvent { get; set; }
                    public int BeatSubdivisions { get; set; }
                    public double SegmentGroupTolerance { get; set; }
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(RenderOptionsAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("ChannelWidth", "20000", "(0, 10000]");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NegativeSamplesPerEventAssignment_ReportsError()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var options = new KaedePhi.Tool.Render.KaedePhi.KpcRenderOptions();
                    options.SamplesPerEvent = {|#0:-1|};
                }
            }

            namespace KaedePhi.Tool.Render.KaedePhi
            {
                public class KpcRenderOptions
                {
                    public float PixelsPerBeat { get; set; }
                    public int ChannelWidth { get; set; }
                    public int SamplesPerEvent { get; set; }
                    public int BeatSubdivisions { get; set; }
                    public double SegmentGroupTolerance { get; set; }
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(RenderOptionsAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("SamplesPerEvent", "-1", "(0, 4096]");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NegativeSegmentGroupToleranceAssignment_ReportsError()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var options = new KaedePhi.Tool.Render.KaedePhi.KpcRenderOptions();
                    options.SegmentGroupTolerance = {|#0:-1e-6|};
                }
            }

            namespace KaedePhi.Tool.Render.KaedePhi
            {
                public class KpcRenderOptions
                {
                    public float PixelsPerBeat { get; set; }
                    public int ChannelWidth { get; set; }
                    public int SamplesPerEvent { get; set; }
                    public int BeatSubdivisions { get; set; }
                    public double SegmentGroupTolerance { get; set; }
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(RenderOptionsAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("SegmentGroupTolerance", "-1E-06", "[0, \u221e)");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ValidRenderOptionValues_DoesNotReportDiagnostic()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var options = new KaedePhi.Tool.Render.KaedePhi.KpcRenderOptions();
                    options.PixelsPerBeat = 100f;
                    options.ChannelWidth = 150;
                    options.SamplesPerEvent = 64;
                    options.BeatSubdivisions = 4;
                    options.SegmentGroupTolerance = 1e-6;
                }
            }

            namespace KaedePhi.Tool.Render.KaedePhi
            {
                public class KpcRenderOptions
                {
                    public float PixelsPerBeat { get; set; }
                    public int ChannelWidth { get; set; }
                    public int SamplesPerEvent { get; set; }
                    public int BeatSubdivisions { get; set; }
                    public double SegmentGroupTolerance { get; set; }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source);
    }
}
