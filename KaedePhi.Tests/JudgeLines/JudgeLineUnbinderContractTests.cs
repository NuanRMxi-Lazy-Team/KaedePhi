using KaedePhi.Core.Common;
using KpcEvents = KaedePhi.Core.KaedePhi.Events;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.JudgeLines;
using KaedePhi.Tool.JudgeLines.KaedePhi;
using JudgeLine = KaedePhi.Core.KaedePhi.JudgeLine;

namespace KaedePhi.Tests.JudgeLines;

public sealed class JudgeLineUnbinderContractTests
{
    [Fact]
    public void AdaptiveUnbind_IsAvailableThroughInterface()
    {
        IJudgeLineUnbinder<JudgeLine> unbinder = new JudgeLineUnbinder();
        var lines = new List<JudgeLine> { new() };

        var result = unbinder.FatherUnbindDynamic(
            0,
            lines,
            16d,
            0.1d,
            0.2d,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.Father.Should().Be(-1);
    }

    [Fact]
    public void AdaptiveUnbindWithRenderProfile_IsAvailableThroughInterface()
    {
        IJudgeLineUnbinder<JudgeLine> unbinder = new JudgeLineUnbinder();
        var lines = new List<JudgeLine> { new() };

        var result = unbinder.FatherUnbindDynamic(
            0,
            lines,
            CoordinateProfile.DefaultRenderProfile,
            16d,
            100,
            0.2d,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.Father.Should().Be(-1);
    }

    [Fact]
    public void UnbindMethodsUseTheirDeclaredSamplingStrategy()
    {
        IJudgeLineUnbinder<JudgeLine> unbinder = new JudgeLineUnbinder();

        var equalSpacingResult = unbinder.FatherUnbind(
            1,
            CreateLines(),
            4d,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken
        );
        var adaptiveResult = unbinder.FatherUnbindDynamic(
            1,
            CreateLines(),
            4d,
            0.1d,
            0.1d,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken
        );

        equalSpacingResult.EventLayers[0].MoveXEvents.Should().HaveCount(4);
        adaptiveResult.EventLayers[0].MoveXEvents.Should().HaveCount(1);
    }

    private static List<JudgeLine> CreateLines() =>
        [
            new JudgeLine
            {
                EventLayers =
                [
                    new KpcEvents.EventLayer
                    {
                        MoveXEvents =
                        [
                            new KpcEvents.Event<double>
                            {
                                StartBeat = new Beat(0d),
                                EndBeat = new Beat(1d),
                                StartValue = 0d,
                                EndValue = 100d,
                            },
                        ],
                    },
                ],
            },
            new JudgeLine
            {
                Father = 0,
                EventLayers = [new KpcEvents.EventLayer()],
            },
        ];
}
