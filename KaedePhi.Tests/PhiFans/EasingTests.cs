using KaedePhi.Core.PhiFans;
using Newtonsoft.Json;

namespace KaedePhi.Tests.PhiFans;

/// <summary>
/// 验证 PhiFans 缓动类型的结构：与 PhiEdit 趋同（缓存函数、无截取参数）。
/// </summary>
public class EasingTests
{
    [Fact]
    public void Get_WithinRange_ReturnsCachedInstance()
    {
        var first = Easing.Get(0);
        var second = Easing.Get(0);

        ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact]
    public void Get_OutOfRange_NotCached()
    {
        var first = Easing.Get(31);
        var second = Easing.Get(31);

        ReferenceEquals(first, second).Should().BeFalse();
    }

    [Fact]
    public void Linear_IsZero()
    {
        ((int)Easing.Linear).Should().Be(0);
        ((int)Easing.Linear).Should().Be((int)Easing.Get(0));
    }

    [Fact]
    public void Interpolate_Linear_ReturnsLinearValue()
    {
        var easing = new Easing(0);

        easing.Interpolate(0f, 10f, 0.5f).Should().Be(5f);
        easing.Interpolate(0d, 1d, 1d).Should().Be(1d);
    }

    [Fact]
    public void Interpolate_EaseInQuad_ReturnsExpectedValue()
    {
        var easing = new Easing(4);

        easing.Interpolate(0d, 1d, 0.5d).Should().BeApproximately(0.25d, 1e-9);
    }

    [Fact]
    public void ImplicitConversion_NumberRoundTrip()
    {
        Easing easing = 5;

        ((int)easing).Should().Be(5);
        ReferenceEquals(easing, Easing.Get(5)).Should().BeTrue();
    }

    [Fact]
    public void JsonRoundTrip_KeepsNumber()
    {
        const string json = "5";

        var deserialized = JsonConvert.DeserializeObject<Easing>(json);

        ((int)deserialized!).Should().Be(5);
        JsonConvert.SerializeObject(deserialized).Should().Be(json);
    }
}
