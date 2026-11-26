namespace GoEngine.Core;

/// <summary>
/// The result of <see cref="IGoEngine.Play(Point)"/>, <see cref="IGoEngine.Play(Point, Color)"/>,
/// <see cref="IGoEngine.Pass()"/> or <see cref="IGoEngine.Pass(Color)"/>, compared by value.
/// </summary>
/// <remarks>
/// Only the factories create instances, so a rejected move with captures, or a move that both
/// captures and self-captures, cannot be built. <c>default</c> is not <see cref="IsOk"/>.
/// </remarks>
public readonly record struct MoveResult
{
    private MoveResult(MoveStatus status, int captured, int selfCaptured)
    {
        Status = status;
        Captured = captured;
        SelfCaptured = selfCaptured;
    }

    /// <summary>Whether the move was played and, if not, why.</summary>
    public MoveStatus Status { get; }

    /// <summary>Opponent stones the move removed.</summary>
    public int Captured { get; }

    /// <summary>Own stones, the placed one included, removed by an allowed suicide.</summary>
    public int SelfCaptured { get; }

    /// <summary>Whether <see cref="Status"/> is <see cref="MoveStatus.Ok"/>.</summary>
    public bool IsOk => Status == MoveStatus.Ok;

    /// <summary>A played move that removed none of its own stones.</summary>
    /// <param name="captured">The <see cref="Captured"/> count; not negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">The count is out of range.</exception>
    public static MoveResult Success(int captured)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(captured);

        return new MoveResult(MoveStatus.Ok, captured, 0);
    }

    /// <summary>A suicide played under <see cref="RuleSet.AllowSuicide"/>.</summary>
    /// <param name="selfCaptured">The <see cref="SelfCaptured"/> count; at least <c>1</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The count is out of range.</exception>
    public static MoveResult SelfCapture(int selfCaptured)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(selfCaptured, 1);

        return new MoveResult(MoveStatus.Ok, 0, selfCaptured);
    }

    /// <summary>The result of a move refused for <paramref name="status"/>.</summary>
    /// <param name="status">A defined status other than <see cref="MoveStatus.Ok"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The status is out of range.</exception>
    public static MoveResult Rejected(MoveStatus status)
    {
        if (status == MoveStatus.Ok || !Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Not a rejection status.");
        }

        return new MoveResult(status, 0, 0);
    }
}
