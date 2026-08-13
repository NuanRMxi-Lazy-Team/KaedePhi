using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    KaedePhi.Tool.Analyzers.ToleranceAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace KaedePhi.Tool.Analyzers.Tests;

public sealed class ToleranceAnalyzerTests
{
    [Fact]
    public async Task FitEvents_WithToleranceOverHundred_ReportsError()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var fit = new KaedePhi.Tool.Event.KaedePhi.EventFit<double>();
                    fit.FitEvents(new List<double>(), {|#0:101d|});
                }
            }

            namespace KaedePhi.Tool.Event
            {
                public interface IEventFit<TEvent>
                {
                    List<TEvent> FitEvents(List<TEvent> events, double tolerance);
                }
            }

            namespace KaedePhi.Tool.Event.KaedePhi
            {
                public sealed class EventFit<T> : IEventFit<T>
                {
                    public List<T> FitEvents(List<T> events, double tolerance) => events;
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(ToleranceAnalyzer.DiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("101");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task EventListCompressSlope_WithNegativeTolerance_ReportsError()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var compressor = new KaedePhi.Tool.Event.KaedePhi.EventCompressor<double>();
                    compressor.EventListCompressSlope(new List<double>(), {|#0:-1d|});
                }
            }

            namespace KaedePhi.Tool.Event
            {
                public interface IEventCompressor<TEvent>
                {
                    List<TEvent> EventListCompressSqrt(List<TEvent> events, double tolerance);
                    List<TEvent> EventListCompressSlope(List<TEvent> events, double tolerance);
                }
            }

            namespace KaedePhi.Tool.Event.KaedePhi
            {
                public sealed class EventCompressor<T> : IEventCompressor<T>
                {
                    public List<T> EventListCompressSqrt(List<T> events, double tolerance) => events;
                    public List<T> EventListCompressSlope(List<T> events, double tolerance) => events;
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(ToleranceAnalyzer.NegativeToleranceDiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("-1");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task LayerMergePlus_WithZeroTolerance_ReportsInfo()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var processor = new KaedePhi.Tool.Layer.KaedePhi.LayerProcessor();
                    processor.LayerMergePlus(new List<object>(), 64d, {|#0:0d|});
                }
            }

            namespace KaedePhi.Tool.Layer
            {
                public interface ILayerProcessor<TLayer>
                {
                    TLayer LayerMergePlus(List<TLayer> layers, double precision, double tolerance, object progress = null);
                }
            }

            namespace KaedePhi.Tool.Layer.KaedePhi
            {
                public sealed class LayerProcessor : ILayerProcessor<object>
                {
                    public object LayerMergePlus(List<object> layers, double precision, double tolerance, object progress = null) => new();
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(ToleranceAnalyzer.ZeroToleranceDiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Info)
            .WithArguments("0");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task LayerEventsCompress_WithValidTolerance_DoesNotReportDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var processor = new KaedePhi.Tool.Layer.KaedePhi.LayerProcessor();
                    processor.LayerEventsCompress(new object(), 0.5d);
                }
            }

            namespace KaedePhi.Tool.Layer
            {
                public interface ILayerProcessor<TLayer>
                {
                    void LayerEventsCompress(TLayer layer, double tolerance, object progress = null);
                }
            }

            namespace KaedePhi.Tool.Layer.KaedePhi
            {
                public sealed class LayerProcessor : ILayerProcessor<object>
                {
                    public void LayerEventsCompress(object layer, double tolerance, object progress = null) { }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task UnrelatedMethod_WithToleranceParameter_DoesNotReportDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var processor = new Other.Processor();
                    processor.Process(new List<object>(), 200d);
                }
            }

            namespace Other
            {
                public sealed class Processor
                {
                    public void Process(List<object> events, double tolerance) { }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source);
    }
}
