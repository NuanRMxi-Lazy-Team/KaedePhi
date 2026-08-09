using System.IO;
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
}
