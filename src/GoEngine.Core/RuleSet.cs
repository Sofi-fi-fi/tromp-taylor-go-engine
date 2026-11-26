namespace GoEngine.Core;

/// <summary>The rule parameters of one game, compared by value.</summary>
/// <remarks>
/// The constructor is private and the properties are get-only, so a <c>with</c> expression
/// cannot bypass the checks in <see cref="Create"/>.
/// </remarks>
public sealed record RuleSet
{
    /// <summary>The smallest board size accepted by <see cref="Create"/>.</summary>
    public const int MinSize = 9;

    /// <summary>The largest board size accepted by any factory.</summary>
    public const int MaxSize = 19;

    internal const int MinTestSize = 2;

    /// <summary>The largest absolute <see cref="Komi"/>: the points on the largest board.</summary>
    /// <remarks>
    /// A larger komi cannot change who wins, and the bound keeps score arithmetic far from
    /// <see cref="decimal"/> overflow.
    /// </remarks>
    public const decimal MaxAbsKomi = MaxSize * MaxSize;

    private RuleSet(int size, decimal komi, bool allowSuicide)
    {
        Size = size;
        Komi = komi;
        AllowSuicide = allowSuicide;
    }

    /// <summary>The number of lines on each side of the square board.</summary>
    public int Size { get; }

    /// <summary>The points added to White's score; may be zero, negative or fractional.</summary>
    /// <remarks>Decimal, so a komi such as <c>6.4</c> and scores built on it stay exact.</remarks>
    public decimal Komi { get; }

    /// <summary>Whether a move that leaves its own chain without liberties is legal.</summary>
    public bool AllowSuicide { get; }

    /// <summary>The only public way to get a rule set; it validates size and komi.</summary>
    /// <remarks><paramref name="size"/> is checked before <paramref name="komi"/>.</remarks>
    /// <param name="size">From <see cref="MinSize"/> to <see cref="MaxSize"/>.</param>
    /// <param name="komi">Absolute value at most <see cref="MaxAbsKomi"/>.</param>
    /// <param name="allowSuicide">The value of <see cref="AllowSuicide"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">An argument is out of range.</exception>
    public static RuleSet Create(int size, decimal komi = 7.5m, bool allowSuicide = false)
    {
        return CreateValidated(size, MinSize, komi, allowSuicide);
    }

    // Boards from MinTestSize exist only so that tests can run both engines on small boards.
    internal static RuleSet CreateForTests(int size, decimal komi = 7.5m, bool allowSuicide = false)
    {
        return CreateValidated(size, MinTestSize, komi, allowSuicide);
    }

    // Parameters are named like the factories' ones: the throw helpers take ParamName from them.
    private static RuleSet CreateValidated(int size, int minSize, decimal komi, bool allowSuicide)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, minSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, MaxSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(komi, MaxAbsKomi);
        ArgumentOutOfRangeException.ThrowIfLessThan(komi, -MaxAbsKomi);

        return new RuleSet(size, komi, allowSuicide);
    }
}
