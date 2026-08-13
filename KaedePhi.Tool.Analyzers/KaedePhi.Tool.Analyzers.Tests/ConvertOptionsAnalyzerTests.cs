using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    KaedePhi.Tool.Analyzers.ConvertOptionsAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace KaedePhi.Tool.Analyzers.Tests;

public sealed class ConvertOptionsAnalyzerTests
{
    [Fact]
    public async Task NonPositivePrecisionAssignment_ReportsError()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var options = new KaedePhi.Tool.Converter.PhiEdit.Model.KpcToPhiEditConvertOptions();
                    options.Cutting.UnsupportedEasingPrecision = {|#0:-1|};
                }
            }

            namespace KaedePhi.Tool.Converter.PhiEdit.Model
            {
                public class KpcToPhiEditConvertOptions
                {
                    public CuttingOptions Cutting { get; set; } = new();
                    public double TrailingBeatPadding { get; set; }

                    public class CuttingOptions
                    {
                        public double UnsupportedEasingPrecision { get; set; }
                        public double CutTolerance { get; set; }
                    }
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(ConvertOptionsAnalyzer.PositiveDiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("UnsupportedEasingPrecision", "-1");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task OutOfRangeToleranceAssignment_ReportsError()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var options = new KaedePhi.Tool.Converter.PhiEdit.Model.KpcToPhiEditConvertOptions();
                    options.Cutting.CutTolerance = {|#0:101d|};
                }
            }

            namespace KaedePhi.Tool.Converter.PhiEdit.Model
            {
                public class KpcToPhiEditConvertOptions
                {
                    public CuttingOptions Cutting { get; set; } = new();
                    public double TrailingBeatPadding { get; set; }

                    public class CuttingOptions
                    {
                        public double UnsupportedEasingPrecision { get; set; }
                        public double CutTolerance { get; set; }
                    }
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(ConvertOptionsAnalyzer.ToleranceDiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("CutTolerance", "101");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NegativeTrailingBeatPaddingAssignment_ReportsError()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var options = new KaedePhi.Tool.Converter.PhiEdit.Model.KpcToPhiEditConvertOptions();
                    options.TrailingBeatPadding = {|#0:-0.5|};
                }
            }

            namespace KaedePhi.Tool.Converter.PhiEdit.Model
            {
                public class KpcToPhiEditConvertOptions
                {
                    public CuttingOptions Cutting { get; set; } = new();
                    public double TrailingBeatPadding { get; set; }

                    public class CuttingOptions
                    {
                        public double UnsupportedEasingPrecision { get; set; }
                        public double CutTolerance { get; set; }
                    }
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(ConvertOptionsAnalyzer.NonNegativeDiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("TrailingBeatPadding", "-0.5");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NonPositivePrecisionInObjectInitializer_ReportsError()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var options = new KaedePhi.Tool.Converter.PhiEdit.Model.KpcToPhiEditConvertOptions
                    {
                        Cutting = new() { UnsupportedEasingPrecision = {|#0:-1|} },
                    };
                }
            }

            namespace KaedePhi.Tool.Converter.PhiEdit.Model
            {
                public class KpcToPhiEditConvertOptions
                {
                    public CuttingOptions Cutting { get; set; } = new();
                    public double TrailingBeatPadding { get; set; }

                    public class CuttingOptions
                    {
                        public double UnsupportedEasingPrecision { get; set; }
                        public double CutTolerance { get; set; }
                    }
                }
            }
            """;

        var expected = Verifier
            .Diagnostic(ConvertOptionsAnalyzer.PositiveDiagnosticId)
            .WithLocation(0)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("UnsupportedEasingPrecision", "-1");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ValidOptionValues_DoesNotReportDiagnostic()
    {
        const string source = """
            public static class Test
            {
                public static void Run()
                {
                    var options = new KaedePhi.Tool.Converter.PhiEdit.Model.KpcToPhiEditConvertOptions();
                    options.Cutting.UnsupportedEasingPrecision = 64;
                    options.Cutting.CutTolerance = 0.1;
                    options.TrailingBeatPadding = 0;
                }
            }

            namespace KaedePhi.Tool.Converter.PhiEdit.Model
            {
                public class KpcToPhiEditConvertOptions
                {
                    public CuttingOptions Cutting { get; set; } = new();
                    public double TrailingBeatPadding { get; set; }

                    public class CuttingOptions
                    {
                        public double UnsupportedEasingPrecision { get; set; }
                        public double CutTolerance { get; set; }
                    }
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source);
    }
}
