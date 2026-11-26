using GoEngine.Core;
using Xunit;

namespace GoEngine.Tests.Contract;

public class PositionSnapshotTests
{
    private const PointState E = PointState.Empty;
    private const PointState B = PointState.Black;
    private const PointState W = PointState.White;

    // 3x3, row-major, no symmetry: (0,0) Black, (2,0) White, (2,1) Black, (2,2) White.
    private static PointState[] BaseColoring() =>
    [
        B, E, W,
        E, E, B,
        E, E, W,
    ];

    private static PositionSnapshot Snapshot(
        int size = 3,
        PointState[]? coloring = null,
        Color toMove = Color.Black,
        int consecutivePasses = 0,
        int blackStonesRemoved = 0,
        int whiteStonesRemoved = 0)
    {
        return new PositionSnapshot(
            size,
            coloring ?? BaseColoring(),
            toMove,
            consecutivePasses,
            blackStonesRemoved,
            whiteStonesRemoved);
    }

    private static void AssertNotEqual(PositionSnapshot a, PositionSnapshot b)
    {
        Assert.False(a.Equals(b));
        Assert.False(a.Equals((object)b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void Constructor_ValidArguments_ExposesAllValues()
    {
        PositionSnapshot snapshot = Snapshot(3, BaseColoring(), Color.White, 1, 2, 3);

        Assert.Equal(3, snapshot.Size);
        Assert.Equal(Color.White, snapshot.ToMove);
        Assert.Equal(1, snapshot.ConsecutivePasses);
        Assert.Equal(2, snapshot.BlackStonesRemoved);
        Assert.Equal(3, snapshot.WhiteStonesRemoved);
    }

    [Fact]
    public void Equals_SameContentsInDifferentLists_AreEqualWithSameHash()
    {
        PositionSnapshot a = Snapshot(3, BaseColoring(), Color.White, 1, 2, 3);
        PositionSnapshot b = Snapshot(3, BaseColoring(), Color.White, 1, 2, 3);

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(8)]
    public void Equals_DifferentStateAtOnePoint_AreNotEqual(int index)
    {
        PointState[] other = BaseColoring();
        other[index] = other[index] == E ? B : E;

        AssertNotEqual(Snapshot(), Snapshot(coloring: other));
    }

    [Fact]
    public void Equals_DifferentToMove_AreNotEqual()
    {
        AssertNotEqual(Snapshot(toMove: Color.Black), Snapshot(toMove: Color.White));
    }

    [Fact]
    public void Equals_DifferentConsecutivePasses_AreNotEqual()
    {
        AssertNotEqual(Snapshot(consecutivePasses: 0), Snapshot(consecutivePasses: 1));
    }

    [Fact]
    public void Equals_DifferentBlackStonesRemoved_AreNotEqual()
    {
        AssertNotEqual(Snapshot(blackStonesRemoved: 0), Snapshot(blackStonesRemoved: 1));
    }

    [Fact]
    public void Equals_DifferentWhiteStonesRemoved_AreNotEqual()
    {
        AssertNotEqual(Snapshot(whiteStonesRemoved: 0), Snapshot(whiteStonesRemoved: 1));
    }

    [Fact]
    public void Equals_SameStonesOnDifferentSize_AreNotEqual()
    {
        PositionSnapshot small = Snapshot(2, [B, E, E, E]);
        PositionSnapshot large = Snapshot(3, [B, E, E, E, E, E, E, E, E]);

        AssertNotEqual(small, large);
    }

    [Fact]
    public void Equals_Null_IsFalse()
    {
        PositionSnapshot snapshot = Snapshot();

        Assert.False(snapshot.Equals(null));
        Assert.False(snapshot.Equals((object?)null));
    }

    [Fact]
    public void EqualityOperators_NullOnEitherSide_CompareAsNotEqual()
    {
        PositionSnapshot snapshot = Snapshot();
        PositionSnapshot? none = null;

        Assert.False(snapshot == none);
        Assert.False(none == snapshot);
        Assert.True(snapshot != none);
        Assert.True(none != snapshot);
    }

    [Fact]
    public void EqualityOperators_NullOnBothSides_CompareAsEqual()
    {
        PositionSnapshot? left = null;
        PositionSnapshot? right = null;

        Assert.True(left == right);
        Assert.False(left != right);
    }

    [Fact]
    public void Equals_ObjectOfAnotherType_IsFalse()
    {
        Assert.False(Snapshot().Equals((object)"position"));
    }

    [Fact]
    public void Constructor_SourceArrayChangedAfterwards_SnapshotUnchanged()
    {
        PointState[] coloring = BaseColoring();
        PositionSnapshot snapshot = Snapshot(coloring: coloring);

        coloring[0] = W;
        coloring[4] = B;

        Assert.Equal(B, snapshot[new Point(0, 0)]);
        Assert.Equal(E, snapshot[new Point(1, 1)]);
    }

    [Fact]
    public void Indexer_Corners_ReturnRowMajorState()
    {
        PositionSnapshot snapshot = Snapshot();

        Assert.Equal(B, snapshot[new Point(0, 0)]);
        Assert.Equal(W, snapshot[new Point(2, 0)]);
        Assert.Equal(E, snapshot[new Point(0, 2)]);
        Assert.Equal(W, snapshot[new Point(2, 2)]);
    }

    [Fact]
    public void Indexer_NonSymmetricPoint_UsesXAsColumnAndYAsRow()
    {
        PositionSnapshot snapshot = Snapshot();

        Assert.Equal(B, snapshot[new Point(2, 1)]);
        Assert.Equal(E, snapshot[new Point(1, 2)]);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(3, 0)]
    [InlineData(0, 3)]
    public void Indexer_PointOutsideBoard_Throws(int x, int y)
    {
        PositionSnapshot snapshot = Snapshot();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => snapshot[new Point(x, y)]);

        Assert.Equal("point", ex.ParamName);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void IsOver_IsTrueOnlyAfterTwoPasses(int consecutivePasses, bool expected)
    {
        Assert.Equal(expected, Snapshot(consecutivePasses: consecutivePasses).IsOver);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void Constructor_SizeOutsideRange_Throws(int size)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Snapshot(size, new PointState[size * size]));

        Assert.Equal("size", ex.ParamName);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(19)]
    public void Constructor_SizeAtBoundary_KeepsSize(int size)
    {
        Assert.Equal(size, Snapshot(size, new PointState[size * size]).Size);
    }

    [Fact]
    public void Constructor_NullColoring_Throws()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new PositionSnapshot(3, null!, Color.Black, 0, 0, 0));

        Assert.Equal("coloring", ex.ParamName);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    public void Constructor_ColoringLengthNotSizeSquared_Throws(int length)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Snapshot(3, new PointState[length]));

