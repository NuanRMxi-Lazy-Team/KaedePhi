using System.IO;
using System.Text;
using KaedePhi.Tool.Common;

namespace KaedePhi.Tests.Common;

public class ChartGetTypeTests
{
    [Theory]
    [InlineData("{\"formatVersion\":1}", ChartType.PhigrosV1)]
    [InlineData("{\"formatVersion\":3}", ChartType.PhigrosV3)]
    public void GetType_WithIntegerFormatVersion_ReturnsPhigrosType(
        string chartText,
        ChartType expected
    )
    {
        using var reader = new StringReader(chartText);
        ChartGetType.GetType(reader).Should().Be(expected);
    }

    [Theory]
    [InlineData("{\"formatVersion\":3.0}")]
    [InlineData("{\"formatVersion\":\"3\"}")]
    [InlineData("{\"formatVersion\":null}")]
    [InlineData("{\"formatVersion\":2}")]
    public void GetType_WithInvalidFormatVersion_ThrowsNotSupportedException(string chartText)
    {
        var act = () => ChartGetType.GetType(new StringReader(chartText));

        act.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData(
        "{\"META\":{},\"judgeLineList\":[{\"Texture\":\"Pictures\\\\isSubtract0.png\"}]}",
        ChartType.StellateRePhiEditExtended
    )]
    [InlineData(
        "{\"judgeLineList\":[{\"Texture\":\"isSubtract1.png\"}],\"META\":{}}",
        ChartType.StellateRePhiEditExtended
    )]
    [InlineData("{\"META\":{},\"judgeLineList\":[{\"Texture\":\"line.png\"}]}", ChartType.RePhiEdit)]
    public void GetType_WithRePhiEditTextureMarker_ReturnsExpectedType(
        string chartText,
        ChartType expected
    )
    {
        using var reader = new StringReader(chartText);

        ChartGetType.GetType(reader).Should().Be(expected);
    }

    [Fact]
    public async Task GetTypeAsync_WithRePhiEditTextureMarker_ReturnsExtendedType()
    {
        const string chartText =
            "{\"META\":{},\"judgeLineList\":[{\"Texture\":\"Pictures\\\\isSubtract1.png\"}]}";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(chartText));

        var detected = await ChartGetType.GetTypeAsync(
            stream,
            TestContext.Current.CancellationToken
        );

        detected.Should().Be(ChartType.StellateRePhiEditExtended);
    }
}
