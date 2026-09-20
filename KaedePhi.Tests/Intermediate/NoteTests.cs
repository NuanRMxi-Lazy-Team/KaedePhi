using KaedePhi.Core.Intermediate;

namespace KaedePhi.Tests.Intermediate;

public class NoteTests
{
    [Fact]
    public void Clone_PreservesReservedTimes()
    {
        var original = new Note { StartTime = 1.25f, EndTime = 3.75f };

        var clone = original.Clone();

        clone.StartTime.Should().Be(original.StartTime);
        clone.EndTime.Should().Be(original.EndTime);
    }
}
