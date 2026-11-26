namespace GoEngine.Core;

/// <summary>The area score of a position, compared by value.</summary>
/// <remarks>A class, because a <c>default</c> struct would read as a real draw.</remarks>
public sealed record ScoreResult
{
    /// <summary>Creates a score from the point counts and komi.</summary>
    /// <remarks>Arguments are checked in parameter order.</remarks>
    /// <param name="blackArea">Not negative.</param>
    /// <param name="whiteArea">Not negative.</param>
    /// <param name="neutralPoints">Not negative.</param>
    /// <param name="komi">Absolute value at most <see cref="RuleSet.MaxAbsKomi"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">An argument is out of range.</exception>
    public ScoreResult(int blackArea, int whiteArea, int neutralPoints, decimal komi)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(blackArea);
        ArgumentOutOfRangeException.ThrowIfNegative(whiteArea);
        ArgumentOutOfRangeException.ThrowIfNegative(neutralPoints);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(komi, RuleSet.MaxAbsKomi);
        ArgumentOutOfRangeException.ThrowIfLessThan(komi, -RuleSet.MaxAbsKomi);

        BlackArea = blackArea;
        WhiteArea = whiteArea;
        NeutralPoints = neutralPoints;
        Komi = komi;
    }

    /// <summary>Points that count for Black.</summary>
    public int BlackArea { get; }

    /// <summary>Points that count for White, without komi.</summary>
    public int WhiteArea { get; }

    /// <summary>Points that count for neither player.</summary>
    public int NeutralPoints { get; }

    /// <summary>The <see cref="RuleSet.Komi"/> the score was computed with.</summary>
    public decimal Komi { get; }

    /// <summary><see cref="WhiteArea"/> plus <see cref="Komi"/>.</summary>
    public decimal WhiteScore => WhiteArea + Komi;

    /// <summary>
    /// The player with the higher score, <see cref="BlackArea"/> against <see cref="WhiteScore"/>;
    /// <see langword="null"/> for a draw.
    /// </summary>
    public Color? Winner
    {
        get
        {
            if (BlackArea > WhiteScore)
            {
                return Color.Black;
            }

            if (BlackArea < WhiteScore)
            {
                return Color.White;
            }

            return null;
        }
    }

    /// <summary>Whether both scores are equal.</summary>
    public bool IsDraw => BlackArea == WhiteScore;

    /// <summary>The absolute difference between the two scores.</summary>
    public decimal Margin => Math.Abs(BlackArea - WhiteScore);
}
