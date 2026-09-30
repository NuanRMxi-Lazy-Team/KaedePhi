using KaedePhi.Core.Intermediate.Model;
using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Event.Intermediate;
using IrEvents = KaedePhi.Core.Intermediate.Model.Events;

namespace KaedePhi.Tests.Event;

public class EventFitIntTests
{
    private readonly EventFit<int> _fit = new();

    [Fact]
    public void FitEvents_QuantizedAlphaRamp_MergesToSingleLinearEvent()
    {
        // 0→128→255 的分段整数斜坡：中点 128 与理想直线 127.5 相差 0.5，
        // 若按未量化的连续值比较，0.1% 容差（255 × 0.1% = 0.255）会拒绝整段拟合
        var events = new List<IrEvents.Event<int>>
        {
            CreateEvent(0, 1, 0, 128),
            CreateEvent(1, 2, 128, 255),
        };

        var result = _fit.FitEvents(events, 0.1);

        result.Should().ContainSingle();
        ((int)result[0].Easing).Should().Be(1);
        result[0].StartValue.Should().Be(0);
        result[0].EndValue.Should().Be(255);
        result[0].EndBeat.Should().Be(new Beat(2));
    }

    [Fact]
    public void FitEvents_RoundedStaircase_MergesToSingleEvent()
    {
        var events = new List<IrEvents.Event<int>>();
        for (var i = 0; i < 64; i++)
        {
            events.Add(
                CreateEvent(
                    i * 0.25,
                    (i + 1) * 0.25,
                    (int)Math.Round(255.0 * i / 64),
                    (int)Math.Round(255.0 * (i + 1) / 64)
                )
            );
        }

        var result = _fit.FitEvents(events, 0.1);

        result.Should().ContainSingle();
        ((int)result[0].Easing).Should().Be(1);
        result[0].StartValue.Should().Be(0);
        result[0].EndValue.Should().Be(255);
    }

    [Fact]
    public void FitEvents_CurvedStaircase_RecoversEasing()
    {
        // 由 EaseInOutSine（编号 4）曲线截断采样得到的整数阶梯应还原为对应缓动事件
        var events = new List<IrEvents.Event<int>>();
        for (var i = 0; i < 16; i++)
        {
            var startBeat = i * 0.25;
            var endBeat = (i + 1) * 0.25;
            events.Add(
                CreateEvent(
                    startBeat,
                    endBeat,
                    (int)(255 * Easings.Evaluate(4, 0d, 1d, startBeat / 4d)),
                    (int)(255 * Easings.Evaluate(4, 0d, 1d, endBeat / 4d))
                )
            );
        }

        var result = _fit.FitEvents(events, 0.1);

        result.Should().ContainSingle();
        ((int)result[0].Easing).Should().Be(4);
        result[0].StartValue.Should().Be(0);
        result[0].EndValue.Should().Be(255);
    }

    [Fact]
    public void FitEvents_AbruptSlopeBreak_DoesNotMerge()
    {
        // 前段斜率远大于后段，整段与两段前缀均无法在容差内拟合，不得因量化放宽而错误合并
        var events = new List<IrEvents.Event<int>>
        {
            CreateEvent(0, 1, 0, 200),
            CreateEvent(1, 2, 200, 255),
        };

        var result = _fit.FitEvents(events, 0.1);

        result.Should().HaveCount(2);
        result[0].StartValue.Should().Be(0);
        result[0].EndValue.Should().Be(200);
        result[1].StartValue.Should().Be(200);
        result[1].EndValue.Should().Be(255);
    }

    [Fact]
    public void FitEvents_DoublePayload_KeepsStrictRelativeTolerance()
    {
        // 量化放宽仅适用于 int；double 载荷仍按连续值比较，0.1% 容差下 0.5 的偏差被拒绝
        var fit = new EventFit<double>();
        var events = new List<IrEvents.Event<double>>
        {
            CreateDoubleEvent(0, 1, 0, 128),
            CreateDoubleEvent(1, 2, 128, 255),
        };

        var result = fit.FitEvents(events, 0.1);

        result.Should().HaveCount(2);
    }

    private static IrEvents.Event<int> CreateEvent(
        double startBeat,
        double endBeat,
        int startValue,
        int endValue
    )
    {
        return new IrEvents.Event<int>
        {
            StartBeat = new Beat(startBeat),
            EndBeat = new Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue,
        };
    }

    private static IrEvents.Event<double> CreateDoubleEvent(
        double startBeat,
        double endBeat,
        double startValue,
        double endValue
    )
    {
        return new IrEvents.Event<double>
        {
            StartBeat = new Beat(startBeat),
            EndBeat = new Beat(endBeat),
            StartValue = startValue,
            EndValue = endValue,
        };
    }
}
