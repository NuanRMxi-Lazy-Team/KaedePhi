using KaedePhi.Core.Formats.RePhiEdit.Model;
using KaedePhi.Core.Formats.RePhiEdit.Serialization;
using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter;
using KaedePhi.Tool.Converter.Phigros.v3;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using PhigrosAreaEaseType = KaedePhi.Core.Formats.Phigros.v3.Model.AreaEaseType;
using PhigrosBlockArea = KaedePhi.Core.Formats.Phigros.v3.Model.BlockArea;
using IrBlockArea = KaedePhi.Core.Intermediate.Model.BlockArea;
using IrEvent = KaedePhi.Core.Intermediate.Model.Events.Event<double>;
using RpeEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<float>;
using RpeEventLayer = KaedePhi.Core.Formats.RePhiEdit.Model.Events.EventLayer;
using RpeExtendLayer = KaedePhi.Core.Formats.RePhiEdit.Model.Events.ExtendLayer;
using RpeJudgeLine = KaedePhi.Core.Formats.RePhiEdit.Model.JudgeLine;
using Xunit;

namespace KaedePhi.Tests.Converter;

public class StellateRePhiEditExtendedConverterTests
{
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
        Assert.Equal(1f, Assert.Single(firstArea.MoveEvents).EndPosition.X);
        Assert.Equal(0f, Assert.Single(firstArea.MoveEvents).EndPosition.Y);
        Assert.Equal(-90f, Assert.Single(firstArea.RotateEvents).Rotation);
        Assert.Equal(2f, Assert.Single(firstArea.ScaleEvents).Scale.X);
        Assert.Equal(0.5f, Assert.Single(firstArea.ScaleEvents).Scale.Y);
        Assert.Equal(
            PhigrosAreaEaseType.EaseInQuad,
            Assert.Single(firstArea.MoveEvents).EaseTypeX.Type
        );
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

        Assert.Equal(2, area.RotateEvents.Count);
        Assert.Equal(1f, area.RotateEvents[0].Time, 6);
        Assert.Equal(0f, area.RotateEvents[0].Rotation);
        Assert.Equal(1.5f, area.RotateEvents[1].Time, 6);
        Assert.Equal(-90f, area.RotateEvents[1].Rotation, 4);
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
            Assert.Single(Assert.Single(exported.BlockAreaList).MoveEvents).EaseTypeX.Type
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
            Assert.Single(Assert.Single(exported.BlockAreaList).MoveEvents).EaseTypeX.Type
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
            Assert.Single(Assert.Single(exported.BlockAreaList).MoveEvents).EaseTypeX.Type
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
        foreach (var moveEvent in area.MoveEvents.OrderBy(evt => evt.Time))
        {
            if (time <= moveEvent.Time)
                return moveEvent.GetPositionAtTime(time, previousTime, startPosition).X;
            previousTime = moveEvent.Time;
            startPosition = moveEvent.EndPosition;
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
}
