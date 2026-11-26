namespace GoEngine.Core;

/// <summary>
/// The rule parameters of one game: board size, komi and whether suicide is allowed.
/// Instances are compared by value.
/// </summary>
/// <remarks>
/// The properties are get-only and the constructor is private, so a <c>with</c> expression
/// cannot produce a rule set that bypasses the checks in <see cref="Create"/>.
/// </remarks>
public sealed record RuleSet
{
    /// <summary>
    /// The smallest board size accepted by <see cref="Create"/>.
    /// </summary>
    public const int MinSize = 9;

    /// <summary>
    /// The largest board size accepted by <see cref="Create"/> and <see cref="CreateForTests"/>.
    /// </summary>
    public const int MaxSize = 19;

    /// <summary>
    /// The smallest board size accepted by <see cref="CreateForTests"/>.
    /// </summary>
    internal const int MinTestSize = 2;

    /// <summary>
    /// The largest absolute value of <see cref="Komi"/> accepted by <see cref="Create"/> and
    /// <see cref="CreateForTests"/>: the number of points on the largest board.
    /// </summary>
    /// <remarks>
    /// A larger komi cannot change the outcome (who wins, or a draw), and the bound keeps
    /// score arithmetic far from <see cref="decimal"/> overflow.
    /// </remarks>
    public const decimal MaxAbsKomi = MaxSize * MaxSize;

    private RuleSet(int size, decimal komi, bool allowSuicide)
    {
        Size = size;
        Komi = komi;
        AllowSuicide = allowSuicide;
    }

    /// <summary>
    /// The number of lines in each direction of the square board.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// The points added to White's score; may be zero, negative or fractional, and its
    /// absolute value is at most <see cref="MaxAbsKomi"/>.
    /// </summary>
    public decimal Komi { get; }

    /// <summary>
    /// Whether a move that leaves the player's own chain without liberties is legal.
    /// </summary>
    public bool AllowSuicide { get; }

    /// <summary>
    /// Creates a rule set for a board from <see cref="MinSize"/> to <see cref="MaxSize"/> lines.
    /// </summary>
    /// <param name="size">The board size, from <see cref="MinSize"/> to <see cref="MaxSize"/> inclusive.</param>
    /// <param name="komi">The points added to White's score, with absolute value at most <see cref="MaxAbsKomi"/>.</param>
    /// <param name="allowSuicide">Whether suicide moves are legal.</param>
    /// <returns>A rule set with the given parameters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="size"/> is less than <see cref="MinSize"/> or greater than <see cref="MaxSize"/>,
    /// or the absolute value of <paramref name="komi"/> is greater than <see cref="MaxAbsKomi"/>.
    /// <paramref name="size"/> is checked before <paramref name="komi"/>, so when both are out of range
    /// the exception names <paramref name="size"/>.
    /// </exception>
    public static RuleSet Create(int size, decimal komi = 7.5m, bool allowSuicide = false)
    {
        return CreateValidated(size, MinSize, komi, allowSuicide);
    }

    /// <summary>
    /// Creates a rule set for a board from <see cref="MinTestSize"/> to <see cref="MaxSize"/> lines.
    /// </summary>
    /// <remarks>
    /// Boards below <see cref="MinSize"/> exist only for exhaustive tests on small boards;
    /// they are not part of the public contract.
    /// </remarks>
    /// <param name="size">The board size, from <see cref="MinTestSize"/> to <see cref="MaxSize"/> inclusive.</param>
    /// <param name="komi">The points added to White's score, with absolute value at most <see cref="MaxAbsKomi"/>.</param>
    /// <param name="allowSuicide">Whether suicide moves are legal.</param>
    /// <returns>A rule set with the given parameters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="size"/> is less than <see cref="MinTestSize"/> or greater than <see cref="MaxSize"/>,
    /// or the absolute value of <paramref name="komi"/> is greater than <see cref="MaxAbsKomi"/>.
    /// <paramref name="size"/> is checked before <paramref name="komi"/>, so when both are out of range
    /// the exception names <paramref name="size"/>.
    /// </exception>
    internal static RuleSet CreateForTests(int size, decimal komi = 7.5m, bool allowSuicide = false)
    {
        return CreateValidated(size, MinTestSize, komi, allowSuicide);
    }

    /// <summary>
    /// Checks <paramref name="size"/> against <paramref name="minSize"/> and <see cref="MaxSize"/>,
    /// then <paramref name="komi"/> against <see cref="MaxAbsKomi"/>, and creates the rule set.
    /// </summary>
    /// <remarks>
    /// The parameters are named like the factories' parameters and passed to the throw helpers
    /// directly, because the helpers take <see cref="ArgumentException.ParamName"/> from the
    /// argument expression.
    /// </remarks>
    /// <param name="size">The board size to check.</param>
    /// <param name="minSize">The smallest board size the calling factory accepts.</param>
    /// <param name="komi">The komi to check.</param>
    /// <param name="allowSuicide">Whether suicide moves are legal.</param>
    /// <returns>A rule set with the given parameters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="size"/> or <paramref name="komi"/> is out of range.
    /// </exception>
    private static RuleSet CreateValidated(int size, int minSize, decimal komi, bool allowSuicide)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, minSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, MaxSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(komi, MaxAbsKomi);
        ArgumentOutOfRangeException.ThrowIfLessThan(komi, -MaxAbsKomi);

        return new RuleSet(size, komi, allowSuicide);
    }
}