        Assert.Equal("coloring", ex.ParamName);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(3, 4)]
    [InlineData(3, 8)]
    public void Constructor_UndefinedPointState_Throws(int value, int index)
    {
        PointState[] coloring = BaseColoring();
        coloring[index] = (PointState)value;

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Snapshot(coloring: coloring));

        Assert.Equal("coloring", ex.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(3)]
    public void Constructor_UndefinedToMove_Throws(int value)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Snapshot(toMove: (Color)value));

        Assert.Equal("toMove", ex.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void Constructor_ConsecutivePassesOutsideRange_Throws(int consecutivePasses)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Snapshot(consecutivePasses: consecutivePasses));

        Assert.Equal("consecutivePasses", ex.ParamName);
    }

    [Fact]
    public void Constructor_NegativeBlackStonesRemoved_Throws()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Snapshot(blackStonesRemoved: -1));

        Assert.Equal("blackStonesRemoved", ex.ParamName);
    }

    [Fact]
    public void Constructor_NegativeWhiteStonesRemoved_Throws()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Snapshot(whiteStonesRemoved: -1));

        Assert.Equal("whiteStonesRemoved", ex.ParamName);
    }

    [Fact]
    public void Constructor_InvalidSizeAndNullColoring_NamesSize()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new PositionSnapshot(1, null!, Color.Black, 0, 0, 0));

        Assert.Equal("size", ex.ParamName);
    }

    [Fact]
    public void Constructor_InvalidSizeAndColoringLength_NamesSize()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Snapshot(1, [E, E, E, E]));

        Assert.Equal("size", ex.ParamName);
    }

    [Fact]
    public void Constructor_UndefinedPointStateAndToMove_NamesColoring()
    {
        PointState[] coloring = BaseColoring();
        coloring[4] = (PointState)3;

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Snapshot(coloring: coloring, toMove: (Color)0));

        Assert.Equal("coloring", ex.ParamName);
    }

    [Fact]
    public void Constructor_UndefinedToMoveAndNegativePasses_NamesToMove()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Snapshot(toMove: (Color)0, consecutivePasses: -1));

        Assert.Equal("toMove", ex.ParamName);
    }

    [Fact]
    public void Constructor_PassesOutsideRangeAndNegativeBlackRemoved_NamesConsecutivePasses()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Snapshot(consecutivePasses: 3, blackStonesRemoved: -1));

        Assert.Equal("consecutivePasses", ex.ParamName);
    }

    [Fact]
    public void Constructor_NegativeBlackAndWhiteRemoved_NamesBlackStonesRemoved()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => Snapshot(blackStonesRemoved: -1, whiteStonesRemoved: -1));

        Assert.Equal("blackStonesRemoved", ex.ParamName);
    }
}
