using System.Threading.Tasks;
using Xunit;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    KaedePhi.Core.Analyzers.EasingNumberAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace KaedePhi.Core.Analyzers.Tests;

public class EasingNumberAnalyzerTests
{
    private const string KpcEasingStub =
        @"
namespace KaedePhi.Core.KaedePhi
{
    public class Easing
    {
        public Easing(int easingNumber) { }
    }
}
";

    private const string PeEasingStub =
        @"
namespace KaedePhi.Core.PhiEdit
{
    public class Easing
    {
        public Easing(int easingNumber) { }
    }
}
";

    private const string RePhiEditEasingStub =
        @"
namespace KaedePhi.Core.RePhiEdit
{
    public class Easing
    {
        public Easing(int easingNumber) { }
    }
}
";

    private const string PhiFansEasingStub =
        @"
namespace KaedePhi.Core.PhiFans
{
    public class Easing
    {
        public Easing(int easingNumber) { }
    }
}
";

    [Fact]
    public async Task KpcEasing_WithinRange_NoDiagnostic()
    {
        const string text =
            @"
class Program
{
    static void Main()
    {
        var easing = new KaedePhi.Core.KaedePhi.Easing(31);
    }
}
" + KpcEasingStub;

        await Verifier.VerifyAnalyzerAsync(text);
    }

    [Fact]
    public async Task KpcEasing_AboveMax_ReportsDiagnostic()
    {
        const string text =
            @"
class Program
{
    static void Main()
    {
        var easing = new KaedePhi.Core.KaedePhi.Easing(32);
    }
}
" + KpcEasingStub;

        var expected = Verifier
            .Diagnostic()
            .WithLocation(6, 56)
            .WithArguments("32", "KPC", "1", "31");
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }

    [Fact]
    public async Task KpcEasing_BelowMin_ReportsDiagnostic()
    {
        const string text =
            @"
class Program
{
    static void Main()
    {
        var easing = new KaedePhi.Core.KaedePhi.Easing(0);
    }
}
" + KpcEasingStub;

        var expected = Verifier
            .Diagnostic()
            .WithLocation(6, 56)
            .WithArguments("0", "KPC", "1", "31");
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }

    [Fact]
    public async Task PeEasing_WithinRange_NoDiagnostic()
    {
        const string text =
            @"
class Program
{
    static void Main()
    {
        var easing = new KaedePhi.Core.PhiEdit.Easing(29);
    }
}
" + PeEasingStub;

        await Verifier.VerifyAnalyzerAsync(text);
    }

    [Fact]
    public async Task PeEasing_AboveMax_ReportsDiagnostic()
    {
        const string text =
            @"
class Program
{
    static void Main()
    {
        var easing = new KaedePhi.Core.PhiEdit.Easing(30);
    }
}
" + PeEasingStub;

        var expected = Verifier
            .Diagnostic()
            .WithLocation(6, 55)
            .WithArguments("30", "PE", "1", "29");
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }

    [Fact]
    public async Task RePhiEditEasing_AboveMax_ReportsDiagnostic()
    {
        const string text =
            @"
class Program
{
    static void Main()
    {
        var easing = new KaedePhi.Core.RePhiEdit.Easing(45);
    }
}
" + RePhiEditEasingStub;

        var expected = Verifier
            .Diagnostic()
            .WithLocation(6, 57)
            .WithArguments("45", "RePhiEdit", "1", "29");
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }

    [Fact]
    public async Task Easing_NonConstantNumber_NoDiagnostic()
    {
        const string text =
            @"
class Program
{
    static void Main()
    {
        var count = 32;
        var easing = new KaedePhi.Core.KaedePhi.Easing(count);
    }
}
" + KpcEasingStub;

        await Verifier.VerifyAnalyzerAsync(text);
    }

    [Fact]
    public async Task PhiFansEasing_WithinRange_NoDiagnostic()
    {
        const string text =
            @"
class Program
{
    static void Main()
    {
        var easing = new KaedePhi.Core.PhiFans.Easing(30);
    }
}
" + PhiFansEasingStub;

        await Verifier.VerifyAnalyzerAsync(text);
    }

    [Fact]
    public async Task PhiFansEasing_AboveMax_ReportsDiagnostic()
    {
        const string text =
            @"
class Program
{
    static void Main()
    {
        var easing = new KaedePhi.Core.PhiFans.Easing(31);
    }
}
" + PhiFansEasingStub;

        var expected = Verifier
            .Diagnostic()
            .WithLocation(6, 55)
            .WithArguments("31", "PhiFans", "0", "30");
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }

    [Fact]
    public async Task PhiFansEasing_BelowMin_ReportsDiagnostic()
    {
        const string text =
            @"
class Program
{
    static void Main()
    {
        var easing = new KaedePhi.Core.PhiFans.Easing(-1);
    }
}
" + PhiFansEasingStub;

        var expected = Verifier
            .Diagnostic()
            .WithLocation(6, 55)
            .WithArguments("-1", "PhiFans", "0", "30");
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
}
