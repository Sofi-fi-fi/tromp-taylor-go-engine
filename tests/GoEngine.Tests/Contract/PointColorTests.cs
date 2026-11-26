using GoEngine.Core;
using Xunit;

namespace GoEngine.Tests.Contract;

public class PointColorTests
{
    [Fact]
    public void Point_HasValueEquality()
    {
        Point a = new(3, 5);
        Point b = new(3, 5);

        Assert.True(a == b);
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());

        Assert.NotEqual(a, new Point(4, 5));
        Assert.NotEqual(a, new Point(3, 6));
        Assert.NotEqual(a, new Point(5, 3));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(19, 19)]
    public void Point_OutsideAnyBoard_IsRepresentableAndKeepsCoordinates(int x, int y)
    {
        Point point = new(x, y);

        Assert.Equal(x, point.X);
        Assert.Equal(y, point.Y);
    }

    [Theory]
    [InlineData(Color.Black, Color.White)]
    [InlineData(Color.White, Color.Black)]
    public void Opposite_ReturnsOtherColor(Color color, Color expected)
    {
        Assert.Equal(expected, color.Opposite());
    }

    [Theory]
    [InlineData(Color.Black)]
    [InlineData(Color.White)]
    public void Opposite_AppliedTwice_ReturnsOriginal(Color color)
    {
        Assert.Equal(color, color.Opposite().Opposite());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(3)]
    public void Opposite_UndefinedColor_Throws(int value)
    {
        Color color = (Color)value;

        Assert.Throws<ArgumentOutOfRangeException>(() => color.Opposite());
    }

    [Theory]
    [InlineData(Color.Black, PointState.Black)]
    [InlineData(Color.White, PointState.White)]
    public void ToPointState_ReturnsMatchingState(Color color, PointState expected)
    {
        Assert.Equal(expected, color.ToPointState());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(3)]
    public void ToPointState_UndefinedColor_Throws(int value)
    {
        Color color = (Color)value;

        Assert.Throws<ArgumentOutOfRangeException>(() => color.ToPointState());
    }

    [Fact]
    public void DefaultPointState_IsEmpty()
    {
        Assert.Equal(PointState.Empty, default(PointState));
    }
}
