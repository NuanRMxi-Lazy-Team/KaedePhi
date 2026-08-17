using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    KaedePhi.Core.Analyzers.TotalNumberOfNotesAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace KaedePhi.Core.Analyzers.Tests;

public class TotalNumberOfNotesAnalyzerTests
{
    private const string JudgeLineStub =
        @"
namespace KaedePhi.Core.RePhiEdit
{
    public class JudgeLine
    {
        public string Name { get; set; } = """";

        public int TotalNumberOfNotes { get; }
    }
}
";

    private const string OtherTypeStub =
        @"
namespace KaedePhi.Core.Other
{
    public class Holder
    {
        public int TotalNumberOfNotes { get; }
    }
}
";

    [Fact]
    public async Task TotalNumberOfNotes_Access_ReportsDiagnostic()
    {
        const string text =
            @"
class Program
{
    static void Main()
    {
        var line = new KaedePhi.Core.RePhiEdit.JudgeLine();
        var count = line.TotalNumberOfNotes;
    }
}
" + JudgeLineStub;

        var expected = Verifier
            .Diagnostic()
            .WithLocation(7, 21)
            .WithSeverity(DiagnosticSeverity.Warning);
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }

    [Fact]
    public async Task JudgeLine_OtherProperty_NoDiagnostic()
    {
        const string text =
            @"
class Program
{
    static void Main()
    {
        var line = new KaedePhi.Core.RePhiEdit.JudgeLine();
        var name = line.Name;
    }
}
" + JudgeLineStub;

        await Verifier.VerifyAnalyzerAsync(text);
    }

    [Fact]
    public async Task SameNamedProperty_OnOtherType_NoDiagnostic()
    {
        const string text =
            @"
class Program
{
    static void Main()
    {
        var holder = new KaedePhi.Core.Other.Holder();
        var count = holder.TotalNumberOfNotes;
    }
}
" + OtherTypeStub;

        await Verifier.VerifyAnalyzerAsync(text);
    }
}
