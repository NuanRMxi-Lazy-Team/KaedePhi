using KaedePhi.Core.Formats.Phigros.v3.Model;
using KaedePhi.Core.Formats.Phigros.v3.Serialization;
using Xunit;

namespace KaedePhi.Tests.Serialization;

public class PhigrosV3BlockAreaTests
{
    [Fact]
    public void BlockAreaListUsesCanonicalJsonNameAndReadsDocumentedFields()
    {
        const string json =
            """
            {
              "blockAreaList": [{
                "topRightPercentage": { "x": 1.5, "y": 1.2 },
                "bottomLeftPercentage": { "x": 0.5, "y": -0.2 },
                "appearTime": 1.0,
                "enableTime": 2.0,
                "disableTime": 3.0,
                "disappearTime": 4.0,
                "isSubtract": true,
                "rotateEvents": [{ "anchor": { "x": 0.5, "y": 0.5 }, "time": 2.5, "easeType": 1, "rotation": 90.0 }],
                "moveEvents": [{ "endPosition": { "x": 0.25, "y": 0.75 }, "time": 2.5, "easeTypeX": 2, "easeTypeY": 3 }],
                "scaleEvents": [{ "anchor": { "x": 0.5, "y": 0.5 }, "time": 2.5, "scale": { "x": 2.0, "y": 1.0 }, "easeTypeX": 4, "easeTypeY": 5 }]
              }]
            }
            """;

        var chart = ChartSerialization.LoadFromJson(json);
        var area = Assert.Single(chart.BlockAreaList);
        var serialized = chart.ExportToJson(format: false);
        var legacyChart = ChartSerialization.LoadFromJson("{\"BlockAreaList\":[{}]}");

        Assert.Equal(1.5f, area.TopRightPercentage.X);
        Assert.Equal(-0.2f, area.BottomLeftPercentage.Y);
        Assert.True(area.IsSubtract);
        Assert.Equal(90f, Assert.Single(area.RotateEvents).Rotation);
        Assert.Equal(0.75f, Assert.Single(area.MoveEvents).EndPosition.Y);
        Assert.Equal(2f, Assert.Single(area.ScaleEvents).Scale.X);
        Assert.Equal(AreaEaseType.InSine, Assert.Single(area.RotateEvents).EaseType.Type);
        Assert.Single(legacyChart.BlockAreaList);
        Assert.Contains("\"blockAreaList\"", serialized);
        Assert.Contains("\"easeType\":1", serialized);
        Assert.DoesNotContain("\"BlockAreaList\"", serialized);
        Assert.DoesNotContain("\"center\"", serialized);
    }

    [Theory]
    [InlineData(0, 0.25f, 0.25f)]
    [InlineData(1, 0.5f, 0.25f)]
    [InlineData(2, 0.5f, 0.75f)]
    [InlineData(3, 0.25f, 0.125f)]
    [InlineData(4, 0.5f, 0.125f)]
    [InlineData(7, 0.5f, 0.0625f)]
    [InlineData(10, 0.5f, 0.03125f)]
    [InlineData(13, 0.5f, 0f)]
    [InlineData(14, 0.5f, 1f)]
    public void EaseValuesFollowDocumentedCurves(int type, float progress, float expected)
    {
        Assert.Equal(expected, AreaEase.GetEaseWithProgress(progress, type), precision: 6);
    }

    [Fact]
    public void EaseProgressIsClampedAtTableEndpoints()
    {
        Assert.Equal(0f, AreaEase.GetEaseWithProgress(-0.02f, AreaEaseType.Linear));
        Assert.Equal(1f, AreaEase.GetEaseWithProgress(1.5f, AreaEaseType.Linear));

        var legacyEvent = new AreaMoveEvent { EaseTypeX = 2 };
        Assert.Equal(2, (int)legacyEvent.EaseTypeX);
    }

    [Fact]
    public void InterpolationAppliesTheEasedProgress()
    {
        Assert.Equal(12.5f, AreaEase.Interpolate(10f, 20f, 0.5f, AreaEaseType.InSine));
    }

    [Fact]
    public void AreaEventsEvaluateTheirKeyframeValuesAtTheRequestedTime()
    {
        var moveEvent = new AreaMoveEvent
        {
            Time = 3f,
            EndPosition = new PositionUnit { X = 1f, Y = 1f },
            EaseTypeX = AreaEaseType.InSine,
            EaseTypeY = AreaEaseType.OutSine,
        };
        var rotationEvent = new AreaRotateEvent
        {
            Time = 3f,
            Anchor = new PositionUnit { X = 1f, Y = 1f },
            Rotation = 90f,
            EaseType = AreaEaseType.InSine,
        };
        var scaleEvent = new AreaScaleEvent
        {
            Time = 3f,
            Anchor = new PositionUnit { X = 1f, Y = 1f },
            Scale = new PositionUnit { X = 2f, Y = 2f },
            EaseTypeX = AreaEaseType.InSine,
            EaseTypeY = AreaEaseType.OutSine,
        };
        var start = new PositionUnit { X = 0f, Y = 0f };

        var position = moveEvent.GetPositionAtTime(2f, 1f, start);
        var rotation = rotationEvent.GetRotationAtTime(2f, 1f, 0f);
        var rotationAnchor = rotationEvent.GetAnchorAtTime(2f, 1f, start);
        var scale = scaleEvent.GetScaleAtTime(2f, 1f, new PositionUnit { X = 1f, Y = 1f });
        var scaleAnchor = scaleEvent.GetAnchorAtTime(2f, 1f, start);

        Assert.Equal(0.25f, position.X, precision: 6);
        Assert.Equal(0.75f, position.Y, precision: 6);
        Assert.Equal(22.5f, rotation, precision: 6);
        Assert.Equal(0.25f, rotationAnchor.X, precision: 6);
        Assert.Equal(0.25f, rotationAnchor.Y, precision: 6);
        Assert.Equal(1.25f, scale.X, precision: 6);
        Assert.Equal(1.75f, scale.Y, precision: 6);
        Assert.Equal(0.25f, scaleAnchor.X, precision: 6);
        Assert.Equal(0.75f, scaleAnchor.Y, precision: 6);
    }

    [Fact]
    public void BlockAreaPhaseUsesDocumentedTimeBoundaries()
    {
        var area = new BlockArea
        {
            TopRightPercentage = new PositionUnit { X = 1f, Y = 1f },
            BottomLeftPercentage = new PositionUnit { X = 0f, Y = 0f },
            AppearTime = 1f,
            EnableTime = 2f,
            DisableTime = 3f,
            DisappearTime = 4f,
        };

        Assert.Equal(BlockAreaPhase.HiddenBefore, area.GetPhase(0.99f));
        Assert.Equal(new PositionUnit { X = 0.5f, Y = 0.5f }, area.Center);
        Assert.Equal(BlockAreaPhase.Disabled, area.GetPhase(1f));
        Assert.Equal(BlockAreaPhase.Ready, area.GetPhase(1.5f));
        Assert.Equal(BlockAreaPhase.Active, area.GetPhase(2f));
        Assert.Equal(BlockAreaPhase.Disabled, area.GetPhase(3f));
        Assert.Equal(BlockAreaPhase.HiddenAfter, area.GetPhase(4f));
    }

    [Fact]
    public void BlockAreaCanAppearAlreadyActive()
    {
        var area = new BlockArea
        {
            AppearTime = 2f,
            EnableTime = 1.5f,
            DisableTime = 3f,
            DisappearTime = 4f,
        };

        Assert.Equal(BlockAreaPhase.Active, area.GetPhase(2f));
    }
}
