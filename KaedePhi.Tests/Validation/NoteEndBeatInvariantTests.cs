using System.Reflection;
using KaedePhi.Core.Primitives;
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
using Ir = KaedePhi.Core.Intermediate.Model;
using Pc = KaedePhi.Core.Formats.PhiChain.v6.Model;
using Pe = KaedePhi.Core.Formats.PhiEdit.Model;
using Pf = KaedePhi.Core.Formats.PhiFans.Model;
using Phigros = KaedePhi.Core.Formats.Phigros.v3.Model;
using Rpe = KaedePhi.Core.Formats.RePhiEdit.Model;

namespace KaedePhi.Tests.Validation;

public class NoteEndBeatInvariantTests
{
    [Fact]
    public void NormalizeAndValidateNoteEndBeats_NullChartThrows()
    {
        Action act = () => IrChartNormalizer.NormalizeAndValidateNoteEndBeats(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(NoteType.Tap)]
    [InlineData(NoteType.Drag)]
    [InlineData(NoteType.Flick)]
    public void NormalizeAndValidateNoteEndBeats_NonHoldReturnsIndependentNormalizedCopy(
        NoteType type
    )
    {
        var source = CreateIrChart(type, 3, 9);

        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(source);

        ((double)normalized.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        ((double)source.JudgeLineList[0].Notes[0].EndBeat).Should().Be(9);
        normalized.Should().NotBeSameAs(source);
        normalized.JudgeLineList[0].Should().NotBeSameAs(source.JudgeLineList[0]);
        normalized.JudgeLineList[0].Notes[0].Should().NotBeSameAs(source.JudgeLineList[0].Notes[0]);
    }

    [Fact]
    public void NormalizeAndValidateNoteEndBeats_HoldWithoutExplicitEndPassesThrough()
    {
        var source = CreateIrChartWithNote(
            new Ir.Note { Type = NoteType.Hold, StartBeat = Beat(3) }
        );

        Action act = () => IrChartNormalizer.NormalizeAndValidateNoteEndBeats(source);

        act.Should().NotThrow();
        ((double)source.JudgeLineList[0].Notes[0].EndBeat).Should().Be(1);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(2)]
    public void NormalizeAndValidateNoteEndBeats_HoldNotAfterStartPassesThrough(double endBeat)
    {
        var source = CreateIrChart(NoteType.Hold, 3, endBeat);

        Action act = () => IrChartNormalizer.NormalizeAndValidateNoteEndBeats(source);

        act.Should().NotThrow();
        ((double)source.JudgeLineList[0].Notes[0].EndBeat).Should().Be(endBeat);
    }

    [Fact]
    public void NormalizeAndValidateNoteEndBeats_ValidHoldKeepsEndOnIndependentCopy()
    {
        var source = CreateIrChart(NoteType.Hold, 3, 5);

        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(source);

        ((double)normalized.JudgeLineList[0].Notes[0].EndBeat).Should().Be(5);
        normalized.JudgeLineList[0].Notes[0].Should().NotBeSameAs(source.JudgeLineList[0].Notes[0]);
    }

    [Fact]
    public void IntermediateConverter_ToIrAndFromIrReturnIndependentNormalizedCopies()
    {
        var source = CreateIrChart(NoteType.Tap, 3, 9);
        var converter = new IntermediateConverter();

        var imported = converter.ToIr(source, null);
        var exported = converter.FromIr(source, null);

        ((double)imported.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        ((double)exported.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        imported.Should().NotBeSameAs(source);
        exported.Should().NotBeSameAs(source);
        ((double)source.JudgeLineList[0].Notes[0].EndBeat).Should().Be(9);
    }

    [Fact]
    public void PhiEditConverter_ToIrNormalizesNonHoldAndRejectsInvalidSourceHold()
    {
        var converter = new PhiEditConverter();
        var nonHold = new Pe.Chart
        {
            JudgeLineList =
            [
                new Pe.JudgeLine
                {
                    NoteList =
                    [
                        new Pe.Note
                        {
                            Type = Pe.NoteType.Tap,
                            StartBeat = 3,
                            EndBeat = 9,
                        },
                    ],
                },
            ],
        };
        var invalidHold = new Pe.Chart
        {
            JudgeLineList =
            [
                new Pe.JudgeLine
                {
                    NoteList =
                    [
                        new Pe.Note
                        {
                            Type = Pe.NoteType.Hold,
                            StartBeat = 3,
                            EndBeat = 3,
                        },
                    ],
                },
            ],
        };

        var converted = converter.ToIr(nonHold, new PhiEditToIrConvertOptions());
        Action act = () => converter.ToIr(invalidHold, new PhiEditToIrConvertOptions());

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void PhiEditConverter_ToIrRejectsNonFiniteHoldEndBeat(float endBeat)
    {
        var source = new Pe.Chart
        {
            JudgeLineList =
            [
                new Pe.JudgeLine
                {
                    NoteList =
                    [
                        new Pe.Note
                        {
                            Type = Pe.NoteType.Hold,
                            StartBeat = 3,
                            EndBeat = endBeat,
                        },
                    ],
                },
            ],
        };

        Action act = () => new PhiEditConverter().ToIr(source, new PhiEditToIrConvertOptions());

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhiEditConverter_ToIrKeepsValidHoldEndBeat()
    {
        var source = new Pe.Chart
        {
            JudgeLineList =
            [
                new Pe.JudgeLine
                {
                    NoteList =
                    [
                        new Pe.Note
                        {
                            Type = Pe.NoteType.Hold,
                            StartBeat = 3,
                            EndBeat = 5,
                        },
                    ],
                },
            ],
        };

        var converted = new PhiEditConverter().ToIr(source, new PhiEditToIrConvertOptions());

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(5);
    }

    [Fact]
    public void PhiFansConverter_ToIrNormalizesNonHoldAndRejectsMissingSourceHoldEnd()
    {
        var converter = new PhiFansConverter();
        var nonHold = CreatePhiFansChart(
            new Pf.Note
            {
                Type = Pf.NoteType.Tap,
                Beat = Beat(3),
                HoldEndBeat = Beat(9),
            }
        );
        var invalidHold = CreatePhiFansChart(
            new Pf.Note { Type = Pf.NoteType.Hold, Beat = Beat(-1) }
        );

        var converted = converter.ToIr(nonHold, null);
        Action act = () => converter.ToIr(invalidHold, null);

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhiFansConverter_ToIrKeepsValidHoldEndBeat()
    {
        var source = CreatePhiFansChart(
            new Pf.Note
            {
                Type = Pf.NoteType.Hold,
                Beat = Beat(3),
                HoldEndBeat = Beat(5),
            }
        );

        var converted = new PhiFansConverter().ToIr(source, null);

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(5);
    }

    [Fact]
    public void PhigrosV3Converter_ToIrNormalizesNonHoldAndRejectsMissingSourceHoldTime()
    {
        var converter = new PhigrosV3Converter();
        var nonHold = CreatePhigrosChart(
            new Phigros.Note
            {
                Type = Phigros.NoteType.Tap,
                Time = 96,
                HoldTime = 192,
            }
        );
        var invalidHold = CreatePhigrosChart(
            new Phigros.Note { Type = Phigros.NoteType.Hold, Time = -32 }
        );

        var converted = converter.ToIr(nonHold, null);
        Action act = () => converter.ToIr(invalidHold, null);

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void PhigrosV3Converter_ToIrRejectsNonFiniteHoldTime(float holdTime)
    {
        var source = CreatePhigrosChart(
            new Phigros.Note
            {
                Type = Phigros.NoteType.Hold,
                Time = 96,
                HoldTime = holdTime,
            }
        );

        Action act = () => new PhigrosV3Converter().ToIr(source, null);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhigrosV3Converter_ToIrKeepsValidHoldEndBeat()
    {
        var source = CreatePhigrosChart(
            new Phigros.Note
            {
                Type = Phigros.NoteType.Hold,
                Time = 96,
                HoldTime = 64,
            }
        );

        var converted = new PhigrosV3Converter().ToIr(source, null);

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(5);
    }

    [Fact]
    public void RePhiEditConverter_ToIrNormalizesNonHoldAndRejectsMissingSourceHoldEnd()
    {
        var converter = new RePhiEditConverter();
        var nonHold = CreateRePhiEditChart(
            new Rpe.Note
            {
                Type = NoteType.Tap,
                StartBeat = Beat(3),
                EndBeat = Beat(9),
            }
        );
        var invalidHold = CreateRePhiEditChart(
            new Rpe.Note { Type = NoteType.Hold, StartBeat = Beat(0) }
        );

        var converted = converter.ToIr(nonHold, null);
        Action act = () => converter.ToIr(invalidHold, null);

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void RePhiEditConverter_ToIrKeepsValidHoldEndBeat()
    {
        var source = CreateRePhiEditChart(
            new Rpe.Note
            {
                Type = NoteType.Hold,
                StartBeat = Beat(3),
                EndBeat = Beat(5),
            }
        );

        var converted = new RePhiEditConverter().ToIr(source, null);

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(5);
    }

    [Fact]
    public void PhiChainConverter_ToIrNormalizesNonHoldAndRejectsZeroDurationSourceHold()
    {
        var converter = new PhiChainConverter();
        var nonHold = CreatePhiChainChart(new Pc.Note { Type = Pc.NoteType.Tap, Beat = Beat(3) });
        var invalidHold = CreatePhiChainChart(
            new Pc.Note
            {
                Type = Pc.NoteType.Hold,
                Beat = Beat(3),
                HoldBeat = Beat(0),
            }
        );

        var converted = converter.ToIr(nonHold, new PhiChainToIrConvertOptions());
        Action act = () => converter.ToIr(invalidHold, new PhiChainToIrConvertOptions());

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhiChainConverter_ToIrKeepsValidOrdinaryHoldEndBeat()
    {
        var source = CreatePhiChainChart(
            new Pc.Note
            {
                Type = Pc.NoteType.Hold,
                Beat = Beat(3),
                HoldBeat = Beat(2),
            }
        );

        var converted = new PhiChainConverter().ToIr(source, new PhiChainToIrConvertOptions());

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(5);
    }

    [Fact]
    public void PhiChainConverter_ToIrRejectsCurveHoldWithoutDuration()
    {
        var line = new Pc.SerializedLine
        {
            Notes = [new Pc.Note { Beat = Beat(0) }, new Pc.Note { Beat = Beat(1) }],
            CurveNoteTracks =
            [
                new Pc.CurveNoteTrack
                {
                    From = 0,
                    To = 1,
                    NoteType = Pc.NoteType.Hold,
                },
            ],
        };
        var source = new Pc.Chart { Lines = [line] };

        Action act = () => new PhiChainConverter().ToIr(source, new PhiChainToIrConvertOptions());

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhiChainConverter_ToIrKeepsValidCurveHoldEndBeat()
    {
        var line = new Pc.SerializedLine
        {
            Notes = [new Pc.Note { Beat = Beat(0) }, new Pc.Note { Beat = Beat(1) }],
            CurveNoteTracks =
            [
                new Pc.CurveNoteTrack
                {
                    From = 0,
                    To = 1,
                    NoteType = Pc.NoteType.Hold,
                    HoldBeat = Beat(2),
                    Density = 2,
                },
            ],
        };
        var source = new Pc.Chart { Lines = [line] };

        var converted = new PhiChainConverter().ToIr(source, new PhiChainToIrConvertOptions());

        converted.JudgeLineList[0].Notes.Should().HaveCount(3);
        converted.JudgeLineList[0].Notes[2].Type.Should().Be(NoteType.Hold);
        ((double)converted.JudgeLineList[0].Notes[2].EndBeat).Should().Be(2.5);
    }

    [Fact]
    public void FromIr_InvalidHoldPassesThroughWithoutMutatingInput()
    {
        var source = CreateIrChartWithNote(
            new Ir.Note { Type = NoteType.Hold, StartBeat = Beat(3) }
        );

        Action act = () => new IntermediateConverter().FromIr(source, null);

        act.Should().NotThrow();
        ((double)source.JudgeLineList[0].Notes[0].EndBeat).Should().Be(1);
    }

    [Fact]
    public void FromIr_NonHoldEndIsNormalizedForAllExternalFormatsWithoutMutatingInput()
    {
        var source = CreateIrChart(NoteType.Tap, 3, 9);

        var pe = new PhiEditConverter().FromIr(source, new IrToPhiEditConvertOptions());
        var pf = new PhiFansConverter().FromIr(source, new IrToPhiFansConvertOptions());
        var pc = new PhiChainConverter().FromIr(source, new IrToPhiChainConvertOptions());
        var phigros = new PhigrosV3Converter().FromIr(source, new IrToPhigrosV3ConvertOptions());
        var rpe = new RePhiEditConverter().FromIr(source, new ConvertOption());

        pe.JudgeLineList[0].NoteList[0].EndBeat.Should().Be(3);
        ((double)pf.JudgeLineList[0].NoteList[0].HoldEndBeat).Should().Be(3);
        ((double)pc.Lines[0].Notes[0].HoldBeat).Should().Be(0);
        phigros.JudgeLineList[0].NotesAbove[0].HoldTime.Should().Be(0);
        ((double)rpe.JudgeLineList[0].Notes![0].EndBeat).Should().Be(3);
        ((double)source.JudgeLineList[0].Notes[0].EndBeat).Should().Be(9);
    }

    [Fact]
    public void ChartPipeline_NormalizesSourceConverterResultBeforeTargetConverter()
    {
        var sourceChart = CreateIrChart(NoteType.Tap, 3, 9);
        var sourceConverter = new UnvalidatedIrConverter { ToIrResult = sourceChart };
        var targetConverter = new UnvalidatedIrConverter();

        var pipeline = ChartPipeline.From<Ir.Chart, Unit?, Unit?>(
            new Ir.Chart(),
            sourceConverter,
            null,
            TestContext.Current.CancellationToken
        );
        _ = pipeline.To<Ir.Chart, Unit?, Unit?>(targetConverter, null);

        targetConverter.ReceivedIr.Should().NotBeNull();
        ((double)targetConverter.ReceivedIr!.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        targetConverter.ReceivedIr.Should().NotBeSameAs(sourceChart);
        ((double)sourceChart.JudgeLineList[0].Notes[0].EndBeat).Should().Be(9);
    }

    [Fact]
    public void ChartPipeline_AllowsInvalidSourceConverterResult()
    {
        var sourceConverter = new UnvalidatedIrConverter
        {
            ToIrResult = CreateIrChartWithNote(
                new Ir.Note { Type = NoteType.Hold, StartBeat = Beat(3) }
            ),
        };

        Action act = () =>
            ChartPipeline.From<Ir.Chart, Unit?, Unit?>(
                new Ir.Chart(),
                sourceConverter,
                null,
                TestContext.Current.CancellationToken
            );

        act.Should().NotThrow();
    }

    [Fact]
    public void ChartPipeline_ReNormalizesEachTargetInputAndIsolatesSourceConverterReference()
    {
        var sourceChart = CreateIrChart(NoteType.Tap, 3, 9);
        var sourceConverter = new UnvalidatedIrConverter { ToIrResult = sourceChart };
        var firstTargetEndBeat = 0d;
        var firstTarget = new UnvalidatedIrConverter
        {
            OnFromIr = chart =>
            {
                firstTargetEndBeat = (double)chart.JudgeLineList[0].Notes[0].EndBeat;
                chart.JudgeLineList[0].Notes[0].EndBeat = Beat(15);
            },
        };
        var secondTarget = new UnvalidatedIrConverter();

        var pipeline = ChartPipeline.From<Ir.Chart, Unit?, Unit?>(
            new Ir.Chart(),
            sourceConverter,
            null,
            TestContext.Current.CancellationToken
        );
        sourceChart.JudgeLineList[0].Notes[0].EndBeat = Beat(11);
        _ = pipeline.To<Ir.Chart, Unit?, Unit?>(firstTarget, null);
        _ = pipeline.To<Ir.Chart, Unit?, Unit?>(secondTarget, null);

        firstTargetEndBeat.Should().Be(3);
        secondTarget.ReceivedIr.Should().NotBeNull();
        ((double)secondTarget.ReceivedIr!.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        firstTarget.ReceivedIr.Should().NotBeSameAs(secondTarget.ReceivedIr);
        ((double)sourceChart.JudgeLineList[0].Notes[0].EndBeat).Should().Be(11);
    }

    [Fact]
    public async Task ChartFormatDescriptor_ImportAsyncNormalizesIndependentCopy()
    {
        var importedChart = CreateIrChart(NoteType.Tap, 3, 9);
        var descriptor = CreateDescriptor();
        SetDescriptorDelegate(
            descriptor,
            "Importer",
            (Func<string, object?, ChartLogSink, CancellationToken, Task<Ir.Chart>>)(
                (_, _, _, _) => Task.FromResult(importedChart)
            )
        );

        var imported = await descriptor.ImportAsync(
            "ignored",
            ct: TestContext.Current.CancellationToken
        );

        ((double)imported.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        imported.Should().NotBeSameAs(importedChart);
        ((double)importedChart.JudgeLineList[0].Notes[0].EndBeat).Should().Be(9);
    }

    [Fact]
    public async Task ChartFormatDescriptor_ImportStreamAsyncNormalizesIndependentCopy()
    {
        var importedChart = CreateIrChart(NoteType.Drag, 3, 9);
        var descriptor = CreateDescriptor();
        SetDescriptorDelegate(
            descriptor,
            "StreamImporter",
            (Func<Stream, object?, ChartLogSink, CancellationToken, Task<Ir.Chart>>)(
                (_, _, _, _) => Task.FromResult(importedChart)
            )
        );
        await using var stream = new MemoryStream();

        var imported = await descriptor.ImportStreamAsync(
            stream,
            ct: TestContext.Current.CancellationToken
        );

        ((double)imported.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        imported.Should().NotBeSameAs(importedChart);
        ((double)importedChart.JudgeLineList[0].Notes[0].EndBeat).Should().Be(9);
    }

    [Fact]
    public async Task ChartFormatDescriptor_ExportAsyncPassesNormalizedIndependentCopyToExporter()
    {
        var sourceChart = CreateIrChart(NoteType.Flick, 3, 9);
        Ir.Chart? exportedChart = null;
        var descriptor = CreateDescriptor();
        SetDescriptorDelegate(
            descriptor,
            "Exporter",
            (Func<
                Ir.Chart,
                string,
                ChartWriteSettings,
                object?,
                ChartLogSink,
                CancellationToken,
                Task
            >)(
                (chart, _, _, _, _, _) =>
                {
                    exportedChart = chart;
                    return Task.CompletedTask;
                }
            )
        );

        await descriptor.ExportAsync(
            sourceChart,
            "ignored",
            ct: TestContext.Current.CancellationToken
        );

        exportedChart.Should().NotBeNull();
        ((double)exportedChart!.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        exportedChart.Should().NotBeSameAs(sourceChart);
        ((double)sourceChart.JudgeLineList[0].Notes[0].EndBeat).Should().Be(9);
    }

    [Fact]
    public async Task ChartFormatDescriptor_ExportAsyncPassesInvalidHoldToExporter()
    {
        var exporterStarted = false;
        var descriptor = CreateDescriptor();
        SetDescriptorDelegate(
            descriptor,
            "Exporter",
            (Func<
                Ir.Chart,
                string,
                ChartWriteSettings,
                object?,
                ChartLogSink,
                CancellationToken,
                Task
            >)(
                (_, _, _, _, _, _) =>
                {
                    exporterStarted = true;
                    return Task.CompletedTask;
                }
            )
        );
        var sourceChart = CreateIrChartWithNote(
            new Ir.Note { Type = NoteType.Hold, StartBeat = Beat(3) }
        );

        Func<Task> act = () =>
            descriptor.ExportAsync(
                sourceChart,
                "ignored",
                ct: TestContext.Current.CancellationToken
            );

        await act.Should().NotThrowAsync();
        exporterStarted.Should().BeTrue();
    }

    private static ChartFormatDescriptor CreateDescriptor() =>
        new() { Type = ChartType.PhiEdit, FileExtension = "test" };

    private static void SetDescriptorDelegate(
        ChartFormatDescriptor descriptor,
        string propertyName,
        object value
    )
    {
        var property = typeof(ChartFormatDescriptor).GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        property.Should().NotBeNull();
        property!.SetValue(descriptor, value);
    }

    private static Ir.Chart CreateIrChart(NoteType type, double startBeat, double endBeat) =>
        CreateIrChartWithNote(
            new Ir.Note
            {
                Type = type,
                StartBeat = Beat(startBeat),
                EndBeat = Beat(endBeat),
            }
        );

    private static Ir.Chart CreateIrChartWithNote(Ir.Note note) =>
        new() { JudgeLineList = [new Ir.JudgeLine { Notes = [note] }] };

    private static Pf.Chart CreatePhiFansChart(Pf.Note note) =>
        new() { JudgeLineList = [new Pf.Line { NoteList = [note] }] };

    private static Phigros.Chart CreatePhigrosChart(Phigros.Note note) =>
        new() { JudgeLineList = [new Phigros.JudgeLine { NotesAbove = [note], Bpm = 120 }] };

    private static Rpe.Chart CreateRePhiEditChart(Rpe.Note note) =>
        new() { JudgeLineList = [new Rpe.JudgeLine { Notes = [note] }] };

    private static Pc.Chart CreatePhiChainChart(Pc.Note note) =>
        new() { Lines = [new Pc.SerializedLine { Notes = [note] }] };

    private static Beat Beat(double value) => new(value);

    private sealed class UnvalidatedIrConverter : IChartConverter<Ir.Chart, Unit?, Unit?>
    {
        public Ir.Chart? ToIrResult { get; init; }

        public Ir.Chart? ReceivedIr { get; private set; }

        public Action<Ir.Chart>? OnFromIr { get; init; }

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

        public Ir.Chart ToIr(Ir.Chart input, Unit? options) => ToIrResult ?? input;

        public Ir.Chart FromIr(Ir.Chart input, Unit? options)
        {
            ReceivedIr = input;
            OnFromIr?.Invoke(input);
            return input;
        }
    }

    private sealed class TestDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
