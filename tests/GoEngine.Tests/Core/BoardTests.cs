using GoEngine.Core;
using Xunit;

namespace GoEngine.Tests.Core;

// Design §4 "Board": flat array indexed y * size + x, precomputed neighbours, colouring,
// clone and value equality, sizes 2..19.
public class BoardTests
{
    public static TheoryData<int> Sizes => [2, 5, 19];

    public static TheoryData<int> SizesWithEdges => [5, 19];

    // Rejects: diagonal neighbours, wrap-around (torus) neighbours, a missing bounds check at a corner.
    [Theory]
    [MemberData(nameof(Sizes))]
    public void Neighbours_OfCorner_AreTheTwoOrthogonalPoints(int size)
    {
        Board board = new(size);
        int last = size - 1;

        Assert.Equal(Set(new(1, 0), new(0, 1)), NeighbourPoints(board, new(0, 0)));
        Assert.Equal(Set(new(last - 1, 0), new(last, 1)), NeighbourPoints(board, new(last, 0)));
        Assert.Equal(Set(new(1, last), new(0, last - 1)), NeighbourPoints(board, new(0, last)));
        Assert.Equal(Set(new(last - 1, last), new(last, last - 1)), NeighbourPoints(board, new(last, last)));
    }

    // Rejects: an edge point losing a neighbour along the edge or gaining one across it.
    [Theory]
    [MemberData(nameof(SizesWithEdges))]
    public void Neighbours_OfEdge_AreThree(int size)
    {
        Board board = new(size);
        int mid = size / 2;
        int last = size - 1;

        Assert.Equal(Set(new(mid - 1, 0), new(mid + 1, 0), new(mid, 1)), NeighbourPoints(board, new(mid, 0)));
        Assert.Equal(Set(new(0, mid - 1), new(0, mid + 1), new(1, mid)), NeighbourPoints(board, new(0, mid)));
        Assert.Equal(Set(new(last, mid - 1), new(last, mid + 1), new(last - 1, mid)), NeighbourPoints(board, new(last, mid)));
        Assert.Equal(Set(new(mid - 1, last), new(mid + 1, last), new(mid, last - 1)), NeighbourPoints(board, new(mid, last)));
    }

    // Rejects: a centre point with diagonals (8) or with a side dropped (3).
    [Theory]
    [MemberData(nameof(SizesWithEdges))]
    public void Neighbours_OfCentre_AreFour(int size)
    {
        Board board = new(size);
        int mid = size / 2;

        Assert.Equal(
            Set(new(mid, mid - 1), new(mid - 1, mid), new(mid + 1, mid), new(mid, mid + 1)),
            NeighbourPoints(board, new(mid, mid)));
    }

    // Rejects: a precomputation bug at any single point (e.g. the last row or column), duplicate
    // entries, or a neighbour index outside the board. Expected sets come from point arithmetic,
    // not from the board's index layout.
    [Theory]
    [MemberData(nameof(Sizes))]
    public void Neighbours_OfEveryPoint_AreExactlyTheOrthogonalOnBoardPoints(int size)
    {
        Board board = new(size);

        for (int index = 0; index < board.PointCount; index++)
        {
            int[] neighbours = board.Neighbours(index).ToArray();
            Assert.All(neighbours, n => Assert.InRange(n, 0, board.PointCount - 1));
            Assert.Equal(neighbours.Length, neighbours.Distinct().Count());

            Point p = board.PointAt(index);
            HashSet<Point> expected = [.. new Point[] { new(p.X, p.Y - 1), new(p.X - 1, p.Y), new(p.X + 1, p.Y), new(p.X, p.Y + 1) }
                .Where(q => q.X >= 0 && q.X < size && q.Y >= 0 && q.Y < size)];
            Assert.Equal(expected, neighbours.Select(board.PointAt).ToHashSet());
        }
    }

