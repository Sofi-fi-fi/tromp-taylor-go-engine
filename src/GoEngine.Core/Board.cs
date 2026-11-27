namespace GoEngine.Core;

// Mutable colouring of a square board. Points are int indices y * size + x, the same row-major
// layout as PositionSnapshot, so a snapshot converts without remapping. Ints rather than Point
// keep GroupTracker's union-find arrays flat and indexable.
internal sealed class Board : IEquatable<Board>
{
    // Neighbour lists depend only on the size, so they are built once per size and shared by all
    // boards: Clone copies only the colouring, and the hot path does no bounds arithmetic.
    private static readonly int[][][] NeighbourTable = BuildNeighbourTable();

    private readonly PointState[] _points;
    private readonly int[][] _neighbours;

    public Board(int size)
    {
        // The same range as RuleSet.CreateForTests: a board exists for every rule set and no other.
        ArgumentOutOfRangeException.ThrowIfLessThan(size, RuleSet.MinTestSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, RuleSet.MaxSize);

        Size = size;
        _points = new PointState[size * size];
        _neighbours = NeighbourTable[size];
    }

    private Board(Board source)
    {
        Size = source.Size;
        _points = (PointState[])source._points.Clone();
        _neighbours = source._neighbours;
    }

    public int Size { get; }

    public int PointCount => _points.Length;

    public PointState this[int index]
    {
        get => _points[index];
        set => _points[index] = value;
    }

    // Unchecked: an off-board point is a move error that MoveResolver reports, not a Board concern.
    public int IndexOf(Point point)
    {
        return point.Y * Size + point.X;
    }

    public Point PointAt(int index)
    {
        return new Point(index % Size, index / Size);
    }

    // A span, so callers cannot change the shared list.
    public ReadOnlySpan<int> Neighbours(int index)
    {
        return _neighbours[index];
    }

    public Board Clone()
    {
        return new Board(this);
    }

    public bool Equals(Board? other)
    {
        return other is not null
            && Size == other.Size
            && _points.AsSpan().SequenceEqual(other._points);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as Board);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Size);
        foreach (PointState state in _points)
        {
            hash.Add(state);
        }

        return hash.ToHashCode();
    }

    private static int[][][] BuildNeighbourTable()
    {
        int[][][] table = new int[RuleSet.MaxSize + 1][][];
        for (int size = RuleSet.MinTestSize; size <= RuleSet.MaxSize; size++)
        {
            table[size] = new int[size * size][];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    List<int> neighbours = new(4);
                    if (y > 0)
                    {
                        neighbours.Add((y - 1) * size + x);
                    }

                    if (x > 0)
                    {
                        neighbours.Add(y * size + x - 1);
                    }

                    if (x < size - 1)
                    {
                        neighbours.Add(y * size + x + 1);
                    }

                    if (y < size - 1)
                    {
                        neighbours.Add((y + 1) * size + x);
                    }

                    table[size][y * size + x] = [.. neighbours];
                }
            }
        }

        return table;
    }
}
