#pragma warning disable CS0618

using KaedePhi.Core.Intermediate;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using KaedePhi.Tool.Render.KaedePhi;
using Kpc = KaedePhi.Core.KaedePhi;
using KpcCommon = KaedePhi.Core.Common;
using KpcControls = KaedePhi.Core.KaedePhi.Controls;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;
using IrChart = KaedePhi.Core.Intermediate.Chart;
using IrEvents = KaedePhi.Core.Intermediate.Events;

namespace KaedePhi.Tests.Compatibility;

/// <summary>
/// 验证废弃 KPC 兼容层的行为与迁移后的 IR 实现保持一致。
/// </summary>
public class KpcCompatibilityShimTests
{
    private const double Tolerance = 0.5;

    [Fact]
    public void KpcChartNormalizer_RoundTrip_PreservesChartData()
    {
        var chart = CreateLegacyChart();

        var normalized = KpcChartNormalizer.NormalizeAndValidateNoteEndBeats(chart);

        normalized.Should().NotBeSameAs(chart);
        normalized.Meta.Name.Should().Be("compat-chart");
        normalized.Meta.Offset.Should().Be(-25);
        normalized.BpmList.Should().HaveCount(2);
        normalized.BpmList[1].Bpm.Should().Be(180f);
        ((double)normalized.BpmList[1].StartBeat).Should().Be(4d);

        normalized.JudgeLineList.Should().HaveCount(1);
        var line = normalized.JudgeLineList[0];
        line.Name.Should().Be("line-0");
        line.Father.Should().Be(-1);
        line.AttachUi.Should().Be(KpcCommon.AttachUi.Pause);
        line.BpmFactor.Should().Be(2f);
        line.Anchor.Should().Equal(0.25f, 0.75f);
        line.EventLayers.Should().HaveCount(1);
        line.Notes.Should().HaveCount(2);
        line.Extended.TextEvents.Should().NotBeNull();
        line.PositionControls.Should().HaveCount(1);

        // 非 Hold 音符的结束拍被规范为起始拍
        line.Notes[0].Type.Should().Be(KpcCommon.NoteType.Tap);
        line.Notes[0].EndBeat.Should().Be(line.Notes[0].StartBeat);
        // Hold 音符的结束拍保持不变
        line.Notes[1].Type.Should().Be(KpcCommon.NoteType.Hold);
        ((double)line.Notes[1].EndBeat).Should().Be(6d);

        var speed = line.EventLayers[0].SpeedEvents![0];
        speed.StartValue.Should().Be(1.5f);
        speed.EndValue.Should().Be(2.5f);
        ((int)speed.Easing).Should().Be(5);
        speed.Font.Should().BeNull();

        var text = line.Extended.TextEvents![0];
        text.StartValue.Should().Be("start");
        text.EndValue.Should().Be("end");
        text.Font.Should().Be("font.ttf");

        var color = line.Extended.ColorEvents![0];
        color.StartValue.Should().Equal(1, 2, 3);
        color.EndValue.Should().Equal(4, 5, 6);

        line.PositionControls[0].Pos.Should().Be(0.5f);
        line.PositionControls[0].X.Should().Be(1.25f);
        ((int)line.PositionControls[0].Easing).Should().Be(7);
    }

