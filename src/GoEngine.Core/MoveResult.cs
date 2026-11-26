namespace GoEngine.Core;

/// <summary>
/// The result of an attempt to play a move: its <see cref="Status"/> and, for a played move,
/// the number of stones it removed from the board. Instances are compared by value.
/// </summary>
/// <remarks>
/// The constructor is private and the properties are get-only, so every instance other than
/// <c>default</c> comes from <see cref="Success"/>, <see cref="SelfCapture"/> or
/// <see cref="Rejected"/>. A <c>default</c> instance has an undefined <see cref="Status"/>, so
/// <see cref="IsOk"/> is <see langword="false"/> for it.
/// A move either captures opponent stones or removes its own chain, never both, so at most one
/// of <see cref="Captured"/> and <see cref="SelfCaptured"/> is non-zero.
/// </remarks>
public readonly record struct MoveResult
{
    private MoveResult(MoveStatus status, int captured, int selfCaptured)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <see cref="MoveStatus.Ok"/> if the move was played, otherwise the reason it was rejected.
    /// </summary>
    public MoveStatus Status { get; }

    /// <summary>
    /// The number of opponent stones removed by the move; own stones are never counted here.
    /// <c>0</c> for a rejected move.
    /// </summary>
    public int Captured { get; }

    /// <summary>
    /// The number of the player's own stones, including the placed stone, removed by a suicide
    /// move that the rule set allows; <c>0</c> for any other move.
    /// </summary>
    public int SelfCaptured { get; }

    /// <summary>
    /// Whether <see cref="Status"/> is <see cref="MoveStatus.Ok"/>.
    /// </summary>
    public bool IsOk => throw new NotImplementedException();

    /// <summary>
    /// Creates the result of a move that was played and did not remove the player's own stones.
    /// </summary>
    /// <param name="captured">The number of opponent stones the move removed; not negative.</param>
    /// <returns>
    /// A result with <see cref="MoveStatus.Ok"/>, the given capture count and
    /// <see cref="SelfCaptured"/> equal to <c>0</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="captured"/> is negative.
    /// </exception>
    public static MoveResult Success(int captured)
    {
        return new MoveResult(MoveStatus.Ok, captured, 0);
    }

    /// <summary>
    /// Creates the result of a suicide move that was played because the rule set allows suicide.
    /// </summary>
    /// <param name="selfCaptured">
    /// The number of the player's own stones the move removed, including the placed stone; at least <c>1</c>.
    /// </param>
    /// <returns>
    /// A result with <see cref="MoveStatus.Ok"/>, <see cref="Captured"/> equal to <c>0</c> and the
    /// given self-capture count.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="selfCaptured"/> is less than <c>1</c>.
    /// </exception>
    public static MoveResult SelfCapture(int selfCaptured)
    {
        return new MoveResult(MoveStatus.Ok, 0, selfCaptured);
    }

    /// <summary>
    /// Creates the result of a move that was rejected.
    /// </summary>
    /// <param name="status">The reason for the rejection: a defined status other than <see cref="MoveStatus.Ok"/>.</param>
    /// <returns>
    /// A result with the given status and both <see cref="Captured"/> and <see cref="SelfCaptured"/>
    /// equal to <c>0</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="status"/> is <see cref="MoveStatus.Ok"/> or not a defined <see cref="MoveStatus"/> value.
    /// </exception>
    public static MoveResult Rejected(MoveStatus status)
    {
        return new MoveResult(status, 0, 0);
    }
}
