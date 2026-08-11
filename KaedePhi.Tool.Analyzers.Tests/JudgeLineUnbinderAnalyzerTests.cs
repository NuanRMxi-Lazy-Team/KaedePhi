using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
        KaedePhi.Tool.Analyzers.JudgeLineUnbinderAnalyzer,
        Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace KaedePhi.Tool.Analyzers.Tests;

public sealed class JudgeLineUnbinderAnalyzerTests
{
    [Fact]
    public async Task InterfaceCall_WithToleranceAtLeastOneHundred_ReportsDiagnostics()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    KaedePhi.Tool.JudgeLines.IJudgeLineUnbinder<JudgeLine> unbinder =
                        new KaedePhi.Tool.JudgeLines.KaedePhi.JudgeLineUnbinder();
                    unbinder.FatherUnbindDynamic(0, new List<JudgeLine>(), 16d, 100d, 100d);
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
                        List<global::JudgeLine> allTJudgeLines,
                        double precision,
                        double tolerance,
                        double mergeTolerance,
                        object progress = null) => new();
                }
            }
            """;

        var toleranceExpected = Verifier.Diagnostic(JudgeLineUnbinderAnalyzer.DiagnosticId)
            .WithSpan(9, 69, 9, 73)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("100");
        var mergeToleranceExpected = Verifier.Diagnostic(JudgeLineUnbinderAnalyzer.DiagnosticId)
            .WithSpan(9, 75, 9, 79)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("100");
        await Verifier.VerifyAnalyzerAsync(source, toleranceExpected, mergeToleranceExpected);
    }

    [Fact]
    public async Task ImplementationCall_WithToleranceAtLeastOneHundred_ReportsDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var unbinder = new KaedePhi.Tool.JudgeLines.KaedePhi.JudgeLineUnbinder();
                    unbinder.FatherUnbindDynamic(0, new List<JudgeLine>(), 16d, 100d, 5d);
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

        var expected = Verifier.Diagnostic(JudgeLineUnbinderAnalyzer.DiagnosticId)
            .WithSpan(8, 69, 8, 73)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("100");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ToleranceExactlyPointZeroOne_DoesNotReportDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var unbinder = new KaedePhi.Tool.JudgeLines.KaedePhi.JudgeLineUnbinder();
                    unbinder.FatherUnbindDynamic(0, new List<JudgeLine>(), 16d, 0.01d, 1d);
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

        await Verifier.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DynamicToleranceZero_ReportsWarningButZeroMergeToleranceDoesNot()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var unbinder = new KaedePhi.Tool.JudgeLines.KaedePhi.JudgeLineUnbinder();
                    unbinder.FatherUnbindDynamic(0, new List<JudgeLine>(), 16d, 0d, 0d);
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

        var expected = Verifier.Diagnostic(JudgeLineUnbinderAnalyzer.ZeroToleranceDiagnosticId)
            .WithSpan(8, 69, 8, 71)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("0");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ToleranceBelowPointZeroOne_ReportsInfoDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var unbinder = new KaedePhi.Tool.JudgeLines.KaedePhi.JudgeLineUnbinder();
                    unbinder.FatherUnbindDynamic(0, new List<JudgeLine>(), 16d, 0.001d, 1d);
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

        var expected = Verifier.Diagnostic(JudgeLineUnbinderAnalyzer.SmallToleranceDiagnosticId)
            .WithSpan(8, 69, 8, 75)
            .WithSeverity(DiagnosticSeverity.Info)
            .WithArguments("0.001");
        await Verifier.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task UnrelatedMethod_DoesNotReportDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var unbinder = new Other.JudgeLineUnbinder();
                    unbinder.FatherUnbindDynamic(0, new List<JudgeLine>(), 16d, 100d, 100d);
                }
            }

            public sealed class JudgeLine
            {
            }

            namespace Other
            {
                public sealed class JudgeLineUnbinder
                {
                    public global::JudgeLine FatherUnbindDynamic(
                        int targetJudgeLineIndex,
                        List<global::JudgeLine> allJudgeLines,
                        double precision,
                        double tolerance,
                        double mergeTolerance) => new();
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source);
    }
}