    [Fact]
    public void KpcChartValidator_MappedHierarchy_DetectsCycle()
    {
        var lines = new List<Kpc.JudgeLine>
        {
            new() { Father = 1 },
            new() { Father = 0 },
        };

        var act = () => KpcChartValidator.ValidateJudgeLineHierarchy(lines);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void KpcRenderValidator_MappedChart_ValidatesLikeIrImplementation()
    {
        var chart = CreateLegacyChart();
        var options = new KpcRenderOptions();

        var legacyAct = () => KpcRenderValidator.Validate(chart, options);

        var irChart = ToIrChartForValidation(chart);
        var irAct = () => IrRenderValidator.Validate(irChart, options);

        legacyAct.Should().NotThrow();
        irAct.Should().NotThrow();
    }

    [Fact]
    public void EventFit_LegacyOverload_MatchesIrImplementation()
    {
        var legacyEvents = new List<KpcEvents.Event<double>>
        {
            CreateLegacyEvent(0, 1, 0, 100),
            CreateLegacyEvent(1, 2, 100, 200),
            CreateLegacyEvent(2, 3, 200, 300),
        };
        var irEvents = legacyEvents.Select(ToIrEvent).ToList();

        var legacyResult = new KaedePhi.Tool.Event.KaedePhi.EventFit<double>().FitEvents(
            legacyEvents,
            Tolerance
        );
        var irResult = new KaedePhi.Tool.Event.Intermediate.EventFit<double>().FitEvents(
            irEvents,
            Tolerance
        );

        legacyResult.Should().HaveCount(irResult.Count);
        for (var i = 0; i < legacyResult.Count; i++)
        {
            ((double)legacyResult[i].StartBeat).Should().Be((double)irResult[i].StartBeat);
            ((double)legacyResult[i].EndBeat).Should().Be((double)irResult[i].EndBeat);
            legacyResult[i].StartValue.Should().Be(irResult[i].StartValue);
            legacyResult[i].EndValue.Should().Be(irResult[i].EndValue);
            ((int)legacyResult[i].Easing).Should().Be((int)irResult[i].Easing);
        }
    }

    [Fact]
    public async Task ChartFormatDescriptor_LegacyImportExport_RoundTrips()
    {
        var chart = CreateLegacyChart();
        var descriptor = ChartFormatRegistry.Get(ChartType.RePhiEdit);
        var path = Path.Combine(Path.GetTempPath(), $"kpc-compat-{Guid.NewGuid():N}.json");

        try
        {
            await descriptor.ExportAsync(
                chart,
                path,
                exportOptions: new ConvertOption(),
                ct: TestContext.Current.CancellationToken
            );

            var text = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            var imported = await descriptor.ImportAsync(
                text,
                ct: TestContext.Current.CancellationToken
            );

            imported.Meta.Name.Should().Be(chart.Meta.Name);
            imported.BpmList.Should().HaveCount(chart.BpmList.Count);
            imported.JudgeLineList.Should().HaveCount(chart.JudgeLineList.Count);
            imported.JudgeLineList[0].Notes.Should().HaveCount(chart.JudgeLineList[0].Notes.Count);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ChartFormatDescriptor_LegacyOptions_AreAccepted()
    {
        var chart = CreateLegacyChart();
        var descriptor = ChartFormatRegistry.Get(ChartType.PhiEdit);

        var export = async () =>
            await descriptor.ExportAsync(
                chart,
                "unused.pec",
                new ChartWriteSettings { DryRun = true },
                new KaedePhi.Tool.Converter.PhiEdit.Model.KpcToPhiEditConvertOptions()
            );

        await export.Should().NotThrowAsync();
    }

    private static Kpc.Chart CreateLegacyChart()
    {
        var line = new Kpc.JudgeLine
        {
            Name = "line-0",
            Father = -1,
            AttachUi = KpcCommon.AttachUi.Pause,
            BpmFactor = 2f,
            Anchor = [0.25f, 0.75f],
            Extended = new KpcEvents.ExtendLayer
            {
                TextEvents =
                [
                    new KpcEvents.Event<string>
                    {
                        StartBeat = new KpcCommon.Beat(0d),
                        EndBeat = new KpcCommon.Beat(1d),
                        StartValue = "start",
                        EndValue = "end",
                        Font = "font.ttf",
                    },
                ],
                ColorEvents =
                [
                    new KpcEvents.Event<byte[]>
                    {
                        StartBeat = new KpcCommon.Beat(0d),
                        EndBeat = new KpcCommon.Beat(1d),
                        StartValue = [1, 2, 3],
                        EndValue = [4, 5, 6],
                    },
                ],
            },
            EventLayers =
            [
                new KpcEvents.EventLayer
                {
                    SpeedEvents =
                    [
                        new KpcEvents.Event<float>
                        {
                            StartBeat = new KpcCommon.Beat(0d),
                            EndBeat = new KpcCommon.Beat(2d),
                            StartValue = 1.5f,
                            EndValue = 2.5f,
                            Easing = new Kpc.Easing(5),
                        },
                    ],
                },
            ],
            Notes =
            [
                new Kpc.Note
                {
                    Type = KpcCommon.NoteType.Tap,
                    StartBeat = new KpcCommon.Beat(1d),
                    EndBeat = new KpcCommon.Beat(2d),
                    PositionX = 0.5,
                    Tint = [10, 20, 30],
                },
                new Kpc.Note
                {
                    Type = KpcCommon.NoteType.Hold,
                    StartBeat = new KpcCommon.Beat(3d),
                    EndBeat = new KpcCommon.Beat(6d),
                    PositionX = -0.5,
                },
            ],
            PositionControls =
            [
                new KpcControls.XControl
                {
                    Pos = 0.5f,
                    X = 1.25f,
                    Easing = new Kpc.Easing(7),
                },
            ],
        };

        return new Kpc.Chart
        {
            Meta = new Kpc.Meta
            {
                Name = "compat-chart",
                Author = "compat-author",
                Offset = -25,
            },
            BpmList =
            [
                new Kpc.BpmItem { Bpm = 120f, StartBeat = new KpcCommon.Beat(0d) },
                new Kpc.BpmItem { Bpm = 180f, StartBeat = new KpcCommon.Beat(4d) },
            ],
            JudgeLineList = [line],
        };
    }

    private static IrChart ToIrChartForValidation(Kpc.Chart chart)
    {
        return new IrChart
        {
            Meta = new Meta
            {
                Name = chart.Meta.Name,
                Author = chart.Meta.Author,
                Offset = chart.Meta.Offset,
            },
            BpmList =
            [
                new BpmItem { Bpm = 120f, StartBeat = new KaedePhi.Core.Primitives.Beat(0d) },
                new BpmItem { Bpm = 180f, StartBeat = new KaedePhi.Core.Primitives.Beat(4d) },
            ],
            JudgeLineList =
            [
                new JudgeLine
                {
                    Name = "line-0",
                    Father = -1,
                    BpmFactor = 2f,
                    Anchor = [0.25f, 0.75f],
                },
            ],
        };
    }

    private static KpcEvents.Event<double> CreateLegacyEvent(
        double startBeat,
        double endBeat,
        double startValue,
        double endValue
    ) =>
        new()
        {
            StartBeat = new KpcCommon.Beat(startBeat),
            EndBeat = new KpcCommon.Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue,
        };

    private static IrEvents.Event<double> ToIrEvent(KpcEvents.Event<double> evt) =>
        new()
        {
            StartBeat = new KaedePhi.Core.Primitives.Beat((int[])evt.StartBeat),
            EndBeat = new KaedePhi.Core.Primitives.Beat((int[])evt.EndBeat),
            StartValue = evt.StartValue,
            EndValue = evt.EndValue,
            Easing = new Easing((int)evt.Easing),
        };
}
