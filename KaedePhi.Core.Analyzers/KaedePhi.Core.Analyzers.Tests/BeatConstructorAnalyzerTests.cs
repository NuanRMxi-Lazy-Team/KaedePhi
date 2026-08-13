using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    KaedePhi.Core.Analyzers.BeatConstructorAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace KaedePhi.Core.Analyzers.Tests;

public class BeatConstructorAnalyzerTests
{
    [Fact]
    public async Task BeatWithDenominatorZero_ReportsError()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var beat = new KaedePhi.Core.Common.Beat(new[] { 0, 1, {|#0:0|} });
                }
            }

            namespace KaedePhi.Core.Common
            {
                public readonly struct Beat
                {
                    public Beat(int[] beatArray) { }
                    public Beat(double beat) { }
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(BeatConstructorAnalyzer.DenominatorZeroDiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error);
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task BeatWithNegativeDenominator_ReportsError()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var beat = new KaedePhi.Core.Common.Beat(new int[] { 0, 1, {|#0:-2|} });
                }
            }

            namespace KaedePhi.Core.Common
            {
                public readonly struct Beat
                {
                    public Beat(int[] beatArray) { }
                    public Beat(double beat) { }
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(BeatConstructorAnalyzer.DenominatorNegativeDiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("-2");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task BeatArrayLengthNotThree_ReportsError()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var beat = new KaedePhi.Core.Common.Beat({|#0:new[] { 0, 1, 2, 3 }|});
                }
            }

            namespace KaedePhi.Core.Common
            {
                public readonly struct Beat
                {
                    public Beat(int[] beatArray) { }
                    public Beat(double beat) { }
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(BeatConstructorAnalyzer.LengthDiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("4");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task BeatWithNonFiniteDouble_ReportsError()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var beat = new KaedePhi.Core.Common.Beat({|#0:double.NaN|});
                }
            }

            namespace KaedePhi.Core.Common
            {
                public readonly struct Beat
                {
                    public Beat(int[] beatArray) { }
                    public Beat(double beat) { }
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(BeatConstructorAnalyzer.NonFiniteDiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error);
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ValidBeatArray_DoesNotReportDiagnostic()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var beat = new KaedePhi.Core.Common.Beat(new[] { 0, 1, 4 });
                }
            }

            namespace KaedePhi.Core.Common
            {
                public readonly struct Beat
                {
                    public Beat(int[] beatArray) { }
                    public Beat(double beat) { }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ValidBeatDouble_DoesNotReportDiagnostic()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var beat = new KaedePhi.Core.Common.Beat(1.5);
                }
            }

            namespace KaedePhi.Core.Common
            {
                public readonly struct Beat
                {
                    public Beat(int[] beatArray) { }
                    public Beat(double beat) { }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task BeatWithNonConstantArray_DoesNotReportDiagnostic()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    int[] values = { 0, 1, 0 };
                    var beat = new KaedePhi.Core.Common.Beat(values);
                }
            }

            namespace KaedePhi.Core.Common
            {
                public readonly struct Beat
                {
                    public Beat(int[] beatArray) { }
                    public Beat(double beat) { }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source);
    }
}
