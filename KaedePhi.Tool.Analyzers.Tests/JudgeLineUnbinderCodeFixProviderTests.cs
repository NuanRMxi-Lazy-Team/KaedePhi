using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
        KaedePhi.Tool.Analyzers.JudgeLineUnbinderAnalyzer,
        KaedePhi.Tool.Analyzers.JudgeLineUnbinderCodeFixProvider,
        Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace KaedePhi.Tool.Analyzers.Tests;

public sealed class JudgeLineUnbinderCodeFixProviderTests
{
    [Fact]
    public async Task InterfaceCall_WithIntegerTolerance_ReplacesWithPercentageValue()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    KaedePhi.Tool.JudgeLines.IJudgeLineUnbinder<JudgeLine> unbinder =
                        new KaedePhi.Tool.JudgeLines.KaedePhi.JudgeLineUnbinder();
                    unbinder.FatherUnbindDynamic(0, new List<JudgeLine>(), 16d, 100, 5d);
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
        const string fixedSource = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    KaedePhi.Tool.JudgeLines.IJudgeLineUnbinder<JudgeLine> unbinder =
                        new KaedePhi.Tool.JudgeLines.KaedePhi.JudgeLineUnbinder();
                    unbinder.FatherUnbindDynamic(0, new List<JudgeLine>(), 16d, 100 / 100d, 5d);
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
            .WithSpan(9, 69, 9, 72)
            .WithSeverity(DiagnosticSeverity.Error)
            .WithArguments("100");
        await Verifier.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Fact]
    public async Task ImplementationCall_WithSmallTolerance_MultipliesByOneHundred()
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
        const string fixedSource = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    var unbinder = new KaedePhi.Tool.JudgeLines.KaedePhi.JudgeLineUnbinder();
                    unbinder.FatherUnbindDynamic(0, new List<JudgeLine>(), 16d, 0.001d * 100d, 1d);
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
        await Verifier.VerifyCodeFixAsync(source, expected, fixedSource);
    }

    [Fact]
    public async Task DynamicToleranceZero_ReplacesWithRegularUnbinder()
    {
        const string source = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    KaedePhi.Tool.JudgeLines.IJudgeLineUnbinder<JudgeLine> unbinder =
                        new KaedePhi.Tool.JudgeLines.KaedePhi.JudgeLineUnbinder();
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
                    TJudgeLine FatherUnbind(
                        int targetJudgeLineIndex,
                        List<TJudgeLine> allTJudgeLines,
                        double precision,
                        object progress = null);
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
                    public global::JudgeLine FatherUnbind(
                        int targetJudgeLineIndex,
                        List<global::JudgeLine> allJudgeLines,
                        double precision,
                        object progress = null) => new();

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
        const string fixedSource = """
            using System.Collections.Generic;

            public static class Test
            {
                public static void Run()
                {
                    KaedePhi.Tool.JudgeLines.IJudgeLineUnbinder<JudgeLine> unbinder =
                        new KaedePhi.Tool.JudgeLines.KaedePhi.JudgeLineUnbinder();
                    unbinder.FatherUnbind(0, new List<JudgeLine>(), 16d);
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
                    public global::JudgeLine FatherUnbind(
                        int targetJudgeLineIndex,
                        List<global::JudgeLine> allJudgeLines,
                        double precision,
                        object progress = null) => new();

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
            .WithSpan(9, 69, 9, 71)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("0");
        await Verifier.VerifyCodeFixAsync(source, expected, fixedSource);
    }
}
