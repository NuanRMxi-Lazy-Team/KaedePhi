using System.Threading.Tasks;
using Xunit;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    KaedePhi.Tool.Analyzers.EventCutterAnalyzer,
    KaedePhi.Tool.Analyzers.EventCutterCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace KaedePhi.Tool.Analyzers.Tests;

public sealed class EventCutterCodeFixProviderTests
{
    [Fact]
    public async Task DoubleCutLengthGreaterThanOne_ReplacesWithReciprocal()
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

            namespace KaedePhi.Tool.Event.KaedePhi
            {
                public sealed class EventCutter<T>
                {
                    public void CutEventToLinear(T evt, double cutLength) { }
                }
            }
            """;
        const string fixedSource = """
            using KaedePhi.Tool.Event.KaedePhi;

            public static class Test
            {
                public static void Run()
                {
                    var cutter = new EventCutter<int>();
                    cutter.CutEventToLinear(0, 1d / 4d);
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

        var expected = Verifier
            .Diagnostic(EventCutterAnalyzer.DiagnosticId)
            .WithSpan(8, 36, 8, 38)
            .WithArguments("4");
        await Verifier.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Fact]
    public async Task BeatCutLengthGreaterThanOne_ReplacesConstructorArgumentWithReciprocal()
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
        const string fixedSource = """
            using KaedePhi.Core.Common;
            using KaedePhi.Tool.Event.KaedePhi;

            public static class Test
            {
                public static void Run()
                {
                    var cutter = new EventCutter<int>();
                    cutter.CutEventToLinear(0, new Beat(1d / 4d));
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

        var expected = Verifier
            .Diagnostic(EventCutterAnalyzer.DiagnosticId)
            .WithSpan(9, 36, 9, 48)
            .WithArguments("4");
        await Verifier.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Fact]
    public async Task CutLengthEqualsOne_DoesNotOfferCodeFix()
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

        var expected = Verifier
            .Diagnostic(EventCutterAnalyzer.EqualOneDiagnosticId)
            .WithSpan(8, 36, 8, 38);
        await Verifier.VerifyCodeFixAsync(source, expected, source);
    }
}
