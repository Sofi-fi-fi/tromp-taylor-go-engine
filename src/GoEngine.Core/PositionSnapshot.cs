namespace GoEngine.Core;

/// <summary>
/// An immutable game position: board coloring, player to move, consecutive passes and removed
/// stones. Instances are compared by value.
/// </summary>
/// <remarks>
/// Not a record, because record equality would compare the coloring array by reference and
/// <c>with</c> would skip validation; not a struct, because its <c>default</c> would be an invalid position.
/// </remarks>
public sealed class PositionSnapshot : IEquatable<PositionSnapshot>
{
    private const int MinSnapshotSize = 2;

    // Two passes in a row end the game, so no larger count can occur.
    private const int MaxConsecutivePasses = 2;

    private readonly PointState[] _coloring;

    /// <summary>
    /// Creates a snapshot from a copy of <paramref name="coloring"/>, so later changes to the list
    /// do not affect it.
    /// </summary>
    /// <remarks>
    /// Sizes start at <c>2</c>, not <see cref="RuleSet.MinSize"/>: tests that run both engines on small
    /// boards build snapshots, and a snapshot does not know which factory created its <see cref="RuleSet"/>.
    /// </remarks>
    /// <param name="size">The number of lines on each side, from <c>2</c> to <see cref="RuleSet.MaxSize"/>.</param>
    /// <param name="coloring">
    /// Exactly <c>size * size</c> defined <see cref="PointState"/> values in row-major order: the state
    /// of <see cref="Point"/> <c>p</c> is at index <c>p.Y * size + p.X</c>.
    /// </param>
    /// <param name="toMove">A defined <see cref="Color"/>: the player who moves next.</param>
    /// <param name="consecutivePasses">Passes played in a row before this position, from <c>0</c> to <c>2</c>.</param>
    /// <param name="blackStonesRemoved">Black stones removed so far; not negative.</param>
    /// <param name="whiteStonesRemoved">White stones removed so far; not negative.</param>
    /// <exception cref="ArgumentNullException"><paramref name="coloring"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// An argument is outside the range stated in its parameter description. Arguments are checked
    /// in parameter order.
    /// </exception>
    public PositionSnapshot(
        int size,
        IReadOnlyList<PointState> coloring,
        Color toMove,
        int consecutivePasses,
        int blackStonesRemoved,
        int whiteStonesRemoved)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, MinSnapshotSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, RuleSet.MaxSize);

        ArgumentNullException.ThrowIfNull(coloring);
        if (coloring.Count != size * size)
        {
            throw new ArgumentOutOfRangeException(
                nameof(coloring), coloring.Count, "The coloring must have size * size elements.");
        }

        _coloring = new PointState[size * size];
        for (int i = 0; i < _coloring.Length; i++)
        {
            _coloring[i] = coloring[i];
        }

        // Validate the copy, not the input: the list may change or compute values between two reads.
        for (int i = 0; i < _coloring.Length; i++)
        {
            if (!Enum.IsDefined(_coloring[i]))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coloring), _coloring[i], $"Undefined point state at index {i}.");
            }
        }

        if (!Enum.IsDefined(toMove))
        {
            throw new ArgumentOutOfRangeException(nameof(toMove), toMove, "Undefined color.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(consecutivePasses);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(consecutivePasses, MaxConsecutivePasses);
        ArgumentOutOfRangeException.ThrowIfNegative(blackStonesRemoved);
        ArgumentOutOfRangeException.ThrowIfNegative(whiteStonesRemoved);

        Size = size;
        ToMove = toMove;
        ConsecutivePasses = consecutivePasses;
        BlackStonesRemoved = blackStonesRemoved;
        WhiteStonesRemoved = whiteStonesRemoved;
    }

    /// <summary>The number of lines on each side of the board.</summary>
    public int Size { get; }

    /// <summary>The player who moves next.</summary>
    public Color ToMove { get; }

    /// <summary>Passes played in a row immediately before this position.</summary>
    public int ConsecutivePasses { get; }

    /// <summary>Whether the game has ended; computed from <see cref="ConsecutivePasses"/>, not stored.</summary>
    public bool IsOver => ConsecutivePasses == MaxConsecutivePasses;

    /// <summary>
    /// Black stones removed so far: captured by White (<see cref="MoveResult.Captured"/>) and removed
    /// by Black's own suicide (<see cref="MoveResult.SelfCaptured"/>).
    /// </summary>
    public int BlackStonesRemoved { get; }

    /// <summary>
    /// White stones removed so far: captured by Black (<see cref="MoveResult.Captured"/>) and removed
    /// by White's own suicide (<see cref="MoveResult.SelfCaptured"/>).
    /// </summary>
    public int WhiteStonesRemoved { get; }

    /// <summary>Gets the state of a point.</summary>
    /// <param name="point">A point with both coordinates from <c>0</c> to <see cref="Size"/> <c>- 1</c>.</param>
    /// <returns>The state of <paramref name="point"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// An argument is outside the range stated in its parameter description.
    /// </exception>
    public PointState this[Point point]
    {
        get
        {
            if (point.X < 0 || point.X >= Size || point.Y < 0 || point.Y >= Size)
            {
                throw new ArgumentOutOfRangeException(nameof(point), point, "The point is outside the board.");
            }

            return _coloring[point.Y * Size + point.X];
        }
    }

    /// <summary>Compares two snapshots by value.</summary>
    /// <param name="left">A snapshot or <see langword="null"/>.</param>
    /// <param name="right">A snapshot or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if both are <see langword="null"/> or they are equal.</returns>
    public static bool operator ==(PositionSnapshot? left, PositionSnapshot? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    /// <summary>Compares two snapshots by value.</summary>
    /// <param name="left">A snapshot or <see langword="null"/>.</param>
    /// <param name="right">A snapshot or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if exactly one is <see langword="null"/> or they differ.</returns>
    public static bool operator !=(PositionSnapshot? left, PositionSnapshot? right)
    {
        return !(left == right);
    }

    /// <summary>Compares size, every point, player to move, passes and both removed-stone counts.</summary>
    /// <remarks>Not a positional superko test, which compares the board coloring only.</remarks>
    /// <param name="other">A snapshot or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if all of them are equal.</returns>
    public bool Equals(PositionSnapshot? other)
    {
        if (other is null)
        {
            return false;
        }

        return Size == other.Size
            && ToMove == other.ToMove
            && ConsecutivePasses == other.ConsecutivePasses
            && BlackStonesRemoved == other.BlackStonesRemoved
            && WhiteStonesRemoved == other.WhiteStonesRemoved
            && _coloring.AsSpan().SequenceEqual(other._coloring);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is PositionSnapshot other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Size);
        hash.Add(ToMove);
        hash.Add(ConsecutivePasses);
        hash.Add(BlackStonesRemoved);
        hash.Add(WhiteStonesRemoved);
        foreach (PointState state in _coloring)
        {
            hash.Add(state);
        }

        return hash.ToHashCode();
    }
}
