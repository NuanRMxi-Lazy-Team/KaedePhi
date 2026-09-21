using System.Reflection;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter;
using KaedePhi.Tool.Converter.Intermediate;
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
using KaedePhi.Tool.Render.Intermediate;
using Ir = KaedePhi.Core.Intermediate.Model;

namespace KaedePhi.Tests.Validation;

public class ExportHierarchyValidationTests
{
    [Fact]
    public void IntermediateConverter_FromIrAllowsSelfReferencingJudgeLine()
    {
        Action act = () => new IntermediateConverter().FromIr(CreateSelfReferencingChart(), null);

        act.Should().NotThrow();
    }

    [Fact]
    public void PhiEditConverter_FromIrRejectsSelfReferencingJudgeLine()
    {
        Action act = () =>
            new PhiEditConverter().FromIr(
                CreateSelfReferencingChart(),
                new IrToPhiEditConvertOptions()
            );

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhiFansConverter_FromIrRejectsSelfReferencingJudgeLine()
    {
        Action act = () =>
            new PhiFansConverter().FromIr(
                CreateSelfReferencingChart(),
                new IrToPhiFansConvertOptions()
            );

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhiChainConverter_FromIrRejectsSelfReferencingJudgeLine()
    {
        Action act = () =>
            new PhiChainConverter().FromIr(
                CreateSelfReferencingChart(),
                new IrToPhiChainConvertOptions()
            );

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhigrosV3Converter_FromIrRejectsSelfReferencingJudgeLine()
    {
        Action act = () =>
            new PhigrosV3Converter().FromIr(
                CreateSelfReferencingChart(),
                new IrToPhigrosV3ConvertOptions()
            );

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void RePhiEditConverter_FromIrRejectsSelfReferencingJudgeLine()
    {
        Action act = () =>
            new RePhiEditConverter().FromIr(CreateSelfReferencingChart(), new ConvertOption());

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ChartPipelineSource_ToAllowsSelfReferencingJudgeLine()
    {
        var source = ChartPipeline.From<Ir.Chart, Unit?, Unit?>(
            CreateSelfReferencingChart(),
            new UnvalidatedConverter(),
            null,
            TestContext.Current.CancellationToken
        );

        Action act = () => source.To<Ir.Chart, Unit?, Unit?>(new UnvalidatedConverter(), null);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task ChartFormatDescriptor_ExportAsyncAllowsSelfReferencingJudgeLine()
    {
        var exporterStarted = false;
        var descriptor = new ChartFormatDescriptor
        {
            Type = ChartType.PhiEdit,
            FileExtension = "test",
        };
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

        await act.Should().NotThrowAsync();
        exporterStarted.Should().BeTrue();
    }

    [Fact]
    public void IrChartRenderExporter_ExportChartAllowsSelfReferencingJudgeLine()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            Action act = () =>
                new IrChartRenderExporter().ExportChart(
                    CreateSelfReferencingChart(),
                    outputDir,
                    new IrRenderOptions()
                );

            act.Should().NotThrow();
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);
        }
    }

    [Fact]
    public void IrChartRenderExporter_ExportChartAllowsValidJudgeLineTree()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var chart = new Ir.Chart
        {
            JudgeLineList = [new Ir.JudgeLine(), new Ir.JudgeLine { Father = 0 }],
        };
        try
        {
            Action act = () =>
                new IrChartRenderExporter().ExportChart(chart, outputDir, new IrRenderOptions());

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
            IrChartValidator.ValidateJudgeLineHierarchy(
                CreateSelfReferencingChart().JudgeLineList
            );

        act.Should().Throw<FormatException>();
    }

    private static Ir.Chart CreateSelfReferencingChart() =>
        new() { JudgeLineList = [new Ir.JudgeLine { Father = 0 }] };

    private static void SetExporter(
        ChartFormatDescriptor descriptor,
        Func<
            Ir.Chart,
            string,
            ChartWriteSettings,
            object?,
            ChartLogSink,
            CancellationToken,
            Task
        > exporter
    )
    {
        var property = typeof(ChartFormatDescriptor).GetProperty(
            "Exporter",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        property.Should().NotBeNull();
        property!.SetValue(descriptor, exporter);
    }

    private sealed class UnvalidatedConverter : IChartConverter<Ir.Chart, Unit?, Unit?>
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

        public Ir.Chart ToIr(Ir.Chart input, Unit? options) => input;

        public Ir.Chart FromIr(Ir.Chart input, Unit? options) => input;
    }

    private sealed class TestDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
