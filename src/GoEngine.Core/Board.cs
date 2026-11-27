namespace GoEngine.Core;

// Stub for the Board tests; implemented in the next group.
internal sealed class Board : IEquatable<Board>
{
    public Board(int size)
    {
        throw new NotImplementedException();
    }

    public int Size => throw new NotImplementedException();

    public int PointCount => throw new NotImplementedException();

    public PointState this[int index]
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public int IndexOf(Point point)
    {
        throw new NotImplementedException();
    }

    public Point PointAt(int index)
    {
        throw new NotImplementedException();
    }

    public ReadOnlySpan<int> Neighbours(int index)
    {
        throw new NotImplementedException();
    }

    public Board Clone()
    {
        throw new NotImplementedException();
    }

    public bool Equals(Board? other)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
