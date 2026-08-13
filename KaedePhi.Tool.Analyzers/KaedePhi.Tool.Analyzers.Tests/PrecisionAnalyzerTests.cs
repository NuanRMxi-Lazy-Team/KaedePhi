using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    KaedePhi.Tool.Analyzers.PrecisionAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace KaedePhi.Tool.Analyzers.Tests;

public sealed class PrecisionAnalyzerTests
{
    [Fact]
    public async Task LayerMerge_WithZeroPrecision_ReportsError()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var processor = new KaedePhi.Tool.Layer.KaedePhi.LayerProcessor();
                    processor.LayerMerge(new List<object>(), {|#0:0d|});
                }
            }

            namespace KaedePhi.Tool.Layer
            {
                public interface ILayerProcessor<TLayer>
                {
                    TLayer LayerMerge(List<TLayer> layers, double precision, object progress = null);
                }
            }

            namespace KaedePhi.Tool.Layer.KaedePhi
            {
                public sealed class LayerProcessor : ILayerProcessor<object>
                {
                    public object LayerMerge(List<object> layers, double precision, object progress = null) => new();
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(PrecisionAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("0");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task FatherUnbindDynamic_WithExcessivePrecision_ReportsError()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var unbinder = new KaedePhi.Tool.JudgeLines.KaedePhi.JudgeLineUnbinder();
                    unbinder.FatherUnbindDynamic(0, new List<JudgeLine>(), {|#0:2048d|}, 0.1d, 0.1d);
                }
            }

            public sealed class JudgeLine
            {
            }

            namespace KaedePhi.Tool.JudgeLines
            {
                public interface IJudgeLineUnbinder<TJudgeLine>
                {
                    TJudgeLine FatherUnbindDynamic(
                        int targetJudgeLineIndex,
                        List<TJudgeLine> allTJudgeLines,
                        double precision,
                        double tolerance,
                        double mergeTolerance,
                        object progress = null);
                }
            }

            namespace KaedePhi.Tool.JudgeLines.KaedePhi
            {
                public sealed class JudgeLineUnbinder : IJudgeLineUnbinder<global::JudgeLine>
                {
                    public global::JudgeLine FatherUnbindDynamic(
                        int targetJudgeLineIndex,
                        List<global::JudgeLine> allJudgeLines,
                        double precision,
                        double tolerance,
                        double mergeTolerance,
                        object progress = null) => new();
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(PrecisionAnalyzer.ExcessivePrecisionDiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("2048");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task FatherUnbind_WithHighPrecision_ReportsWarning()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var unbinder = new KaedePhi.Tool.JudgeLines.KaedePhi.JudgeLineUnbinder();
                    unbinder.FatherUnbind(0, new List<JudgeLine>(), {|#0:512d|});
                }
            }

            public sealed class JudgeLine
            {
            }

            namespace KaedePhi.Tool.JudgeLines
            {
                public interface IJudgeLineUnbinder<TJudgeLine>
                {
                    TJudgeLine FatherUnbind(
                        int targetJudgeLineIndex,
                        List<TJudgeLine> allTJudgeLines,
                        double precision,
                        object progress = null);
                }
            }

            namespace KaedePhi.Tool.JudgeLines.KaedePhi
            {
                public sealed class JudgeLineUnbinder : IJudgeLineUnbinder<global::JudgeLine>
                {
                    public global::JudgeLine FatherUnbind(
                        int targetJudgeLineIndex,
                        List<global::JudgeLine> allJudgeLines,
                        double precision,
                        object progress = null) => new();
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(PrecisionAnalyzer.HighPrecisionDiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("512");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task EventListMerge_WithValidPrecision_DoesNotReportDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var merger = new KaedePhi.Tool.Event.KaedePhi.EventListMerger<double>();
                    merger.EventListMerge(new List<double>(), new List<double>(), 64d);
                }
            }

            namespace KaedePhi.Tool.Event
            {
                public interface IEventListMerger<TEvent>
                {
                    List<TEvent> EventListMerge(List<TEvent> toEvents, List<TEvent> fromEvents, double precision);
                }
            }

            namespace KaedePhi.Tool.Event.KaedePhi
            {
                public sealed class EventListMerger<T> : IEventListMerger<T>
                {
                    public List<T> EventListMerge(List<T> toEvents, List<T> fromEvents, double precision) => toEvents;
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task UnrelatedMethod_WithPrecisionParameter_DoesNotReportDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var processor = new Other.Processor();
                    processor.LayerMerge(new List<object>(), 0d);
                }
            }

            namespace Other
            {
                public sealed class Processor
                {
                    public object LayerMerge(List<object> layers, double precision) => new();
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source);
    }
}
