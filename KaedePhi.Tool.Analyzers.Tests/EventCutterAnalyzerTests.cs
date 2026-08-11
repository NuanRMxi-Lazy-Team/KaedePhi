using System.Threading.Tasks;
using Xunit;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
        KaedePhi.Tool.Analyzers.EventCutterAnalyzer,
        Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace KaedePhi.Tool.Analyzers.Tests;

public sealed class EventCutterAnalyzerTests
{
    [Fact]
    public async Task DoubleCutLengthGreaterThanOne_ReportsDiagnostic()
    {
        const string source = """
            using KaedePhi.Tool.Event.KaedePhi;

            public static class Test
            {
                public static void Run()
                {
                    var cutter = new EventCutter<int>();
                    cutter.CutEventToLinear(0, 4d);
                }
            }

            namespace KaedePhi.Core.Common
            {
                public readonly struct Beat
                {
                    public Beat(double value) { }
                }
            }

            namespace KaedePhi.Tool.Event.KaedePhi
            {
                public sealed class EventCutter<T>
                {
                    public void CutEventToLinear(T evt, double cutLength) { }
                }
            }
            """;

        var expected = Verifier.Diagnostic(EventCutterAnalyzer.DiagnosticId)
            .WithSpan(8, 36, 8, 38)
            .WithArguments("4");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task BeatCutLengthGreaterThanOne_ReportsDiagnostic()
    {
        const string source = """
            using KaedePhi.Core.Common;
            using KaedePhi.Tool.Event.KaedePhi;

            public static class Test
            {
                public static void Run()
                {
                    var cutter = new EventCutter<int>();
                    cutter.CutEventToLinear(0, new Beat(4d));
                }
            }

            namespace KaedePhi.Core.Common
            {
                public readonly struct Beat
                {
                    public Beat(double value) { }
                }
            }

            namespace KaedePhi.Tool.Event.KaedePhi
            {
                public sealed class EventCutter<T>
                {
                    public void CutEventToLinear(T evt, Beat cutLength) { }
                }
            }
            """;

        var expected = Verifier.Diagnostic(EventCutterAnalyzer.DiagnosticId)
            .WithSpan(9, 36, 9, 48)
            .WithArguments("4");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task CutLengthEqualsOne_ReportsDiagnosticWithoutReciprocalMessage()
    {
        const string source = """
            using KaedePhi.Tool.Event.KaedePhi;

            public static class Test
            {
                public static void Run()
                {
                    var cutter = new EventCutter<int>();
                    cutter.CutEventToLinear(0, 1d);
                }
            }

            namespace KaedePhi.Tool.Event.KaedePhi
            {
                public sealed class EventCutter<T>
                {
                    public void CutEventToLinear(T evt, double cutLength) { }
                }
            }
            """;

        var expected = Verifier.Diagnostic(EventCutterAnalyzer.EqualOneDiagnosticId)
            .WithSpan(8, 36, 8, 38);
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NonConstantOrSmallCutLength_DoesNotReportDiagnostic()
    {
        const string source = """
            using KaedePhi.Tool.Event.KaedePhi;

            public static class Test
            {
                public static void Run()
                {
                    var cutter = new EventCutter<int>();
                    var precision = 4d;
                    cutter.CutEventToLinear(0, precision);
                    cutter.CutEventToLinear(0, 0.5d);
                }
            }

            namespace KaedePhi.Tool.Event.KaedePhi
            {
                public sealed class EventCutter<T>
                {
                    public void CutEventToLinear(T evt, double cutLength) { }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SameMethodNameOutsideEventCutter_DoesNotReportDiagnostic()
    {
        const string source = """
            using Other;

            public static class Test
            {
                public static void Run()
                {
                    var cutter = new EventCutter<int>();
                    cutter.CutEventToLinear(0, 4d);
                }
            }

            namespace Other
            {
                public sealed class EventCutter<T>
                {
                    public void CutEventToLinear(T evt, double cutLength) { }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source);
    }
}
