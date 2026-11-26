namespace GoEngine.Core;

/// <summary>
/// The area score of a position: the points that count for each player, the points that count
/// for neither, and the komi added to White's score. Instances are compared by value.
/// </summary>
public sealed record ScoreResult
{
    /// <summary>
    /// Creates a score from the point counts and komi.
    /// </summary>
    /// <param name="blackArea">The number of points that count for Black; not negative.</param>
    /// <param name="whiteArea">The number of points that count for White, without komi; not negative.</param>
    /// <param name="neutralPoints">The number of points that count for neither player; not negative.</param>
    /// <param name="komi">The points added to White's score, with absolute value at most <see cref="RuleSet.MaxAbsKomi"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blackArea"/>, <paramref name="whiteArea"/> or <paramref name="neutralPoints"/>
    /// is negative, or the absolute value of <paramref name="komi"/> is greater than
    /// <see cref="RuleSet.MaxAbsKomi"/>. The arguments are checked in parameter order, so
    /// <paramref name="komi"/> is checked after the counts and the exception names the first
    /// invalid argument.
    /// </exception>
    public ScoreResult(int blackArea, int whiteArea, int neutralPoints, decimal komi)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// The number of points that count for Black.
    /// </summary>
    public int BlackArea { get; }

    /// <summary>
    /// The number of points that count for White, without komi.
    /// </summary>
    public int WhiteArea { get; }

    /// <summary>
    /// The number of points that count for neither player.
    /// </summary>
    public int NeutralPoints { get; }

    /// <summary>
    /// The points added to White's score; may be zero, negative or fractional, and its
    /// absolute value is at most <see cref="RuleSet.MaxAbsKomi"/>.
    /// </summary>
    public decimal Komi { get; }

    /// <summary>
    /// White's final score: <see cref="WhiteArea"/> plus <see cref="Komi"/>.
    /// </summary>
    public decimal WhiteScore => throw new NotImplementedException();

    /// <summary>
    /// The player whose score is higher, comparing <see cref="BlackArea"/> with
    /// <see cref="WhiteScore"/>; <see langword="null"/> when the scores are equal.
    /// </summary>
    public Color? Winner => throw new NotImplementedException();

    /// <summary>
    /// Whether <see cref="BlackArea"/> equals <see cref="WhiteScore"/>.
    /// </summary>
    public bool IsDraw => throw new NotImplementedException();

    /// <summary>
    /// The absolute difference between <see cref="BlackArea"/> and <see cref="WhiteScore"/>;
    /// <c>0</c> for a draw.
    /// </summary>
    public decimal Margin => throw new NotImplementedException();
}
