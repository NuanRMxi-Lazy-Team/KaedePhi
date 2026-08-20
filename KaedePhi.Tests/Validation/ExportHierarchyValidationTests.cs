using System.Reflection;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter;
using KaedePhi.Tool.Converter.KaedePhi;
using KaedePhi.Tool.Converter.PhiChain;
using KaedePhi.Tool.Converter.PhiChain.Model;
using KaedePhi.Tool.Converter.PhiEdit;
using KaedePhi.Tool.Converter.PhiEdit.Model;
using KaedePhi.Tool.Converter.PhiFans;
using KaedePhi.Tool.Converter.PhiFans.Model;
using KaedePhi.Tool.Converter.Phigros.v3;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using KaedePhi.Tool.Converter.RePhiEdit;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using KaedePhi.Tool.Render.KaedePhi;
using Kpc = KaedePhi.Core.KaedePhi;

namespace KaedePhi.Tests.Validation;

public class ExportHierarchyValidationTests
{
    [Fact]
    public void KaedePhiConverter_FromKpcRejectsSelfReferencingJudgeLine()
    {
        Action act = () => new KaedePhiConverter().FromKpc(CreateSelfReferencingChart(), null);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhiEditConverter_FromKpcRejectsSelfReferencingJudgeLine()
    {
        Action act = () =>
            new PhiEditConverter().FromKpc(CreateSelfReferencingChart(), new KpcToPhiEditConvertOptions());

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhiFansConverter_FromKpcRejectsSelfReferencingJudgeLine()
    {
        Action act = () =>
            new PhiFansConverter().FromKpc(CreateSelfReferencingChart(), new KpcToPhiFansConvertOptions());

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhiChainConverter_FromKpcRejectsSelfReferencingJudgeLine()
    {
        Action act = () =>
            new PhiChainConverter().FromKpc(CreateSelfReferencingChart(), new KpcToPhiChainConvertOptions());

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhigrosV3Converter_FromKpcRejectsSelfReferencingJudgeLine()
    {
        Action act = () =>
            new PhigrosV3Converter().FromKpc(
                CreateSelfReferencingChart(),
                new KpcToPhigrosV3ConvertOptions()
            );

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void RePhiEditConverter_FromKpcRejectsSelfReferencingJudgeLine()
    {
        Action act = () =>
            new RePhiEditConverter().FromKpc(CreateSelfReferencingChart(), new ConvertOption());

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ChartPipelineSource_ToRejectsSelfReferencingJudgeLineBeforeCallingConverter()
    {
        var source = ChartPipeline.From<Kpc.Chart, Unit?, Unit?>(
            CreateSelfReferencingChart(),
            new UnvalidatedConverter(),
            null,
            TestContext.Current.CancellationToken
        );

        Action act = () => source.To<Kpc.Chart, Unit?, Unit?>(new UnvalidatedConverter(), null);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public async Task ChartFormatDescriptor_ExportAsyncRejectsSelfReferencingJudgeLineBeforeExporter()
    {
        var exporterStarted = false;
        var descriptor = new ChartFormatDescriptor { Type = ChartType.PhiEdit, FileExtension = "test" };
        SetExporter(
            descriptor,
            (_, _, _, _, _, _) =>
            {
                exporterStarted = true;
                return Task.CompletedTask;
            }
        );

        Func<Task> act = () =>
            descriptor.ExportAsync(
                CreateSelfReferencingChart(),
                "ignored",
                ct: TestContext.Current.CancellationToken
            );

        await act.Should().ThrowAsync<FormatException>();
        exporterStarted.Should().BeFalse();
    }

    [Fact]
    public void KpcChartRenderExporter_ExportChartRejectsSelfReferencingJudgeLine()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            Action act = () =>
                new KpcChartRenderExporter().ExportChart(
                    CreateSelfReferencingChart(),
                    outputDir,
                    new KpcRenderOptions()
                );

            act.Should().Throw<FormatException>();
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public void KpcChartRenderExporter_ExportChartAllowsValidJudgeLineTree()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var chart = new Kpc.Chart { JudgeLineList = [new Kpc.JudgeLine(), new Kpc.JudgeLine { Father = 0 }] };
        try
        {
            Action act = () => new KpcChartRenderExporter().ExportChart(chart, outputDir, new KpcRenderOptions());

            act.Should().NotThrow();
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public void ValidateJudgeLineHierarchy_RejectsSelfReferencingJudgeLine()
    {
        Action act = () =>
            ChartProcessingValidator.ValidateJudgeLineHierarchy(CreateSelfReferencingChart().JudgeLineList);

        act.Should().Throw<FormatException>();
    }

    private static Kpc.Chart CreateSelfReferencingChart() =>
        new() { JudgeLineList = [new Kpc.JudgeLine { Father = 0 }] };

    private static void SetExporter(
        ChartFormatDescriptor descriptor,
        Func<Kpc.Chart, string, ChartWriteSettings, object?, ChartLogSink, CancellationToken, Task> exporter
    )
    {
        var property = typeof(ChartFormatDescriptor).GetProperty(
            "Exporter",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        property.Should().NotBeNull();
        property!.SetValue(descriptor, exporter);
    }

    private sealed class UnvalidatedConverter : IChartConverter<Kpc.Chart, Unit?, Unit?>
    {
        public Action<string>? OnInfo { get; set; }

        public Action<string>? OnWarning { get; set; }

        public Action<string>? OnError { get; set; }

        public Action<string>? OnDebug { get; set; }

        public IDisposable SubscribeLog(
            Action<string>? info = null,
            Action<string>? warning = null,
            Action<string>? error = null,
            Action<string>? debug = null
        ) => new TestDisposable();

        public Kpc.Chart ToKpc(Kpc.Chart input, Unit? options) => input;

        public Kpc.Chart FromKpc(Kpc.Chart input, Unit? options) => input;
    }

    private sealed class TestDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
