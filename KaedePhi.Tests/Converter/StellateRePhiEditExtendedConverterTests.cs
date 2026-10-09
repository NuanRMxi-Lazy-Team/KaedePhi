using KaedePhi.Core.Formats.RePhiEdit.Model;
using KaedePhi.Core.Formats.RePhiEdit.Serialization;
using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter;
using KaedePhi.Tool.Converter.Phigros.v3;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using PhigrosAreaEase = KaedePhi.Core.Formats.Phigros.v3.Model.AreaEase;
using PhigrosAreaEaseType = KaedePhi.Core.Formats.Phigros.v3.Model.AreaEaseType;
using PhigrosBlockArea = KaedePhi.Core.Formats.Phigros.v3.Model.BlockArea;
using IrBlockArea = KaedePhi.Core.Intermediate.Model.BlockArea;
using IrEvent = KaedePhi.Core.Intermediate.Model.Events.Event<double>;
using RpeEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<float>;
using RpeEventLayer = KaedePhi.Core.Formats.RePhiEdit.Model.Events.EventLayer;
using RpeExtendLayer = KaedePhi.Core.Formats.RePhiEdit.Model.Events.ExtendLayer;
using RpeJudgeLine = KaedePhi.Core.Formats.RePhiEdit.Model.JudgeLine;
using IrBpmItem = KaedePhi.Core.Intermediate.Model.BpmItem;
using IrChart = KaedePhi.Core.Intermediate.Model.Chart;
using IrEasing = KaedePhi.Core.Intermediate.Model.Easing;
using IrJudgeLine = KaedePhi.Core.Intermediate.Model.JudgeLine;
using Xunit;

namespace KaedePhi.Tests.Converter;

public class StellateRePhiEditExtendedConverterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExportEncodesIrBlockAreasAsEditableMarkerLines(bool useStream)
    {
        var source = new IrChart
        {
            BpmList = [new IrBpmItem { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [new IrJudgeLine { Texture = "line.png" }],
            BlockAreaList =
            [
                new IrBlockArea
                {
                    TopRightX = 0.75d,
                    TopRightY = 0.75d,
                    BottomLeftX = -0.25d,
                    BottomLeftY = -0.25d,
                    AppearBeat = new Beat(1),
                    EnableBeat = new Beat(2),
                    DisableBeat = new Beat(3),
                    DisappearBeat = new Beat(4),
                    IsSubtract = true,
                    MoveXEvents = [CreateIrEvent(0, 4, 0.25d, 0.5d, 5)],
                    MoveYEvents = [CreateIrEvent(0, 4, 0.25d, -0.25d)],
                    RotateEvents = [CreateIrEvent(0, 4, 0d, 90d)],
                    ScaleXEvents = [CreateIrEvent(0, 4, 1d, 2d)],
                    ScaleYEvents = [CreateIrEvent(0, 4, 1d, 0.5d)],
                },
            ],
        };

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            var descriptor = ChartFormatRegistry.Get(ChartType.StellateRePhiEditExtended);
            Assert.True(descriptor.CanExport);
            await descriptor.ExportAsync(
                source,
                path,
                new ChartWriteSettings { UseStream = useStream },
                ct: TestContext.Current.CancellationToken
            );

            var json = await File.ReadAllTextAsync(
                path,
                TestContext.Current.CancellationToken
            );
            var exported = await ChartSerialization.LoadFromJsonAsync(json);
            Assert.Equal(2, exported.JudgeLineList.Count);
            var markerLine = exported.JudgeLineList[1];
            Assert.Equal(@"Pictures\isSubtract1.png", markerLine.Texture);
            Assert.Equal(4, markerLine.EventLayers[0].SpeedEvents!.Count);
            var alphaEvents = markerLine.EventLayers[0].AlphaEvents!;
            Assert.Equal(
                new[] { 0, 128, 255, 0 },
                alphaEvents.Select(evt => evt.EndValue)
            );
            Assert.Equal(
                new[] { 0, 1, 2, 3 }.Select(beat => new Beat(beat)),
                alphaEvents.Select(evt => evt.StartBeat)
            );

            var (detectedType, converted) = await ChartFormatRegistry.ImportIrAsync(
                json,
                ct: TestContext.Current.CancellationToken
            );

            Assert.Equal(ChartType.StellateRePhiEditExtended, detectedType);
            Assert.Single(converted.JudgeLineList);
            var area = Assert.Single(converted.BlockAreaList);
            Assert.True(area.IsSubtract);
            Assert.Equal(0.75d, area.TopRightX, 6);
            Assert.Equal(0.75d, area.TopRightY, 6);
            Assert.Equal(-0.25d, area.BottomLeftX, 6);
            Assert.Equal(-0.25d, area.BottomLeftY, 6);
            Assert.Equal(new Beat(1), area.AppearBeat);
            Assert.Equal(new Beat(2), area.EnableBeat);
            Assert.Equal(new Beat(3), area.DisableBeat);
            Assert.Equal(new Beat(4), area.DisappearBeat);
            var moveXEvent = Assert.Single(area.MoveXEvents!);
            Assert.Equal(0.5d, moveXEvent.EndValue, 6);
            Assert.Equal(5, (int)moveXEvent.Easing);
            Assert.Equal(-0.25d, area.MoveYEvents!.Single().EndValue, 6);
            Assert.Equal(90d, area.RotateEvents!.Single().EndValue, 6);
            Assert.Equal(2d, area.ScaleXEvents!.Single().EndValue, 6);
            Assert.Equal(0.5d, area.ScaleYEvents!.Single().EndValue, 6);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ExportUsesActiveOpacityWhenAppearAndEnableBeatsCoincide()
    {
        var source = new IrChart
        {
            BlockAreaList =
            [
                new IrBlockArea
                {
                    TopRightX = 0.5d,
                    TopRightY = 0.5d,
                    BottomLeftX = -0.5d,
                    BottomLeftY = -0.5d,
                    AppearBeat = new Beat(2),
                    EnableBeat = new Beat(2),
                    DisableBeat = new Beat(4),
                    DisappearBeat = new Beat(5),
                },
            ],
        };

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            await ChartFormatRegistry.Get(ChartType.StellateRePhiEditExtended).ExportAsync(
                source,
                path,
                ct: TestContext.Current.CancellationToken
            );
            var json = await File.ReadAllTextAsync(
                path,
                TestContext.Current.CancellationToken
            );
            var exported = await ChartSerialization.LoadFromJsonAsync(json);
            var alphaEvents = exported.JudgeLineList[0].EventLayers[0].AlphaEvents!;

            Assert.Equal(
                new[] { 0, 255, 0 },
                alphaEvents.Select(evt => evt.EndValue)
            );
            Assert.Equal(
                new[] { 0, 2, 4 }.Select(beat => new Beat(beat)),
                alphaEvents.Select(evt => evt.StartBeat)
            );
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ExportBakesChangingScaleAndRotationAnchorsIntoPosition()
    {
        var source = new IrChart
        {
            BpmList = [new IrBpmItem { Bpm = 120f, StartBeat = new Beat(0) }],
            BlockAreaList =
            [
                new IrBlockArea
                {
                    TopRightX = 0.5d,
                    TopRightY = 0.5d,
                    BottomLeftX = -0.5d,
                    BottomLeftY = -0.5d,
                    AppearBeat = new Beat(0),
                    EnableBeat = new Beat(0),
                    DisableBeat = new Beat(5),
                    DisappearBeat = new Beat(6),
                    RotateEvents = [CreateIrEvent(0, 4, 0d, 90d)],
                    RotateAnchorXEvents = [CreateIrEvent(0, 4, 0d, 0d)],
                    RotateAnchorYEvents = [CreateIrEvent(0, 4, 0d, 0.5d)],
                    ScaleXEvents = [CreateIrEvent(0, 4, 1d, 2d)],
                    ScaleYEvents = [CreateIrEvent(0, 4, 1d, 1d)],
                    ScaleAnchorXEvents = [CreateIrEvent(0, 4, 0d, 0.5d)],
                    ScaleAnchorYEvents = [CreateIrEvent(0, 4, 0d, 0d)],
                },
            ],
        };

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            await ChartFormatRegistry.Get(ChartType.StellateRePhiEditExtended).ExportAsync(
                source,
                path,
                ct: TestContext.Current.CancellationToken
            );
            var json = await File.ReadAllTextAsync(
                path,
                TestContext.Current.CancellationToken
            );
            var (_, converted) = await ChartFormatRegistry.ImportIrAsync(
                json,
                ct: TestContext.Current.CancellationToken
            );

            var area = Assert.Single(converted.BlockAreaList);
            var centerX = GetIrCenterX(area);
            var centerY = (area.TopRightY + area.BottomLeftY) / 2d;
            var movedX = EvaluateIrTrack(area.MoveXEvents, new Beat(4), centerX);
            var movedY = EvaluateIrTrack(area.MoveYEvents, new Beat(4), centerY);

            Assert.Equal(1d / 3d, movedX, 3);
            Assert.Equal(-0.25d, movedY, 3);
            Assert.Null(area.RotateAnchorXEvents);
            Assert.Null(area.ScaleAnchorXEvents);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ExportAppliesScaleAnchorBeforeTheFinalMove()
    {
        var source = new IrChart
        {
            BpmList = [new IrBpmItem { Bpm = 120f, StartBeat = new Beat(0) }],
            BlockAreaList =
            [
                new IrBlockArea
                {
                    TopRightX = 1d,
                    TopRightY = 1d,
                    BottomLeftX = -1d,
                    BottomLeftY = -1d,
                    AppearBeat = new Beat(0),
                    EnableBeat = new Beat(0),
                    DisableBeat = new Beat(4),
                    DisappearBeat = new Beat(5),
                    MoveXEvents = [CreateIrEvent(0, 2, 0d, 0.5d)],
                    ScaleXEvents = [CreateIrEvent(0, 2, 1d, 2d)],
                    ScaleYEvents = [CreateIrEvent(0, 2, 1d, 1d)],
                    ScaleAnchorXEvents = [CreateIrEvent(0, 2, -1d, -1d)],
                    ScaleAnchorYEvents = [CreateIrEvent(0, 2, -1d, -1d)],
                },
            ],
        };

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            await ChartFormatRegistry.Get(ChartType.StellateRePhiEditExtended).ExportAsync(
                source,
                path,
                ct: TestContext.Current.CancellationToken
            );
            var json = await File.ReadAllTextAsync(
                path,
                TestContext.Current.CancellationToken
            );
            var (_, converted) = await ChartFormatRegistry.ImportIrAsync(
                json,
                ct: TestContext.Current.CancellationToken
            );

            var area = Assert.Single(converted.BlockAreaList);
            var movedX = EvaluateIrTrack(area.MoveXEvents, new Beat(2), GetIrCenterX(area));

            // 原中心 0 以 -1 为锚点放大到 1，再应用移动偏移 0.5。
            Assert.Equal(1.5d, movedX, 3);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ExportBakesScaleThenRotationThenMove()
    {
        var source = new IrChart
        {
            BpmList = [new IrBpmItem { Bpm = 120f, StartBeat = new Beat(0) }],
            BlockAreaList =
            [
                new IrBlockArea
                {
                    TopRightX = 1d,
                    TopRightY = 1d,
                    BottomLeftX = -1d,
                    BottomLeftY = -1d,
                    AppearBeat = new Beat(0),
                    EnableBeat = new Beat(0),
                    DisableBeat = new Beat(4),
                    DisappearBeat = new Beat(5),
                    MoveXEvents = [CreateIrEvent(0, 2, 0d, 0.5d)],
                    RotateEvents = [CreateIrEvent(0, 2, 90d, 90d)],
                    RotateAnchorXEvents = [CreateIrEvent(0, 2, 0d, 0d)],
                    RotateAnchorYEvents = [CreateIrEvent(0, 2, 0d, 0d)],
                    ScaleXEvents = [CreateIrEvent(0, 2, 2d, 2d)],
                    ScaleYEvents = [CreateIrEvent(0, 2, 1d, 1d)],
                    ScaleAnchorXEvents = [CreateIrEvent(0, 2, -1d, -1d)],
                    ScaleAnchorYEvents = [CreateIrEvent(0, 2, 0d, 0d)],
                },
            ],
        };

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            await ChartFormatRegistry.Get(ChartType.StellateRePhiEditExtended).ExportAsync(
                source,
                path,
                ct: TestContext.Current.CancellationToken
            );
            var json = await File.ReadAllTextAsync(
                path,
                TestContext.Current.CancellationToken
            );
            var (_, converted) = await ChartFormatRegistry.ImportIrAsync(
                json,
                ct: TestContext.Current.CancellationToken
            );

            var area = Assert.Single(converted.BlockAreaList);
            var centerX = GetIrCenterX(area);
            var centerY = (area.TopRightY + area.BottomLeftY) / 2d;

            Assert.Equal(0.5d, EvaluateIrTrack(area.MoveXEvents, new Beat(2), centerX), 3);
            Assert.Equal(1.5d, EvaluateIrTrack(area.MoveYEvents, new Beat(2), centerY), 3);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ExportBakesPhigrosLeftFrameAbsoluteAnchorBeforeMove()
    {
        var source = new KaedePhi.Core.Formats.Phigros.v3.Model.Chart
        {
            JudgeLineList =
            [
                new KaedePhi.Core.Formats.Phigros.v3.Model.JudgeLine { Bpm = 120f },
            ],
            BlockAreaList =
            [
                new PhigrosBlockArea
                {
                    TopRightPercentage = new() { X = 0.75f, Y = 0.75f },
                    BottomLeftPercentage = new() { X = 0.25f, Y = 0.25f },
                    AppearTime = 0f,
                    EnableTime = 0f,
                    DisableTime = 4f,
                    DisappearTime = 5f,
                    MoveEvents =
                    [
                        new()
                        {
                            Time = 0f,
                            EndPosition = new() { X = 0.5f, Y = 0.5f },
                            EaseTypeX = PhigrosAreaEaseType.Linear,
                            EaseTypeY = PhigrosAreaEaseType.Linear,
                        },
                        new()
                        {
                            Time = 2f,
                            EndPosition = new() { X = 0.75f, Y = 0.5f },
                            EaseTypeX = PhigrosAreaEaseType.Linear,
                            EaseTypeY = PhigrosAreaEaseType.Linear,
                        },
                    ],
                    ScaleEvents =
                    [
                        new()
                        {
                            Time = 0f,
                            Anchor = new() { X = 0.25f, Y = 0.5f },
                            Scale = new() { X = 1f, Y = 1f },
                            EaseTypeX = PhigrosAreaEaseType.Linear,
                            EaseTypeY = PhigrosAreaEaseType.Linear,
                        },
                        new()
                        {
                            Time = 2f,
                            Anchor = new() { X = 0.75f, Y = 0.5f },
                            Scale = new() { X = 2f, Y = 1f },
                            EaseTypeX = PhigrosAreaEaseType.Linear,
                            EaseTypeY = PhigrosAreaEaseType.Linear,
                        },
                    ],
                },
            ],
        };
        var intermediate = new PhigrosV3Converter().ToIr(source, null);
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            await ChartFormatRegistry.Get(ChartType.StellateRePhiEditExtended).ExportAsync(
                intermediate,
                path,
                ct: TestContext.Current.CancellationToken
            );
            var json = await File.ReadAllTextAsync(
                path,
                TestContext.Current.CancellationToken
            );
            var (_, converted) = await ChartFormatRegistry.ImportIrAsync(
                json,
                ct: TestContext.Current.CancellationToken
            );

            var area = Assert.Single(converted.BlockAreaList);
            var centerX = GetIrCenterX(area);
            Assert.Equal(0.5d, EvaluateIrTrack(area.MoveXEvents, new Beat(2), centerX), 2);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ExportSeedsBlockAreaTracksThatStartAfterZero()
    {
        var source = new IrChart
        {
            BpmList = [new IrBpmItem { Bpm = 120f, StartBeat = new Beat(0) }],
            BlockAreaList =
            [
                new IrBlockArea
                {
                    TopRightX = 0.5d,
                    TopRightY = 0.5d,
                    BottomLeftX = -0.5d,
                    BottomLeftY = -0.5d,
                    AppearBeat = new Beat(0),
                    EnableBeat = new Beat(1),
                    DisableBeat = new Beat(3),
                    DisappearBeat = new Beat(4),
                    MoveXEvents = [CreateIrEvent(2, 4, 0d, 0.5d)],
                    RotateEvents = [CreateIrEvent(2, 4, 0d, 45d)],
                    ScaleXEvents = [CreateIrEvent(2, 4, 1d, 2d)],
                },
            ],
        };

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        try
        {
            await ChartFormatRegistry.Get(ChartType.StellateRePhiEditExtended).ExportAsync(
                source,
                path,
                ct: TestContext.Current.CancellationToken
            );
            var json = await File.ReadAllTextAsync(
                path,
                TestContext.Current.CancellationToken
            );
            var (_, converted) = await ChartFormatRegistry.ImportIrAsync(
                json,
                ct: TestContext.Current.CancellationToken
            );

            var area = Assert.Single(converted.BlockAreaList);
            Assert.Equal(0.5d, area.TopRightX, 6);
            Assert.Equal(-0.5d, area.BottomLeftX, 6);
            Assert.Equal(new Beat(0), area.MoveXEvents![0].StartBeat);
            Assert.Equal(new Beat(2), area.MoveXEvents[0].EndBeat);
            Assert.Equal(0d, area.MoveXEvents[0].EndValue, 6);
            Assert.Equal(0.5d, area.MoveXEvents[^1].EndValue, 6);
            Assert.Equal(new Beat(0), area.RotateEvents![0].StartBeat);
            Assert.Equal(new Beat(2), area.RotateEvents[0].EndBeat);
            Assert.Equal(0d, area.RotateEvents[0].EndValue, 6);
            Assert.Equal(45d, area.RotateEvents[^1].EndValue, 6);
            Assert.Equal(new Beat(0), area.ScaleXEvents![0].StartBeat);
            Assert.Equal(new Beat(2), area.ScaleXEvents[0].EndBeat);
            Assert.Equal(1d, area.ScaleXEvents[0].EndValue, 6);
            Assert.Equal(2d, area.ScaleXEvents[^1].EndValue, 6);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ImportDetectsMarkerLinesAndExportsThemAsPhigrosBlockAreas()
    {
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList =
            [
                new RpeJudgeLine { Texture = "line.png" },
                CreateBlockAreaLine("Pictures\\isSubtract1.png"),
                CreateBlockAreaLine("isSubtract0.png"),
                new RpeJudgeLine { Texture = "other.png" },
            ],
        };
        source.JudgeLineList[1].EventLayers[0].MoveXEvents![0].Easing = new(5);
        SetFullLifecycleMarkers(source.JudgeLineList[1]);
        SetFullLifecycleMarkers(source.JudgeLineList[2]);

        var (detectedType, converted) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );

        Assert.Equal(ChartType.StellateRePhiEditExtended, detectedType);
        Assert.Equal(
            new[] { "line.png", "other.png" },
            converted.JudgeLineList.Select(line => line.Texture)
        );
        Assert.Equal(2, converted.BlockAreaList.Count);
        Assert.True(converted.BlockAreaList[0].IsSubtract);
        Assert.False(converted.BlockAreaList[1].IsSubtract);
        Assert.Equal(450d / 675d, converted.BlockAreaList[0].TopRightX, 6);
        Assert.Equal(-450d / 675d, converted.BlockAreaList[0].BottomLeftX, 6);
        Assert.Equal(1d, converted.BlockAreaList[0].TopRightY, 6);
        Assert.Equal(-1d, converted.BlockAreaList[0].BottomLeftY, 6);

        var phigros = new PhigrosV3Converter().FromIr(
            converted,
            new IrToPhigrosV3ConvertOptions()
        );
        var firstArea = phigros.BlockAreaList[0];

        Assert.Equal(5f / 6f, firstArea.TopRightPercentage.X, 6);
        Assert.Equal(1f / 6f, firstArea.BottomLeftPercentage.X, 6);
        Assert.Equal(1f, firstArea.TopRightPercentage.Y);
        Assert.Equal(0f, firstArea.BottomLeftPercentage.Y);
        Assert.Equal(0f, firstArea.AppearTime);
        Assert.Equal(0.5f, firstArea.EnableTime);
        Assert.Equal(1f, firstArea.DisableTime);
        Assert.Equal(2f, firstArea.DisappearTime);
        Assert.Equal(2, firstArea.MoveEvents.Count);
        Assert.Equal(1f, firstArea.MoveEvents[^1].EndPosition.X);
        Assert.Equal(0f, firstArea.MoveEvents[^1].EndPosition.Y);
        Assert.Equal(
            PhigrosAreaEaseType.EaseInQuad,
            firstArea.MoveEvents[0].EaseTypeX.Type
        );
        Assert.Equal(PhigrosAreaEaseType.One, firstArea.MoveEvents[^1].EaseTypeX.Type);
        Assert.Equal(-90f, Assert.Single(firstArea.RotateEvents).Rotation);
        Assert.Equal(2f, Assert.Single(firstArea.ScaleEvents).Scale.X);
        Assert.Equal(0.5f, Assert.Single(firstArea.ScaleEvents).Scale.Y);
    }

    [Fact]
    public async Task ExplicitRePhiEditImportDoesNotApplyTheExtendedConversion()
    {
        var source = new Chart
        {
            JudgeLineList = [new RpeJudgeLine { Texture = "isSubtract1.png" }],
        };
        var imported = await ChartFormatRegistry
            .Get(ChartType.RePhiEdit)
            .ImportIrAsync(
                source.ExportToJson(false),
                ct: TestContext.Current.CancellationToken
            );

        Assert.Single(imported.JudgeLineList);
        Assert.Equal("isSubtract1.png", imported.JudgeLineList[0].Texture);
        Assert.Empty(imported.BlockAreaList);
    }

    [Fact]
    public async Task ImportAcceptsMarkedLinesWithMissingPhaseEvents()
    {
        var line = CreateBlockAreaLine("isSubtract0.png");
        line.EventLayers[0].SpeedEvents!.RemoveAt(3);
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [line],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );

        var area = Assert.Single(imported.BlockAreaList);
        Assert.Equal(new Beat(4), area.DisappearBeat);
    }

    [Fact]
    public async Task ImportMapsPhaseTimesWithoutEnforcingTheirOrder()
    {
        var line = CreateBlockAreaLine("isSubtract0.png");
        line.EventLayers[0].SpeedEvents =
        [
            CreateEvent(4, 4, 1f, 1f),
            CreateEvent(1, 1, 2f, 2f),
            CreateEvent(3, 3, 3f, 3f),
            CreateEvent(2, 2, 4f, 4f),
        ];
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [line],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        Assert.Equal(3, imported.BlockAreaList.Count);
        Assert.Equal(new Beat(1), imported.BlockAreaList[0].AppearBeat);
        Assert.Equal(new Beat(1), imported.BlockAreaList[0].EnableBeat);
        Assert.Equal(new Beat(2), imported.BlockAreaList[0].DisableBeat);
        Assert.Equal(new Beat(2), imported.BlockAreaList[0].DisappearBeat);
        Assert.Equal(new Beat(3), imported.BlockAreaList[1].AppearBeat);
        Assert.Equal(new Beat(3), imported.BlockAreaList[1].EnableBeat);
        Assert.Equal(new Beat(3), imported.BlockAreaList[1].DisableBeat);
        Assert.Equal(new Beat(4), imported.BlockAreaList[1].DisappearBeat);
        Assert.Equal(new Beat(4), imported.BlockAreaList[2].AppearBeat);
        Assert.Equal(new Beat(4), imported.BlockAreaList[2].DisappearBeat);
    }

    [Fact]
    public async Task ImportUsesNextLifecycleStartForMissingPhasesAndKeepsFullTransformEvents()
    {
        var line = CreateBlockAreaLine("isSubtract0.png");
        line.EventLayers[0].MoveXEvents = null;
        line.EventLayers[0].MoveYEvents = null;
        line.EventLayers[0].RotateEvents = [CreateEvent(0, 6, 0f, 60f)];
        line.EventLayers[0].SpeedEvents =
        [
            CreateEvent(1, 1, 1f, 1f),
            CreateEvent(2, 2, 2f, 2f),
            CreateEvent(3, 3, 1f, 1f),
            CreateEvent(4, 4, 2f, 2f),
            CreateEvent(5, 6, 4f, 4f),
        ];
        line.Extended.ScaleXEvents = null;
        line.Extended.ScaleYEvents = null;
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [line],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );

        Assert.Equal(2, imported.BlockAreaList.Count);
        Assert.Equal(new Beat(1), imported.BlockAreaList[0].AppearBeat);
        Assert.Equal(new Beat(2), imported.BlockAreaList[0].EnableBeat);
        Assert.Equal(new Beat(3), imported.BlockAreaList[0].DisableBeat);
        Assert.Equal(new Beat(3), imported.BlockAreaList[0].DisappearBeat);
        Assert.Equal(new Beat(3), imported.BlockAreaList[1].AppearBeat);
        Assert.Equal(new Beat(4), imported.BlockAreaList[1].EnableBeat);
        Assert.Equal(new Beat(5), imported.BlockAreaList[1].DisableBeat);
        Assert.Equal(new Beat(5), imported.BlockAreaList[1].DisappearBeat);

        var firstRotateEvents = imported.BlockAreaList[0].RotateEvents!;
        var secondRotateEvents = imported.BlockAreaList[1].RotateEvents!;
        Assert.Contains(firstRotateEvents, evt => evt.StartBeat == new Beat(0));
        Assert.Contains(firstRotateEvents, evt => evt.EndBeat == new Beat(6));
        Assert.Equal(
            firstRotateEvents.Select(evt =>
                (evt.StartBeat, evt.EndBeat, evt.StartValue, evt.EndValue)
            ),
            secondRotateEvents.Select(evt =>
                (evt.StartBeat, evt.EndBeat, evt.StartValue, evt.EndValue)
            )
        );
    }

    [Fact]
    public async Task ImportBakesParentTranslationIntoMarkedBlockArea()
    {
        var marker = CreateBlockAreaLine("isSubtract1.png");
        marker.Father = 0;
        marker.EventLayers[0].MoveXEvents = null;
        marker.EventLayers[0].MoveYEvents = null;
        var parent = new RpeJudgeLine
        {
            EventLayers =
            [
                new RpeEventLayer { MoveXEvents = [CreateEvent(0, 4, 0f, 337.5f)] },
            ],
        };
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [parent, marker],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        var area = Assert.Single(imported.BlockAreaList);

        Assert.Equal(0.5d, area.MoveXEvents!.Last().EndValue, 3);
        Assert.Single(imported.JudgeLineList);
        Assert.Equal(-1, imported.JudgeLineList[0].Father);
    }

    [Fact]
    public async Task ImportBakesParentRotationIntoMarkedBlockAreaPosition()
    {
        var marker = CreateBlockAreaLine("isSubtract0.png");
        marker.Father = 0;
        marker.EventLayers[0].MoveXEvents = [CreateEvent(0, 4, 337.5f, 337.5f)];
        marker.EventLayers[0].MoveYEvents = null;
        marker.EventLayers[0].RotateEvents = null;
        var parent = new RpeJudgeLine
        {
            EventLayers =
            [
                new RpeEventLayer { RotateEvents = [CreateEvent(0, 4, 0f, 90f)] },
            ],
        };
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [parent, marker],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        var area = Assert.Single(imported.BlockAreaList);

        Assert.Equal(0d, area.MoveXEvents!.Last().EndValue, 3);
        Assert.Equal(-0.75d, area.MoveYEvents!.Last().EndValue, 3);
    }

    [Fact]
    public async Task ImportDoesNotBakeParentMovementBreaksIntoIndependentRotation()
    {
        var marker = CreateBlockAreaLine("isSubtract0.png");
        marker.Father = 0;
        marker.RotateWithFather = false;
        marker.EventLayers[0].MoveXEvents = null;
        marker.EventLayers[0].MoveYEvents = null;
        marker.EventLayers[0].RotateEvents = [CreateEvent(0, 1, 0f, 0f)];
        marker.EventLayers.Add(
            new RpeEventLayer
            {
                RotateEvents =
                [
                    CreateEvent(0, 1, 0f, 0f),
                    CreateEvent(0, 2, 0f, 0f),
                    CreateEvent(2, 3, 0f, 90f),
                ],
            }
        );
        SetFullLifecycleMarkers(marker);
        marker.Extended.ScaleXEvents = null;
        marker.Extended.ScaleYEvents = null;
        var parent = new RpeJudgeLine
        {
            EventLayers =
            [
                new RpeEventLayer
                {
                    MoveXEvents = Enumerable.Range(0, 64)
                        .Select(index =>
                        {
                            var start = index / 4d;
                            return new RpeEvent
                            {
                                StartBeat = new Beat(start),
                                EndBeat = new Beat(start + 0.25d),
                                StartValue = (float)start,
                                EndValue = (float)(start + 0.25d),
                            };
                        })
                        .ToList(),
                },
            ],
        };
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [parent, marker],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        var exported = new PhigrosV3Converter().FromIr(
            imported,
            new IrToPhigrosV3ConvertOptions()
        );
        var area = Assert.Single(exported.BlockAreaList);

        Assert.Equal(3, area.RotateEvents.Count);
        Assert.Equal(0f, area.RotateEvents[0].Time, 6);
        Assert.Equal(0f, area.RotateEvents[0].Rotation);
        Assert.Equal(1f, area.RotateEvents[1].Time, 6);
        Assert.Equal(0f, area.RotateEvents[1].Rotation);
        Assert.Equal(1.5f, area.RotateEvents[2].Time, 6);
        Assert.Equal(-90f, area.RotateEvents[2].Rotation, 4);
    }

    [Fact]
    public async Task ImportDoesNotTreatParentMovementBreaksAsRotationHardPoints()
    {
        var marker = CreateBlockAreaLine("isSubtract0.png");
        marker.Father = 0;
        marker.RotateWithFather = false;
        marker.EventLayers[0].MoveXEvents = null;
        marker.EventLayers[0].MoveYEvents = null;
        marker.EventLayers[0].RotateEvents = [CreateEvent(0, 4, 0f, 30f)];
        marker.EventLayers[0].RotateEvents![0].Easing = new(5);
        marker.EventLayers.Add(
            new RpeEventLayer { RotateEvents = [CreateEvent(0, 4, 0f, 20f)] }
        );
        marker.EventLayers[1].RotateEvents![0].Easing = new(8);
        marker.Extended.ScaleXEvents = null;
        marker.Extended.ScaleYEvents = null;
        SetFullLifecycleMarkers(marker);
        var parent = new RpeJudgeLine
        {
            EventLayers =
            [
                new RpeEventLayer
                {
                    MoveXEvents = Enumerable
                        .Range(0, 64)
                        .Select(index =>
                        {
                            var start = index / 4d;
                            return new RpeEvent
                            {
                                StartBeat = new Beat(start),
                                EndBeat = new Beat(start + 0.25d),
                                StartValue = (float)start,
                                EndValue = (float)(start + 0.25d),
                            };
                        })
                        .ToList(),
                },
            ],
        };
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [parent, marker],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        var exported = new PhigrosV3Converter().FromIr(
            imported,
            new IrToPhigrosV3ConvertOptions()
        );
        var area = Assert.Single(exported.BlockAreaList);

        Assert.True(area.RotateEvents.Count < 40);
    }

    [Fact]
    public async Task ImportBakesTextureAnchorIntoBlockAreaBounds()
    {
        var line = CreateBlockAreaLine("isSubtract1.png");
        line.Anchor = [0f, 0f];
        line.EventLayers[0].MoveXEvents = null;
        line.EventLayers[0].MoveYEvents = null;
        line.EventLayers[0].RotateEvents = null;
        line.Extended.ScaleXEvents = null;
        line.Extended.ScaleYEvents = null;
        SetFullLifecycleMarkers(line);
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [line],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        var area = Assert.Single(imported.BlockAreaList);

        Assert.Equal(4d / 3d, area.TopRightX, 6);
        Assert.Equal(0d, area.BottomLeftX, 6);
        Assert.Equal(2d, area.TopRightY, 6);
        Assert.Equal(0d, area.BottomLeftY, 6);
    }

    [Theory]
    [InlineData(-5, PhigrosAreaEaseType.EaseInQuad)]
    [InlineData(29, PhigrosAreaEaseType.Linear)]
    public async Task ImportNormalizesRpeEasingTypesForBlockAreaCurves(
        int rpeEasing,
        PhigrosAreaEaseType expected
    )
    {
        var line = CreateBlockAreaLine("isSubtract0.png");
        line.EventLayers[0].MoveYEvents = null;
        line.EventLayers[0].RotateEvents = null;
        line.Extended.ScaleXEvents = null;
        line.Extended.ScaleYEvents = null;
        line.EventLayers[0].MoveXEvents![0].Easing = new(rpeEasing);
        SetFullLifecycleMarkers(line);
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [line],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        var exported = new PhigrosV3Converter().FromIr(
            imported,
            new IrToPhigrosV3ConvertOptions()
        );

        Assert.Equal(
            expected,
            Assert.Single(exported.BlockAreaList).MoveEvents[0].EaseTypeX.Type
        );
    }

    [Fact]
    public async Task ImportSamplesRpeElasticEasingWithReferenceCurve()
    {
        var line = CreateBlockAreaLine("isSubtract0.png");
        line.EventLayers[0].MoveXEvents = [CreateEvent(0, 4, 0f, 675f)];
        line.EventLayers[0].MoveXEvents![0].Easing = new(24);
        line.EventLayers[0].MoveYEvents = null;
        line.EventLayers[0].RotateEvents = null;
        line.Extended.ScaleXEvents = null;
        line.Extended.ScaleYEvents = null;
        SetFullLifecycleMarkers(line);
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [line],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        var area = Assert.Single(imported.BlockAreaList);
        var progress = 0.125d;
        var expected =
            Math.Pow(2d, -10d * progress)
                * Math.Sin((10d * progress - 0.75d) * 2.094395d)
            + 1d;
        var actual = EvaluateIrTrack(
            area.MoveXEvents,
            new Beat(0.5d),
            GetIrCenterX(area)
        );

        Assert.InRange(Math.Abs(actual - expected), 0d, 0.02d);
    }

    [Fact]
    public async Task ImportBakesCurveStartingAfterZeroFromItsExtrapolatedValue()
    {
        var line = CreateBlockAreaLine("isSubtract0.png");
        line.EventLayers[0].MoveXEvents = [CreateEvent(2, 4, 0f, 675f)];
        line.EventLayers[0].MoveXEvents![0].Easing = new(5);
        line.EventLayers[0].MoveYEvents = null;
        line.EventLayers[0].RotateEvents = null;
        line.Extended.ScaleXEvents = null;
        line.Extended.ScaleYEvents = null;
        SetFullLifecycleMarkers(line);
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [line],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        var area = Assert.Single(imported.BlockAreaList);
        var irValue = EvaluateIrTrack(
            area.MoveXEvents,
            new Beat(1),
            GetIrCenterX(area)
        );
        var exported = new PhigrosV3Converter().FromIr(
            imported,
            new IrToPhigrosV3ConvertOptions()
        );
        var phigrosArea = Assert.Single(exported.BlockAreaList);
        var phigrosValue = EvaluatePhigrosMoveX(phigrosArea, 0.5f);

        Assert.Equal(1d, GetIrCenterX(area), 6);
        Assert.Equal(0.25d, irValue, 2);
        Assert.Equal(0.625f, phigrosValue, 2);
    }

    [Fact]
    public async Task ImportUsesLongestSameStartCurveLikeReference()
    {
        var line = CreateBlockAreaLine("isSubtract0.png");
        line.EventLayers[0].MoveXEvents =
        [
            CreateEvent(0, 4, 0f, 675f),
            CreateEvent(0, 2, 0f, -675f),
        ];
        line.EventLayers[0].MoveYEvents = null;
        line.EventLayers[0].RotateEvents = null;
        line.Extended.ScaleXEvents = null;
        line.Extended.ScaleYEvents = null;
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [line],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        var area = Assert.Single(imported.BlockAreaList);
        var irValue = EvaluateIrTrack(area.MoveXEvents, new Beat(3), GetIrCenterX(area));
        var exported = new PhigrosV3Converter().FromIr(
            imported,
            new IrToPhigrosV3ConvertOptions()
        );
        var phigrosValue = EvaluatePhigrosMoveX(Assert.Single(exported.BlockAreaList), 1.5f);

        Assert.Equal(0.75d, irValue, 2);
        Assert.Equal(0.875f, phigrosValue, 2);
    }

    [Fact]
    public async Task ImportUsesReferenceBezierExtrapolationBeforeFirstEvent()
    {
        var line = CreateBlockAreaLine("isSubtract0.png");
        line.EventLayers[0].MoveXEvents = [CreateEvent(1, 4, 0f, 675f)];
        line.EventLayers[0].MoveXEvents![0].IsBezier = true;
        line.EventLayers[0].MoveXEvents![0].BezierPoints = [1f, 1f, 1f, 1f];
        line.EventLayers[0].MoveYEvents = null;
        line.EventLayers[0].RotateEvents = null;
        line.Extended.ScaleXEvents = null;
        line.Extended.ScaleYEvents = null;
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [line],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        var area = Assert.Single(imported.BlockAreaList);

        Assert.Equal(-1d / 3d, GetIrCenterX(area), 2);
    }

    [Fact]
    public async Task ImportTreatsBezierFlagWithoutFourPointsAsRegularEasing()
    {
        var line = CreateBlockAreaLine("isSubtract0.png");
        line.EventLayers[0].MoveXEvents = [CreateEvent(0, 4, 0f, 675f)];
        line.EventLayers[0].MoveXEvents![0].IsBezier = true;
        line.EventLayers[0].MoveXEvents![0].BezierPoints = [0f, 1f];
        line.EventLayers[0].MoveXEvents![0].Easing = new(5);
        line.EventLayers[0].MoveYEvents = null;
        line.EventLayers[0].RotateEvents = null;
        line.Extended.ScaleXEvents = null;
        line.Extended.ScaleYEvents = null;
        SetFullLifecycleMarkers(line);
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [line],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        var exported = new PhigrosV3Converter().FromIr(
            imported,
            new IrToPhigrosV3ConvertOptions()
        );

        Assert.Equal(
            PhigrosAreaEaseType.EaseInQuad,
            Assert.Single(exported.BlockAreaList).MoveEvents[0].EaseTypeX.Type
        );
    }

    [Fact]
    public async Task ImportTreatsMissingBezierPointsAsRegularEasing()
    {
        const string source =
            """
            {
              "BPMList": [{ "bpm": 120, "startTime": [0, 0, 1] }],
              "META": {},
              "judgeLineList": [{
                "Texture": "isSubtract0.png",
                "eventLayers": [{
                  "moveXEvents": [{
                    "startTime": [0, 0, 1],
                    "endTime": [4, 0, 1],
                    "start": 0,
                    "end": 675,
                    "easingType": 5,
                    "bezier": true,
                    "bezierPoints": null
                  }],
                  "speedEvents": [
                    { "startTime": [0, 0, 1], "start": 1 },
                    { "startTime": [1, 0, 1], "start": 2 },
                    { "startTime": [2, 0, 1], "start": 3 },
                    { "startTime": [4, 0, 1], "start": 4 }
                  ]
                }]
              }]
            }
            """;

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source,
            ct: TestContext.Current.CancellationToken
        );
        var exported = new PhigrosV3Converter().FromIr(
            imported,
            new IrToPhigrosV3ConvertOptions()
        );

        Assert.Equal(
            PhigrosAreaEaseType.EaseInQuad,
            Assert.Single(exported.BlockAreaList).MoveEvents[0].EaseTypeX.Type
        );
    }

    [Fact]
    public async Task ImportNormalizesTinyScaleAgainstFallbackBaseSize()
    {
        var line = CreateBlockAreaLine("isSubtract0.png");
        line.EventLayers[0].MoveXEvents = null;
        line.EventLayers[0].MoveYEvents = null;
        line.EventLayers[0].RotateEvents = null;
        line.Extended.ScaleXEvents = [CreateEvent(0, 4, 1e-14f, 2e-14f)];
        line.Extended.ScaleYEvents = null;
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [line],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        var area = Assert.Single(imported.BlockAreaList);
        var exported = new PhigrosV3Converter().FromIr(
            imported,
            new IrToPhigrosV3ConvertOptions()
        );
        var scaleEvents = Assert.Single(exported.BlockAreaList).ScaleEvents;

        Assert.InRange(Math.Abs(area.ScaleXEvents!.Last().EndValue - 2e-14d), 0d, 1e-20d);
        Assert.InRange(Math.Abs(scaleEvents[^1].Scale.X - 2e-14f), 0f, 1e-20f);
    }

    [Fact]
    public async Task ImportSamplesScaleCrossingThroughZeroAsPositiveSize()
    {
        var line = CreateBlockAreaLine("isSubtract0.png");
        line.EventLayers[0].MoveXEvents = null;
        line.EventLayers[0].MoveYEvents = null;
        line.EventLayers[0].RotateEvents = null;
        line.Extended.ScaleXEvents = [CreateEvent(0, 4, -1f, 1f)];
        line.Extended.ScaleYEvents = null;
        var source = new Chart
        {
            BpmList = [new() { Bpm = 120f, StartBeat = new Beat(0) }],
            JudgeLineList = [line],
        };

        var (_, imported) = await ChartFormatRegistry.ImportIrAsync(
            source.ExportToJson(false),
            ct: TestContext.Current.CancellationToken
        );
        var area = Assert.Single(imported.BlockAreaList);
        var scaleAtZero = EvaluateIrTrack(area.ScaleXEvents, new Beat(2), 1d);

        Assert.Equal(0d, scaleAtZero, 3);
    }

    private static double GetIrCenterX(IrBlockArea area) =>
        (area.TopRightX + area.BottomLeftX) / 2d;

    private static double EvaluateIrTrack(
        List<IrEvent>? events,
        Beat beat,
        double defaultValue
    )
    {
        var dominant = events?.LastOrDefault(evt => evt.StartBeat <= beat);
        if (dominant is null)
            return defaultValue;
        return beat <= dominant.EndBeat
            ? dominant.GetValueAtBeatAsDouble(beat)
            : dominant.EndValue;
    }

    private static float EvaluatePhigrosMoveX(PhigrosBlockArea area, float time)
    {
        var previousTime = 0f;
        var startPosition = area.Center;
        var leftEase = PhigrosAreaEaseType.Linear;
        foreach (var moveEvent in area.MoveEvents.OrderBy(evt => evt.Time))
        {
            if (time <= moveEvent.Time)
            {
                var progress = PhigrosAreaEase.GetProgress(
                    previousTime,
                    moveEvent.Time,
                    time
                );
                var easedProgress = PhigrosAreaEase.GetEaseWithProgress(progress, leftEase);
                return startPosition.X
                    + (moveEvent.EndPosition.X - startPosition.X) * easedProgress;
            }
            previousTime = moveEvent.Time;
            startPosition = moveEvent.EndPosition;
            leftEase = moveEvent.EaseTypeX.Type;
        }
        return startPosition.X;
    }

    private static RpeJudgeLine CreateBlockAreaLine(string texture) =>
        new()
        {
            Texture = texture,
            EventLayers =
            [
                new RpeEventLayer
                {
                    MoveXEvents = [CreateEvent(0, 4, 0f, 675f)],
                    MoveYEvents = [CreateEvent(0, 4, 0f, -450f)],
                    RotateEvents = [CreateEvent(0, 4, 0f, 90f)],
                    SpeedEvents =
                    [
                        CreateEvent(0, 1, 0f, 1f),
                        CreateEvent(1, 2, 1f, 2f),
                        CreateEvent(2, 3, 2f, 3f),
                        CreateEvent(3, 4, 3f, 4f),
                    ],
                },
            ],
            Extended = new RpeExtendLayer
            {
                ScaleXEvents = [CreateEvent(0, 4, 1f, 2f)],
                ScaleYEvents = [CreateEvent(0, 4, 1f, 0.5f)],
            },
        };

    private static void SetFullLifecycleMarkers(RpeJudgeLine line)
    {
        line.EventLayers[0].SpeedEvents =
        [
            CreateEvent(0, 1, 1f, 1f),
            CreateEvent(1, 2, 2f, 2f),
            CreateEvent(2, 3, 3f, 3f),
            CreateEvent(4, 4, 4f, 4f),
        ];
    }

    private static RpeEvent CreateEvent(int startBeat, int endBeat, float start, float end) =>
        new()
        {
            StartBeat = new Beat(startBeat),
            EndBeat = new Beat(endBeat),
            StartValue = start,
            EndValue = end,
        };

    private static IrEvent CreateIrEvent(
        int startBeat,
        int endBeat,
        double start,
        double end,
        int easing = 1
    ) =>
        new()
        {
            StartBeat = new Beat(startBeat),
            EndBeat = new Beat(endBeat),
            StartValue = start,
            EndValue = end,
            Easing = new IrEasing(easing),
        };
}
