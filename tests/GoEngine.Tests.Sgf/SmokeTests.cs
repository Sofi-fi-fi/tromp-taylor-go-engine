using FsCheck.Xunit;
using Xunit;

namespace GoEngine.Tests.Sgf;

public class SmokeTests
{
    [Fact]
    public void Addition_Works()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Property]
    public void ReversingArrayTwice_ReturnsOriginal(int[] values)
    {
        int[] twice = Enumerable.Reverse(Enumerable.Reverse(values)).ToArray();
        Assert.Equal(values, twice);
    }
}
