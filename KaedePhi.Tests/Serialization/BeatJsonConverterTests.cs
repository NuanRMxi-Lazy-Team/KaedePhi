using KaedePhi.Core.Common;
using Newtonsoft.Json;

namespace KaedePhi.Tests.Serialization;

public class BeatJsonConverterTests
{
    [Fact]
    public void Serialize_Beat_ProducesIntArray()
    {
        var beat = new Beat([2, 3, 4]);

        var json = JsonConvert.SerializeObject(beat);

        json.Should().Be("[2,3,4]");
    }

    [Fact]
    public void Deserialize_IntArray_ProducesBeat()
    {
        var json = "[2,3,4]";

        var beat = JsonConvert.DeserializeObject<Beat>(json);

        beat.Should().NotBeNull();
        beat[0].Should().Be(2);
        beat[1].Should().Be(3);
        beat[2].Should().Be(4);
    }

    [Fact]
    public void RoundTrip_PreservesValues()
    {
        var original = new Beat([5, 7, 8]);

        var json = JsonConvert.SerializeObject(original);
        var deserialized = JsonConvert.DeserializeObject<Beat>(json);

        deserialized.Should().NotBeNull();
        ((double)deserialized).Should().BeApproximately(original, 1e-10);
        deserialized[0].Should().Be(original[0]);
        deserialized[1].Should().Be(original[1]);
        deserialized[2].Should().Be(original[2]);
    }

    [Fact]
    public void Deserialize_ZeroBeat_Works()
    {
        var json = "[0,0,1]";

        var beat = JsonConvert.DeserializeObject<Beat>(json);

        beat.Should().NotBeNull();
        ((double)beat).Should().Be(0.0);
    }

    [Fact]
    public void Deserialize_NegativeWholePart_Works()
    {
        var json = "[-2,1,4]";

        var beat = JsonConvert.DeserializeObject<Beat>(json);

        beat.Should().NotBeNull();
        ((double)beat).Should().BeApproximately(-1.75, 1e-10);
    }

    [Fact]
    public void Serialize_BeatInObject_WorksCorrectly()
    {
        var obj = new TestObject { Name = "test", Beat = new Beat([1, 1, 2]) };

        var json = JsonConvert.SerializeObject(obj);

        json.Should().Contain("[1,1,2]");
    }

    [Fact]
    public void Deserialize_BeatInObject_WorksCorrectly()
    {
        var json = """{"Name":"test","Beat":[1,1,2]}""";

        var obj = JsonConvert.DeserializeObject<TestObject>(json);

        obj.Should().NotBeNull();
        obj.Name.Should().Be("test");
        ((double)obj.Beat).Should().BeApproximately(1.5, 1e-10);
    }

    [Fact]
    public void RoundTrip_BeatInObject_PreservesValues()
    {
        var original = new TestObject { Name = "test", Beat = new Beat([3, 5, 8]) };

        var json = JsonConvert.SerializeObject(original);
        var deserialized = JsonConvert.DeserializeObject<TestObject>(json);

        deserialized.Should().NotBeNull();
        deserialized.Name.Should().Be(original.Name);
        ((double)deserialized.Beat).Should().BeApproximately(original.Beat, 1e-10);
    }

    [Fact]
    public void Deserialize_NullArray_CreatesDefaultBeat()
    {
        var json = "null";

        var beat = JsonConvert.DeserializeObject<Beat>(json);

        // Converter returns default(Beat) for null JSON
        ((double)beat)
            .Should()
            .Be(0.0);
    }

    [Fact]
    public void Serialize_DefaultBeat_ProducesValidZeroArray()
    {
        var json = JsonConvert.SerializeObject(default(Beat));

        json.Should().Be("[0,0,1]");
    }

    [Fact]
    public void Deserialize_InvalidArray_ThrowsException()
    {
        var json = "[1,2]"; // Only 2 elements

        var act = () => JsonConvert.DeserializeObject<Beat>(json);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Deserialize_ZeroDenominator_ThrowsException()
    {
        var json = "[1,0,0]";

        var act = () => JsonConvert.DeserializeObject<Beat>(json);

        act.Should().Throw<Exception>();
    }

    private class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public Beat Beat { get; set; } = new([0, 0, 1]);
    }
}
