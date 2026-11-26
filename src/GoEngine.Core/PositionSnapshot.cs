namespace GoEngine.Core;

/// <summary>
/// An immutable view of a game position: the board coloring, the player to move, the number of
/// consecutive passes and the number of stones of each color removed from the board so far.
/// Instances are compared by value.
/// </summary>
/// <remarks>
/// This is a class with hand-written equality rather than a record for three reasons. A record's
/// generated equality would compare the coloring array by reference, so two snapshots of the same
/// position would be unequal. A <c>with</c> expression would create an instance without running
/// the constructor's validation. A struct would have a <c>default</c> value that looks like a
/// position but is not a valid one: size <c>0</c>, no coloring and an undefined player to move.
/// </remarks>
public sealed class PositionSnapshot : IEquatable<PositionSnapshot>
{
    /// <summary>
    /// Creates a snapshot from a board coloring and the game counters.
    /// </summary>
    /// <remarks>
    /// The snapshot's own public size range is from <c>2</c> to <see cref="RuleSet.MaxSize"/>, wider
    /// than the range from <see cref="RuleSet.MinSize"/> accepted by <see cref="RuleSet.Create"/>.
    /// The Reference engine, which lives in another assembly, and the exhaustive tests build
    /// snapshots of small boards, and a snapshot does not know which factory created its
    /// <see cref="RuleSet"/>.
    /// The constructor copies <paramref name="coloring"/>, so later changes to the caller's list
    /// do not affect the snapshot.
    /// </remarks>
    /// <param name="size">
    /// The number of lines in each direction of the square board, from <c>2</c> to
    /// <see cref="RuleSet.MaxSize"/> inclusive.
    /// </param>
    /// <param name="coloring">
    /// The state of every point in row-major order: the state of <see cref="Point"/>
    /// <c>p</c> is at index <c>p.Y * size + p.X</c>. Has exactly <c>size * size</c> elements, each a
    /// defined <see cref="PointState"/> value.
    /// </param>
    /// <param name="toMove">The player who makes the next move; a defined <see cref="Color"/> value.</param>
    /// <param name="consecutivePasses">
    /// The number of passes played in a row immediately before this position, from <c>0</c> to
    /// <c>2</c> inclusive.
    /// </param>
    /// <param name="blackStonesRemoved">
    /// The number of Black stones removed from the board so far; not negative.
    /// </param>
    /// <param name="whiteStonesRemoved">
    /// The number of White stones removed from the board so far; not negative.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="coloring"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="size"/> is less than <c>2</c> or greater than <see cref="RuleSet.MaxSize"/>;
    /// <paramref name="coloring"/> does not have
    /// <c>size * size</c> elements or contains an undefined <see cref="PointState"/> value;
    /// <paramref name="toMove"/> is not a defined <see cref="Color"/> value;
    /// <paramref name="consecutivePasses"/> is outside its range; or
    /// <paramref name="blackStonesRemoved"/> or <paramref name="whiteStonesRemoved"/> is negative.
    /// The arguments are checked in parameter order, so the exception names the first invalid
    /// argument.
    /// </exception>
    public PositionSnapshot(
        int size,
        IReadOnlyList<PointState> coloring,
        Color toMove,
        int consecutivePasses,
        int blackStonesRemoved,
        int whiteStonesRemoved)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// The number of lines in each direction of the square board.
    /// </summary>
    public int Size => throw new NotImplementedException();

    /// <summary>
    /// The player who makes the next move.
    /// </summary>
    public Color ToMove => throw new NotImplementedException();

    /// <summary>
    /// The number of passes played in a row immediately before this position, from <c>0</c> to
    /// <c>2</c>.
    /// </summary>
    public int ConsecutivePasses => throw new NotImplementedException();

    /// <summary>
    /// Whether the game has ended: <see cref="ConsecutivePasses"/> equals <c>2</c>.
    /// </summary>
    /// <remarks>
    /// The value is computed from <see cref="ConsecutivePasses"/> and not stored, so the two
    /// cannot disagree.
    /// </remarks>
    public bool IsOver => throw new NotImplementedException();

    /// <summary>
    /// The number of Black stones removed from the board so far: both those captured by White,
    /// counted in <see cref="MoveResult.Captured"/> of White's moves, and those removed by Black's
    /// own suicide, counted in <see cref="MoveResult.SelfCaptured"/> of Black's moves.
    /// </summary>
    public int BlackStonesRemoved => throw new NotImplementedException();

    /// <summary>
    /// The number of White stones removed from the board so far: both those captured by Black,
    /// counted in <see cref="MoveResult.Captured"/> of Black's moves, and those removed by White's
    /// own suicide, counted in <see cref="MoveResult.SelfCaptured"/> of White's moves.
    /// </summary>
    public int WhiteStonesRemoved => throw new NotImplementedException();

    /// <summary>
    /// Gets the state of a point on the board.
    /// </summary>
    /// <param name="point">
    /// A point with <see cref="Point.X"/> and <see cref="Point.Y"/> from <c>0</c> to
    /// <see cref="Size"/> <c>- 1</c> inclusive.
    /// </param>
    /// <returns>The state of <paramref name="point"/> in this position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="point"/> is outside the board.
    /// </exception>
    public PointState this[Point point] => throw new NotImplementedException();

    /// <summary>
    /// Compares two snapshots by value.
    /// </summary>
    /// <param name="left">The first snapshot, or <see langword="null"/>.</param>
    /// <param name="right">The second snapshot, or <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if both are <see langword="null"/> or <paramref name="left"/>
    /// equals <paramref name="right"/> by <see cref="Equals(PositionSnapshot?)"/>; otherwise
    /// <see langword="false"/>.
    /// </returns>
    public static bool operator ==(PositionSnapshot? left, PositionSnapshot? right)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Compares two snapshots by value.
    /// </summary>
    /// <param name="left">The first snapshot, or <see langword="null"/>.</param>
    /// <param name="right">The second snapshot, or <see langword="null"/>.</param>
    /// <returns>The negation of the result of the equality operator for the same arguments.</returns>
    public static bool operator !=(PositionSnapshot? left, PositionSnapshot? right)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Whether <paramref name="other"/> describes the same position: equal <see cref="Size"/>,
    /// the same state at every point, and equal <see cref="ToMove"/>,
    /// <see cref="ConsecutivePasses"/>, <see cref="BlackStonesRemoved"/> and
    /// <see cref="WhiteStonesRemoved"/>.
    /// </summary>
    /// <remarks>
    /// Because equality also compares <see cref="ToMove"/>, <see cref="ConsecutivePasses"/>,
    /// <see cref="BlackStonesRemoved"/> and <see cref="WhiteStonesRemoved"/>, it is not a test for
    /// positional superko, which compares the board coloring only.
    /// </remarks>
    /// <param name="other">The snapshot to compare with, or <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if all of the listed values are equal; <see langword="false"/> if any
    /// differs or <paramref name="other"/> is <see langword="null"/>.
    /// </returns>
    public bool Equals(PositionSnapshot? other)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Whether <paramref name="obj"/> is a <see cref="PositionSnapshot"/> equal to this one by
    /// <see cref="Equals(PositionSnapshot?)"/>.
    /// </summary>
    /// <param name="obj">The object to compare with, or <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> is an equal <see cref="PositionSnapshot"/>;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns a hash code consistent with <see cref="Equals(PositionSnapshot?)"/>: equal snapshots
    /// have equal hash codes.
    /// </summary>
    /// <returns>A hash code of all the values compared by <see cref="Equals(PositionSnapshot?)"/>.</returns>
    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