    // Rejects: an index/point mapping that is not a bijection over the board.
    [Theory]
    [MemberData(nameof(Sizes))]
    public void IndexAndPoint_RoundTrip(int size)
    {
        Board board = new(size);
        Assert.Equal(size * size, board.PointCount);

        for (int index = 0; index < board.PointCount; index++)
        {
            Assert.Equal(index, board.IndexOf(board.PointAt(index)));
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Assert.Equal(new Point(x, y), board.PointAt(board.IndexOf(new Point(x, y))));
            }
        }
    }

    // Rejects: the transposed layout x * size + y, which still round-trips but disagrees with
    // PositionSnapshot's p.Y * size + p.X.
    [Fact]
    public void IndexOf_IsRowMajor()
    {
        Board board = new(5);

        Assert.Equal(0, board.IndexOf(new Point(0, 0)));
        Assert.Equal(1, board.IndexOf(new Point(1, 0)));
        Assert.Equal(5, board.IndexOf(new Point(0, 1)));
        Assert.Equal(2 * 5 + 3, board.IndexOf(new Point(3, 2)));
    }

    // Rejects: a board that does not start empty, or a setter that writes to the wrong point.
    [Fact]
    public void Colouring_NewBoardIsEmpty_AndSetChangesOnlyThatPoint()
    {
        Board board = new(3);
        Assert.All(Enumerable.Range(0, board.PointCount), i => Assert.Equal(PointState.Empty, board[i]));

        board[4] = PointState.Black;
        board[7] = PointState.White;

        for (int i = 0; i < board.PointCount; i++)
        {
            PointState expected = i switch { 4 => PointState.Black, 7 => PointState.White, _ => PointState.Empty };
            Assert.Equal(expected, board[i]);
        }
    }

    // Rejects: a shallow clone that shares the point array with the original.
    [Fact]
    public void Clone_IsEqualAndIndependent()
    {
        Board original = new(3);
        original[0] = PointState.Black;

        Board clone = original.Clone();
        Assert.Equal(original, clone);
        Assert.Equal(original.Size, clone.Size);

        clone[1] = PointState.White;
        Assert.Equal(PointState.Empty, original[1]);

        original[2] = PointState.Black;
        Assert.Equal(PointState.Empty, clone[2]);
    }

    // Rejects: reference equality, or a hash code that ignores value equality.
    [Fact]
    public void Equals_SameColouring_IsEqual()
    {
        Board a = new(3);
        Board b = new(3);
        a[3] = PointState.Black;
        b[3] = PointState.Black;
        a[5] = PointState.White;
        b[5] = PointState.White;

        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // Rejects: a comparison that skips some points (e.g. an off-by-one loop missing the last one)
    // or that compares only stone counts.
    [Theory]
    [InlineData(PointState.Black)]
    [InlineData(PointState.White)]
    public void Equals_OnePointDifferent_IsNotEqual(PointState state)
    {
        for (int index = 0; index < 9; index++)
        {
            Board a = new(3);
            Board b = new(3);
            b[index] = state;

            Assert.False(a.Equals(b), $"Boards differing at index {index} compared equal.");
            Assert.False(b.Equals(a), $"Boards differing at index {index} compared equal.");
        }
    }

    // Rejects: a comparison of colourings only, under which two empty boards of different sizes,
    // or Black versus White at the same point, would compare equal.
    [Fact]
    public void Equals_DifferentSizeOrColour_IsNotEqual()
    {
        Assert.False(new Board(2).Equals(new Board(3)));

        Board black = new(3);
        Board white = new(3);
        black[0] = PointState.Black;
        white[0] = PointState.White;
        Assert.False(black.Equals(white));

        Assert.False(new Board(3).Equals(null));
        Assert.False(new Board(3).Equals((object?)null));
    }

    // Rejects: a size range that drifts from RuleSet's test range 2..19.
    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void Constructor_SizeOutOfRange_Throws(int size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Board(size));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(19)]
    public void Constructor_SizeAtBounds_IsAccepted(int size)
    {
        Assert.Equal(size, new Board(size).Size);
    }

    private static HashSet<Point> Set(params Point[] points)
    {
        return [.. points];
    }

    private static HashSet<Point> NeighbourPoints(Board board, Point p)
    {
        return board.Neighbours(board.IndexOf(p)).ToArray().Select(board.PointAt).ToHashSet();
    }
}
