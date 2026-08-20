using System.Reflection;
using KaedePhi.Core.Common;
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
using Kpc = KaedePhi.Core.KaedePhi;
using Pc = KaedePhi.Core.PhiChain.v6;
using Pe = KaedePhi.Core.PhiEdit;
using Pf = KaedePhi.Core.PhiFans;
using Phigros = KaedePhi.Core.Phigros.v3;
using Rpe = KaedePhi.Core.RePhiEdit;

namespace KaedePhi.Tests.Validation;

public class NoteEndBeatInvariantTests
{
    [Fact]
    public void NormalizeAndValidateNoteEndBeats_NullChartThrows()
    {
        Action act = () => ChartProcessingValidator.NormalizeAndValidateNoteEndBeats(null!);

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
        var source = CreateKpcChart(type, 3, 9);

        var normalized = ChartProcessingValidator.NormalizeAndValidateNoteEndBeats(source);

        ((double)normalized.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        ((double)source.JudgeLineList[0].Notes[0].EndBeat).Should().Be(9);
        normalized.Should().NotBeSameAs(source);
        normalized.JudgeLineList[0].Should().NotBeSameAs(source.JudgeLineList[0]);
        normalized.JudgeLineList[0].Notes[0].Should().NotBeSameAs(source.JudgeLineList[0].Notes[0]);
    }

    [Fact]
    public void NormalizeAndValidateNoteEndBeats_HoldWithoutExplicitEndThrowsWithLocation()
    {
        var source = CreateKpcChartWithNote(
            new Kpc.Note { Type = NoteType.Hold, StartBeat = Beat(3) }
        );

        Action act = () => ChartProcessingValidator.NormalizeAndValidateNoteEndBeats(source);

        act.Should().Throw<FormatException>().WithMessage("*0*0*");
    }

    [Theory]
    [InlineData(3)]
    [InlineData(2)]
    public void NormalizeAndValidateNoteEndBeats_HoldNotAfterStartThrows(double endBeat)
    {
        var source = CreateKpcChart(NoteType.Hold, 3, endBeat);

        Action act = () => ChartProcessingValidator.NormalizeAndValidateNoteEndBeats(source);

        act.Should().Throw<FormatException>().WithMessage("*0*0*");
        ((double)source.JudgeLineList[0].Notes[0].EndBeat).Should().Be(endBeat);
    }

    [Fact]
    public void NormalizeAndValidateNoteEndBeats_ValidHoldKeepsEndOnIndependentCopy()
    {
        var source = CreateKpcChart(NoteType.Hold, 3, 5);

        var normalized = ChartProcessingValidator.NormalizeAndValidateNoteEndBeats(source);

        ((double)normalized.JudgeLineList[0].Notes[0].EndBeat).Should().Be(5);
        normalized.JudgeLineList[0].Notes[0].Should().NotBeSameAs(source.JudgeLineList[0].Notes[0]);
    }

    [Fact]
    public void KaedePhiConverter_ToKpcAndFromKpcReturnIndependentNormalizedCopies()
    {
        var source = CreateKpcChart(NoteType.Tap, 3, 9);
        var converter = new KaedePhiConverter();

        var imported = converter.ToKpc(source, null);
        var exported = converter.FromKpc(source, null);

        ((double)imported.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        ((double)exported.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        imported.Should().NotBeSameAs(source);
        exported.Should().NotBeSameAs(source);
        ((double)source.JudgeLineList[0].Notes[0].EndBeat).Should().Be(9);
    }

    [Fact]
    public void PhiEditConverter_ToKpcNormalizesNonHoldAndRejectsInvalidSourceHold()
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

        var converted = converter.ToKpc(nonHold, new PhiEditToKpcConvertOptions());
        Action act = () => converter.ToKpc(invalidHold, new PhiEditToKpcConvertOptions());

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void PhiEditConverter_ToKpcRejectsNonFiniteHoldEndBeat(float endBeat)
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

        Action act = () =>
            new PhiEditConverter().ToKpc(source, new PhiEditToKpcConvertOptions());

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhiEditConverter_ToKpcKeepsValidHoldEndBeat()
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

        var converted = new PhiEditConverter().ToKpc(
            source,
            new PhiEditToKpcConvertOptions()
        );

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(5);
    }

    [Fact]
    public void PhiFansConverter_ToKpcNormalizesNonHoldAndRejectsMissingSourceHoldEnd()
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

        var converted = converter.ToKpc(nonHold, null);
        Action act = () => converter.ToKpc(invalidHold, null);

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhiFansConverter_ToKpcKeepsValidHoldEndBeat()
    {
        var source = CreatePhiFansChart(
            new Pf.Note
            {
                Type = Pf.NoteType.Hold,
                Beat = Beat(3),
                HoldEndBeat = Beat(5),
            }
        );

        var converted = new PhiFansConverter().ToKpc(source, null);

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(5);
    }

    [Fact]
    public void PhigrosV3Converter_ToKpcNormalizesNonHoldAndRejectsMissingSourceHoldTime()
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

        var converted = converter.ToKpc(nonHold, null);
        Action act = () => converter.ToKpc(invalidHold, null);

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void PhigrosV3Converter_ToKpcRejectsNonFiniteHoldTime(float holdTime)
    {
        var source = CreatePhigrosChart(
            new Phigros.Note
            {
                Type = Phigros.NoteType.Hold,
                Time = 96,
                HoldTime = holdTime,
            }
        );

        Action act = () => new PhigrosV3Converter().ToKpc(source, null);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhigrosV3Converter_ToKpcKeepsValidHoldEndBeat()
    {
        var source = CreatePhigrosChart(
            new Phigros.Note
            {
                Type = Phigros.NoteType.Hold,
                Time = 96,
                HoldTime = 64,
            }
        );

        var converted = new PhigrosV3Converter().ToKpc(source, null);

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(5);
    }

    [Fact]
    public void RePhiEditConverter_ToKpcNormalizesNonHoldAndRejectsMissingSourceHoldEnd()
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

        var converted = converter.ToKpc(nonHold, null);
        Action act = () => converter.ToKpc(invalidHold, null);

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void RePhiEditConverter_ToKpcKeepsValidHoldEndBeat()
    {
        var source = CreateRePhiEditChart(
            new Rpe.Note
            {
                Type = NoteType.Hold,
                StartBeat = Beat(3),
                EndBeat = Beat(5),
            }
        );

        var converted = new RePhiEditConverter().ToKpc(source, null);

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(5);
    }

    [Fact]
    public void PhiChainConverter_ToKpcNormalizesNonHoldAndRejectsZeroDurationSourceHold()
    {
        var converter = new PhiChainConverter();
        var nonHold = CreatePhiChainChart(
            new Pc.Note { Type = Pc.NoteType.Tap, Beat = Beat(3) }
        );
        var invalidHold = CreatePhiChainChart(
            new Pc.Note
            {
                Type = Pc.NoteType.Hold,
                Beat = Beat(3),
                HoldBeat = Beat(0),
            }
        );

        var converted = converter.ToKpc(nonHold, new PhiChainToKpcConvertOptions());
        Action act = () =>
            converter.ToKpc(invalidHold, new PhiChainToKpcConvertOptions());

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhiChainConverter_ToKpcKeepsValidOrdinaryHoldEndBeat()
    {
        var source = CreatePhiChainChart(
            new Pc.Note
            {
                Type = Pc.NoteType.Hold,
                Beat = Beat(3),
                HoldBeat = Beat(2),
            }
        );

        var converted = new PhiChainConverter().ToKpc(
            source,
            new PhiChainToKpcConvertOptions()
        );

        ((double)converted.JudgeLineList[0].Notes[0].EndBeat).Should().Be(5);
    }

    [Fact]
    public void PhiChainConverter_ToKpcRejectsCurveHoldWithoutDuration()
    {
        var line = new Pc.SerializedLine
        {
            Notes =
            [
                new Pc.Note { Beat = Beat(0) },
                new Pc.Note { Beat = Beat(1) },
            ],
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

        Action act = () =>
            new PhiChainConverter().ToKpc(source, new PhiChainToKpcConvertOptions());

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void PhiChainConverter_ToKpcKeepsValidCurveHoldEndBeat()
    {
        var line = new Pc.SerializedLine
        {
            Notes =
            [
                new Pc.Note { Beat = Beat(0) },
                new Pc.Note { Beat = Beat(1) },
            ],
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

        var converted = new PhiChainConverter().ToKpc(
            source,
            new PhiChainToKpcConvertOptions()
        );

        converted.JudgeLineList[0].Notes.Should().HaveCount(3);
        converted.JudgeLineList[0].Notes[2].Type.Should().Be(NoteType.Hold);
        ((double)converted.JudgeLineList[0].Notes[2].EndBeat).Should().Be(2.5);
    }

    [Theory]
    [InlineData("KaedePhi")]
    [InlineData("PhiEdit")]
    [InlineData("PhiFans")]
    [InlineData("PhiChain")]
    [InlineData("PhigrosV3")]
    [InlineData("RePhiEdit")]
    public void FromKpc_InvalidHoldIsRejectedWithoutMutatingInput(string format)
    {
        var source = CreateKpcChartWithNote(
            new Kpc.Note { Type = NoteType.Hold, StartBeat = Beat(3) }
        );

        Action act = () => ConvertFromKpc(format, source);

        act.Should().Throw<FormatException>();
        ((double)source.JudgeLineList[0].Notes[0].EndBeat).Should().Be(1);
    }

    [Fact]
    public void FromKpc_NonHoldEndIsNormalizedForAllExternalFormatsWithoutMutatingInput()
    {
        var source = CreateKpcChart(NoteType.Tap, 3, 9);

        var pe = new PhiEditConverter().FromKpc(source, new KpcToPhiEditConvertOptions());
        var pf = new PhiFansConverter().FromKpc(source, new KpcToPhiFansConvertOptions());
        var pc = new PhiChainConverter().FromKpc(source, new KpcToPhiChainConvertOptions());
        var phigros = new PhigrosV3Converter().FromKpc(
            source,
            new KpcToPhigrosV3ConvertOptions()
        );
        var rpe = new RePhiEditConverter().FromKpc(source, new ConvertOption());

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
        var sourceChart = CreateKpcChart(NoteType.Tap, 3, 9);
        var sourceConverter = new UnvalidatedKpcConverter { ToKpcResult = sourceChart };
        var targetConverter = new UnvalidatedKpcConverter();

        var pipeline = ChartPipeline.From<Kpc.Chart, Unit?, Unit?>(
            new Kpc.Chart(),
            sourceConverter,
            null,
            TestContext.Current.CancellationToken
        );
        _ = pipeline.To<Kpc.Chart, Unit?, Unit?>(targetConverter, null);

        targetConverter.ReceivedKpc.Should().NotBeNull();
        ((double)targetConverter.ReceivedKpc!.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        targetConverter.ReceivedKpc.Should().NotBeSameAs(sourceChart);
        ((double)sourceChart.JudgeLineList[0].Notes[0].EndBeat).Should().Be(9);
    }

    [Fact]
    public void ChartPipeline_RejectsInvalidSourceConverterResultBeforeTargetConverterCanRun()
    {
        var sourceConverter = new UnvalidatedKpcConverter
        {
            ToKpcResult = CreateKpcChartWithNote(
                new Kpc.Note { Type = NoteType.Hold, StartBeat = Beat(3) }
            ),
        };

        Action act = () =>
            ChartPipeline.From<Kpc.Chart, Unit?, Unit?>(
                new Kpc.Chart(),
                sourceConverter,
                null,
                TestContext.Current.CancellationToken
            );

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ChartPipeline_ReNormalizesEachTargetInputAndIsolatesSourceConverterReference()
    {
        var sourceChart = CreateKpcChart(NoteType.Tap, 3, 9);
        var sourceConverter = new UnvalidatedKpcConverter { ToKpcResult = sourceChart };
        var firstTargetEndBeat = 0d;
        var firstTarget = new UnvalidatedKpcConverter
        {
            OnFromKpc = chart =>
            {
                firstTargetEndBeat = (double)chart.JudgeLineList[0].Notes[0].EndBeat;
                chart.JudgeLineList[0].Notes[0].EndBeat = Beat(15);
            },
        };
        var secondTarget = new UnvalidatedKpcConverter();

        var pipeline = ChartPipeline.From<Kpc.Chart, Unit?, Unit?>(
            new Kpc.Chart(),
            sourceConverter,
            null,
            TestContext.Current.CancellationToken
        );
        sourceChart.JudgeLineList[0].Notes[0].EndBeat = Beat(11);
        _ = pipeline.To<Kpc.Chart, Unit?, Unit?>(firstTarget, null);
        _ = pipeline.To<Kpc.Chart, Unit?, Unit?>(secondTarget, null);

        firstTargetEndBeat.Should().Be(3);
        secondTarget.ReceivedKpc.Should().NotBeNull();
        ((double)secondTarget.ReceivedKpc!.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        firstTarget.ReceivedKpc.Should().NotBeSameAs(secondTarget.ReceivedKpc);
        ((double)sourceChart.JudgeLineList[0].Notes[0].EndBeat).Should().Be(11);
    }

    [Fact]
    public async Task ChartFormatDescriptor_ImportAsyncNormalizesIndependentCopy()
    {
        var importedChart = CreateKpcChart(NoteType.Tap, 3, 9);
        var descriptor = CreateDescriptor();
        SetDescriptorDelegate(
            descriptor,
            "Importer",
            (Func<string, object?, ChartLogSink, CancellationToken, Task<Kpc.Chart>>)(
                (_, _, _, _) => Task.FromResult(importedChart)
            )
        );

        var imported = await descriptor.ImportAsync("ignored", ct: TestContext.Current.CancellationToken);

        ((double)imported.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        imported.Should().NotBeSameAs(importedChart);
        ((double)importedChart.JudgeLineList[0].Notes[0].EndBeat).Should().Be(9);
    }

    [Fact]
    public async Task ChartFormatDescriptor_ImportStreamAsyncNormalizesIndependentCopy()
    {
        var importedChart = CreateKpcChart(NoteType.Drag, 3, 9);
        var descriptor = CreateDescriptor();
        SetDescriptorDelegate(
            descriptor,
            "StreamImporter",
            (Func<Stream, object?, ChartLogSink, CancellationToken, Task<Kpc.Chart>>)(
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
        var sourceChart = CreateKpcChart(NoteType.Flick, 3, 9);
        Kpc.Chart? exportedChart = null;
        var descriptor = CreateDescriptor();
        SetDescriptorDelegate(
            descriptor,
            "Exporter",
            (Func<Kpc.Chart, string, ChartWriteSettings, object?, ChartLogSink, CancellationToken, Task>)(
                (chart, _, _, _, _, _) =>
                {
                    exportedChart = chart;
                    return Task.CompletedTask;
                }
            )
        );

        await descriptor.ExportAsync(sourceChart, "ignored", ct: TestContext.Current.CancellationToken);

        exportedChart.Should().NotBeNull();
        ((double)exportedChart!.JudgeLineList[0].Notes[0].EndBeat).Should().Be(3);
        exportedChart.Should().NotBeSameAs(sourceChart);
        ((double)sourceChart.JudgeLineList[0].Notes[0].EndBeat).Should().Be(9);
    }

    [Fact]
    public async Task ChartFormatDescriptor_ExportAsyncRejectsInvalidHoldBeforeExporterStarts()
    {
        var exporterStarted = false;
        var descriptor = CreateDescriptor();
        SetDescriptorDelegate(
            descriptor,
            "Exporter",
            (Func<Kpc.Chart, string, ChartWriteSettings, object?, ChartLogSink, CancellationToken, Task>)(
                (_, _, _, _, _, _) =>
                {
                    exporterStarted = true;
                    return Task.CompletedTask;
                }
            )
        );
        var sourceChart = CreateKpcChartWithNote(
            new Kpc.Note { Type = NoteType.Hold, StartBeat = Beat(3) }
        );

        Func<Task> act = () =>
            descriptor.ExportAsync(sourceChart, "ignored", ct: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FormatException>();
        exporterStarted.Should().BeFalse();
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

    private static void ConvertFromKpc(string format, Kpc.Chart source)
    {
        switch (format)
        {
            case "KaedePhi":
                _ = new KaedePhiConverter().FromKpc(source, null);
                break;
            case "PhiEdit":
                _ = new PhiEditConverter().FromKpc(source, new KpcToPhiEditConvertOptions());
                break;
            case "PhiFans":
                _ = new PhiFansConverter().FromKpc(source, new KpcToPhiFansConvertOptions());
                break;
            case "PhiChain":
                _ = new PhiChainConverter().FromKpc(source, new KpcToPhiChainConvertOptions());
                break;
            case "PhigrosV3":
                _ = new PhigrosV3Converter().FromKpc(
                    source,
                    new KpcToPhigrosV3ConvertOptions()
                );
                break;
            case "RePhiEdit":
                _ = new RePhiEditConverter().FromKpc(source, new ConvertOption());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    private static Kpc.Chart CreateKpcChart(NoteType type, double startBeat, double endBeat) =>
        CreateKpcChartWithNote(
            new Kpc.Note
            {
                Type = type,
                StartBeat = Beat(startBeat),
                EndBeat = Beat(endBeat),
            }
        );

    private static Kpc.Chart CreateKpcChartWithNote(Kpc.Note note) =>
        new() { JudgeLineList = [new Kpc.JudgeLine { Notes = [note] }] };

    private static Pf.Chart CreatePhiFansChart(Pf.Note note) =>
        new() { JudgeLineList = [new Pf.Line { NoteList = [note] }] };

    private static Phigros.Chart CreatePhigrosChart(Phigros.Note note) =>
        new()
        {
            JudgeLineList =
            [
                new Phigros.JudgeLine
                {
                    NotesAbove = [note],
                    Bpm = 120,
                },
            ],
        };

    private static Rpe.Chart CreateRePhiEditChart(Rpe.Note note) =>
        new() { JudgeLineList = [new Rpe.JudgeLine { Notes = [note] }] };

    private static Pc.Chart CreatePhiChainChart(Pc.Note note) =>
        new() { Lines = [new Pc.SerializedLine { Notes = [note] }] };

    private static Beat Beat(double value) => new(value);

    private sealed class UnvalidatedKpcConverter : IChartConverter<Kpc.Chart, Unit?, Unit?>
    {
        public Kpc.Chart? ToKpcResult { get; init; }

        public Kpc.Chart? ReceivedKpc { get; private set; }

        public Action<Kpc.Chart>? OnFromKpc { get; init; }

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

        public Kpc.Chart ToKpc(Kpc.Chart input, Unit? options) => ToKpcResult ?? input;

        public Kpc.Chart FromKpc(Kpc.Chart input, Unit? options)
        {
            ReceivedKpc = input;
            OnFromKpc?.Invoke(input);
            return input;
        }
    }

    private sealed class TestDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
